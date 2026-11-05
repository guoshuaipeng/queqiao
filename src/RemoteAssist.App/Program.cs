using System.Windows.Forms;
using RemoteAssist.Agent;

namespace RemoteAssist.App;

internal static class Program
{
    private const int DefaultPort = 17890;
    private const int DefaultConfirmTimeoutSeconds = 60;

    [STAThread]
    private static void Main(string[] args)
    {
        var port = DefaultPort;
        var autoYes = args.Any(a => a is "--yes" or "--auto-yes");
        var headless = args.Any(a => a is "--headless");
        var insecure = args.Any(a => a is "--insecure");
        var forceAccept = args.Any(a => a is "--accept" or "--agent");
        var forceControl = args.Any(a => a is "--control" or "--helper");
        var confirmTimeoutSeconds = DefaultConfirmTimeoutSeconds;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                port = p;
            if (args[i] is "--confirm-timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t))
                confirmTimeoutSeconds = Math.Clamp(t, 5, 600);
        }

        if (headless || autoYes)
        {
            // 兼容被控 CLI：转调独立 Agent 入口逻辑（无模式页）
            RunAgentHeadlessAsync(port, autoYes, headless, confirmTimeoutSeconds, insecure).GetAwaiter().GetResult();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        AppMode? initial = null;
        if (forceAccept && !forceControl) initial = AppMode.Accept;
        else if (forceControl && !forceAccept) initial = AppMode.Control;

        Application.Run(new AppShell(port, insecure, confirmTimeoutSeconds, initial));
    }

    private static async Task RunAgentHeadlessAsync(int port, bool autoYes, bool headless, int confirmTimeoutSeconds, bool insecure)
    {
        NativeConsole.EnsureForCli();
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* ignore */ }

        var auditPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteAssist", "audit.log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes, headless: true, confirmTimeoutSeconds, audit, auditPath, insecure);
        host.PrintBanner();
        host.Start();
        var exit = new TaskCompletionSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            host.Stop();
            exit.TrySetResult();
        };
        await exit.Task.ConfigureAwait(false);
    }
}

internal enum AppMode
{
    Accept,
    Control
}
