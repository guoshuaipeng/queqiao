using System.Windows.Forms;

namespace RemoteAssist.Agent;

internal static class Program
{
    private const int DefaultPort = 17890;
    private const int DefaultConfirmTimeoutSeconds = 60;

    public static async Task Main(string[] args)
    {
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

        // WinExe：CLI / headless 需要控制台；UI 模式可不挂控制台
        if (headless || autoYes)
            NativeConsole.EnsureForCli();
        else
        {
            // 开发时从终端启动仍尽量挂上父控制台，便于看审计；失败则静默（安装后双击无黑窗）
            NativeConsole.TryAttachParent();
        }

        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* ignore */ }

        var auditPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteAssist", "audit.log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes, headless, confirmTimeoutSeconds, audit, auditPath, insecure);
        host.PrintBanner();

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
                try { Console.WriteLine($"[主界面失败] {ex}"); } catch { /* ignore */ }
                try
                {
                    MessageBox.Show(ex.ToString(), "鹊桥启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { /* ignore */ }
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

