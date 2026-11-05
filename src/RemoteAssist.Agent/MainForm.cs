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
    private Panel _trustTip = null!;
    private Label _trustTipText = null!;
    private LinkLabel _trustTipRevert = null!;
    private RadioButton _radioAsk = null!;
    private RadioButton _radioRelaxed = null!;
    private RadioButton _radioFullTrust = null!;
    private ListView _confirmedView = null!;
    private bool _suppressTrustRadio;
    private int _baseClientHeight = 620;

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
            Size = new Size(760, 88),
            BackColor = Color.FromArgb(0xFE, 0xF7, 0xE0),
            Visible = false,
            Name = "banner"
        };
        _bannerText = new Label
        {
            AutoSize = false,
            Location = new Point(16, 6),
            Size = new Size(728, 44),
            ForeColor = Color.FromArgb(0x5F, 0x3A, 0x00),
            Text = FriendConnectGuide.MomentAAllow.Replace("\n", Environment.NewLine)
        };
        _btnFirewall = new Button
        {
            Text = FriendConnectGuide.OpenFirewallButtonText,
            Location = new Point(16, 52),
            Size = new Size(180, 28),
            AutoSize = false,
            Visible = false
        };
        _btnFirewall.Click += (_, _) => FriendConnectGuide.OpenWindowsFirewallSettings();
        _btnBannerDismiss = new Button
        {
            Text = "知道了",
            Location = new Point(560, 52),
            Size = new Size(72, 28)
        };
        _btnBannerDismiss.Click += (_, _) => DismissBanner();
        var btnMaybeDenied = new Button
        {
            Text = "我点了取消",
            Location = new Point(644, 52),
            Size = new Size(100, 28)
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

        var btnCopyCodePwd = MakeButton("复制码+密码", 16, 160, 220);
        btnCopyCodePwd.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
        btnCopyCodePwd.Height = 40;
        btnCopyCodePwd.FlatStyle = FlatStyle.Flat;
        btnCopyCodePwd.BackColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopyCodePwd.ForeColor = Color.White;
        btnCopyCodePwd.FlatAppearance.BorderSize = 0;
        btnCopyCodePwd.Cursor = Cursors.Hand;
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

        var btnMore = MakeButton("更多", 248, 168, 80);
        btnMore.Height = 28;
        btnMore.FlatStyle = FlatStyle.Flat;
        btnMore.BackColor = Color.White;
        btnMore.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
        btnMore.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnMore.FlatAppearance.BorderSize = 1;
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
        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.BackColor = Color.White;
        btnRefresh.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
        btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnRefresh.FlatAppearance.BorderSize = 1;
        btnRefresh.Click += (_, _) => ConfirmAndRefreshSession();
        var btnDisconnect = MakeButton("断开全部", 124, 164, 90);
        btnDisconnect.FlatStyle = FlatStyle.Flat;
        btnDisconnect.BackColor = Color.White;
        btnDisconnect.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
        btnDisconnect.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnDisconnect.FlatAppearance.BorderSize = 1;
        btnDisconnect.Click += (_, _) =>
        {
            var n = _host.DisconnectAllSessions();
            SetStatus(n == 0 ? "当前没有已连接的协助方。" : $"已断开 {n} 个会话。");
            RefreshClients();
        };
        var btnKick = MakeButton("断开选中", 222, 164, 90);
        btnKick.FlatStyle = FlatStyle.Flat;
        btnKick.BackColor = Color.White;
        btnKick.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
        btnKick.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnKick.FlatAppearance.BorderSize = 1;
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
            Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。"
        });

        _btnAdvanced = MakeButton("高级 ▸", 16, y0 + 376, 100);
        _btnAdvanced.Click += (_, _) => ToggleAdvanced();

        _advancedPanel = MakeCard(16, y0 + 412, 728, 460);
        _advancedPanel.Visible = false;
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 8),
            Size = new Size(200, 20),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
            Text = "高级"
        });

        // R007：信任等级三选一
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 32),
            Size = new Size(400, 22),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
            Text = TrustLevelCopy.GroupTitle,
            Name = "trustGroupTitle"
        });
        _radioAsk = new RadioButton
        {
            AutoSize = false,
            Location = new Point(16, 54),
            Size = new Size(140, 22),
            Text = TrustLevelCopy.AskEveryTimeName,
            Checked = true,
            Name = "radioTrustAsk"
        };
        _radioRelaxed = new RadioButton
        {
            AutoSize = false,
            Location = new Point(16, 96),
            Size = new Size(140, 22),
            Text = TrustLevelCopy.RelaxedName,
            Name = "radioTrustRelaxed"
        };
        _radioFullTrust = new RadioButton
        {
            AutoSize = false,
            Location = new Point(16, 138),
            Size = new Size(90, 22),
            Text = TrustLevelCopy.FullTrustName,
            Name = "radioTrustFullTrust"
        };
        _advancedPanel.Controls.Add(_radioAsk);
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(160, 54),
            Size = new Size(540, 22),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = TrustLevelCopy.AskEveryTimeBlurb,
            Name = "trustAskBlurb"
        });
        _advancedPanel.Controls.Add(_radioRelaxed);
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(160, 96),
            Size = new Size(540, 22),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = TrustLevelCopy.RelaxedBlurb,
            Name = "trustRelaxedBlurb"
        });
        _advancedPanel.Controls.Add(_radioFullTrust);
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(110, 138),
            Size = new Size(50, 22),
            ForeColor = Color.FromArgb(0xB0, 0x60, 0x00),
            Font = new Font(Font.FontFamily, 8.5F),
            Text = TrustLevelCopy.FullTrustOnceHint,
            Name = "trustFullTrustOnceHint"
        });
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(160, 138),
            Size = new Size(540, 22),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = TrustLevelCopy.FullTrustBlurb,
            Name = "trustFullTrustBlurb"
        });
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 164),
            Size = new Size(680, 20),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Font = new Font(Font.FontFamily, 8.5F),
            Text = TrustLevelCopy.HighRiskSupplement,
            Name = "trustHighRiskNote"
        });
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 184),
            Size = new Size(680, 20),
            ForeColor = Color.FromArgb(0x80, 0x86, 0x8B),
            Font = new Font(Font.FontFamily, 8.5F),
            Text = TrustLevelCopy.GuestCeilingNote,
            Name = "trustGuestCeiling"
        });
        _radioAsk.CheckedChanged += OnTrustRadioChanged;
        _radioRelaxed.CheckedChanged += OnTrustRadioChanged;
        _radioFullTrust.CheckedChanged += OnTrustRadioChanged;

        // R007：本次协助已确认的改动
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 210),
            Size = new Size(400, 20),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
            Text = TrustLevelCopy.ConfirmedChangesTitle,
            Name = "confirmedChangesTitle"
        });
        _confirmedView = new ListView
        {
            Location = new Point(12, 232),
            Size = new Size(704, 72),
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.None,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Name = "confirmedChangesList"
        };
        _confirmedView.Columns.Add("line", 680);
        _advancedPanel.Controls.Add(_confirmedView);

        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 312),
            Size = new Size(200, 18),
            Font = new Font(Font.FontFamily, 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "调试"
        });
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 332),
            Size = new Size(60, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "本机 IP"
        });
        _hostCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(76, 332),
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
            Location = new Point(270, 332),
            Size = new Size(80, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Consolas", 10F, FontStyle.Bold),
            Text = $":{_host.Port}"
        };
        _advancedPanel.Controls.Add(_portValue);
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(360, 332),
            Size = new Size(70, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "TLS 指纹"
        });
        _tlsValue = new Label
        {
            AutoSize = false,
            Location = new Point(430, 332),
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
            Location = new Point(12, 360),
            Size = new Size(700, 18),
            Font = new Font("Consolas", 8F),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = ""
        };
        _advancedPanel.Controls.Add(_listenRawValue);
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 378),
            Size = new Size(120, 18),
            Text = "最近审计"
        });
        var openAudit = new LinkLabel { AutoSize = true, Location = new Point(620, 378), Text = "打开日志文件" };
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
            Location = new Point(12, 396),
            Size = new Size(704, 56),
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.None,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Name = "recentAuditList"
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

        _trustTip = new Panel
        {
            Location = new Point(0, 64),
            Size = new Size(760, 40),
            BackColor = Color.FromArgb(0xE8, 0xF0, 0xFE),
            Visible = false,
            Name = "trustTip"
        };
        _trustTipText = new Label
        {
            AutoSize = false,
            Location = new Point(20, 8),
            Size = new Size(560, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0x17, 0x4E, 0xA6),
            Font = new Font(Font.FontFamily, 9F),
            BackColor = Color.Transparent,
            Name = "trustTipText"
        };
        _trustTipRevert = new LinkLabel
        {
            AutoSize = true,
            Location = new Point(600, 10),
            Text = TrustLevelCopy.RevertToAskEveryTime,
            LinkColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            ActiveLinkColor = Color.FromArgb(0x15, 0x5F, 0xC0),
            Name = "trustTipRevert"
        };
        _trustTipRevert.LinkClicked += (_, _) =>
        {
            // 宽松：改回每次询问；完全信任：结束完全信任（不改平时偏好）
            if (_host.TrustLevelIsFullTrust)
                _host.EndFullTrustSession();
            else
                _host.RevertTrustToAskEveryTime();
            RefreshTrustUi();
        };
        _trustTip.Controls.Add(_trustTipText);
        _trustTip.Controls.Add(_trustTipRevert);

        Controls.Add(header);
        Controls.Add(_banner);
        Controls.Add(_trustTip);
        Controls.Add(sessionCard);
        Controls.Add(statusCard);
        Controls.Add(tipCard);
        Controls.Add(_btnAdvanced);
        Controls.Add(_advancedPanel);
        Controls.Add(_statusLabel);

        foreach (Control c in Controls)
        {
            if (c.Name is "header" or "banner" or "trustTip") continue;
            _baseTops[c] = c.Top;
        }

        FormClosing += OnFormClosing;
        _host.SessionChanged += OnSessionChanged;
        _host.PairedClientsChanged += OnPairedClientsChanged;
        _host.RelayStatusChanged += OnRelayStatusChanged;
        _host.Audit.EntryWritten += OnAuditWritten;
        _host.TrustLevelChanged += OnTrustLevelChanged;

        _auditTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        _auditTimer.Tick += (_, _) =>
        {
            RefreshAudit();
            RefreshConfirmedChanges();
        };
        _auditTimer.Start();

        Shown += (_, _) => MaybeShowBanner();

        RefreshFromHost();
        RefreshClients();
        RefreshAudit();
        RefreshTrustUi();
        UpdateFriendStatus();
    }

    private void MaybeShowBanner() => RefreshBannerVisibility();

    private void ShowDeniedBanner()
    {
        _settings.NetworkAllowMaybeDenied = true;
        _settings.NetworkAllowBannerDismissed = false;
        _settings.Save();
        ApplyDeniedBannerUi();
    }

    private void DismissBanner()
    {
        _settings.NetworkAllowBannerDismissed = true;
        _settings.NetworkAllowMaybeDenied = false;
        _settings.Save();
        _banner.Visible = false;
        RelayoutForBanner();
    }

    /// <summary>
    /// R005：已连上网络时收起「请点允许」；仅未连上或用户点了「我点了取消」时显示。
    /// </summary>
    private void RefreshBannerVisibility()
    {
        if (_settings.NetworkAllowMaybeDenied)
        {
            ApplyDeniedBannerUi();
            return;
        }

        if (_settings.NetworkAllowBannerDismissed)
        {
            if (_banner.Visible)
            {
                _banner.Visible = false;
                RelayoutForBanner();
            }
            return;
        }

        if (IsNetworkReadyForShare())
        {
            if (_banner.Visible)
            {
                _banner.Visible = false;
                RelayoutForBanner();
            }
            return;
        }

        _bannerText.Text = FriendConnectGuide.MomentAAllow.Replace("\n", Environment.NewLine);
        _btnFirewall.Visible = false;
        if (!_banner.Visible)
        {
            _banner.Visible = true;
            RelayoutForBanner();
        }
    }

    private void ApplyDeniedBannerUi()
    {
        _bannerText.Text = FriendConnectGuide.MomentADenied + Environment.NewLine + FriendConnectGuide.MomentAFirewallHint;
        _btnFirewall.Visible = true;
        if (!_banner.Visible)
        {
            _banner.Visible = true;
            RelayoutForBanner();
        }
        else
        {
            RelayoutForBanner();
        }
    }

    private bool IsNetworkReadyForShare()
    {
        var phase = _host.RelayPhase;
        if (phase is "ready" or "waiting" or "paired")
            return true;
        var text = _listenValue.Text ?? "";
        return text == FriendConnectGuide.StatusReadyToShare
               || text == FriendConnectGuide.StatusWaitingPeer
               || text == "对方已连接";
    }

    private void RelayoutForBanner()
    {
        var bannerH = _banner.Visible ? _banner.Height : 0;
        var tipH = _trustTip is { Visible: true } ? _trustTip.Height : 0;
        _banner.Top = 64;
        if (_trustTip is not null)
            _trustTip.Top = 64 + bannerH;
        var shift = bannerH + tipH;
        foreach (var kv in _baseTops)
        {
            // 托盘提示始终单独钉在窗体底部，避免叠进「高级」单选组（曾挡住「宽松」）
            if (ReferenceEquals(kv.Key, _statusLabel)) continue;
            kv.Key.Top = kv.Value + shift;
        }

        if (_advancedOpen)
        {
            // 高级面板完整露出 + 底部状态行
            var needH = _advancedPanel.Bottom + _statusLabel.Height + 10;
            ClientSize = new Size(760, Math.Max(_baseClientHeight + shift, needH));
            _statusLabel.Top = _advancedPanel.Bottom + 6;
            _advancedPanel.BringToFront();
            _statusLabel.BringToFront();
        }
        else
        {
            ClientSize = new Size(760, _baseClientHeight + shift);
            var baseStatus = _baseTops.TryGetValue(_statusLabel, out var st) ? st : 592;
            _statusLabel.Top = baseStatus + shift;
        }
    }

    private void ToggleAdvanced()
    {
        _advancedOpen = !_advancedOpen;
        _advancedPanel.Visible = _advancedOpen;
        _btnAdvanced.Text = _advancedOpen ? "高级 ▾" : "高级 ▸";
        RelayoutForBanner();
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


    private void ConfirmAndRefreshSession()
    {
        using var dlg = new Form
        {
            Text = "刷新会话码",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(420, 160),
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = Font
        };
        dlg.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 20),
            Size = new Size(380, 60),
            Text = FriendConnectGuide.RefreshSessionConfirm
        });
        var ok = new Button
        {
            Text = FriendConnectGuide.RefreshSessionConfirmOk,
            DialogResult = DialogResult.OK,
            Location = new Point(120, 100),
            Size = new Size(120, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            ForeColor = Color.White
        };
        ok.FlatAppearance.BorderSize = 0;
        var cancel = new Button
        {
            Text = FriendConnectGuide.RefreshSessionConfirmCancel,
            DialogResult = DialogResult.Cancel,
            Location = new Point(250, 100),
            Size = new Size(90, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        };
        cancel.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        cancel.FlatAppearance.BorderSize = 1;
        dlg.Controls.Add(ok);
        dlg.Controls.Add(cancel);
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _host.RefreshSessionCode();
        SetStatus("已刷新会话码（旧配对已全部断开）。");
        RefreshClients();
    }
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
        RefreshConfirmedChanges();
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

        RefreshBannerVisibility();
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

    private void OnTrustLevelChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(RefreshTrustUi); return; }
        RefreshTrustUi();
    }

    private void RefreshTrustUi()
    {
        SyncTrustRadios();
        RefreshTrustTip();
        RefreshConfirmedChanges();
        RelayoutForBanner();
    }

    private void SyncTrustRadios()
    {
        if (_radioAsk is null) return;
        var level = _host.TrustLevelEffective;
        _suppressTrustRadio = true;
        try
        {
            _radioAsk.Checked = level == TrustLevelKind.AskEveryTime;
            _radioRelaxed.Checked = level == TrustLevelKind.Relaxed;
            _radioFullTrust.Checked = level == TrustLevelKind.FullTrust;
        }
        finally { _suppressTrustRadio = false; }
    }

    private void RefreshTrustTip()
    {
        if (_trustTip is null) return;
        var level = _host.TrustLevelEffective;
        if (level == TrustLevelKind.AskEveryTime)
        {
            if (_trustTip.Visible)
            {
                _trustTip.Visible = false;
            }
            return;
        }

        if (level == TrustLevelKind.Relaxed)
        {
            _trustTip.BackColor = Color.FromArgb(0xE8, 0xF0, 0xFE);
            _trustTipText.ForeColor = Color.FromArgb(0x17, 0x4E, 0xA6);
            _trustTipText.Text = TrustLevelCopy.RelaxedBanner;
            _trustTipRevert.Text = TrustLevelCopy.RevertToAskEveryTime;
        }
        else
        {
            _trustTip.BackColor = Color.FromArgb(0xFE, 0xF7, 0xE0);
            _trustTipText.ForeColor = Color.FromArgb(0xB0, 0x60, 0x00);
            _trustTipText.Text = TrustLevelCopy.FullTrustBanner;
            _trustTipRevert.Text = TrustLevelCopy.EndFullTrust;
        }
        _trustTip.Visible = true;
    }

    private void OnTrustRadioChanged(object? sender, EventArgs e)
    {
        if (_suppressTrustRadio) return;
        if (sender is not RadioButton { Checked: true } rb) return;

        if (ReferenceEquals(rb, _radioAsk))
        {
            _host.SetTrustLevelPreference(TrustLevelKind.AskEveryTime);
            RefreshTrustUi();
            return;
        }

        if (ReferenceEquals(rb, _radioRelaxed))
        {
            _host.SetTrustLevelPreference(TrustLevelKind.Relaxed);
            RefreshTrustUi();
            return;
        }

        if (ReferenceEquals(rb, _radioFullTrust))
        {
            // 二次确认；取消则档位不变
            using var dlg = new FullTrustConfirmDialog();
            var result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                _host.EnableFullTrustAfterConfirm();
                RefreshTrustUi();
            }
            else
            {
                SyncTrustRadios();
            }
        }
    }

    private void RefreshConfirmedChanges()
    {
        if (_confirmedView is null) return;
        try
        {
            var entries = _host.Audit.ReadRecent(80);
            var filtered = ConfirmedChangeList.Filter(entries.Select(e => (e.EventType, e.Detail)));
            _confirmedView.BeginUpdate();
            try
            {
                _confirmedView.Items.Clear();
                // ReadRecent 旧→新；列表展示新在上
                foreach (var (eventType, detail) in filtered.Reverse())
                {
                    var matching = entries.LastOrDefault(e => e.EventType == eventType && e.Detail == detail);
                    var ts = matching?.Timestamp;
                    var line = ConfirmedChangeList.DisplayLine(eventType, detail, ts is { Length: >= 19 } ? ts[..19] : ts);
                    _confirmedView.Items.Add(new ListViewItem(line));
                }
                if (_confirmedView.Items.Count == 0)
                    _confirmedView.Items.Add(new ListViewItem("（暂无已确认的改动）") { ForeColor = Color.Gray });
            }
            finally { _confirmedView.EndUpdate(); }
        }
        catch { /* ignore */ }
    }

    /// <summary>UiShot：展开高级面板。</summary>
    internal void OpenAdvancedForShot()
    {
        if (!_advancedOpen) ToggleAdvanced();
    }

    /// <summary>UiShot：收起高级面板。</summary>
    internal void CloseAdvancedForShot()
    {
        if (_advancedOpen) ToggleAdvanced();
    }

    /// <summary>UiShot：模拟选完全信任后点取消，档位回到每次询问。</summary>
    internal void SimulateFullTrustCancelForShot()
    {
        using var dlg = new FullTrustConfirmDialog();
        // 不真正 ShowDialog（无人工点击）；直接走取消分支：档位不变
        SyncTrustRadios();
        RefreshTrustUi();
    }

    /// <summary>UiShot：刷新审计与已确认列表；确保最近审计从顶部可见。</summary>
    internal void RefreshListsForShot()
    {
        RefreshAudit();
        RefreshConfirmedChanges();
        if (_auditView.Items.Count > 0)
            _auditView.EnsureVisible(0);
        if (_confirmedView.Items.Count > 0)
            _confirmedView.EnsureVisible(0);
    }

    /// <summary>UiShot：同步信任档 UI（不弹二次确认）。</summary>
    internal void ApplyTrustForShot(TrustLevelKind level)
    {
        if (level == TrustLevelKind.FullTrust)
            _host.EnableFullTrustAfterConfirm();
        else
            _host.SetTrustLevelPreference(level);
        RefreshTrustUi();
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
            _host.TrustLevelChanged -= OnTrustLevelChanged;
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
