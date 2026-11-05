using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RemoteAssist;

namespace RemoteAssist.App;

/// <summary>
/// 启动首页：接受协助 / 去协助别人。
/// </summary>
internal sealed class ModeSelectForm : Form
{
    public event Action? AcceptClicked;
    public event Action? ControlClicked;

    private readonly Label _footerHint;
    private readonly Label _advancedDetail;
    private readonly LinkLabel _advancedToggle;
    private bool _advancedOpen;

    public ModeSelectForm()
    {
        Text = "鹊桥";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(560, 420);
        BackColor = Color.FromArgb(0xF7, 0xF8, 0xFA);
        Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = CreateAppIcon();

        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(560, 88),
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8)
        };
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(24, 18),
            Size = new Size(500, 32),
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            Text = "鹊桥远程协助 · 智能体",
            BackColor = Color.Transparent
        });
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(24, 52),
            Size = new Size(500, 24),
            ForeColor = Color.FromArgb(0xE3, 0xF0, 0xFF),
            Text = "一个应用，两种角色 — 先选你要做什么",
            BackColor = Color.Transparent
        });
        Controls.Add(header);

        var btnAccept = MakeModeButton(
            "接受协助",
            "生成本机会话码和验证密码，让亲友或同事连过来帮你排查。危险操作需你本机确认。",
            Color.FromArgb(0x1A, 0x73, 0xE8),
            24, 112);
        btnAccept.Click += (_, _) => AcceptClicked?.Invoke();
        Controls.Add(btnAccept);

        var btnControl = MakeModeButton(
            "去协助别人",
            "粘贴对方发来的会话码和验证密码，连接后查看进程 / 日志 / 文件，或提议执行命令。",
            Color.FromArgb(0x0F, 0x9D, 0x58),
            24, 230);
        btnControl.Click += (_, _) => ControlClicked?.Invoke();
        Controls.Add(btnControl);

        _footerHint = new Label
        {
            AutoSize = false,
            Location = new Point(24, 360),
            Size = new Size(400, 24),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "选上面一项即可。"
        };
        Controls.Add(_footerHint);

        _advancedToggle = new LinkLabel
        {
            AutoSize = true,
            Location = new Point(430, 360),
            Text = "高级",
            LinkColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            ActiveLinkColor = Color.FromArgb(0x0F, 0x5C, 0xBF),
            LinkBehavior = LinkBehavior.HoverUnderline
        };
        _advancedToggle.LinkClicked += (_, _) => ToggleAdvanced();
        Controls.Add(_advancedToggle);

        _advancedDetail = new Label
        {
            AutoSize = false,
            Location = new Point(24, 384),
            Size = new Size(512, 48),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Visible = false,
            Text = "默认公网中继 121.196.45.81:17891（code+密码；设置可改）；局域网仍可用于调试。可用 --accept / --control 直达模式。"
        };
        Controls.Add(_advancedDetail);

        UiWindowPlacement.Bind(this, restoreSize: false);
    }

    private void ToggleAdvanced()
    {
        _advancedOpen = !_advancedOpen;
        _advancedDetail.Visible = _advancedOpen;
        _advancedToggle.Text = _advancedOpen ? "收起" : "高级";
        ClientSize = new Size(560, _advancedOpen ? 448 : 420);
    }

    private static Button MakeModeButton(string title, string subtitle, Color accent, int x, int y)
    {
        var btn = new Button
        {
            Location = new Point(x, y),
            Size = new Size(512, 100),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 12, 18, 12),
            Cursor = Cursors.Hand,
            Font = new Font("Microsoft YaHei UI", 9.5F)
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xF0, 0xF4, 0xFF);

        btn.Text = "";
        var titleLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 18),
            Size = new Size(460, 28),
            Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
            ForeColor = accent,
            Text = title,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var subLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 50),
            Size = new Size(470, 40),
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = subtitle,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        titleLabel.Click += (_, _) => btn.PerformClick();
        subLabel.Click += (_, _) => btn.PerformClick();
        btn.Controls.Add(titleLabel);
        btn.Controls.Add(subLabel);

        var bar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(6, 100),
            BackColor = accent
        };
        btn.Controls.Add(bar);
        return btn;
    }

    internal static Icon CreateAppIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(0x1A, 0x73, 0xE8));
            g.FillEllipse(brush, 2, 2, 28, 28);
            using var pen = new Pen(Color.White, 3);
            g.DrawEllipse(pen, 8, 8, 16, 16);
            g.DrawEllipse(pen, 12, 12, 8, 8);
        }
        using var tmp = Icon.FromHandle(bmp.GetHicon());
        return (Icon)tmp.Clone();
    }
}
