using System.Drawing;
using System.Windows.Forms;
using QRCoder;

namespace RemoteAssist.Agent;

/// <summary>
/// 会话码 + 二维码展示。可切换写入 QR 的本机 IP。关闭窗口默认隐藏到托盘。
/// </summary>
internal sealed class SessionInfoForm : Form
{
    private readonly AgentHost _host;
    private readonly Label _codeLabel;
    private readonly ComboBox _hostCombo;
    private readonly Label _portLabel;
    private readonly Label _payloadLabel;
    private readonly PictureBox _qrBox;
    private Image? _qrImage;
    private bool _exitApp;
    private bool _suppressHostChange;

    public SessionInfoForm(AgentHost host)
    {
        _host = host;

        Text = "远端协助 — 会话信息";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        ClientSize = new Size(420, 560);
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var title = new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(388, 28),
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            Text = "把会话码告诉协助方，或扫二维码"
        };

        var hostCaption = new Label
        {
            AutoSize = false,
            Location = new Point(16, 48),
            Size = new Size(80, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "主机 IP"
        };

        _hostCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(96, 48),
            Size = new Size(220, 28)
        };
        foreach (var ip in _host.LanIps)
            _hostCombo.Items.Add(ip);
        if (!_hostCombo.Items.Contains("127.0.0.1"))
            _hostCombo.Items.Add("127.0.0.1");
        var selected = _host.SelectedHost;
        var idx = _hostCombo.Items.IndexOf(selected);
        _hostCombo.SelectedIndex = idx >= 0 ? idx : 0;
        _hostCombo.SelectedIndexChanged += OnHostSelected;

        _portLabel = new Label
        {
            AutoSize = false,
            Location = new Point(324, 48),
            Size = new Size(80, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = $":{_host.Port}"
        };

        var codeCaption = new Label
        {
            AutoSize = false,
            Location = new Point(16, 88),
            Size = new Size(388, 22),
            Text = "会话码"
        };

        _codeLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 112),
            Size = new Size(388, 48),
            Font = new Font("Consolas", 28F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = ""
        };

        _qrBox = new PictureBox
        {
            Location = new Point(85, 170),
            Size = new Size(250, 250),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };

        _payloadLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 430),
            Size = new Size(388, 56),
            ForeColor = Color.DimGray,
            Font = new Font(Font.FontFamily, 8F),
            Text = ""
        };

        var copyBtn = new Button
        {
            Text = "复制载荷",
            Location = new Point(148, 494),
            Size = new Size(100, 28)
        };
        copyBtn.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(_host.QrPayload);
            }
            catch
            {
                /* ignore */
            }
        };

        var refreshBtn = new Button
        {
            Text = "刷新会话码",
            Location = new Point(16, 494),
            Size = new Size(120, 28)
        };
        refreshBtn.Click += (_, _) => _host.RefreshSessionCode();

        var hideBtn = new Button
        {
            Text = "隐藏到托盘",
            Location = new Point(284, 494),
            Size = new Size(120, 28)
        };
        hideBtn.Click += (_, _) => HideToTray();

        Controls.Add(title);
        Controls.Add(hostCaption);
        Controls.Add(_hostCombo);
        Controls.Add(_portLabel);
        Controls.Add(codeCaption);
        Controls.Add(_codeLabel);
        Controls.Add(_qrBox);
        Controls.Add(_payloadLabel);
        Controls.Add(refreshBtn);
        Controls.Add(copyBtn);
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

    private void HideToTray() => Hide();

    private void OnHostSelected(object? sender, EventArgs e)
    {
        if (_suppressHostChange) return;
        if (_hostCombo.SelectedItem is string ip)
            _host.SetSelectedHost(ip);
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
        _suppressHostChange = true;
        try
        {
            var want = _host.SelectedHost;
            var idx = _hostCombo.Items.IndexOf(want);
            if (idx < 0 && _hostCombo.Items.Count > 0)
            {
                // 若当前选项不在列表（少见），追加
                _hostCombo.Items.Add(want);
                idx = _hostCombo.Items.Count - 1;
            }
            if (idx >= 0)
                _hostCombo.SelectedIndex = idx;
        }
        finally
        {
            _suppressHostChange = false;
        }

        _portLabel.Text = $":{_host.Port}";
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

