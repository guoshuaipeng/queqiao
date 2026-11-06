using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Helper.UI;
using RemoteAssist.Protocol;

namespace RemoteAssist.App;

/// <summary>
/// R049：唯一主窗口。首页 / 接受协助 / 去协助在页内切换，不新开第二主窗。
/// PV-002：首页客户区默认 640×360，与业务页分设尺寸；禁止把壳永久缩死压他页。
/// </summary>
internal sealed class ShellMainForm : Form
{
    /// <summary>首页首次客户区（DIP）。</summary>
    public static readonly Size HomeClientSize = new(640, 360);

    /// <summary>首页窄参考最小客户区。</summary>
    public static readonly Size HomeMinClientSize = new(520, 340);

    /// <summary>接受协助页参考客户区（对齐 Agent MainForm）。</summary>
    public static readonly Size AcceptClientSize = new(760, 700);

    /// <summary>协助端参考客户区（对齐 Helper MainForm）。</summary>
    public static readonly Size ControlClientSize = new(840, 820);

    private readonly Panel _hostPanel;
    private readonly ModeSelectForm _homePage;
    private Control? _acceptPage;
    private Control? _controlPage;
    private enum Page { Home, Accept, Control }
    private Page _page = Page.Home;
    private bool _applyingChrome;

    public event Action? AcceptRequested;
    public event Action? ControlRequested;

    /// <summary>用户点窗口关闭且确认可退出时。</summary>
    public event Action? ExitRequested;

    public ShellMainForm()
    {
        Text = "鹊桥";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(0xF5, 0xF6, 0xF8);
        Icon = ModeSelectForm.CreateAppIcon();

        _hostPanel = new Panel { Dock = DockStyle.Fill, BackColor = BackColor, AutoScroll = true };
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
        ApplyHomeChrome(center: true);
        ShowHome();
    }

    public void ShowHome()
    {
        _page = Page.Home;
        ApplyHomeChrome(center: false);
        ShowOnly(_homePage);
        Text = "鹊桥";
    }

    public void ShowAcceptPage(Control page)
    {
        _acceptPage = page;
        _page = Page.Accept;
        ApplyBusinessChrome(AcceptClientSize);
        EmbedChild(page);
        ShowOnly(page);
        Text = "鹊桥 · 接受协助";
    }

    public void ShowControlPage(Control page)
    {
        _controlPage = page;
        _page = Page.Control;
        ApplyBusinessChrome(ControlClientSize);
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

    private void ApplyHomeChrome(bool center)
    {
        if (_applyingChrome) return;
        _applyingChrome = true;
        try
        {
            // 仅首页使用紧凑客户区；回首页时恢复，不永久钉死业务页
            MinimumSize = SizeFromClient(HomeMinClientSize);
            if (WindowState == FormWindowState.Normal)
            {
                ClientSize = FitToWorkingArea(HomeClientSize);
                if (center)
                    CenterToScreen();
            }
        }
        finally
        {
            _applyingChrome = false;
        }
    }

    private void ApplyBusinessChrome(Size preferredClient)
    {
        if (_applyingChrome) return;
        _applyingChrome = true;
        try
        {
            // 业务页抬高最小尺寸，避免被首页紧凑最小尺寸卡住
            var min = new Size(
                Math.Max(HomeMinClientSize.Width, Math.Min(preferredClient.Width, 640)),
                Math.Max(HomeMinClientSize.Height, Math.Min(preferredClient.Height, 480)));
            MinimumSize = SizeFromClient(min);
            if (WindowState == FormWindowState.Normal)
                ClientSize = FitToWorkingArea(preferredClient);
        }
        finally
        {
            _applyingChrome = false;
        }
    }

    private Size FitToWorkingArea(Size desiredClient)
    {
        var wa = Screen.FromControl(this).WorkingArea;
        var frameW = Width - ClientSize.Width;
        var frameH = Height - ClientSize.Height;
        var maxClientW = Math.Max(HomeMinClientSize.Width, wa.Width - frameW - 16);
        var maxClientH = Math.Max(HomeMinClientSize.Height, wa.Height - frameH - 16);
        return new Size(
            Math.Min(desiredClient.Width, maxClientW),
            Math.Min(desiredClient.Height, maxClientH));
    }

    private Size SizeFromClient(Size client)
    {
        var frameW = Math.Max(0, Width - ClientSize.Width);
        var frameH = Math.Max(0, Height - ClientSize.Height);
        // 首次布局时 Width/Client 可能尚未稳定，给一个保守边框余量
        if (frameW < 8) frameW = 16;
        if (frameH < 8) frameH = 39;
        return new Size(client.Width + frameW, client.Height + frameH);
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

