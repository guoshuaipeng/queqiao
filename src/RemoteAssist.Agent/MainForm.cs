using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QRCoder;
using RemoteAssist;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 被协助端主界面（R003）：首屏会话码+密码+复制；连不上引导；高级折叠调试信息。
/// </summary>
internal sealed class MainForm : Form
{
    private readonly AgentHost _host;
    private readonly HelperUiSettings _settings;
    private readonly Label _codeLabel;
    private readonly Label _pwdLabel;
    private readonly ComboBox _hostCombo;
    private readonly Label _portValue;
    private readonly Label _tlsValue;
    private readonly Label _listenValue;
    private readonly Label _listenRawValue;
    private readonly Label _statusLabel;
    private readonly Label _headerSubtitle;
    private readonly Label _bannerText;
    private readonly Panel _banner;
    private readonly Button _btnFirewall;
    private readonly Button _btnBannerDismiss;
    private readonly PictureBox _qrBox;
    private readonly ListView _clientsView;
    private readonly ListView _auditView;
    private readonly Panel _advancedPanel;
    private readonly Button _btnAdvanced;
    private bool _advancedOpen;
    private Image? _qrImage;
    private bool _exitApp;
    private bool _suppressHostChange;
    private readonly System.Windows.Forms.Timer _auditTimer;
    private ToolTip? _tip;
    private readonly bool _enableReturnHome;
    private readonly ContextMenuStrip _moreMenu;
    private readonly Dictionary<Control, int> _baseTops = new();

    public event Action? HomeRequested;

