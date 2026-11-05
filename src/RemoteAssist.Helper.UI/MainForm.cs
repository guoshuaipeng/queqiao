using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Protocol;
using RemoteAssist;
using RemoteAssist.Protocol.Messages;
using RemoteAssist.Relay.Stub;

namespace RemoteAssist.Helper.UI;

/// <summary>
/// 协助端主界面（R003）：默认粘贴码+密码连中继；高级折叠；失败用时刻 B 白话。
/// </summary>
internal sealed class MainForm : Form
{
    private readonly TextBox _inputBox;
    private readonly TextBox _hostBox;
    private readonly TextBox _portBox;
    private readonly TextBox _fpBox;
    private readonly ComboBox _permBox;
    private readonly CheckBox _insecureBox;
    private readonly Label _previewLabel;
    private readonly Label _statusLabel;
    private readonly Label _failHintLabel;
    private readonly TextBox _argBox;
    private readonly TextBox _outputBox;
    private readonly TextBox _pwdBox;
    private readonly TextBox _relayBox;
    private readonly TextBox _accountUserBox;
    private readonly TextBox _accountPassBox;
    private readonly ListBox _deviceList;
    private readonly Button _btnConnect;
    private readonly Button _btnDisconnect;
    private readonly Button[] _toolButtons;
    private readonly Panel _advancedPanel;
    private readonly Button _btnAdvanced;
    private readonly Button _btnOwnConnect;
    private readonly Label _ownSelectHint;
    private readonly Panel _toolCard;
    private readonly Panel _settingsCard;
    private bool _advancedOpen;

    private AssistClient? _client;
    private readonly HelperUiSettings _settings;
    private bool _busy;
    private bool _selfDisconnect;
    private readonly bool _enableReturnHome;
    private bool _returningHome;

    public event Action? HomeRequested;

