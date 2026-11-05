using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 配对确认窗口：显示来源 IP、客户端、请求权限；允许 / 仅访客 / 拒绝；超时自动拒绝。
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

        Text = "远端协助 — 确认配对";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(520, 260);
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var title = new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(488, 28),
            Font = new Font(Font, FontStyle.Bold),
            Text = "有人请求与本机建立协助会话"
        };

        var remoteLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 48),
            Size = new Size(488, 24),
            Text = $"来源 IP：{info.RemoteEndpoint}"
        };

        var clientLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 76),
            Size = new Size(488, 24),
            Text = $"客户端：{info.ClientName}"
        };

        var permLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 104),
            Size = new Size(488, 24),
            Text = $"请求权限：{info.RequestedPermission}"
        };

        var hint = new Label
        {
            AutoSize = false,
            Location = new Point(16, 136),
            Size = new Size(488, 40),
            ForeColor = Color.DimGray,
            Text = "「允许」按请求权限授权；「仅访客」只读；「拒绝」断开。配对与写操作确认会排队，一次只弹一个窗。"
        };

        _remainingSeconds = Math.Max(1, info.TimeoutSeconds);
        _countdownLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 184),
            Size = new Size(200, 24),
            ForeColor = Color.DarkRed,
            Text = $"超时将自动拒绝（{_remainingSeconds}s）"
        };

        var allowBtn = new Button
        {
            Text = "允许",
            Location = new Point(200, 212),
            Size = new Size(96, 32)
        };
        allowBtn.Click += (_, _) =>
        {
            GrantedPermission = _requested;
            DialogResult = DialogResult.Yes;
            Close();
        };

        var guestBtn = new Button
        {
            Text = "仅访客",
            Location = new Point(304, 212),
            Size = new Size(96, 32)
        };
        guestBtn.Click += (_, _) =>
        {
            GrantedPermission = PermissionLevel.Guest;
            DialogResult = DialogResult.OK;
            Close();
        };

        var denyBtn = new Button
        {
            Text = "拒绝",
            Location = new Point(408, 212),
            Size = new Size(96, 32)
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
        Controls.Add(remoteLabel);
        Controls.Add(clientLabel);
        Controls.Add(permLabel);
        Controls.Add(hint);
        Controls.Add(_countdownLabel);
        Controls.Add(allowBtn);
        Controls.Add(guestBtn);
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