    public MainForm(AgentHost host, bool enableReturnHome = false)
    {
        _host = host;
        _enableReturnHome = enableReturnHome;
        _settings = HelperUiSettings.Load();

        Text = "鹊桥";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        ClientSize = new Size(760, 620);
        BackColor = Color.FromArgb(0xF7, 0xF8, 0xFA);
        Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = AppIcons.CreateAppIcon();

        UiWindowPlacement.Bind(this, restoreSize: false);

        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(760, 64),
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            Name = "header"
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
            Text = "接受协助 · 把会话码和验证密码发给对方",
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

        _banner = new Panel
        {
            Location = new Point(0, 64),
            Size = new Size(760, 72),
            BackColor = Color.FromArgb(0xFE, 0xF7, 0xE0),
            Visible = false,
            Name = "banner"
        };
        _bannerText = new Label
        {
            AutoSize = false,
            Location = new Point(16, 8),
            Size = new Size(540, 56),
            ForeColor = Color.FromArgb(0x5F, 0x3A, 0x00),
            Text = FriendConnectGuide.MomentAAllow
        };
        _btnFirewall = new Button
        {
            Text = FriendConnectGuide.OpenFirewallButtonText,
            Location = new Point(564, 10),
            Size = new Size(160, 28),
            Visible = false
        };
        _btnFirewall.Click += (_, _) => FriendConnectGuide.OpenWindowsFirewallSettings();
        _btnBannerDismiss = new Button
        {
            Text = "知道了",
            Location = new Point(564, 42),
            Size = new Size(72, 24)
        };
        _btnBannerDismiss.Click += (_, _) => DismissBanner();
        var btnMaybeDenied = new Button
        {
            Text = "我点了取消",
            Location = new Point(644, 42),
            Size = new Size(88, 24)
        };
        btnMaybeDenied.Click += (_, _) => ShowDeniedBanner();
        _banner.Controls.Add(_bannerText);
        _banner.Controls.Add(_btnFirewall);
        _banner.Controls.Add(_btnBannerDismiss);
        _banner.Controls.Add(btnMaybeDenied);

        const int y0 = 80;

        var sessionCard = MakeCard(16, y0, 360, 360);
        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(328, 22),
            Text = "把会话码和验证密码发给协助方",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 42),
            Size = new Size(328, 20),
            Text = "会话码",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        _codeLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 64),
            Size = new Size(328, 52),
            Font = new Font("Consolas", 32F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            BackColor = Color.FromArgb(0xEE, 0xF3, 0xFF),
            Text = ""
        };
        sessionCard.Controls.Add(_codeLabel);
        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 124),
            Size = new Size(70, 22),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "验证密码",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        _pwdLabel = new Label
        {
            AutoSize = false,
            Location = new Point(90, 120),
            Size = new Size(250, 30),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Consolas", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            Text = ""
        };
        sessionCard.Controls.Add(_pwdLabel);

        var btnCopyCodePwd = MakeButton("复制码+密码", 16, 160, 200);
        btnCopyCodePwd.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        btnCopyCodePwd.Height = 36;
        btnCopyCodePwd.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(FriendConnectGuide.FormatShareClipboard(_host.SessionCode, _host.SessionPassword));
                SetStatus("已复制会话码和验证密码。");
            }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        };
        sessionCard.Controls.Add(btnCopyCodePwd);

        var btnMore = MakeButton("更多", 228, 164, 100);
        _moreMenu = new ContextMenuStrip();
        _moreMenu.Items.Add("复制载荷", null, (_, _) => CopyPayload());
        _moreMenu.Items.Add("复制 MCP 配置", null, (_, _) =>
        {
            var outcome = McpConfigCopyHelper.Copy(_host.QrPayload, this);
            SetStatus(outcome.Message);
        });
        _moreMenu.Items.Add("复制会话码", null, (_, _) =>
        {
            try { Clipboard.SetText(_host.SessionCode); SetStatus("已复制会话码。"); }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        });
        btnMore.Click += (_, _) => _moreMenu.Show(btnMore, new Point(0, btnMore.Height));
        sessionCard.Controls.Add(btnMore);

        _qrBox = new PictureBox
        {
            Location = new Point(80, 212),
            Size = new Size(180, 130),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        sessionCard.Controls.Add(_qrBox);

        var statusCard = MakeCard(392, y0, 352, 200);
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
            Size = new Size(320, 44),
            Text = FriendConnectGuide.StatusReadyToShare
        };
        statusCard.Controls.Add(_listenValue);
        statusCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 88),
            Size = new Size(320, 20),
            Text = "已连接的协助方",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        _clientsView = new ListView
        {
            Location = new Point(16, 110),
            Size = new Size(320, 48),
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.None,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        _clientsView.Columns.Add("client", 300);
        statusCard.Controls.Add(_clientsView);
        var btnRefresh = MakeButton("刷新会话码", 16, 164, 100);
        btnRefresh.Click += (_, _) =>
        {
            _host.RefreshSessionCode();
            SetStatus("已刷新会话码（旧配对已全部断开）。");
            RefreshClients();
        };
        var btnDisconnect = MakeButton("断开全部", 124, 164, 90);
        btnDisconnect.Click += (_, _) =>
        {
            var n = _host.DisconnectAllSessions();
            SetStatus(n == 0 ? "当前没有已连接的协助方。" : $"已断开 {n} 个会话。");
            RefreshClients();
        };
        var btnKick = MakeButton("断开选中", 222, 164, 90);
        btnKick.Click += (_, _) => DisconnectSelected();
        statusCard.Controls.Add(btnRefresh);
        statusCard.Controls.Add(btnDisconnect);
        statusCard.Controls.Add(btnKick);

        var tipCard = MakeCard(392, y0 + 216, 352, 88);
        tipCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(320, 64),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。关闭窗口不退出，程序继续在托盘运行。"
        });

        _btnAdvanced = MakeButton("高级 ▸", 16, y0 + 376, 100);
        _btnAdvanced.Click += (_, _) => ToggleAdvanced();

        _advancedPanel = MakeCard(16, y0 + 412, 728, 150);
        _advancedPanel.Visible = false;
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 8),
            Size = new Size(200, 20),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
            Text = "高级（调试）"
        });
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 36),
            Size = new Size(60, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "本机 IP"
        });
        _hostCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(76, 36),
            Size = new Size(180, 26)
        };
        foreach (var ip in _host.LanIps)
            _hostCombo.Items.Add(ip);
        if (!_hostCombo.Items.Contains("127.0.0.1"))
            _hostCombo.Items.Add("127.0.0.1");
        var selected = _host.SelectedHost;
        var ix = _hostCombo.Items.IndexOf(selected);
        _hostCombo.SelectedIndex = ix >= 0 ? ix : 0;
        _hostCombo.SelectedIndexChanged += OnHostSelected;
        _advancedPanel.Controls.Add(_hostCombo);
        _portValue = new Label
        {
            AutoSize = false,
            Location = new Point(270, 36),
            Size = new Size(80, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Consolas", 10F, FontStyle.Bold),
            Text = $":{_host.Port}"
        };
        _advancedPanel.Controls.Add(_portValue);
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(360, 36),
            Size = new Size(70, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "TLS 指纹"
        });
        _tlsValue = new Label
        {
            AutoSize = false,
            Location = new Point(430, 36),
            Size = new Size(280, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Consolas", 8.5F),
            ForeColor = Color.FromArgb(0x3C, 0x40, 0x43),
            Text = ""
        };
        _advancedPanel.Controls.Add(_tlsValue);
        _listenRawValue = new Label
        {
            AutoSize = false,
            Location = new Point(12, 68),
            Size = new Size(700, 20),
            Font = new Font("Consolas", 8F),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = ""
        };
        _advancedPanel.Controls.Add(_listenRawValue);
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 92),
            Size = new Size(120, 20),
            Text = "最近审计"
        });
        var openAudit = new LinkLabel { AutoSize = true, Location = new Point(620, 92), Text = "打开日志文件" };
        openAudit.LinkClicked += (_, _) =>
        {
            try
            {
                var ap = _host.AuditPath;
                Directory.CreateDirectory(Path.GetDirectoryName(ap)!);
                if (!File.Exists(ap)) File.WriteAllText(ap, "");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = ap, UseShellExecute = true });
            }
            catch (Exception ex) { SetStatus("无法打开审计日志：" + ex.Message); }
        };
        _advancedPanel.Controls.Add(openAudit);
        _auditView = new ListView
        {
            Location = new Point(12, 112),
            Size = new Size(704, 30),
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.None,
            MultiSelect = false,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White
        };
        _auditView.Columns.Add("line", 680);
        _advancedPanel.Controls.Add(_auditView);

        _statusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 592),
            Size = new Size(728, 22),
            ForeColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            Font = new Font(Font.FontFamily, 8.5F),
            Text = "就绪。关闭窗口后会继续在托盘运行。"
        };

        Controls.Add(header);
        Controls.Add(_banner);
        Controls.Add(sessionCard);
        Controls.Add(statusCard);
        Controls.Add(tipCard);
        Controls.Add(_btnAdvanced);
        Controls.Add(_advancedPanel);
        Controls.Add(_statusLabel);

        foreach (Control c in Controls)
        {
            if (c.Name is "header" or "banner") continue;
            _baseTops[c] = c.Top;
        }

        FormClosing += OnFormClosing;
        _host.SessionChanged += OnSessionChanged;
        _host.PairedClientsChanged += OnPairedClientsChanged;
        _host.RelayStatusChanged += OnRelayStatusChanged;
        _host.Audit.EntryWritten += OnAuditWritten;

        _auditTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        _auditTimer.Tick += (_, _) => RefreshAudit();
        _auditTimer.Start();

        Shown += (_, _) => MaybeShowBanner();

        RefreshFromHost();
        RefreshClients();
        RefreshAudit();
        UpdateFriendStatus();
    }

    private void MaybeShowBanner()
    {
        if (_settings.NetworkAllowBannerDismissed) return;
        if (_settings.NetworkAllowMaybeDenied) ShowDeniedBanner();
        else
        {
            _bannerText.Text = FriendConnectGuide.MomentAAllow;
            _btnFirewall.Visible = false;
            _banner.Visible = true;
            RelayoutForBanner();
        }
    }

    private void ShowDeniedBanner()
    {
        _settings.NetworkAllowMaybeDenied = true;
        _settings.Save();
        _bannerText.Text = FriendConnectGuide.MomentADenied + Environment.NewLine + FriendConnectGuide.MomentAFirewallHint;
        _btnFirewall.Visible = true;
        _banner.Visible = true;
        RelayoutForBanner();
    }

    private void DismissBanner()
    {
        _settings.NetworkAllowBannerDismissed = true;
        _settings.Save();
        _banner.Visible = false;
        RelayoutForBanner();
    }

    private void RelayoutForBanner()
    {
        var shift = _banner.Visible ? _banner.Height : 0;
        foreach (var kv in _baseTops)
            kv.Key.Top = kv.Value + shift;
        ClientSize = new Size(760, 620 + shift);
    }

    private void ToggleAdvanced()
    {
        _advancedOpen = !_advancedOpen;
        _advancedPanel.Visible = _advancedOpen;
        _btnAdvanced.Text = _advancedOpen ? "高级 ▾" : "高级 ▸";
    }

    private void CopyPayload()
    {
        try { Clipboard.SetText(_host.QrPayload); SetStatus("已复制会话载荷。"); }
        catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
    }

    private static Panel MakeCard(int x, int y, int w, int h) => new()
    {
        Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle
    };

    private static Button MakeButton(string text, int x, int y, int w) => new()
    {
        Text = text, Location = new Point(x, y), Size = new Size(w, 28), FlatStyle = FlatStyle.System
    };

    public void RequestExit() { _exitApp = true; Close(); }

    public void ShowAndActivate()
    {
        UiWindowPlacement.MainWindow = this;
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void DisconnectSelected()
    {
        if (_clientsView.SelectedItems.Count == 0 || _clientsView.SelectedItems[0].Tag is not string token)
        {
            SetStatus("请先在列表中选中一个协助方。");
            return;
        }
        if (_host.DisconnectSession(token))
        {
            SetStatus("已断开选中协助方。");
            RefreshClients();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exitApp || e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
        SetStatus("已隐藏到托盘。双击托盘图标可重新打开。");
    }

    private void OnHostSelected(object? sender, EventArgs e)
    {
        if (_suppressHostChange) return;
        if (_hostCombo.SelectedItem is string ip) _host.SetSelectedHost(ip);
    }

    private void OnSessionChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(RefreshFromHost); return; }
        RefreshFromHost();
        UpdateFriendStatus();
    }

    private void OnRelayStatusChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(OnRelayStatusChanged); return; }
        UpdateFriendStatus();
    }

    private void OnPairedClientsChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(RefreshClients); return; }
        RefreshClients();
        UpdateFriendStatus();
    }

    private void OnAuditWritten()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(OnAuditWritten); return; }
        RefreshAudit();
        var latest = _host.Audit.ReadRecent(1).LastOrDefault();
        if (latest is not null && latest.EventType == "pair_owner_auto")
            SetStatus("已用你的账号连接，无需再确认配对（写操作仍须本机确认）");
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
            if (idx >= 0) _hostCombo.SelectedIndex = idx;
        }
        finally { _suppressHostChange = false; }

        _portValue.Text = $":{_host.Port}";
        _codeLabel.Text = _host.SessionCode;
        _pwdLabel.Text = _host.SessionPassword;
        var payload = _host.QrPayload;

        if (_host.UseTls && _host.CertificateFingerprint is { } fp)
        {
            var shortFp = fp.Length > 23 ? fp[..10] + "…" + fp[^8..] : fp;
            _tlsValue.Text = shortFp;
            _tlsValue.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
            Tip().SetToolTip(_tlsValue, fp);
        }
        else
        {
            _tlsValue.Text = "明文模式（仅调试）";
            _tlsValue.ForeColor = Color.DarkOrange;
        }

        UpdateFriendStatus();
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

    private void UpdateFriendStatus()
    {
        var phase = _host.RelayPhase;
        var clients = _host.ListPairedClients().Count;
        if (clients > 0 || phase == "paired")
            _listenValue.Text = "对方已连接";
        else if (phase == "waiting")
            _listenValue.Text = FriendConnectGuide.StatusWaitingPeer;
        else if (phase is "ready")
            _listenValue.Text = FriendConnectGuide.StatusReadyToShare;
        else if (phase == "error")
        {
            var friendly = FriendConnectGuide.Classify(_host.LastRelayError);
            _listenValue.Text = friendly is FriendConnectFailKind.None or FriendConnectFailKind.Other
                ? FriendConnectGuide.FailRelayUnreachable
                : FriendConnectGuide.MessageFor(friendly);
        }
        else if (phase is "dialing" or "starting")
            _listenValue.Text = "正在连网…";
        else
            _listenValue.Text = FriendConnectGuide.StatusReadyToShare;

        _listenRawValue.Text = _host.PreferRelay
            ? $"phase={phase}; code={_host.SessionCode}"
            : $"lan port={_host.Port} host={_host.SelectedHost}";

        if (FriendConnectGuide.LooksTechnicalLeak(_listenValue.Text))
            _listenValue.Text = FriendConnectGuide.StatusReadyToShare;
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
                var ownerTag = c.OwnerLink ? "账号免密 · " : "";
                _clientsView.Items.Add(new ListViewItem($"{ownerTag}{c.ClientName} · {shortTok}…") { Tag = c.Token });
            }
            if (_clientsView.Items.Count == 0)
                _clientsView.Items.Add(new ListViewItem("（暂无）") { ForeColor = Color.Gray });
        }
        finally { _clientsView.EndUpdate(); }
        UpdateFriendStatus();
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
                {
                    var line = FriendConnectGuide.LooksTechnicalLeak(e.DisplaySummary) ? e.EventType : e.DisplaySummary;
                    _auditView.Items.Add(new ListViewItem(line));
                }
                if (_auditView.Items.Count == 0)
                    _auditView.Items.Add(new ListViewItem("（暂无审计记录）") { ForeColor = Color.Gray });
            }
            finally { _auditView.EndUpdate(); }
        }
        catch { }
    }

    private void SetStatus(string text) => _statusLabel.Text = text;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _host.SessionChanged -= OnSessionChanged;
            _host.PairedClientsChanged -= OnPairedClientsChanged;
            _host.RelayStatusChanged -= OnRelayStatusChanged;
            _host.Audit.EntryWritten -= OnAuditWritten;
            _auditTimer.Stop();
            _auditTimer.Dispose();
            _qrImage?.Dispose();
            _tip?.Dispose();
            _moreMenu.Dispose();
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
        if (!AttachConsole(AttachParentProcess)) AllocConsole();
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; } catch { }
    }

    public static void TryAttachParent()
    {
        AttachConsole(AttachParentProcess);
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; } catch { }
    }
}
