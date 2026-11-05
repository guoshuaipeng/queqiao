using System.Drawing;
using System.Windows.Forms;

namespace RemoteAssist.Agent;

/// <summary>
/// 系统托盘常驻：打开主界面 / 已配对客户端断开 / 刷新会话码 / 退出。
/// </summary>
internal sealed class AgentTrayContext : ApplicationContext
{
    private readonly AgentHost _host;
    private readonly NotifyIcon _tray;
    private readonly MainForm _mainForm;
    private readonly Icon _icon;
    private readonly ToolStripMenuItem _clientsMenu;

    public AgentTrayContext(AgentHost host)
    {
        _host = host;
        ConfirmPrompt.EnsureWinFormsInitialized();

        _icon = AppIcons.CreateTrayIcon();
        _mainForm = new MainForm(host);

        _clientsMenu = new ToolStripMenuItem("已配对客户端");

        var menu = new ContextMenuStrip();
        menu.Items.Add("打开主界面", null, (_, _) => ShowMain());
        menu.Items.Add("复制 MCP 配置片段", null, (_, _) => CopyMcpConfigSnippet());
        menu.Items.Add("刷新会话码", null, (_, _) =>
        {
            _host.RefreshSessionCode();
            ShowMain();
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_clientsMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApp());
        menu.Opening += (_, _) => RebuildClientsMenu();

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "远端协助",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowMain();

        _host.PairedClientsChanged += OnPairedClientsChanged;

        ShowMain();
        _tray.ShowBalloonTip(2500, "远端协助", "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。", ToolTipIcon.Info);
    }

    private void OnPairedClientsChanged()
    {
        try
        {
            var n = _host.ListPairedClients().Count;
            _tray.Text = n == 0 ? "远端协助" : $"远端协助（{n} 个已配对）";
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

    private void ShowMain() => _mainForm.ShowAndActivate();

    private void ExitApp()
    {
        _host.PairedClientsChanged -= OnPairedClientsChanged;
        _tray.Visible = false;
        _mainForm.RequestExit();
        _host.Stop();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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
        base.Dispose(disposing);
    }
}
