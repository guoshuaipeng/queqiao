using System.Windows.Forms;

namespace RemoteAssist.Agent;

internal static class Program
{
    private const int DefaultPort = 17890;
    private const int DefaultConfirmTimeoutSeconds = 60;

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var port = DefaultPort;
        var autoYes = args.Any(a => a is "--yes" or "--auto-yes");
        var headless = args.Any(a => a is "--headless");
        var insecure = args.Any(a => a is "--insecure");
        var confirmTimeoutSeconds = DefaultConfirmTimeoutSeconds;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                port = p;
            if (args[i] is "--confirm-timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t))
                confirmTimeoutSeconds = Math.Clamp(t, 5, 600);
        }

        var auditPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteAssist", "audit.log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes, headless, confirmTimeoutSeconds, audit, auditPath, insecure);
        host.PrintBanner();

        // headless / --yes：纯 CLI，不启托盘与会话窗
        if (headless || autoYes)
        {
            host.Start();
            var exit = new TaskCompletionSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                host.Stop();
                exit.TrySetResult();
            };
            await exit.Task.ConfigureAwait(false);
            return;
        }

        // UI 模式：STA 消息循环 + 托盘；监听在后台
        await RunTrayUiAsync(host).ConfigureAwait(false);
    }

    private static Task RunTrayUiAsync(AgentHost host)
    {
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                ConfirmPrompt.EnsureWinFormsInitialized();
                host.Start();

                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    try
                    {
                        host.Stop();
                        Application.Exit();
                    }
                    catch { /* ignore */ }
                };

                using var ctx = new AgentTrayContext(host);
                Application.Run(ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[托盘 UI 失败] {ex}");
                tcs.TrySetException(ex);
                return;
            }
            finally
            {
                host.Stop();
            }

            tcs.TrySetResult();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = false;
        thread.Name = "RemoteAssist.UI";
        thread.Start();
        return tcs.Task;
    }
}
