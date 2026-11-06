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
    private Control? _embedHost;
    private Action? _activateShell;

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
            Text = "鹊桥 · 被控",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowMain();

        _host.PairedClientsChanged += OnPairedClientsChanged;

        // 统一 App（enableReturnHome）：由外壳嵌入展示，避免第二主窗
        if (!enableReturnHome)
            ShowMain();
        _tray.ShowBalloonTip(2500, "鹊桥",
            enableReturnHome
                ? "被控模式已启动。可用「返回首页」切换模式（有会话须本机确认结束）。"
                : "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。",
            ToolTipIcon.Info);
    }

    private void OnPairedClientsChanged()
    {
        try
        {
            var n = _host.ListPairedClients().Count;
            _tray.Text = n == 0 ? "鹊桥 · 被控" : $"鹊桥 · 被控（{n} 个已配对）";
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
                    _tray.ShowBalloonTip(2000, "鹊桥", $"已断开 {c.ClientName}", ToolTipIcon.Info);
                    RebuildClientsMenu();
                }
            }));
            _clientsMenu.DropDownItems.Add(item);
        }

        _clientsMenu.DropDownItems.Add(new ToolStripSeparator());
        _clientsMenu.DropDownItems.Add(new ToolStripMenuItem("断开全部", null, (_, _) =>
        {
            var n = _host.DisconnectAllSessions();
            _tray.ShowBalloonTip(2000, "鹊桥", $"已断开 {n} 个会话", ToolTipIcon.Info);
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
        _tray.ShowBalloonTip(5000, "鹊桥", tip, tipIcon);
    }

    public void ShowMain()
    {
        if (_embedHost is not null)
        {
            _activateShell?.Invoke();
            return;
        }
        _mainForm.ShowAndActivate();
    }

    /// <summary>R049：嵌入统一主窗口；托盘「打开主界面」激活外壳而非第二主窗。</summary>
    public void AttachToShell(Control hostPanel, Action activateShell)
    {
        _embedHost = hostPanel;
        _activateShell = activateShell;
        _mainForm.SetEmbeddedInShell(true);
        // 不在此处 Show：由外壳 ShowAcceptPage 负责可见性
    }

    public Form MainFormControl => _mainForm;

    /// <summary>若有活动被控会话，本机确认结束后才返回 true。</summary>
    public bool ConfirmEndSessionOrCancel(IWin32Window? owner)
    {
        var n = _host.ListPairedClients().Count;
        var listening = _started_or_active();
        if (n == 0 && !listening)
            return true;

        var msg = n > 0
            ? $"当前有 {n} 个已配对协助方。结束被控将断开全部会话并停止监听。是否结束？"
            : "正在接受协助（中继已连接/等待中）。返回或切换将结束本次被控。是否结束？";
        var r = MessageBox.Show(
            owner,
            msg,
            "结束接受协助",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        return r == DialogResult.Yes;
    }

    private bool _started_or_active()
    {
        // PreferRelay 启动后即视为活动会话（等待对方也算），避免留下隐藏监听
        try
        {
            if (_host.PreferRelay)
            {
                var phase = _host.RelayPhase ?? "";
                if (phase is not ("error" or ""))
                    return true;
            }
            return _host.ListPairedClients().Count > 0;
        }
        catch { return true; }
    }

    private void TryReturnHome()
    {
        if (!_enableReturnHome) return;
        IWin32Window? owner = _mainForm.Visible && !_mainForm.IsDisposed ? _mainForm : null;
        if (!ConfirmEndSessionOrCancel(owner))
            return; // 取消：保持原会话原页
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
