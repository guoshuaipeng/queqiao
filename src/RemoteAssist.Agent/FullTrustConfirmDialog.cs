using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>打开「完全信任」前的本机二次确认（原文见 TrustLevelCopy / trust-levels.md）。</summary>
internal sealed class FullTrustConfirmDialog : Form
{
    public FullTrustConfirmDialog()
    {
        Text = TrustLevelCopy.FullTrustConfirmTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(480, 260);
        Font = new Font("Microsoft YaHei UI", 9.5F);

        Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 16),
            Size = new Size(440, 28),
            Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
            Text = TrustLevelCopy.FullTrustConfirmTitle,
            Name = "fullTrustConfirmTitle"
        });
        Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 52),
            Size = new Size(440, 130),
            Text = TrustLevelCopy.FullTrustConfirmBody,
            Name = "fullTrustConfirmBody"
        });

        var ok = new Button
        {
            Text = TrustLevelCopy.FullTrustConfirmOk,
            DialogResult = DialogResult.OK,
            Location = new Point(100, 200),
            Size = new Size(180, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0xE3, 0x74, 0x00),
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
            Name = "fullTrustConfirmOk"
        };
        ok.FlatAppearance.BorderSize = 0;

        var cancel = new Button
        {
            Text = TrustLevelCopy.FullTrustConfirmCancel,
            DialogResult = DialogResult.Cancel,
            Location = new Point(300, 200),
            Size = new Size(90, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Name = "fullTrustConfirmCancel"
        };
        cancel.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        cancel.FlatAppearance.BorderSize = 1;

        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
