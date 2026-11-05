using System.Drawing;
using System.Windows.Forms;

namespace RemoteAssist.Agent;

/// <summary>
/// 系统托盘常驻：会话信息 / 已配对客户端断开 / 刷新会话码 / 退出。
/// </summary>
internal sealed class AgentTrayContext : ApplicationContext
{
    private readonly AgentHost _host;
    private readonly NotifyIcon _tray;
    private readonly SessionInfoForm _sessionForm;
    private readonly Icon _icon;
    private readonly ToolStripMenuItem _clientsMenu;

    public AgentTrayContext(AgentHost host)
    {
        _host = host;
        ConfirmPrompt.EnsureWinFormsInitialized();

        _icon = CreateTrayIcon();
        _sessionForm = new SessionInfoForm(host);

        _clientsMenu = new ToolStripMenuItem("已配对客户端");

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示会话信息", null, (_, _) => ShowSessionInfo());
        menu.Items.Add("复制 MCP 配置片段", null, (_, _) => CopyMcpConfigSnippet());
        menu.Items.Add("刷新会话码", null, (_, _) =>
        {
            _host.RefreshSessionCode();
            ShowSessionInfo();
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_clientsMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApp());
        menu.Opening += (_, _) => RebuildClientsMenu();

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "远端协助 Agent",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowSessionInfo();

        _host.PairedClientsChanged += OnPairedClientsChanged;

        ShowSessionInfo();
        _tray.ShowBalloonTip(2500, "远端协助", "Agent 已在托盘运行。右键可查看会话码、断开客户端或退出。", ToolTipIcon.Info);
    }

    private void OnPairedClientsChanged()
    {
        // 菜单下次 Opening 时重建即可；此处更新托盘提示
        try
        {
            var n = _host.ListPairedClients().Count;
            _tray.Text = n == 0 ? "远端协助 Agent" : $"远端协助 Agent（{n} 个已配对）";
        }
        catch
        {
            /* ignore */
        }
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
        try
        {
            var exe = McpExeLocator.Resolve();
            var payload = _host.QrPayload;
            var text = RemoteAssist.Protocol.McpConfigSnippet.BuildClipboardBundle(exe, payload);
            Clipboard.SetText(text);
            var exists = File.Exists(exe);
            var tip = exists
                ? "已复制 Cursor mcp.json 片段（含当前会话载荷）。粘贴到 MCP 配置即可。"
                : "已复制片段。请先运行 scripts\\publish-mcp.ps1，或把 command 改成实际 MCP exe 路径。"
                  + Environment.NewLine + "当前猜测：" + exe;
            _tray.ShowBalloonTip(4500, "远端协助", tip, exists ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(4000, "远端协助", "复制失败：" + ex.Message, ToolTipIcon.Error);
        }
    }

    private void ShowSessionInfo() => _sessionForm.ShowAndActivate();

    private void ExitApp()
    {
        _host.PairedClientsChanged -= OnPairedClientsChanged;
        _tray.Visible = false;
        _sessionForm.RequestExit();
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
            if (!_sessionForm.IsDisposed)
            {
                _sessionForm.RequestExit();
                _sessionForm.Dispose();
            }
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(0x1A, 0x73, 0xE8));
            g.FillEllipse(brush, 1, 1, 14, 14);
            using var pen = new Pen(Color.White, 2);
            g.DrawEllipse(pen, 4, 4, 8, 8);
        }
        using var tmp = Icon.FromHandle(bmp.GetHicon());
        return (Icon)tmp.Clone();
    }
}

