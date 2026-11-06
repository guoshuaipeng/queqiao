using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Helper.UI;
using RemoteAssist.Protocol;

namespace RemoteAssist.App;

/// <summary>
/// R049：唯一主窗口。首页 / 接受协助 / 去协助在页内切换，不新开第二主窗。
/// 安全确认对话框仍为独立本机窗，不并入本窗。
/// </summary>
internal sealed class ShellMainForm : Form
{
    private readonly Panel _hostPanel;
    private readonly ModeSelectForm _homePage;
    private Control? _acceptPage;
    private Control? _controlPage;
    private enum Page { Home, Accept, Control }
    private Page _page = Page.Home;

    public event Action? AcceptRequested;
    public event Action? ControlRequested;

    /// <summary>用户点窗口关闭且确认可退出时。</summary>
    public event Action? ExitRequested;

    public ShellMainForm()
    {
        Text = "鹊桥";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(780, 780);
        BackColor = Color.FromArgb(0xF5, 0xF6, 0xF8);

        _hostPanel = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
        Controls.Add(_hostPanel);

        _homePage = new ModeSelectForm();
        EmbedChild(_homePage);
        _homePage.AcceptClicked += () => AcceptRequested?.Invoke();
        _homePage.ControlClicked += () => ControlRequested?.Invoke();
        // ModeSelect 作为子页时关闭子窗不应退出应用
        _homePage.FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        };

        FormClosing += OnShellClosing;
        ShowHome();
    }

    public void ShowHome()
    {
        _page = Page.Home;
        ShowOnly(_homePage);
        Text = "鹊桥";
    }

    public void ShowAcceptPage(Control page)
    {
        _acceptPage = page;
        _page = Page.Accept;
        EmbedChild(page);
        ShowOnly(page);
        Text = "鹊桥 · 接受协助";
    }

    public void ShowControlPage(Control page)
    {
        _controlPage = page;
        _page = Page.Control;
        EmbedChild(page);
        ShowOnly(page);
        Text = "鹊桥 · 协助端";
    }

    public void ActivateShell()
    {
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        Show();
        Activate();
        BringToFront();
    }

    private void ShowOnly(Control page)
    {
        foreach (Control c in _hostPanel.Controls)
            c.Visible = ReferenceEquals(c, page);
        if (!page.Visible)
            page.Visible = true;
        page.BringToFront();
    }

    private void EmbedChild(Control page)
    {
        if (page is Form f)
        {
            f.TopLevel = false;
            f.FormBorderStyle = FormBorderStyle.None;
            f.Dock = DockStyle.Fill;
            f.Visible = true;
        }
        else
        {
            page.Dock = DockStyle.Fill;
        }

        if (!ReferenceEquals(page.Parent, _hostPanel))
        {
            page.Parent?.Controls.Remove(page);
            _hostPanel.Controls.Add(page);
        }
    }

    private void OnShellClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing)
            return;

        // 关闭主窗：有活动页时由外壳先走「结束会话」确认（调用方在 Closing 外处理更清晰）
        // 这里仅取消默认关闭，交给 AppShell.HandleShellClose
        e.Cancel = true;
        ExitRequested?.Invoke();
    }
}
