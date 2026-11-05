using System.Drawing;
using System.Windows.Forms;
using RemoteAssist;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 访客配对确认窗（R003）：白话文案；超时自动拒绝。ownerLink 不走此窗。
/// </summary>
internal sealed class PairConfirmDialog : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Label _countdownLabel;
    private readonly PermissionLevel _requested;
    private int _remainingSeconds;

    public bool TimedOut { get; private set; }
    public PermissionLevel? GrantedPermission { get; private set; }

    public PairConfirmDialog(PairRequestInfo info)
    {
        _requested = info.RequestedPermission;

        Text = "有人想协助这台电脑";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(480, 220);
        UiWindowPlacement.PlaceDialog(this);
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.White;

        var title = new Label
        {
            AutoSize = false,
            Location = new Point(20, 16),
            Size = new Size(440, 28),
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            Text = "有人想协助这台电脑"
        };

        var body = new Label
        {
            AutoSize = false,
            Location = new Point(20, 52),
            Size = new Size(440, 72),
            ForeColor = Color.FromArgb(0x3C, 0x40, 0x43),
            Text = "对方可以查看进程、日志和文件，不能直接改你的电脑。真正要改动时，还会再问你。"
        };

        _remainingSeconds = Math.Max(1, info.TimeoutSeconds);
        _countdownLabel = new Label
        {
            AutoSize = false,
            Location = new Point(20, 132),
            Size = new Size(280, 24),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = $"若不操作，将在 {_remainingSeconds} 秒后自动拒绝"
        };

        var allowBtn = new Button
        {
            Text = "允许协助",
            Location = new Point(236, 168),
            Size = new Size(110, 34),
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        allowBtn.FlatAppearance.BorderSize = 0;
        allowBtn.Click += (_, _) =>
        {
            GrantedPermission = _requested;
            DialogResult = DialogResult.Yes;
            Close();
        };

        var denyBtn = new Button
        {
            Text = "拒绝",
            Location = new Point(356, 168),
            Size = new Size(96, 34)
        };
        denyBtn.Click += (_, _) =>
        {
            GrantedPermission = null;
            DialogResult = DialogResult.No;
            Close();
        };

        AcceptButton = allowBtn;
        CancelButton = denyBtn;

        Controls.Add(title);
        Controls.Add(body);
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
                GrantedPermission = null;
                _timer.Stop();
                DialogResult = DialogResult.No;
                Close();
                return;
            }
            _countdownLabel.Text = $"若不操作，将在 {_remainingSeconds} 秒后自动拒绝";
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