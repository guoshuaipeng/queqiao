using System.Drawing;
using System.Windows.Forms;

namespace RemoteAssist.Agent;

/// <summary>
/// 系统托盘常驻：显示会话信息 / 刷新会话码 / 退出。
/// </summary>
internal sealed class AgentTrayContext : ApplicationContext
{
    private readonly AgentHost _host;
    private readonly NotifyIcon _tray;
    private readonly SessionInfoForm _sessionForm;
    private readonly Icon _icon;

    public AgentTrayContext(AgentHost host)
    {
        _host = host;
        ConfirmPrompt.EnsureWinFormsInitialized();

        _icon = CreateTrayIcon();
        _sessionForm = new SessionInfoForm(host);

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示会话信息", null, (_, _) => ShowSessionInfo());
        menu.Items.Add("刷新会话码", null, (_, _) =>
        {
            _host.RefreshSessionCode();
            ShowSessionInfo();
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApp());

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "远端协助 Agent",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowSessionInfo();

        // 启动时展示一次会话窗，用户可关到托盘
        ShowSessionInfo();
        _tray.ShowBalloonTip(2500, "远端协助", "Agent 已在托盘运行。右键可查看会话码或退出。", ToolTipIcon.Info);
    }

    private void ShowSessionInfo() => _sessionForm.ShowAndActivate();

    private void ExitApp()
    {
        _tray.Visible = false;
        _sessionForm.RequestExit();
        _host.Stop();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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

