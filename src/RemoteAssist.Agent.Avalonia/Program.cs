using System.Text;
using Avalonia;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

internal static class Program
{
    public static AgentHost? SharedHost { get; private set; }
    public static string? ShotMode { get; private set; }

    private static int _port = 17890;
    private static bool _autoYes;
    private static bool _insecure;
    private static bool _testAllowPair;
    private static bool _testDenyWrite;
    private static bool _enableLan;
    private static int _confirmTimeoutSeconds = 60;
    private static string? _relayEndpoint;
    private static string _auditPath = "";

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

        _port = 17890;
        _autoYes = args.Any(a => a is "--yes" or "--auto-yes");
        _insecure = args.Any(a => a is "--insecure");
        _testAllowPair = args.Any(a => a is "--test-allow-pair");
        _testDenyWrite = args.Any(a => a is "--test-deny-write");
        _enableLan = args.Any(a => a is "--lan");
        _confirmTimeoutSeconds = 60;
        _relayEndpoint = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                _port = p;
            if (args[i] is "--confirm-timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t))
                _confirmTimeoutSeconds = Math.Clamp(t, 5, 600);
            if (args[i] is "--relay" && i + 1 < args.Length)
                _relayEndpoint = args[++i];
        }

        var auditDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteAssist");
        Directory.CreateDirectory(auditDir);
        _auditPath = Path.Combine(auditDir, "audit-avalonia.log");

        ConfirmPrompt.ForceAllowPair = _testAllowPair;
        ConfirmPrompt.ForceDenyWrites = _testDenyWrite;

        // 双模式：首页先出；接受协助时再 Start 被控 Host。截图模式也不抢先连中继。
        SharedHost = CreateHost();

        if (ShotMode is null && args.Any(a => a is "--accept"))
        {
            EnsureHostStarted();
            Console.WriteLine("鹊桥远程协助已启动（Avalonia · 接受协助）。");
            Console.WriteLine(FriendConnectGuide.FormatShareClipboard(SharedHost.SessionCode, SharedHost.SessionPassword));
            SharedHost.SessionChanged += () =>
            {
                try { Console.WriteLine(FriendConnectGuide.FormatShareClipboard(SharedHost.SessionCode, SharedHost.SessionPassword)); }
                catch { }
            };
        }
        else if (ShotMode is null)
        {
            Console.WriteLine("鹊桥远程协助已启动（Avalonia 双模式）。请在首页选择「接受协助」或「去协助别人」。");
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AgentHost CreateHost()
    {
        var audit = new AuditLog(_auditPath);
        var settings = HelperUiSettings.Load();
        return new AgentHost(
            _port,
            autoYes: _autoYes,
            headless: false,
            confirmTimeoutSeconds: _confirmTimeoutSeconds,
            audit: audit,
            auditPath: _auditPath,
            insecure: _insecure,
            preferRelay: true,
            enableLanListen: _enableLan,
            relayEndpoint: _relayEndpoint ?? settings.RelayEndpoint,
            accountToken: settings.AccountToken,
            deviceId: settings.DeviceId,
            deviceDisplayName: settings.DeviceDisplayName ?? Environment.MachineName);
    }

    public static void EnsureHostStarted()
    {
        SharedHost ??= CreateHost();
        SharedHost.Start();
        // 控制台附带两行凭证，方便联调
        try
        {
            Console.WriteLine(FriendConnectGuide.FormatShareClipboard(SharedHost.SessionCode, SharedHost.SessionPassword));
        }
        catch { /* ignore */ }
    }

    public static void StopHost()
    {
        try { SharedHost?.DisconnectAllSessions(); } catch { /* ignore */ }
        try { SharedHost?.Stop(); } catch { /* ignore */ }
        // Stop 后证书已释放；下次接受协助时重建
        SharedHost = CreateHost();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void PrintHelp()
    {
        Console.WriteLine("鹊桥远程协助 · Avalonia 双模式（Mac/Linux）");
        Console.WriteLine("  默认连公网中继 121.196.45.81:17891");
        Console.WriteLine("  首页可选「接受协助」或「去协助别人」");
        Console.WriteLine("  --accept            启动后直达接受协助");
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
