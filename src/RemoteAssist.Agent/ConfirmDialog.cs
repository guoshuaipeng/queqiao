using System.Drawing;
using System.Windows.Forms;

namespace RemoteAssist.Agent;

/// <summary>
/// 简易本机确认窗口：显示来源、操作，允许/拒绝，超时自动拒绝。
/// </summary>
internal sealed class ConfirmDialog : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Label _countdownLabel;
    private int _remainingSeconds;

    public bool TimedOut { get; private set; }

    public ConfirmDialog(ConfirmRequestInfo info)
    {
        Text = "远端协助 — 确认操作";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(520, 280);
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var title = new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(488, 28),
            Font = new Font(Font, FontStyle.Bold),
            Text = "协助端请求执行写操作，请确认"
        };

        var sourceLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 48),
            Size = new Size(488, 24),
            Text = $"请求来源：{info.Source}"
        };

        var toolLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 76),
            Size = new Size(488, 24),
            Text = $"工具：{info.Tool}"
        };

        var detailCaption = new Label
        {
            AutoSize = false,
            Location = new Point(16, 108),
            Size = new Size(488, 20),
            Text = "拟执行操作："
        };

        var detailBox = new TextBox
        {
            Location = new Point(16, 132),
            Size = new Size(488, 72),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = info.Summary,
            BackColor = SystemColors.Window
        };

        _remainingSeconds = Math.Max(1, info.TimeoutSeconds);
        _countdownLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 212),
            Size = new Size(280, 24),
            ForeColor = Color.DarkRed,
            Text = $"超时将自动拒绝（{_remainingSeconds}s）"
        };

        var allowBtn = new Button
        {
            Text = "允许",
            DialogResult = DialogResult.Yes,
            Location = new Point(300, 232),
            Size = new Size(96, 32)
        };
        var denyBtn = new Button
        {
            Text = "拒绝",
            DialogResult = DialogResult.No,
            Location = new Point(408, 232),
            Size = new Size(96, 32)
        };

        AcceptButton = allowBtn;
        CancelButton = denyBtn;

        Controls.Add(title);
        Controls.Add(sourceLabel);
        Controls.Add(toolLabel);
        Controls.Add(detailCaption);
        Controls.Add(detailBox);
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
            _countdownLabel.Text = $"超时将自动拒绝（{_remainingSeconds}s）";
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