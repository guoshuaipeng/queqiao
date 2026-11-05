using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>结束这次协助前的本机确认（R033）。无超时自动结束；默认偏向取消。</summary>
internal sealed class EndAssistConfirmDialog : Form
{
    public EndAssistConfirmDialog()
    {
        Text = EndAssistCopy.ButtonText;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 170);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        BackColor = Color.White;

        Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 24),
            Size = new Size(380, 60),
            Font = new Font(Font.FontFamily, 11F),
            Text = EndAssistCopy.ConfirmBody,
            Name = "endAssistConfirmBody"
        });

        var ok = new Button
        {
            Text = EndAssistCopy.ConfirmOk,
            DialogResult = DialogResult.OK,
            Location = new Point(100, 110),
            Size = new Size(140, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Name = "endAssistConfirmOk"
        };
        ok.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        ok.FlatAppearance.BorderSize = 1;

        var cancel = new Button
        {
            Text = EndAssistCopy.ConfirmCancel,
            DialogResult = DialogResult.Cancel,
            Location = new Point(260, 110),
            Size = new Size(90, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Name = "endAssistConfirmCancel"
        };
        cancel.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        cancel.FlatAppearance.BorderSize = 1;

        Controls.Add(ok);
        Controls.Add(cancel);
        // 超时/回车不要自动结束：默认与 Escape 都走取消
        AcceptButton = cancel;
        CancelButton = cancel;
    }
}
