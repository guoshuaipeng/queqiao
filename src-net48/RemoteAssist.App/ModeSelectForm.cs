using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RemoteAssist.App;

/// <summary>
/// 启动首页：接受协助（被控） / 去协助别人（控制）。
/// PV-002：收紧空白、卡片更像可点入口、版面更饱满。
/// </summary>
internal sealed class ModeSelectForm : Form
{
    public event Action? AcceptClicked;
    public event Action? ControlClicked;

    public ModeSelectForm()
    {
        Text = "鹊桥";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        // 收紧窗体高度，避免顶栏+双卡偏上、下方大片空灰
        ClientSize = new Size(560, 392);
        BackColor = Color.FromArgb(0xF7, 0xF8, 0xFA);
        Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = CreateAppIcon();

        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(560, 76),
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8)
        };
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(24, 14),
            Size = new Size(500, 28),
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            Text = "鹊桥",
            BackColor = Color.Transparent
        });
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(24, 44),
            Size = new Size(500, 22),
            ForeColor = Color.FromArgb(0xE3, 0xF0, 0xFF),
            Text = "一个应用，两种角色 — 先选你要做什么",
            BackColor = Color.Transparent
        });
        Controls.Add(header);

        var btnAccept = MakeModeButton(
            "接受协助（被控）",
            "点「复制连接凭证」，把复制出来的整段发给要帮你的人。危险操作需你本机确认。",
            Color.FromArgb(0x1A, 0x73, 0xE8),
            20, 92);
        btnAccept.Click += (_, _) => AcceptClicked?.Invoke();
        Controls.Add(btnAccept);

        var btnControl = MakeModeButton(
            "去协助别人（控制）",
            "把对方通过「复制连接凭证」发来的整段粘贴到这里。连接后查看进程 / 日志 / 文件，或提议执行命令。",
            Color.FromArgb(0x0F, 0x9D, 0x58),
            20, 224);
        btnControl.Click += (_, _) => ControlClicked?.Invoke();
        Controls.Add(btnControl);

        Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(24, 356),
            Size = new Size(512, 28),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "默认经公网中继连接。局域网 Host/端口仅在协助端「高级」里。也可用 --accept / --control 直达对应模式。"
        });
    }

    private static Button MakeModeButton(string title, string subtitle, Color accent, int x, int y)
    {
        const int cardH = 120;
        const int cardW = 520;
        var btn = new Button
        {
            Location = new Point(x, y),
            Size = new Size(cardW, cardH),
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
            Location = new Point(20, 16),
            Size = new Size(430, 28),
            Font = new Font("Microsoft YaHei UI", 13.5F, FontStyle.Bold),
            ForeColor = accent,
            Text = title,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var subLabel = new Label
        {
            AutoSize = false,
            Location = new Point(20, 48),
            Size = new Size(430, 44),
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = subtitle,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var hint = new Label
        {
            AutoSize = false,
            Location = new Point(20, 94),
            Size = new Size(200, 18),
            Font = new Font("Microsoft YaHei UI", 8.5F),
            ForeColor = accent,
            Text = "点击进入",
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var chevron = new Label
        {
            AutoSize = false,
            Location = new Point(cardW - 48, 40),
            Size = new Size(36, 40),
            Font = new Font("Segoe UI", 22F, FontStyle.Regular),
            ForeColor = accent,
            Text = "›",
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        void ForwardClick(object? s, EventArgs e) => btn.PerformClick();
        titleLabel.Click += ForwardClick;
        subLabel.Click += ForwardClick;
        hint.Click += ForwardClick;
        chevron.Click += ForwardClick;
        btn.Controls.Add(titleLabel);
        btn.Controls.Add(subLabel);
        btn.Controls.Add(hint);
        btn.Controls.Add(chevron);

        var bar = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(6, cardH),
            BackColor = accent
        };
        btn.Controls.Add(bar);

        btn.MouseEnter += (_, _) =>
        {
            btn.FlatAppearance.BorderColor = accent;
            btn.BackColor = Color.FromArgb(0xF8, 0xFA, 0xFF);
        };
        btn.MouseLeave += (_, _) =>
        {
            // 子控件上的离开会冒泡；仅当鼠标真正离开卡片再还原
            var pt = btn.PointToClient(Cursor.Position);
            if (btn.ClientRectangle.Contains(pt)) return;
            btn.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
            btn.BackColor = Color.White;
        };
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