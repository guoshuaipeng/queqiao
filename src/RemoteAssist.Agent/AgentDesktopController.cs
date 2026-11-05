using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 被控端桌面模式公共入口（统一 App / 独立 Agent 复用）。
/// </summary>
public sealed class AgentDesktopController : IDisposable
{
    private readonly AgentHost _host;
    private readonly bool _enableReturnHome;
    private AgentDesktopUi? _ui;
    private bool _started;
    private bool _disposed;

    public event EventHandler? HomeRequested;
    public event EventHandler? ExitRequested;

    public AgentDesktopController(
        int port = 17890,
        bool autoYes = false,
        bool headless = false,
        int confirmTimeoutSeconds = 60,
        bool insecure = false,
        bool enableReturnHome = true,
        bool? preferRelay = null,
        string? relayEndpoint = null)
    {
        _enableReturnHome = enableReturnHome;
        var settings = HelperUiSettings.Load();
        var auditPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteAssist", "audit.log");
        var audit = new AuditLog(auditPath);
        _host = new AgentHost(
            port,
            autoYes,
            headless,
            confirmTimeoutSeconds,
            audit,
            auditPath,
            insecure,
            preferRelay: preferRelay ?? settings.PreferRelay,
            enableLanListen: settings.EnableLanListen,
            relayEndpoint: relayEndpoint ?? settings.RelayEndpoint,
            accountToken: settings.AccountToken,
            deviceId: settings.DeviceId,
            deviceDisplayName: settings.DeviceDisplayName ?? Environment.MachineName);
    }

    public void Start()
    {
        if (_started) return;
        ConfirmPrompt.EnsureWinFormsInitialized();
        _host.PrintBanner();
        _host.Start();

        _ui = new AgentDesktopUi(_host, _enableReturnHome);
        _ui.HomeRequested += () => HomeRequested?.Invoke(this, EventArgs.Empty);
        _ui.ExitRequested += () => ExitRequested?.Invoke(this, EventArgs.Empty);
        _started = true;
    }

    public void ShowMain() => _ui?.ShowMain();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _ui?.Dispose();
        _ui = null;
        _host.Stop();
    }
}
