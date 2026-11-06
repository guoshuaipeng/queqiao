using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QRCoder;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// Agent 主界面（产品化）：会话码 / 二维码 / TLS 指纹 / 配对状态 / 审计列表。
/// 关闭窗口默认隐藏到托盘，不退出进程。
/// </summary>
internal sealed class MainForm : Form
{
    private readonly AgentHost _host;
    private readonly Label _codeLabel;
    private readonly Label _credentialLabel;
    private readonly Label _relayStatusLabel;
    private readonly ComboBox _hostCombo;
    private readonly Label _portValue;
    private readonly Label _tlsValue;
    private readonly Label _listenValue;
    private readonly Label _statusLabel;
    private readonly Label _headerSubtitle;
    private readonly PictureBox _qrBox;
    private readonly ListView _clientsView;
    private readonly ListView _auditView;
    private Image? _qrImage;
    private bool _exitApp;
    private bool _suppressHostChange;
    private readonly System.Windows.Forms.Timer _auditTimer;
    private ToolTip? _tip;
    private readonly bool _enableReturnHome;

    /// <summary>统一 App 中请求返回模式选择页。</summary>
    public event Action? HomeRequested;

    public MainForm(AgentHost host, bool enableReturnHome = false)
    {
        _host = host;
        _enableReturnHome = enableReturnHome;

        Text = "鹊桥";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        ClientSize = new Size(760, 640);
        BackColor = Color.FromArgb(0xF7, 0xF8, 0xFA);
        Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = AppIcons.CreateAppIcon();

        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(760, 64),
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8)
        };
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 10),
            Size = new Size(500, 28),
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 14F, FontStyle.Bold),
            Text = "鹊桥",
            BackColor = Color.Transparent
        });
        _headerSubtitle = new Label
        {
            AutoSize = false,
            Location = new Point(20, 36),
            Size = new Size(_enableReturnHome ? 600 : 700, 22),
            ForeColor = Color.FromArgb(0xE3, 0xF0, 0xFF),
            Font = new Font(Font.FontFamily, 9F),
            Text = "接受协助 · 点「复制连接凭证」发完整安全载荷 · 危险操作需本机确认",
            BackColor = Color.Transparent
        };
        header.Controls.Add(_headerSubtitle);
        if (_enableReturnHome)
        {
            var btnHome = new Button
            {
                Text = "返回首页",
                Size = new Size(100, 32),
                Location = new Point(header.ClientSize.Width - 100 - 16, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0x15, 0x5F, 0xC0),
                Font = new Font(Font.FontFamily, 9F, FontStyle.Bold)
            };
            btnHome.FlatAppearance.BorderColor = Color.White;
            btnHome.Click += (_, _) => HomeRequested?.Invoke();
            header.Controls.Add(btnHome);
            btnHome.BringToFront();
        }


        var sessionCard = MakeCard(16, 80, 360, 420);
        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(328, 22),
            Text = "点「复制连接凭证」把完整安全载荷发给对方（含业务指纹）；上方短码仅供口述",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 42),
            Size = new Size(60, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "本机 IP"
        });

        _hostCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(76, 42),
            Size = new Size(200, 26)
        };
        foreach (var ip in _host.LanIps)
            _hostCombo.Items.Add(ip);
        if (!_hostCombo.Items.Contains("127.0.0.1"))
            _hostCombo.Items.Add("127.0.0.1");
        var selected = _host.SelectedHost;
        var idx = _hostCombo.Items.IndexOf(selected);
        _hostCombo.SelectedIndex = idx >= 0 ? idx : 0;
        _hostCombo.SelectedIndexChanged += OnHostSelected;
        sessionCard.Controls.Add(_hostCombo);

        _portValue = new Label
        {
            AutoSize = false,
            Location = new Point(284, 42),
            Size = new Size(60, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Consolas", 10F, FontStyle.Bold),
            Text = $":{_host.Port}"
        };
        sessionCard.Controls.Add(_portValue);

        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 78),
            Size = new Size(328, 20),
            Text = "口述短码（不可单独加密连接）",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        _credentialLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 100),
            Size = new Size(328, 40),
            Font = new Font("Consolas", 16F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            BackColor = Color.FromArgb(0xEE, 0xF3, 0xFF),
            Text = ""
        };
        sessionCard.Controls.Add(_credentialLabel);

        _codeLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 142),
            Size = new Size(160, 22),
            Font = new Font("Consolas", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = ""
        };
        sessionCard.Controls.Add(_codeLabel);

        _relayStatusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(180, 142),
            Size = new Size(164, 22),
            Font = new Font(Font.FontFamily, 8.5F),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = ""
        };
        sessionCard.Controls.Add(_relayStatusLabel);

        var btnCopyCredential = MakeButton("复制连接凭证", 16, 168, 160);
        btnCopyCredential.BackColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopyCredential.ForeColor = Color.White;
        btnCopyCredential.FlatStyle = FlatStyle.Flat;
        btnCopyCredential.Click += (_, _) => CopySecureCredential();
        sessionCard.Controls.Add(btnCopyCredential);

        _qrBox = new PictureBox
        {
            Location = new Point(70, 200),
            Size = new Size(200, 180),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        sessionCard.Controls.Add(_qrBox);

        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 392),
            Size = new Size(70, 22),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "TLS 指纹"
        });
        _tlsValue = new Label
        {
            AutoSize = false,
            Location = new Point(86, 392),
            Size = new Size(258, 22),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Consolas", 8.5F),
            ForeColor = Color.FromArgb(0x3C, 0x40, 0x43),
            Text = ""
        };
        sessionCard.Controls.Add(_tlsValue);

        var statusCard = MakeCard(392, 80, 352, 200);
        statusCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(320, 22),
            Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold),
            Text = "运行状态"
        });
        _listenValue = new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(320, 22),
            Text = ""
        };
        statusCard.Controls.Add(_listenValue);
        statusCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 68),
            Size = new Size(320, 20),
            Text = "已配对客户端",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        _clientsView = new ListView
        {
            Location = new Point(16, 90),
            Size = new Size(320, 72),
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.None,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        _clientsView.Columns.Add("client", 300);
        statusCard.Controls.Add(_clientsView);

        var btnRefresh = MakeButton("刷新会话码", 16, 168, 100);
        btnRefresh.Click += (_, _) =>
        {
            _host.RefreshSessionCode();
            SetStatus("已刷新会话码（旧配对已全部断开）。");
            RefreshClients();
        };
        var btnDisconnect = MakeButton("断开全部", 124, 168, 90);
        btnDisconnect.Click += (_, _) =>
        {
            var n = _host.DisconnectAllSessions();
            SetStatus(n == 0 ? "当前没有已配对客户端。" : $"已断开 {n} 个会话。");
            RefreshClients();
        };
        var btnKick = MakeButton("断开选中", 222, 168, 90);
        btnKick.Click += (_, _) => DisconnectSelected();
        statusCard.Controls.Add(btnRefresh);
        statusCard.Controls.Add(btnDisconnect);
        statusCard.Controls.Add(btnKick);

        var actionCard = MakeCard(392, 296, 352, 100);
        actionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(320, 22),
            Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold),
            Text = "分享给协助方"
        });
        var btnCopyPayload = MakeButton("复制载荷", 16, 48, 100);
        btnCopyPayload.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(_host.QrPayload);
                SetStatus("已复制会话载荷（含 IP / 端口 / 会话码 / TLS 指纹）。");
            }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        };
        var btnCopyMcp = MakeButton("复制 MCP 配置", 124, 48, 120);
        btnCopyMcp.Click += (_, _) =>
        {
            var outcome = McpConfigCopyHelper.Copy(_host.QrPayload, this);
            SetStatus(outcome.Message);
        };
        var btnCopyCode = MakeButton("复制连接凭证", 252, 48, 100);
        btnCopyCode.Click += (_, _) => CopySecureCredential();
        actionCard.Controls.Add(btnCopyPayload);
        actionCard.Controls.Add(btnCopyMcp);
        actionCard.Controls.Add(btnCopyCode);

        // 右侧说明
        var tipCard = MakeCard(392, 412, 352, 88);
        tipCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(320, 64),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "亲友使用：安装后打开本程序 → 把会话码或二维码发给协助方。\n协助方连上后，本机弹出确认窗，你点允许才执行。\n关闭本窗口不会退出，程序继续在托盘运行。"
        });

        var auditFull = MakeCard(16, 516, 728, 90);
        auditFull.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 8),
            Size = new Size(200, 20),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
            Text = "最近审计"
        });
        var openAudit = new LinkLabel
        {
            AutoSize = true,
            Location = new Point(620, 8),
            Text = "打开日志文件"
        };
        openAudit.LinkClicked += (_, _) =>
        {
            try
            {
                var path = _host.AuditPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (!File.Exists(path))
                    File.WriteAllText(path, "");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { SetStatus("无法打开审计日志：" + ex.Message); }
        };
        auditFull.Controls.Add(openAudit);

        _auditView = new ListView
        {
            Location = new Point(12, 30),
            Size = new Size(704, 50),
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.None,
            MultiSelect = false,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White
        };
        _auditView.Columns.Add("line", 680);
        auditFull.Controls.Add(_auditView);

        _statusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 612),
            Size = new Size(728, 22),
            ForeColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            Font = new Font(Font.FontFamily, 8.5F),
            Text = "就绪。关闭窗口后会继续在托盘运行。"
        };

        Controls.Add(header);
        Controls.Add(sessionCard);
        Controls.Add(statusCard);
        Controls.Add(actionCard);
        Controls.Add(tipCard);
        Controls.Add(auditFull);
        Controls.Add(_statusLabel);

        FormClosing += OnFormClosing;
        _host.RelayStatusChanged += OnSessionChanged;
        _host.SessionChanged += OnSessionChanged;
        _host.PairedClientsChanged += OnPairedClientsChanged;
        _host.Audit.EntryWritten += OnAuditWritten;

        _auditTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        _auditTimer.Tick += (_, _) => RefreshAudit();
        _auditTimer.Start();

        RefreshFromHost();
        RefreshClients();
        RefreshAudit();
    }

    private static Panel MakeCard(int x, int y, int w, int h) => new()
    {
        Location = new Point(x, y),
        Size = new Size(w, h),
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    private static Button MakeButton(string text, int x, int y, int w) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(w, 28),
        FlatStyle = FlatStyle.System
    };

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

    private void DisconnectSelected()
    {
        if (_clientsView.SelectedItems.Count == 0 || _clientsView.SelectedItems[0].Tag is not string token)
        {
            SetStatus("请先在列表中选中一个客户端。");
            return;
        }
        if (_host.DisconnectSession(token))
        {
            SetStatus("已断开选中客户端。");
            RefreshClients();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exitApp || e.CloseReason != CloseReason.UserClosing)
            return;
        e.Cancel = true;
        Hide();
        SetStatus("已隐藏到托盘。双击托盘图标可重新打开。");
    }

    private void OnHostSelected(object? sender, EventArgs e)
    {
        if (_suppressHostChange) return;
        if (_hostCombo.SelectedItem is string ip)
            _host.SetSelectedHost(ip);
    }

    private void OnSessionChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(RefreshFromHost); return; }
        RefreshFromHost();
    }

    private void OnPairedClientsChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(RefreshClients); return; }
        RefreshClients();
    }

    private void OnAuditWritten()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(RefreshAudit); return; }
        RefreshAudit();
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
                _hostCombo.Items.Add(want);
                idx = _hostCombo.Items.Count - 1;
            }
            if (idx >= 0)
                _hostCombo.SelectedIndex = idx;
        }
        finally { _suppressHostChange = false; }

        _portValue.Text = $":{_host.Port}";
        _codeLabel.Text = "码 " + _host.SessionCode;
        if (_credentialLabel is not null)
            _credentialLabel.Text = FriendConnectGuide.FormatConnectionCredential(_host.SessionCode, _host.SessionPassword);
        if (_relayStatusLabel is not null && _host.PreferRelay)
        {
            var phase = _host.RelayPhase;
            _relayStatusLabel.Text = phase switch
            {
                "dialing" => "正在连接中继…",
                "ready" or "waiting" => "等待对方连接",
                "paired" => "对方已连上",
                "error" => "中继异常",
                _ => "中继准备中"
            };
        }
        if (_host.PreferRelay)
            _listenValue.Text = "中继 " + _host.RelayEndpointAddress + " · " + _host.RelayPhase;

        var payload = _host.QrPayload;

        if (_host.PreferRelay && _host.UseTls && _host.CertificateFingerprint is { } fpRelay)
        {
            var shortFp = fpRelay.Length > 23 ? fpRelay[..10] + "…" + fpRelay[^8..] : fpRelay;
            _tlsValue.Text = "中继 TLS · " + shortFp;
            _tlsValue.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            Tip().SetToolTip(_tlsValue, "业务 TLS 指纹（完整载荷含 fp=）：" + fpRelay + " · 中继 " + _host.RelayEndpointAddress);
        }
        else if (_host.PreferRelay && !_host.UseTls)
        {
            _tlsValue.Text = "中继明文（--insecure 调试）";
            _tlsValue.ForeColor = Color.DarkOrange;
            Tip().SetToolTip(_tlsValue, _host.RelayEndpointAddress);
        }
        else if (_host.UseTls && _host.CertificateFingerprint is { } fp)
        {
            var shortFp = fp.Length > 23 ? fp[..10] + "…" + fp[^8..] : fp;
            _tlsValue.Text = shortFp;
            _tlsValue.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
            Tip().SetToolTip(_tlsValue, fp);
        }
        else
        {
            _tlsValue.Text = "明文模式（--insecure）";
            _tlsValue.ForeColor = Color.DarkOrange;
        }

        UpdateListenLabel();

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var bmp = new QRCode(data).GetGraphic(5);
            var old = _qrImage;
            _qrImage = bmp;
            _qrBox.Image = _qrImage;
            old?.Dispose();
        }
        catch (Exception ex)
        {
            _qrBox.Image = null;
            SetStatus("二维码生成失败：" + ex.Message);
        }
    }

    private ToolTip Tip() => _tip ??= new ToolTip { ShowAlways = true };


    private void CopySecureCredential()
    {
        try
        {
            if (_host.PreferRelay)
            {
                if (!_host.UseTls || string.IsNullOrWhiteSpace(_host.CertificateFingerprint))
                {
                    SetStatus("当前无法生成加密载荷（明文调试或证书未就绪）。");
                    return;
                }
                if (!TlsCertUtil.IsWellFormedFingerprint(_host.CertificateFingerprint))
                {
                    SetStatus("证书指纹异常，请刷新会话后重试。");
                    return;
                }
                // 与 QrPayload / PairQrPayload.BuildRelay 同一形态（含 fp、pwd、relay）
                Clipboard.SetText(_host.QrPayload);
                SetStatus("已复制完整安全连接载荷（请发给协助方一次粘贴）。短码不能单独加密连接。");
                return;
            }

            Clipboard.SetText(_host.QrPayload);
            SetStatus("已复制完整安全连接载荷。");
        }
        catch (Exception ex)
        {
            SetStatus("复制失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
        }
    }

    private void UpdateListenLabel()
    {
        var n = _host.ListPairedClients().Count;
        if (_host.PreferRelay)
        {
            var phase = _host.RelayPhase switch
            {
                "dialing" => "正在连接中继",
                "ready" or "waiting" => "等待对方连接",
                "paired" => "对方已连上",
                "error" => "中继异常（将自动重试）",
                _ => "中继准备中"
            };
            _listenValue.Text = phase + " · 已配对 " + n + " 个";
        }
        else
        {
            var tls = _host.UseTls ? "TLS 开" : "明文";
            _listenValue.Text = $"监听 {_host.Port} · {tls} · 已配对 {n} 个";
        }
        _headerSubtitle.Text = n == 0
            ? "接受协助 · 点「复制连接凭证」发完整安全载荷 · 危险操作需本机确认"
            : $"被协助端 · 已有 {n} 个协助方连接 · 可随时断开";
    }

    private void RefreshClients()
    {
        _clientsView.BeginUpdate();
        try
        {
            _clientsView.Items.Clear();
            foreach (var c in _host.ListPairedClients())
            {
                var shortTok = c.Token.Length >= 8 ? c.Token[..8] : c.Token;
                _clientsView.Items.Add(new ListViewItem($"{c.ClientName} · {c.Permission} · {c.RemoteEndpoint} · {shortTok}…")
                {
                    Tag = c.Token
                });
            }
            if (_clientsView.Items.Count == 0)
                _clientsView.Items.Add(new ListViewItem("（暂无）") { ForeColor = Color.Gray });
        }
        finally { _clientsView.EndUpdate(); }
        UpdateListenLabel();
    }

    private void RefreshAudit()
    {
        try
        {
            var entries = _host.Audit.ReadRecent(8);
            _auditView.BeginUpdate();
            try
            {
                _auditView.Items.Clear();
                foreach (var e in entries.Reverse())
                    _auditView.Items.Add(new ListViewItem(e.DisplaySummary));
                if (_auditView.Items.Count == 0)
                    _auditView.Items.Add(new ListViewItem("（暂无审计记录）") { ForeColor = Color.Gray });
            }
            finally { _auditView.EndUpdate(); }
        }
        catch { /* ignore */ }
    }

    private void SetStatus(string text) => _statusLabel.Text = text;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _host.RelayStatusChanged -= OnSessionChanged;
            _host.SessionChanged -= OnSessionChanged;
            _host.PairedClientsChanged -= OnPairedClientsChanged;
            _host.Audit.EntryWritten -= OnAuditWritten;
            _auditTimer.Stop();
            _auditTimer.Dispose();
            _qrImage?.Dispose();
            _tip?.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal static class AppIcons
{
    public static Icon CreateAppIcon()
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

    public static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(0x1A, 0x73, 0xE8));
            g.FillEllipse(brush, 1, 1, 14, 14);
            using var pen = new Pen(Color.White, 2);
            g.DrawEllipse(pen, 4, 4, 8, 8);
        }
        using var tmp = Icon.FromHandle(bmp.GetHicon());
        return (Icon)tmp.Clone();
    }
}

internal static class NativeConsole
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    private const int AttachParentProcess = -1;

    public static void EnsureForCli()
    {
        if (!AttachConsole(AttachParentProcess))
            AllocConsole();
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch { /* ignore */ }
    }

    /// <summary>仅挂接父终端（安装后双击启动时失败则保持无控制台黑窗）。</summary>
    public static void TryAttachParent()
    {
        AttachConsole(AttachParentProcess);
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch { /* ignore */ }
    }
}
