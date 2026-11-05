using System.Drawing;
using System.Windows.Forms;

namespace RemoteAssist.Agent;

/// <summary>
/// 被控端桌面会话：主界面 + 托盘。不继承 ApplicationContext，便于统一 App 切换模式。
/// </summary>
internal sealed class AgentDesktopUi : IDisposable
{
    private readonly AgentHost _host;
    private readonly NotifyIcon _tray;
    private readonly MainForm _mainForm;
    private readonly Icon _icon;
    private readonly ToolStripMenuItem _clientsMenu;
    private readonly bool _enableReturnHome;
    private bool _disposed;

    /// <summary>用户选择退出整个进程。</summary>
    public event Action? ExitRequested;

    /// <summary>用户选择返回统一 App 首页（仅 enableReturnHome 时可用）。</summary>
    public event Action? HomeRequested;

    public AgentDesktopUi(AgentHost host, bool enableReturnHome = false)
    {
        _host = host;
        _enableReturnHome = enableReturnHome;
        ConfirmPrompt.EnsureWinFormsInitialized();

        _icon = AppIcons.CreateTrayIcon();
        _mainForm = new MainForm(host, enableReturnHome);
        if (enableReturnHome)
            _mainForm.HomeRequested += TryReturnHome;

        _clientsMenu = new ToolStripMenuItem("已配对客户端");

        var menu = new ContextMenuStrip();
        menu.Items.Add("打开主界面", null, (_, _) => ShowMain());
        if (enableReturnHome)
            menu.Items.Add("返回首页（切换模式）", null, (_, _) => TryReturnHome());
        menu.Items.Add("复制 MCP 配置片段", null, (_, _) => CopyMcpConfigSnippet());
        menu.Items.Add("刷新会话码", null, (_, _) =>
        {
            _host.RefreshSessionCode();
            ShowMain();
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_clientsMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => RequestExit());
        menu.Opening += (_, _) => RebuildClientsMenu();

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "远端协助 · 被控",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowMain();

        _host.PairedClientsChanged += OnPairedClientsChanged;

        ShowMain();
        _tray.ShowBalloonTip(2500, "远端协助",
            enableReturnHome
                ? "被控模式已启动。可返回首页切换为控制端；关闭主窗口会留在托盘。"
                : "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。",
            ToolTipIcon.Info);
    }

    private void OnPairedClientsChanged()
    {
        try
        {
            var n = _host.ListPairedClients().Count;
            _tray.Text = n == 0 ? "远端协助 · 被控" : $"远端协助 · 被控（{n} 个已配对）";
        }
        catch { /* ignore */ }
    }

    private void RebuildClientsMenu()
    {
        _clientsMenu.DropDownItems.Clear();
        var clients = _host.ListPairedClients();
        if (clients.Count == 0)
        {
            _clientsMenu.DropDownItems.Add(new ToolStripMenuItem("（无）") { Enabled = false });
            return;
        }

        foreach (var c in clients)
        {
            var shortTok = c.Token.Length >= 8 ? c.Token[..8] : c.Token;
            var label = $"{c.ClientName} · {c.Permission} · {c.RemoteEndpoint}";
            var item = new ToolStripMenuItem(label);
            var token = c.Token;
            item.DropDownItems.Add(new ToolStripMenuItem($"断开（{shortTok}…）", null, (_, _) =>
            {
                if (_host.DisconnectSession(token))
                {
                    _tray.ShowBalloonTip(2000, "远端协助", $"已断开 {c.ClientName}", ToolTipIcon.Info);
                    RebuildClientsMenu();
                }
            }));
            _clientsMenu.DropDownItems.Add(item);
        }

        _clientsMenu.DropDownItems.Add(new ToolStripSeparator());
        _clientsMenu.DropDownItems.Add(new ToolStripMenuItem("断开全部", null, (_, _) =>
        {
            var n = _host.DisconnectAllSessions();
            _tray.ShowBalloonTip(2000, "远端协助", $"已断开 {n} 个会话", ToolTipIcon.Info);
            RebuildClientsMenu();
        }));
    }

    private void CopyMcpConfigSnippet()
    {
        IWin32Window? owner = _mainForm.Visible && !_mainForm.IsDisposed ? _mainForm : null;
        var outcome = McpConfigCopyHelper.Copy(_host.QrPayload, owner);

        var tipIcon = outcome.Kind switch
        {
            McpConfigCopyHelper.OutcomeKind.Failed => ToolTipIcon.Error,
            McpConfigCopyHelper.OutcomeKind.Cancelled => ToolTipIcon.Info,
            McpConfigCopyHelper.OutcomeKind.CopiedSnippet when outcome.ExeExists => ToolTipIcon.Info,
            McpConfigCopyHelper.OutcomeKind.CopiedSnippet => ToolTipIcon.Warning,
            McpConfigCopyHelper.OutcomeKind.StartedPublish => ToolTipIcon.Info,
            McpConfigCopyHelper.OutcomeKind.CopiedPublishCommand => ToolTipIcon.Info,
            _ => ToolTipIcon.Info
        };

        var tip = outcome.Message;
        if (tip.Length > 220)
            tip = tip[..217] + "…";
        _tray.ShowBalloonTip(5000, "远端协助", tip, tipIcon);
    }

    public void ShowMain() => _mainForm.ShowAndActivate();

    private void TryReturnHome()
    {
        if (!_enableReturnHome) return;
        var n = _host.ListPairedClients().Count;
        if (n > 0)
        {
            var r = MessageBox.Show(
                $"当前有 {n} 个已配对协助方。返回首页将断开全部会话并停止被控监听。是否继续？",
                "返回首页",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) return;
        }

        HomeRequested?.Invoke();
    }

    private void RequestExit()
    {
        ExitRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _host.PairedClientsChanged -= OnPairedClientsChanged;
        _tray.Visible = false;
        _tray.Dispose();
        if (!_mainForm.IsDisposed)
        {
            _mainForm.RequestExit();
            _mainForm.Dispose();
        }
        _icon.Dispose();
    }
}

/// <summary>独立 Agent 进程用的 ApplicationContext 包装。</summary>
internal sealed class AgentTrayContext : ApplicationContext
{
    private readonly AgentHost _host;
    private readonly AgentDesktopUi _ui;

    public AgentTrayContext(AgentHost host)
    {
        _host = host;
        _ui = new AgentDesktopUi(host, enableReturnHome: false);
        _ui.ExitRequested += () =>
        {
            _ui.Dispose();
            _host.Stop();
            ExitThread();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _ui.Dispose();
        base.Dispose(disposing);
    }
}