    public MainForm(bool enableReturnHome = false)
    {
        _enableReturnHome = enableReturnHome;
        _settings = HelperUiSettings.Load();
        if (!_settings.PreferRelay)
            _settings.PreferRelay = true;

        Text = "鹊桥 · 协助端";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(780, 1040);
        BackColor = Color.FromArgb(0xF7, 0xF8, 0xFA);
        Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = AppIcons.CreateAppIcon();

        UiWindowPlacement.Bind(this, restoreSize: false);

        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(780, 64),
            BackColor = Color.FromArgb(0x0F, 0x9D, 0x58)
        };
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 10),
            Size = new Size(500, 28),
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 14F, FontStyle.Bold),
            Text = "鹊桥 · 协助端",
            BackColor = Color.Transparent
        });
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 36),
            Size = new Size(_enableReturnHome ? 620 : 720, 22),
            ForeColor = Color.FromArgb(0xE8, 0xF5, 0xE9),
            Font = new Font(Font.FontFamily, 9F),
            Text = "粘贴对方发来的会话码和验证密码，或登录账号免密控制自己的机器",
            BackColor = Color.Transparent
        });
        Controls.Add(header);

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
                BackColor = Color.FromArgb(0x0B, 0x7A, 0x45),
                Font = new Font(Font.FontFamily, 9F, FontStyle.Bold)
            };
            btnHome.FlatAppearance.BorderColor = Color.White;
            btnHome.Click += (_, _) => RequestHome();
            header.Controls.Add(btnHome);
            btnHome.BringToFront();
        }

        var pairCard = MakeCard(16, 80, 748, 250);
        pairCard.Controls.Add(SectionTitle("连接对方", 12, 10));
        pairCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(420, 20),
            Text = "先让对方装好「鹊桥远程协助」",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        var btnInstallGuide = MakeButton("复制安装说明", 450, 36, 140);
        btnInstallGuide.Height = 28;
        btnInstallGuide.FlatStyle = FlatStyle.Flat;
        btnInstallGuide.BackColor = Color.White;
        btnInstallGuide.ForeColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnInstallGuide.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnInstallGuide.FlatAppearance.BorderSize = 1;
        btnInstallGuide.Cursor = Cursors.Hand;
        btnInstallGuide.Click += (_, _) => CopyInstallInstructions();
        pairCard.Controls.Add(btnInstallGuide);

        pairCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 78),
            Size = new Size(560, 20),
            Text = "粘贴对方发来的会话码和验证密码",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        _inputBox = new TextBox
        {
            Location = new Point(16, 102),
            Size = new Size(600, 52),
            Font = new Font("Consolas", 9.5F),
            Multiline = true,
            AcceptsReturn = true,
            ScrollBars = ScrollBars.Vertical
        };
        _inputBox.TextChanged += (_, _) => OnInputChanged();
        pairCard.Controls.Add(_inputBox);

        var btnClip = MakeButton("从剪贴板", 626, 102, 100);
        btnClip.Height = 52;
        btnClip.Click += (_, _) => PasteClipboard();
        pairCard.Controls.Add(btnClip);

        // password kept for advanced/manual; auto-filled by share paste
        _pwdBox = new TextBox
        {
            Location = new Point(16, 164),
            Size = new Size(200, 26),
            Font = new Font("Consolas", 9.5F),
            Visible = false
        };
        _pwdBox.TextChanged += (_, _) => RefreshPreview();
        pairCard.Controls.Add(_pwdBox);

        _previewLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 164),
            Size = new Size(440, 28),
            Font = new Font(Font.FontFamily, 9F),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "粘贴后点「连接」"
        };
        pairCard.Controls.Add(_previewLabel);

        _btnConnect = MakeButton(HelperSessionCopy.ConnectButtonText, 450, 160, 120);
        _btnConnect.Height = 36;
        _btnConnect.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
        _btnConnect.BackColor = Color.FromArgb(0x0F, 0x9D, 0x58);
        _btnConnect.ForeColor = Color.White;
        _btnConnect.FlatStyle = FlatStyle.Flat;
        _btnConnect.FlatAppearance.BorderSize = 0;
        _btnConnect.Cursor = Cursors.Hand;
        _btnConnect.Click += async (_, _) => await ConnectAsync().ConfigureAwait(true);
        pairCard.Controls.Add(_btnConnect);

        _btnDisconnect = MakeButton("断开", 580, 162, 70);
        _btnDisconnect.Height = 32;
        _btnDisconnect.FlatStyle = FlatStyle.Flat;
        _btnDisconnect.BackColor = Color.White;
        _btnDisconnect.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
        _btnDisconnect.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        _btnDisconnect.FlatAppearance.BorderSize = 1;
        _btnDisconnect.Enabled = false;
        _btnDisconnect.Click += (_, _) => Disconnect();
        pairCard.Controls.Add(_btnDisconnect);

        _failHintLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 200),
            Size = new Size(710, 36),
            ForeColor = Color.FromArgb(0xC5, 0x22, 0x1F),
            Text = ""
        };
        pairCard.Controls.Add(_failHintLabel);
        Controls.Add(pairCard);

        _btnAdvanced = MakeButton("高级 ▸", 16, 336, 100);
        _btnAdvanced.Click += (_, _) => ToggleAdvanced();
        Controls.Add(_btnAdvanced);

        _advancedPanel = MakeCard(16, 372, 748, 150);
        _advancedPanel.Visible = false;
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 8),
            Size = new Size(200, 22),
            Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
            Text = "高级（调试）"
        });

        _advancedPanel.Controls.Add(Lbl("主机", 12, 40));
        _hostBox = new TextBox
        {
            Location = new Point(56, 38),
            Size = new Size(140, 26),
            Font = new Font("Consolas", 9.5F),
            Text = _settings.LastHost ?? ""
        };
        _hostBox.TextChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_hostBox);

        _advancedPanel.Controls.Add(Lbl("端口", 210, 40));
        _portBox = new TextBox
        {
            Location = new Point(250, 38),
            Size = new Size(70, 26),
            Font = new Font("Consolas", 9.5F),
            Text = "" // 默认不填 17890；中继路径不用
        };
        _portBox.TextChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_portBox);

        _advancedPanel.Controls.Add(Lbl("密码", 340, 40));
        var pwdVisible = new TextBox
        {
            Location = new Point(380, 38),
            Size = new Size(100, 26),
            Font = new Font("Consolas", 9.5F)
        };
        // sync visible advanced pwd with hidden field used by parser
        pwdVisible.TextChanged += (_, _) =>
        {
            if (_pwdBox.Text != pwdVisible.Text)
                _pwdBox.Text = pwdVisible.Text;
        };
        _pwdBox.TextChanged += (_, _) =>
        {
            if (pwdVisible.Text != _pwdBox.Text)
                pwdVisible.Text = _pwdBox.Text;
        };
        _advancedPanel.Controls.Add(pwdVisible);

        _advancedPanel.Controls.Add(Lbl("权限", 500, 40));
        _permBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(540, 38),
            Size = new Size(90, 26)
        };
        _permBox.Items.AddRange(new object[] { "Assist", "Guest", "Admin" });
        _permBox.SelectedIndex = 0;
        _permBox.SelectedIndexChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_permBox);

        _insecureBox = new CheckBox
        {
            Location = new Point(640, 40),
            Size = new Size(90, 24),
            Text = "明文",
            ForeColor = Color.FromArgb(0xB0, 0x60, 0x00)
        };
        _insecureBox.CheckedChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_insecureBox);

        _advancedPanel.Controls.Add(Lbl("TLS指纹", 12, 78));
        _fpBox = new TextBox
        {
            Location = new Point(86, 76),
            Size = new Size(640, 26),
            Font = new Font("Consolas", 8.5F)
        };
        _fpBox.TextChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_fpBox);

        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 112),
            Size = new Size(80, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "中继"
        });
        _relayBox = new TextBox
        {
            Location = new Point(86, 110),
            Size = new Size(280, 26),
            Font = new Font("Consolas", 9.5F),
            Text = _settings.RelayEndpoint ?? "",
            BackColor = Color.FromArgb(0xEE, 0xF0, 0xF3)
        };
        _advancedPanel.Controls.Add(_relayBox);
        var btnSaveRelay = MakeButton("保存中继", 380, 108, 90);
        btnSaveRelay.Click += (_, _) => SaveRelaySetting();
        _advancedPanel.Controls.Add(btnSaveRelay);
        Controls.Add(_advancedPanel);

        // 状态放在配对卡下方，未连接时不占「常用操作」行
        _statusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(130, 338),
            Size = new Size(748, 22),
            Text = "状态：未连接",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Name = "statusLine"
        };
        Controls.Add(_statusLabel);

        var toolTop = 366;
        var toolCard = MakeCard(16, toolTop, 748, 250);
        toolCard.Name = "tool";
        toolCard.Visible = false; // 连上后再展开
        toolCard.Controls.Add(SectionTitle("常用操作", 12, 10));

        var btnProcs = MakeButton("查看正在运行的程序", 16, 48, 160);
        var btnLs = MakeButton("查看文件夹", 184, 48, 110);
        var btnLogs = MakeButton("查看日志", 302, 48, 100);
        var btnCat = MakeButton("查看文件", 410, 48, 100);
        var btnRun = MakeButton("提议一项改动", 518, 48, 120);
        btnRun.FlatStyle = FlatStyle.Flat;
        btnRun.BackColor = Color.FromArgb(0xFB, 0xBC, 0x04);
        btnRun.FlatAppearance.BorderSize = 0;
        _toolButtons = new[] { btnProcs, btnLs, btnLogs, btnCat, btnRun };
        foreach (var b in _toolButtons)
        {
            toolCard.Controls.Add(b);
        }
        _toolCard = toolCard;
        toolCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 96),
            Size = new Size(200, 20),
            Text = "路径 / 命令参数",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        _argBox = new TextBox
        {
            Location = new Point(16, 118),
            Size = new Size(710, 26),
            Font = new Font("Consolas", 9.5F)
        };
        toolCard.Controls.Add(_argBox);
        _outputBox = new TextBox
        {
            Location = new Point(16, 152),
            Size = new Size(710, 80),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 8.5F),
            BackColor = Color.White
        };
        toolCard.Controls.Add(_outputBox);
        btnProcs.Click += async (_, _) => await RunToolAsync(ToolKind.ListProcesses, new Dictionary<string, string?>()).ConfigureAwait(true);
        btnLs.Click += async (_, _) =>
        {
            var p = RequireArg("path");
            if (p is null) return;
            await RunToolAsync(ToolKind.ListDir, new Dictionary<string, string?> { ["path"] = p }).ConfigureAwait(true);
        };
        btnLogs.Click += async (_, _) =>
        {
            var p = RequireArg("path");
            if (p is null) return;
            await RunToolAsync(ToolKind.ReadLogTail, new Dictionary<string, string?> { ["path"] = p, ["lines"] = "80" }).ConfigureAwait(true);
        };
        btnCat.Click += async (_, _) =>
        {
            var p = RequireArg("path");
            if (p is null) return;
            await RunToolAsync(ToolKind.ReadFile, new Dictionary<string, string?> { ["path"] = p }).ConfigureAwait(true);
        };
        btnRun.Click += async (_, _) =>
        {
            var c = RequireArg("command");
            if (c is null) return;
            await RunToolAsync(ToolKind.RunCommand, new Dictionary<string, string?> { ["command"] = c }).ConfigureAwait(true);
        };
        Controls.Add(toolCard);

        var settingsCard = MakeCard(16, 608, 748, 250);
        settingsCard.Name = "settings";
        settingsCard.Controls.Add(SectionTitle("账号（可选）", 12, 10));
        settingsCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(700, 20),
            Text = "登录后免密连自己的机器（无需再确认配对；写操作仍须对方本机确认）",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        settingsCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 68),
            Size = new Size(48, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "账号"
        });
        _accountUserBox = new TextBox { Location = new Point(64, 68), Size = new Size(140, 26), Text = _settings.AccountUser ?? "" };
        settingsCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(214, 68),
            Size = new Size(48, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "密码"
        });
        _accountPassBox = new TextBox { Location = new Point(262, 68), Size = new Size(140, 26), UseSystemPasswordChar = true };
        settingsCard.Controls.Add(_accountUserBox);
        settingsCard.Controls.Add(_accountPassBox);
        var btnReg = MakeButton("注册", 416, 66, 60);
        btnReg.Click += async (_, _) => await AccountAuthAsync(register: true).ConfigureAwait(true);
        var btnLogin = MakeButton("登录", 482, 66, 60);
        btnLogin.Click += async (_, _) => await AccountAuthAsync(register: false).ConfigureAwait(true);
        var btnList = MakeButton("刷新设备", 548, 66, 80);
        btnList.Click += async (_, _) => await RefreshDevicesAsync().ConfigureAwait(true);
        settingsCard.Controls.Add(btnReg);
        settingsCard.Controls.Add(btnLogin);
        settingsCard.Controls.Add(btnList);
        _deviceList = new ListBox
        {
            Location = new Point(16, 104),
            Size = new Size(700, 72),
            IntegralHeight = false
        };
        _deviceList.SelectedIndexChanged += (_, _) => UpdateOwnConnectUi();
        settingsCard.Controls.Add(_deviceList);
        _btnOwnConnect = MakeButton("连接这台电脑", 16, 186, 160);
        _btnOwnConnect.Enabled = false;
        _btnOwnConnect.Click += async (_, _) => await ConnectOwnedDeviceAsync().ConfigureAwait(true);
        settingsCard.Controls.Add(_btnOwnConnect);
        _ownSelectHint = new Label
        {
            AutoSize = false,
            Location = new Point(186, 188),
            Size = new Size(360, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "请先点选一台自己的电脑"
        };
        settingsCard.Controls.Add(_ownSelectHint);
        _settingsCard = settingsCard;
        Controls.Add(settingsCard);

        var mcpCard = MakeCard(16, 874, 748, 180);
        mcpCard.Name = "mcp";
        mcpCard.Controls.Add(SectionTitle(McpInstallGuide.SectionTitle, 12, 10));
        mcpCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(710, 72),
            ForeColor = Color.FromArgb(0x3C, 0x40, 0x43),
            Text = McpInstallGuide.StepsBlock
        });
        var btnCopyMcp = MakeButton(McpInstallGuide.CopyConfigButton, 16, 122, 140);
        btnCopyMcp.Height = 32;
        btnCopyMcp.FlatStyle = FlatStyle.Flat;
        btnCopyMcp.BackColor = Color.White;
        btnCopyMcp.ForeColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopyMcp.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnCopyMcp.FlatAppearance.BorderSize = 1;
        btnCopyMcp.Click += (_, _) => CopyMcpConfigFromUi();
        mcpCard.Controls.Add(btnCopyMcp);
        var btnCopySkill = MakeButton(McpInstallGuide.CopySkillButton, 166, 122, 140);
        btnCopySkill.Height = 32;
        btnCopySkill.FlatStyle = FlatStyle.Flat;
        btnCopySkill.BackColor = Color.White;
        btnCopySkill.ForeColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopySkill.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        btnCopySkill.FlatAppearance.BorderSize = 1;
        btnCopySkill.Cursor = Cursors.Hand;
        btnCopySkill.Click += (_, _) => CopySkillInstructions();
        mcpCard.Controls.Add(btnCopySkill);
        mcpCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 158),
            Size = new Size(710, 28),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "详细参数见产品文档；写操作仍须对方本机确认"
        });
        // 略增高 MCP 卡以容纳 Skill 按钮行
        mcpCard.Height = Math.Max(mcpCard.Height, 200);
        Controls.Add(mcpCard);

        FormClosing += OnFormClosing;
        RefreshPreview();
        SetConnectedUi(false);
        UpdateOwnConnectUi();
        LayoutForAdvanced();
    }

    private void ToggleAdvanced()
    {
        _advancedOpen = !_advancedOpen;
        _advancedPanel.Visible = _advancedOpen;
        _btnAdvanced.Text = _advancedOpen ? "高级 ▾" : "高级 ▸";
        LayoutForAdvanced();
    }

    private void LayoutForAdvanced()
    {
        var shift = _advancedOpen ? _advancedPanel.Height + 8 : 0;
        var status = Controls.Find("statusLine", false).FirstOrDefault();
        if (status is not null) status.Top = 338 + shift; // beside 高级

        var tool = Controls.Find("tool", false).FirstOrDefault();
        var settings = Controls.Find("settings", false).FirstOrDefault();
        var mcp = Controls.Find("mcp", false).FirstOrDefault();
        var toolVisible = tool is { Visible: true };
        var y = 366 + shift;
        if (tool is not null)
        {
            tool.Top = y;
            if (toolVisible) y += tool.Height + 16;
        }
        if (settings is not null)
        {
            settings.Top = y;
            y += settings.Height + 16;
        }
        if (mcp is not null)
        {
            mcp.Top = y;
            y += mcp.Height + 16;
        }
        ClientSize = new Size(780, Math.Max(720, y + 24));
    }

    private static Panel MakeCard(int x, int y, int w, int h) => new()
    {
        Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle
    };

    private static Label SectionTitle(string text, int x, int y) => new()
    {
        AutoSize = false, Location = new Point(x, y), Size = new Size(400, 24),
        Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold), Text = text
    };

    private static Label Lbl(string text, int x, int y) => new()
    {
        AutoSize = false, Location = new Point(x, y), Size = new Size(52, 26),
        TextAlign = ContentAlignment.MiddleLeft, Text = text
    };

    private static Button MakeButton(string text, int x, int y, int w) => new()
    {
        Text = text, Location = new Point(x, y), Size = new Size(w, 28), FlatStyle = FlatStyle.System
    };



    private void CopyMcpConfigFromUi()
    {
        try
        {
            if (!TryBuildOptions(out var opts, out _, out _))
            {
                // 与连接逻辑一致：缺指纹时按明文生成 MCP 配置，避免亲友被 TLS 拦住
                var saved = _insecureBox.Checked;
                _insecureBox.Checked = true;
                var ok = TryBuildOptions(out opts, out _, out _);
                _insecureBox.Checked = saved;
                if (!ok)
                {
                    SetFailHint(McpInstallGuide.NeedCodePasswordHint);
                    _previewLabel.ForeColor = Color.FromArgb(0xC5, 0x22, 0x1F);
                    return;
                }
            }

            var useTls = !opts.Insecure && !string.IsNullOrWhiteSpace(opts.Fingerprint);
            string payload;
            if (opts.ViaRelay)
            {
                var relay = string.IsNullOrWhiteSpace(opts.RelayEndpoint)
                    ? (string.IsNullOrWhiteSpace(_relayBox.Text) ? RelayEndpoint.DefaultEndpoint : _relayBox.Text.Trim())
                    : opts.RelayEndpoint!;
                var pwd = opts.Password ?? _pwdBox.Text ?? "";
                if (!useTls)
                    payload = PairQrPayload.BuildRelay(relay, opts.Code, pwd, fingerprint: null, useTls: false);
                else
                    payload = PairQrPayload.BuildRelay(relay, opts.Code, pwd, opts.Fingerprint, useTls: true);
            }
            else
            {
                if (!useTls)
                    payload = PairQrPayload.BuildLan(opts.Host, opts.Port, opts.Code, fingerprint: null, useTls: false);
                else
                    payload = PairQrPayload.BuildLan(opts.Host, opts.Port, opts.Code, opts.Fingerprint, useTls: true);
            }

            var exists = McpExeLocator.TryResolveExisting(out var exe);
            if (!exists)
                exe = McpExeLocator.Resolve();

            Clipboard.SetText(McpConfigSnippet.BuildClipboardBundle(exe, payload));
            _previewLabel.Text = exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            SetFailHint("");
            SetStatus(exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint);
        }
        catch (Exception ex)
        {
            SetFailHint("复制 MCP 配置失败：" + ex.Message);
        }
    }

    private void CopySkillInstructions()
    {
        try
        {
            Clipboard.SetText(McpInstallGuide.SkillInstructionsClipboard);
            // 反馈只写预览行一次，不写状态行（对齐 R017 不叠字）
            _previewLabel.Text = McpInstallGuide.SkillCopiedHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            SetFailHint("");
        }
        catch (Exception ex)
        {
            SetFailHint("复制 Skill 说明失败：" + ex.Message);
        }
    }

    private void CopyInstallInstructions()
    {
        try
        {
            Clipboard.SetText(WriteConfirmCopy.InstallInstructionsClipboard);
            _previewLabel.Text = WriteConfirmCopy.InstallCopiedHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            SetFailHint("");
        }
        catch (Exception ex)
        {
            SetFailHint("复制失败：" + ex.Message);
        }
    }
    private void PasteClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                SetFailHint("请粘贴会话码和验证密码");
                return;
            }
            var text = Clipboard.GetText()?.Trim() ?? "";
            if (text.Length == 0)
            {
                SetFailHint("请粘贴会话码和验证密码");
                return;
            }
            ApplyPastedText(text);
        }
        catch (Exception ex)
        {
            SetFailHint("读取剪贴板失败：" + ex.Message);
        }
    }

    private void ApplyPastedText(string text)
    {
        if (text.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            _inputBox.Text = text;
            OnInputChanged();
            SetStatus("已粘贴会话信息。");
            ClearFailHint();
            return;
        }

        if (FriendConnectGuide.TryParseShareText(text, out var code, out var password))
        {
            _inputBox.Text = string.IsNullOrWhiteSpace(password)
                ? code
                : FriendConnectGuide.FormatShareClipboard(code, password!);
            if (!string.IsNullOrWhiteSpace(password))
                _pwdBox.Text = password;
            OnInputChanged();
            SetStatus("已填入会话码和验证密码。");
            ClearFailHint();
            return;
        }

        SetFailHint("请粘贴会话码和验证密码");
    }

    private void OnInputChanged()
    {
        var text = _inputBox.Text ?? "";
        if (PairQrPayload.TryParseFull(text, out var host, out var port, out _, out var fp, out var useTls, out var viaRelay, out var relayEp, out var pwd))
        {
            _hostBox.Text = host;
            if (_advancedOpen) _portBox.Text = port.ToString();
            if (!string.IsNullOrEmpty(fp)) _fpBox.Text = fp;
            _insecureBox.Checked = !useTls;
            if (!string.IsNullOrWhiteSpace(pwd)) _pwdBox.Text = pwd;
            if (!string.IsNullOrWhiteSpace(relayEp)) _relayBox.Text = relayEp;
            _ = viaRelay;
        }
        else if (FriendConnectGuide.TryParseShareText(text, out var code, out var password))
        {
            if (!string.IsNullOrWhiteSpace(password))
                _pwdBox.Text = password;
            // keep multiline share text in box for user clarity; parser uses code+pwd fields
            if (text.Contains('\n') || text.Contains('：') || text.Contains(':'))
            {
                // normalize display optionally — leave as-is
            }
            else
            {
                _inputBox.TextChanged -= InputShield;
            }
            _ = code;
        }

        RefreshPreview();
    }

    private void InputShield(object? s, EventArgs e) { }

    private void RefreshPreview()
    {
        // 空态：未点连接前不报「无法连接」
        var raw = (_inputBox.Text ?? "").Trim();
        if (raw.Length == 0 && string.IsNullOrWhiteSpace(_pwdBox.Text))
        {
            _previewLabel.Text = "粘贴后点「连接」";
            _previewLabel.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
            return;
        }

        if (!TryBuildOptions(out var opts, out _, out var err))
        {
            // 有输入但还不完整：温和提示，不出现 remoteassist:pair / 无法连接
            if (err.Contains("remoteassist", StringComparison.OrdinalIgnoreCase) ||
                err.Contains("载荷", StringComparison.Ordinal))
                _previewLabel.Text = "请粘贴会话码和验证密码";
            else if (err.Contains("密码", StringComparison.Ordinal))
                _previewLabel.Text = "还需要验证密码";
            else
                _previewLabel.Text = "请粘贴会话码和验证密码";
            _previewLabel.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
            return;
        }

        _previewLabel.Text = FriendConnectGuide.FriendConnectionPreview(opts.Code);
        _previewLabel.ForeColor = Color.FromArgb(0x20, 0x24, 0x27);
    }

    private bool TryBuildOptions(out AssistConnectOptions options, out string preview, out string error)
    {
        var perm = Enum.TryParse<PermissionLevel>(_permBox.SelectedItem?.ToString(), true, out var p)
            ? p
            : PermissionLevel.Assist;

        var input = _inputBox.Text ?? "";
        // 若是两行分享文案，提取纯会话码给解析器
        if (!input.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase) &&
            FriendConnectGuide.TryParseShareText(input, out var code, out var sharePwd))
        {
            input = code;
            if (!string.IsNullOrWhiteSpace(sharePwd) && string.IsNullOrWhiteSpace(_pwdBox.Text))
                _pwdBox.Text = sharePwd;
        }

        // 默认 preferRelay；空端口不要回落成展示 17890 的失败路径
        var portField = string.IsNullOrWhiteSpace(_portBox.Text) ? null : _portBox.Text;
        return ConnectDraftParser.TryParse(
            input,
            _hostBox.Text,
            portField,
            _fpBox.Text,
            perm,
            _insecureBox.Checked,
            _pwdBox.Text,
            _relayBox.Text,
            preferRelay: true,
            out options,
            out preview,
            out error);
    }

    private async Task ConnectAsync()
    {
        if (_busy) return;
        if (_client is not null)
        {
            SetStatus("已连接，请先断开。");
            return;
        }

        ClearFailHint();
        var raw = (_inputBox.Text ?? "").Trim();
        if (raw.Length == 0 && string.IsNullOrWhiteSpace(_pwdBox.Text))
        {
            SetFailHint("请粘贴会话码和验证密码");
            return;
        }

        if (!TryBuildOptions(out var opts, out var preview, out var err))
        {
            var tip = (err.Contains("密码", StringComparison.Ordinal))
                ? "请粘贴会话码和验证密码"
                : "请粘贴会话码和验证密码";
            SetFailHint(tip);
            return;
        }

        _busy = true;
        _btnConnect.Enabled = false;
        SetStatus("正在连接…");
        try
        {
            // 中继访客默认允许明文业务管道（指纹可在高级补）；避免亲友被 TLS 指纹拦住
            if (opts.ViaRelay && !opts.Insecure && string.IsNullOrWhiteSpace(opts.Fingerprint))
            {
                opts = new AssistConnectOptions
                {
                    Host = opts.Host,
                    Port = opts.Port,
                    Code = opts.Code,
                    Permission = opts.Permission,
                    Fingerprint = null,
                    Insecure = true,
                    ViaRelay = true,
                    RelayEndpoint = opts.RelayEndpoint,
                    Password = opts.Password
                };
            }

            var client = await AssistClient.PairAsync(opts, "helper-ui").ConfigureAwait(true);
            AttachClient(client);
            _settings.LastHost = opts.Host;
            _settings.LastPort = opts.Port;
            _settings.PreferRelay = true;
            _settings.Save();
            SetConnectedUi(true);
            SetStatus(HelperSessionCopy.StatusConnected);
            AppendOutput("PAIR_OK\r\n");
            ClearFailHint();
            _ = preview;
        }
        catch (Exception ex)
        {
            var friendly = FriendConnectGuide.FriendlyOrRaw(ex);
            SetStatus(HelperSessionCopy.StatusConnectFailed);
            SetFailHint(friendly);
            SetConnectedUi(false);
        }
        finally
        {
            _busy = false;
            if (_client is null)
                _btnConnect.Enabled = true;
        }
    }

    private void SetFailHint(string text) => _failHintLabel.Text = text;
    private void ClearFailHint() => _failHintLabel.Text = "";

    private void Disconnect()
    {
        _selfDisconnect = true;
        try
        {
            if (_client is not null)
                _client.ConnectionLost -= OnClientConnectionLost;
            _client?.Dispose();
        }
        catch { /* ignore */ }
        _client = null;
        _selfDisconnect = false;
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusDisconnectedSelf);
    }

    private void AttachClient(AssistClient client)
    {
        if (_client is not null)
        {
            try { _client.ConnectionLost -= OnClientConnectionLost; } catch { /* ignore */ }
            try { _client.Dispose(); } catch { /* ignore */ }
        }
        _client = client;
        _client.ConnectionLost += OnClientConnectionLost;
    }

    private void OnClientConnectionLost()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(OnClientConnectionLost); } catch { /* closing */ }
            return;
        }
        if (_selfDisconnect) return;
        if (_client is null) return;
        try { _client.ConnectionLost -= OnClientConnectionLost; } catch { /* ignore */ }
        try { _client.Dispose(); } catch { /* ignore */ }
        _client = null;
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusPeerEndedAssist);
        ClearFailHint();
    }

    private void SetConnectedUi(bool connected)
    {
        _btnConnect.Enabled = !connected;
        _btnConnect.Text = HelperSessionCopy.SessionConnectButtonLabel(connected);
        if (connected)
        {
            // R036：已连上 → 不可点「已连接」，非实心绿
            _btnConnect.BackColor = Color.FromArgb(0xE8, 0xEA, 0xED);
            _btnConnect.ForeColor = Color.FromArgb(0x9A, 0xA0, 0xA6);
            _btnConnect.Cursor = Cursors.Default;
        }
        else
        {
            _btnConnect.BackColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            _btnConnect.ForeColor = Color.White;
            _btnConnect.Cursor = Cursors.Hand;
        }
        _btnDisconnect.Enabled = connected;
        foreach (var b in _toolButtons) b.Enabled = connected;
        _inputBox.Enabled = !connected;
        if (_toolCard is not null)
        {
            _toolCard.Visible = connected;
            LayoutForAdvanced();
        }
        UpdateOwnConnectUi();
    }

    private void UpdateOwnConnectUi()
    {
        if (_btnOwnConnect is null || _ownSelectHint is null) return;
        if (_deviceList.SelectedItem is null)
        {
            _btnOwnConnect.Enabled = false;
            _btnOwnConnect.Text = "连接这台电脑";
            _ownSelectHint.Text = "请先点选一台自己的电脑";
            _ownSelectHint.Visible = true;
            return;
        }
        var row = _deviceList.SelectedItem.ToString() ?? "";
        var parts = row.Split('\t');
        var name = parts.Length > 1 ? parts[1] : parts[0];
        if (string.IsNullOrWhiteSpace(name)) name = "这台电脑";
        _btnOwnConnect.Enabled = _client is null;
        _btnOwnConnect.Text = "连接：" + name;
        _ownSelectHint.Text = "将免密连接「" + name + "」";
        _ownSelectHint.Visible = true;
    }

    private string? RequireArg(string key)
    {
        var v = _argBox.Text?.Trim() ?? "";
        if (v.Length == 0)
        {
            MessageBox.Show(this, $"请填写 {key}（上方「路径 / 命令参数」）", "缺少参数",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        return v;
    }

    private async Task RunToolAsync(ToolKind tool, Dictionary<string, string?> args)
    {
        if (_client is null || _busy) return;
        _busy = true;
        SetStatus($"执行 {tool} …");
        try
        {
            var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(true);
            if (!resp.Ok)
            {
                var msg = resp.Error ?? "失败";
                if (resp.DeniedByUser) msg = WriteConfirmCopy.DeniedMessage;
                SetStatus(msg);
                AppendOutput($"[{tool}] FAIL: {msg}\r\n");
                return;
            }
            SetStatus($"{tool} 成功");
            AppendOutput($"[{tool}] OK\r\n{resp.Result}\r\n");
        }
        catch (Exception ex)
        {
            SetStatus("调用失败：" + ex.Message);
            AppendOutput($"[{tool}] ERR: {ex.Message}\r\n");
        }
        finally { _busy = false; }
    }

    private void SaveRelaySetting()
    {
        var text = _relayBox.Text?.Trim() ?? "";
        if (text.Length > 0 && !RelayPlaceholder.TryParseEndpoint(text, out _, out _, out var err))
        {
            MessageBox.Show(this, err, "中继地址格式", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _settings.RelayEndpoint = string.IsNullOrEmpty(text) ? RelayEndpoint.DefaultEndpoint : text;
        _settings.PreferRelay = true;
        _settings.Save();
        SetStatus("已保存中继设置");
    }

    private void RequestHome()
    {
        if (!_enableReturnHome) return;
        if (_client is not null)
        {
            var r = MessageBox.Show(this,
                "当前已连接。返回首页将断开连接。是否继续？",
                "返回首页", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) return;
        }
        _returningHome = true;
        HomeRequested?.Invoke();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_enableReturnHome && !_returningHome && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            RequestHome();
            return;
        }
        Disconnect();
    }

    private async Task AccountAuthAsync(bool register)
    {
        if (!RelayEndpoint.TryParse(_relayBox.Text, out var host, out var port, out var err))
        {
            MessageBox.Show(this, err, "中继地址", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var user = _accountUserBox.Text?.Trim() ?? "";
        var pass = _accountPassBox.Text ?? "";
        if (user.Length == 0 || pass.Length == 0)
        {
            MessageBox.Show(this, "请填写用户名和密码", "账号", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var tok = await RelayClient.AuthAsync(host, port, register, user, pass).ConfigureAwait(true);
            _settings.AccountUser = user;
            _settings.AccountToken = tok;
            _settings.RelayEndpoint = $"{host}:{port}";
            _settings.Save();
            SetStatus((register ? "注册" : "登录") + "成功，已保存令牌");
            await RefreshDevicesAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, FriendConnectGuide.FriendlyOrRaw(ex), "账号失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshDevicesAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.AccountToken))
        {
            SetStatus("请先登录账号");
            return;
        }
        if (!RelayEndpoint.TryParse(_relayBox.Text, out var host, out var port, out var err))
        {
            MessageBox.Show(this, err, "中继地址", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            var list = await RelayClient.ListDevicesAsync(host, port, _settings.AccountToken!).ConfigureAwait(true);
            _deviceList.Items.Clear();
            foreach (var d in list)
                _deviceList.Items.Add($"{d.Id}\t{d.Name}\t{(d.Online ? "在线" : "离线")}");
            UpdateOwnConnectUi();
            SetStatus($"设备 {list.Count} 台");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, FriendConnectGuide.FriendlyOrRaw(ex), "设备列表失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ConnectOwnedDeviceAsync()
    {
        if (_client is not null) { SetStatus("已连接，请先断开"); return; }
        if (string.IsNullOrWhiteSpace(_settings.AccountToken))
        {
            MessageBox.Show(this, "请先登录账号", "免密连接", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_deviceList.SelectedItem is null)
        {
            MessageBox.Show(this, "请先点选一台自己的电脑", "免密连接", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var row = _deviceList.SelectedItem.ToString() ?? "";
        var deviceId = row.Split('\t')[0];
        if (!RelayEndpoint.TryParse(_relayBox.Text, out var host, out var port, out var err))
        {
            MessageBox.Show(this, err, "中继地址", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var fp = _fpBox.Text?.Trim();
        var insecure = _insecureBox.Checked || string.IsNullOrWhiteSpace(fp);
        _busy = true;
        try
        {
            var opts = new AssistConnectOptions
            {
                Host = host,
                Port = port,
                Code = "OWNER",
                Permission = PermissionLevel.Assist,
                Fingerprint = insecure ? null : TlsCertUtil.NormalizeFingerprint(fp),
                Insecure = insecure,
                ViaRelay = true,
                RelayEndpoint = $"{host}:{port}",
                OwnerLink = true,
                AccountToken = _settings.AccountToken,
                DeviceId = deviceId
            };
            var client = await AssistClient.PairAsync(opts, "helper-ui-owner").ConfigureAwait(true);
            AttachClient(client);
            SetConnectedUi(true);
            SetStatus("已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）");
        }
        catch (Exception ex)
        {
            SetFailHint(FriendConnectGuide.FriendlyOrRaw(ex));
            SetConnectedUi(false);
        }
        finally { _busy = false; }
    }

    private void SetStatus(string text) => _statusLabel.Text = text;

    private void AppendOutput(string text)
    {
        _outputBox.AppendText(text);
        if (_outputBox.TextLength > 200_000)
            _outputBox.Text = _outputBox.Text[^100_000..];
    }


    /// <summary>UiShot：已连上对方电脑 + 常用操作可见。</summary>
    internal void PreparePeerConnectedForShot()
    {
        SetConnectedUi(true);
        SetStatus(HelperSessionCopy.StatusConnected);
        ClearFailHint();
        _outputBox.Text = "PAIR_OK\r\n";
        LayoutForAdvanced();
    }

    /// <summary>UiShot：对方结束后主状态 + 五键收起。</summary>
    internal void PreparePeerEndedAssistForShot()
    {
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusPeerEndedAssist);
        ClearFailHint();
        LayoutForAdvanced();
    }

    /// <summary>UiShot：自己断开 — 原文「状态：未连接」。</summary>
    internal void PrepareSelfDisconnectedForShot()
    {
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusDisconnectedSelf);
        ClearFailHint();
        LayoutForAdvanced();
    }

    /// <summary>UiShot：尚未连上失败 — 「连接失败」+ R003 原因句。</summary>
    internal void PrepareConnectFailedForShot(string failHint)
    {
        SetConnectedUi(false);
        SetStatus(HelperSessionCopy.StatusConnectFailed);
        SetFailHint(failHint);
        LayoutForAdvanced();
    }

    /// <summary>UiShot：展开常用操作卡并写入输出区（不真连）。</summary>
    internal void PrepareConnectedToolsForShot(string outputText)
    {
        SetConnectedUi(true);
        _argBox.Text = "";
        _outputBox.Text = outputText ?? "";
        SetStatus("状态：已连接（截图）");
        LayoutForAdvanced();
    }

    /// <summary>UiShot：模拟「复制 MCP 配置」成功后的界面文案。</summary>
    internal void PrepareMcpCopiedForShot(bool exeExists = true)
    {
        var hint = exeExists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint;
        _previewLabel.Text = hint;
        _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
        SetFailHint("");
        SetStatus(hint);
    }

    /// <summary>UiShot：填入会话码/密码，走真实预览刷新。</summary>
    internal void PrepareSessionDraftForShot(string code, string password)
    {
        _inputBox.Text = code ?? "";
        _pwdBox.Text = password ?? "";
        RefreshPreview();
    }
}
