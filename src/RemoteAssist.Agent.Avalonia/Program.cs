using System.Text;
using Avalonia;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

internal static class Program
{
    public static AgentHost? SharedHost { get; private set; }
    public static string? ShotMode { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* ignore */ }

        if (args.Any(a => a is "-h" or "--help" or "help"))
        {
            PrintHelp();
            return 0;
        }

        ShotMode = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--ui-shot" && i + 1 < args.Length)
                ShotMode = args[++i];
        }

        var port = 17890;
        var autoYes = args.Any(a => a is "--yes" or "--auto-yes");
        var insecure = args.Any(a => a is "--insecure");
        var testAllowPair = args.Any(a => a is "--test-allow-pair");
        var testDenyWrite = args.Any(a => a is "--test-deny-write");
        var enableLan = args.Any(a => a is "--lan");
        var confirmTimeoutSeconds = 60;
        string? relayEndpoint = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                port = p;
            if (args[i] is "--confirm-timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t))
                confirmTimeoutSeconds = Math.Clamp(t, 5, 600);
            if (args[i] is "--relay" && i + 1 < args.Length)
                relayEndpoint = args[++i];
        }

        var auditDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteAssist");
        Directory.CreateDirectory(auditDir);
        var auditPath = Path.Combine(auditDir, "audit-avalonia.log");
        var audit = new AuditLog(auditPath);

        // headless:false → Avalonia ConfirmPrompt 弹窗；ShotMode 下可不起真实中继
        ConfirmPrompt.ForceAllowPair = testAllowPair;
        ConfirmPrompt.ForceDenyWrites = testDenyWrite;

        SharedHost = new AgentHost(
            port,
            autoYes: autoYes,
            headless: false,
            confirmTimeoutSeconds: confirmTimeoutSeconds,
            audit: audit,
            auditPath: auditPath,
            insecure: insecure,
            preferRelay: true,
            enableLanListen: enableLan,
            relayEndpoint: relayEndpoint);

        if (ShotMode is null)
        {
            SharedHost.Start();
            // 控制台附带两行凭证，方便联调；亲友主路径仍是窗内「复制码+密码」
            Console.WriteLine("鹊桥远程协助已启动（Avalonia 被控窗）。");
            Console.WriteLine(FriendConnectGuide.FormatShareClipboard(SharedHost.SessionCode, SharedHost.SessionPassword));
            SharedHost.SessionChanged += () =>
            {
                try { Console.WriteLine(FriendConnectGuide.FormatShareClipboard(SharedHost.SessionCode, SharedHost.SessionPassword)); }
                catch { }
            };
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void PrintHelp()
    {
        Console.WriteLine("鹊桥远程协助 · Avalonia 被控窗（Mac/Linux）");
        Console.WriteLine("  默认连公网中继 121.196.45.81:17891");
        Console.WriteLine("  --insecure          明文业务管道（对齐协助端无指纹粘贴）");
        Console.WriteLine("  --relay host:port   覆盖默认中继");
        Console.WriteLine("  --port N            本地端口（默认 17890）");
        Console.WriteLine("  --confirm-timeout N 写确认超时秒（默认 60）");
        Console.WriteLine("  --ui-shot <dir>     导出真实控件截图后退出（验收用）");
        Console.WriteLine("  --yes               自动允许（仅测试）");
        Console.WriteLine("  --test-allow-pair   联调：自动允许配对");
        Console.WriteLine("  --test-deny-write   联调：写操作一律拒绝");
    }
}


