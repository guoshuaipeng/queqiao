using System.Drawing;
using System.Windows.Forms;
using QRCoder;

namespace RemoteAssist.Agent;

/// <summary>
/// 会话码 + 二维码展示。关闭窗口默认隐藏到托盘，不退出进程。
/// </summary>
internal sealed class SessionInfoForm : Form
{
    private readonly AgentHost _host;
    private readonly Label _codeLabel;
    private readonly Label _hostLabel;
    private readonly Label _payloadLabel;
    private readonly PictureBox _qrBox;
    private Image? _qrImage;
    private bool _exitApp;

    public SessionInfoForm(AgentHost host)
    {
        _host = host;

        Text = "远端协助 — 会话信息";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        ClientSize = new Size(420, 520);
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var title = new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(388, 28),
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            Text = "把会话码告诉协助方，或扫二维码"
        };

        _hostLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 48),
            Size = new Size(388, 48),
            Text = ""
        };

        var codeCaption = new Label
        {
            AutoSize = false,
            Location = new Point(16, 100),
            Size = new Size(388, 22),
            Text = "会话码"
        };

        _codeLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 124),
            Size = new Size(388, 48),
            Font = new Font("Consolas", 28F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = ""
        };

        _qrBox = new PictureBox
        {
            Location = new Point(85, 180),
            Size = new Size(250, 250),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };

        _payloadLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 440),
            Size = new Size(388, 40),
            ForeColor = Color.DimGray,
            Font = new Font(Font.FontFamily, 8F),
            Text = ""
        };

        var refreshBtn = new Button
        {
            Text = "刷新会话码",
            Location = new Point(16, 484),
            Size = new Size(120, 28)
        };
        refreshBtn.Click += (_, _) => _host.RefreshSessionCode();

        var hideBtn = new Button
        {
            Text = "隐藏到托盘",
            Location = new Point(284, 484),
            Size = new Size(120, 28)
        };
        hideBtn.Click += (_, _) => HideToTray();

        Controls.Add(title);
        Controls.Add(_hostLabel);
        Controls.Add(codeCaption);
        Controls.Add(_codeLabel);
        Controls.Add(_qrBox);
        Controls.Add(_payloadLabel);
        Controls.Add(refreshBtn);
        Controls.Add(hideBtn);

        RefreshFromHost();
        _host.SessionChanged += OnSessionChanged;

        FormClosing += (_, e) =>
        {
            if (_exitApp)
                return;
            e.Cancel = true;
            HideToTray();
        };

        FormClosed += (_, _) =>
        {
            _host.SessionChanged -= OnSessionChanged;
            _qrImage?.Dispose();
            _qrImage = null;
        };
    }

    public void RequestExit()
    {
        _exitApp = true;
        Close();
    }

    public void ShowAndActivate()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void HideToTray()
    {
        Hide();
    }

    private void OnSessionChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(RefreshFromHost);
            return;
        }
        RefreshFromHost();
    }

    private void RefreshFromHost()
    {
        var ips = _host.LanIps.Count > 0 ? string.Join(", ", _host.LanIps) : "127.0.0.1";
        _hostLabel.Text = $"主机：{ips}{Environment.NewLine}端口：{_host.Port}";
        _codeLabel.Text = _host.SessionCode;
        var payload = _host.QrPayload;
        _payloadLabel.Text = payload;

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qr = new QRCode(data);
            var bmp = qr.GetGraphic(6);
            var old = _qrImage;
            _qrImage = bmp;
            _qrBox.Image = _qrImage;
            old?.Dispose();
        }
        catch (Exception ex)
        {
            _qrBox.Image = null;
            _payloadLabel.Text = $"二维码生成失败：{ex.Message}{Environment.NewLine}{payload}";
        }
    }
}
