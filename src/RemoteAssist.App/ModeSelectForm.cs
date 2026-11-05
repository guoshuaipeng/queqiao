using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RemoteAssist;

namespace RemoteAssist.App;

/// <summary>
/// 启动首页：接受协助 / 去协助别人（R006：整卡可点入口）。
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
        ClientSize = new Size(560, 460);
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
            Color.FromArgb(0xE8, 0xF0, 0xFE),
            24, 108);
        btnAccept.Click += (_, _) => AcceptClicked?.Invoke();
        Controls.Add(btnAccept);

        var btnControl = MakeModeButton(
            "去协助别人",
            "粘贴对方发来的会话码和验证密码，连接后查看进程 / 日志 / 文件，或提议执行命令。",
            Color.FromArgb(0x0F, 0x9D, 0x58),
            Color.FromArgb(0xE6, 0xF4, 0xEA),
            24, 250);
        btnControl.Click += (_, _) => ControlClicked?.Invoke();
        Controls.Add(btnControl);

        _footerHint = new Label
        {
            AutoSize = false,
            Location = new Point(24, 400),
            Size = new Size(400, 24),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "选上面一项即可。"
        };
        Controls.Add(_footerHint);

        _advancedToggle = new LinkLabel
        {
            AutoSize = true,
            Location = new Point(430, 400),
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
            Location = new Point(24, 424),
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
        ClientSize = new Size(560, _advancedOpen ? 488 : 460);
    }

    private static Button MakeModeButton(string title, string subtitle, Color accent, Color hover, int x, int y)
    {
        var btn = new Button
        {
            Location = new Point(x, y),
            Size = new Size(512, 124),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(22, 16, 22, 16),
            Cursor = Cursors.Hand,
            Font = new Font("Microsoft YaHei UI", 9.5F)
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = hover;
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
            Math.Max(0, hover.R - 18), Math.Max(0, hover.G - 18), Math.Max(0, hover.B - 18));

        btn.Text = "";
        var titleLabel = new Label
        {
            AutoSize = false,
            Location = new Point(22, 20),
            Size = new Size(380, 32),
            Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold),
            ForeColor = accent,
            Text = title,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var subLabel = new Label
        {
            AutoSize = false,
            Location = new Point(22, 56),
            Size = new Size(400, 48),
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = subtitle,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var enterLabel = new Label
        {
            AutoSize = false,
            Location = new Point(420, 48),
            Size = new Size(70, 28),
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = accent,
            TextAlign = ContentAlignment.MiddleRight,
            Text = "进入 ›",
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        void Bubble(object? _, EventArgs __) => btn.PerformClick();
        titleLabel.Click += Bubble;
        subLabel.Click += Bubble;
        enterLabel.Click += Bubble;
        btn.Controls.Add(titleLabel);
        btn.Controls.Add(subLabel);
        btn.Controls.Add(enterLabel);

        var bar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(8, 124),
            BackColor = accent
        };
        btn.Controls.Add(bar);

        // Keep child backgrounds in sync with hover/press so the card feels solid.
        void SyncChildren(Color bg)
        {
            titleLabel.BackColor = bg;
            subLabel.BackColor = bg;
            enterLabel.BackColor = bg;
        }
        btn.MouseEnter += (_, _) => SyncChildren(btn.FlatAppearance.MouseOverBackColor);
        btn.MouseLeave += (_, _) => SyncChildren(Color.Transparent);
        btn.MouseDown += (_, _) => SyncChildren(btn.FlatAppearance.MouseDownBackColor);
        btn.MouseUp += (_, _) => SyncChildren(btn.FlatAppearance.MouseOverBackColor);
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
