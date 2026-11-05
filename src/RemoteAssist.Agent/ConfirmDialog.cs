using System.Drawing;
using System.Windows.Forms;
using RemoteAssist;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// R004 写操作确认窗：人话主文 + 次要命令/路径细节；允许/拒绝；超时自动拒绝（不自动允许）。
/// </summary>
internal sealed class ConfirmDialog : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Label _countdownLabel;
    private int _remainingSeconds;

    public bool TimedOut { get; private set; }

    public ConfirmDialog(ConfirmRequestInfo info)
    {
        Text = WriteConfirmCopy.DialogTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(520, 280);
        UiWindowPlacement.PlaceDialog(this);
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.White;

        var title = new Label
        {
            AutoSize = false,
            Location = new Point(20, 16),
            Size = new Size(480, 28),
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            Text = WriteConfirmCopy.DialogTitle
        };

        var headline = new Label
        {
            AutoSize = false,
            Location = new Point(20, 56),
            Size = new Size(480, 48),
            Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            Text = info.Headline
        };

        var detail = new Label
        {
            AutoSize = false,
            Location = new Point(20, 108),
            Size = new Size(480, 56),
            Font = new Font("Consolas", 9F),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = string.IsNullOrWhiteSpace(info.Detail) ? "" : info.Detail
        };

        _remainingSeconds = Math.Max(1, info.TimeoutSeconds);
        _countdownLabel = new Label
        {
            AutoSize = false,
            Location = new Point(20, 176),
            Size = new Size(320, 24),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = WriteConfirmCopy.CountdownText(_remainingSeconds)
        };

        var allowBtn = new Button
        {
            Text = WriteConfirmCopy.AllowButton,
            DialogResult = DialogResult.Yes,
            Location = new Point(288, 220),
            Size = new Size(100, 34),
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        allowBtn.FlatAppearance.BorderSize = 0;

        var denyBtn = new Button
        {
            Text = WriteConfirmCopy.DenyButton,
            DialogResult = DialogResult.No,
            Location = new Point(400, 220),
            Size = new Size(96, 34)
        };

        AcceptButton = allowBtn;
        CancelButton = denyBtn;

        Controls.Add(title);
        Controls.Add(headline);
        Controls.Add(detail);
        Controls.Add(_countdownLabel);
        Controls.Add(allowBtn);
        Controls.Add(denyBtn);

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) =>
        {
            _remainingSeconds--;
            if (_remainingSeconds <= 0)
            {
                TimedOut = true;
                _timer.Stop();
                DialogResult = DialogResult.No;
                Close();
                return;
            }
            _countdownLabel.Text = WriteConfirmCopy.CountdownText(_remainingSeconds);
        };

        Shown += (_, _) =>
        {
            Activate();
            BringToFront();
            _timer.Start();
        };

        FormClosed += (_, _) => _timer.Stop();
    }
}
