using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RemoteAssist.App;

/// <summary>
/// 启动首页：接受协助 / 去协助别人。
/// PV-002 紧凑方案：两卡等分可用高度，卡底→脚注约 20，短文案，无 CLI 脚注。
/// </summary>
internal sealed class ModeSelectForm : Form
{
    public event Action? AcceptClicked;
    public event Action? ControlClicked;

    private const int HeaderH = 64;
    private const int TopPad = 16;
    private const int SidePad = 20;
    private const int CardGap = 12;
    private const int CardToFoot = 20;
    private const int FootH = 34;
    private const int MaxCardH = 160;
    private const int MinCardH = 72;

    private readonly Panel _header;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Button _btnAccept;
    private readonly Button _btnControl;
    private readonly Label _footer;
    private readonly Panel _scrollBody;

    public ModeSelectForm()
    {
        Text = "鹊桥";
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(640, 360);
        BackColor = Color.FromArgb(0xF7, 0xF8, 0xFA);
        Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        AutoScroll = false;
        DoubleBuffered = true;

        _scrollBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = BackColor
        };
        Controls.Add(_scrollBody);

        _header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(640, HeaderH),
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8)
        };
        _titleLabel = new Label
        {
            AutoSize = false,
            Location = new Point(SidePad, 10),
            Size = new Size(600, 26),
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 15F, FontStyle.Bold),
            Text = "鹊桥",
            BackColor = Color.Transparent
        };
        _subtitleLabel = new Label
        {
            AutoSize = false,
            Location = new Point(SidePad, 36),
            Size = new Size(600, 20),
            ForeColor = Color.FromArgb(0xE3, 0xF0, 0xFF),
            Text = "选择你要做的事",
            BackColor = Color.Transparent
        };
        _header.Controls.Add(_titleLabel);
        _header.Controls.Add(_subtitleLabel);
        _scrollBody.Controls.Add(_header);

        _btnAccept = MakeModeButton(
            "接受协助",
            "把连接凭证发给来帮你的人。",
            Color.FromArgb(0x1A, 0x73, 0xE8));
        _btnAccept.Click += (_, _) => AcceptClicked?.Invoke();
        _scrollBody.Controls.Add(_btnAccept);

        _btnControl = MakeModeButton(
            "去协助别人",
            "粘贴对方的连接凭证，开始协助。",
            Color.FromArgb(0x0F, 0x9D, 0x58));
        _btnControl.Click += (_, _) => ControlClicked?.Invoke();
        _scrollBody.Controls.Add(_btnControl);

        _footer = new Label
        {
            AutoSize = false,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "默认通过中继连接",
            BackColor = Color.Transparent
        };
        _scrollBody.Controls.Add(_footer);

        Resize += (_, _) => Relayout();
        Relayout();
    }

    private void Relayout()
    {
        var w = Math.Max(_scrollBody.ClientSize.Width, 480);
        var h = Math.Max(_scrollBody.ClientSize.Height, 280);

        _header.SetBounds(0, 0, w, HeaderH);
        _titleLabel.SetBounds(SidePad, 10, Math.Max(80, w - SidePad * 2), 26);
        _subtitleLabel.SetBounds(SidePad, 36, Math.Max(80, w - SidePad * 2), 20);

        var contentTop = HeaderH + TopPad;
        var availableForCards = h - contentTop - CardToFoot - FootH - CardGap;
        var rawCardH = availableForCards / 2;
        var cardH = Math.Max(MinCardH, Math.Min(MaxCardH, rawCardH));

        // 卡高封顶后剩余高度：整体内容块垂直居中，不堆成卡脚空灰
        var cardsBlockH = cardH + CardGap + cardH;
        var belowHeaderH = cardsBlockH + CardToFoot + FootH;
        var freeBelowHeader = h - HeaderH - TopPad - belowHeaderH;
        var blockTop = contentTop;
        if (rawCardH > MaxCardH && freeBelowHeader > 0)
            blockTop = HeaderH + TopPad + freeBelowHeader / 2;

        var cardW = Math.Max(200, w - SidePad * 2);
        _btnAccept.SetBounds(SidePad, blockTop, cardW, cardH);
        LayoutCardInner(_btnAccept, cardW, cardH);
        _btnControl.SetBounds(SidePad, blockTop + cardH + CardGap, cardW, cardH);
        LayoutCardInner(_btnControl, cardW, cardH);

        var footY = blockTop + cardH + CardGap + cardH + CardToFoot;
        _footer.SetBounds(SidePad, footY, cardW, FootH);

        // 内容高于可视区时靠 AutoScroll 保可达
        var needH = footY + FootH + 8;
        _scrollBody.AutoScrollMinSize = new Size(0, Math.Max(needH, h));
    }

    private static void LayoutCardInner(Button btn, int cardW, int cardH)
    {
        foreach (Control c in btn.Controls)
        {
            if (c is Panel bar && bar.Width <= 8)
            {
                bar.SetBounds(0, 0, 6, cardH);
                continue;
            }
            if (c is Label lbl)
            {
                if (lbl.Text == "›")
                {
                    lbl.SetBounds(cardW - 44, Math.Max(0, (cardH - 40) / 2), 36, 40);
                }
                else if (ReferenceEquals(lbl.Tag, "title"))
                {
                    // title+sub as a group vertically centered
                }
            }
        }

        Label? title = null;
        Label? sub = null;
        Label? chevron = null;
        Panel? accent = null;
        foreach (Control c in btn.Controls)
        {
            if (c is Panel p && p.Width <= 8) accent = p;
            else if (c is Label l)
            {
                if (l.Text == "›") chevron = l;
                else if (Equals(l.Tag, "title")) title = l;
                else if (Equals(l.Tag, "sub")) sub = l;
            }
        }

        accent?.SetBounds(0, 0, 6, cardH);
        chevron?.SetBounds(cardW - 44, Math.Max(0, (cardH - 40) / 2), 36, 40);

        const int textLeft = 20;
        var textW = Math.Max(80, cardW - 68);
        var titleH = 28;
        var subH = Math.Min(44, Math.Max(20, cardH / 3));
        var groupH = titleH + 4 + subH;
        var groupTop = Math.Max(10, (cardH - groupH) / 2);
        if (title is not null)
            title.SetBounds(textLeft, groupTop, textW, titleH);
        if (sub is not null)
            sub.SetBounds(textLeft, groupTop + titleH + 4, textW, subH);
    }

    private static Button MakeModeButton(string title, string subtitle, Color accent)
    {
        var btn = new Button
        {
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0),
            Cursor = Cursors.Hand,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            Text = ""
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xF0, 0xF4, 0xFF);

        var titleLabel = new Label
        {
            Tag = "title",
            AutoSize = false,
            Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
            ForeColor = accent,
            Text = title,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var subLabel = new Label
        {
            Tag = "sub",
            AutoSize = false,
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = subtitle,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        var chevron = new Label
        {
            AutoSize = false,
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
        chevron.Click += ForwardClick;
        btn.Controls.Add(titleLabel);
        btn.Controls.Add(subLabel);
        btn.Controls.Add(chevron);
        btn.Controls.Add(new Panel
        {
            Size = new Size(6, 100),
            BackColor = accent
        });

        btn.MouseEnter += (_, _) =>
        {
            btn.FlatAppearance.BorderColor = accent;
            btn.BackColor = Color.FromArgb(0xF8, 0xFA, 0xFF);
        };
        btn.MouseLeave += (_, _) =>
        {
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

