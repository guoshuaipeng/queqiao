using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;
using RemoteAssist.Relay.Stub;

namespace RemoteAssist.Helper.UI;

/// <summary>
/// 协助端主界面（R043）：粘贴连接凭证/载荷 + 连接；Host/Port/TLS 在高级折叠；默认公网中继。
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
    private readonly TextBox _relayBox;
    private readonly Button _btnConnect;
    private readonly Button _btnDisconnect;
    private readonly Button _btnAdvanced;
    private readonly Panel _advancedPanel;
    private readonly Panel _mcpCard;
    private readonly Panel _toolCard;
    private readonly Button[] _toolButtons;
    private bool _advancedOpen;

    private AssistClient? _client;
    private readonly HelperUiSettings _settings;
    private bool _busy;
    private readonly bool _enableReturnHome;
    private bool _returningHome;

    public event Action? HomeRequested;
    private bool _embeddedInShell;

    public MainForm(bool enableReturnHome = false)
    {
        _enableReturnHome = enableReturnHome;
        _settings = HelperUiSettings.Load();
        if (string.IsNullOrWhiteSpace(_settings.RelayEndpoint))
            _settings.RelayEndpoint = RelayEndpoint.DefaultEndpoint;
        _settings.PreferRelay = true;

        Text = "鹊桥 · 协助端";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(780, 780);
        BackColor = Color.FromArgb(0xF7, 0xF8, 0xFA);
        Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = AppIcons.CreateAppIcon();

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
            Text = FriendConnectGuide.PasteCredentialHint,
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
            btnHome.Click += (_, _) =>
            {
                if (!ConfirmEndSessionOrCancel(this))
                    return; // 取消：保持原会话原页
                _returningHome = true;
                HomeRequested?.Invoke();
            };
            header.Controls.Add(btnHome);
        }

        var pairCard = MakeCard(16, 80, 748, 220);
        pairCard.Controls.Add(SectionTitle("1. 连接", 12, 10));
        pairCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(520, 20),
            Text = FriendConnectGuide.PasteCredentialLabel,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        _inputBox = new TextBox
        {
            Location = new Point(16, 62),
            Size = new Size(600, 26),
            Font = new Font("Consolas", 9.5F)
        };
        _inputBox.TextChanged += (_, _) => RefreshPreview();
        pairCard.Controls.Add(_inputBox);

        var btnClip = MakeButton("从剪贴板", 626, 60, 100);
        btnClip.Click += (_, _) => PasteClipboard();
        pairCard.Controls.Add(btnClip);

        // R048：亲友向「复制安装说明」轻按钮（不抢「连接」主按钮）
        var btnInstallGuide = MakeButton("复制安装说明", 450, 36, 140);
        btnInstallGuide.FlatStyle = FlatStyle.Flat;
        btnInstallGuide.BackColor = Color.White;
        btnInstallGuide.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
        btnInstallGuide.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDC, 0xE0);
        btnInstallGuide.Click += (_, _) => CopyInstallInstructions();
        pairCard.Controls.Add(btnInstallGuide);

        _previewLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 100),
            Size = new Size(540, 28),
            Font = new Font("Consolas", 9F),
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            Text = FriendConnectGuide.PasteCredentialHint
        };
        pairCard.Controls.Add(_previewLabel);

        _btnConnect = MakeButton(FriendConnectGuide.ConnectButtonText, 566, 98, 80);
        _btnConnect.BackColor = Color.FromArgb(0x0F, 0x9D, 0x58);
        _btnConnect.ForeColor = Color.White;
        _btnConnect.FlatStyle = FlatStyle.Flat;
        _btnConnect.Click += async (_, _) => await ConnectAsync().ConfigureAwait(true);
        pairCard.Controls.Add(_btnConnect);

        _btnDisconnect = MakeButton("断开", 654, 98, 70);
        _btnDisconnect.Enabled = false;
        _btnDisconnect.Click += (_, _) => Disconnect();
        pairCard.Controls.Add(_btnDisconnect);

        _failHintLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 136),
            Size = new Size(710, 40),
            ForeColor = Color.FromArgb(0xC5, 0x22, 0x1F),
            Text = ""
        };
        pairCard.Controls.Add(_failHintLabel);

        _btnAdvanced = MakeButton("高级 ▸", 16, 180, 100);
        _btnAdvanced.Click += (_, _) => ToggleAdvanced();
        pairCard.Controls.Add(_btnAdvanced);

        _statusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(130, 182),
            Size = new Size(590, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "状态：未连接 · 经默认中继"
        };
        pairCard.Controls.Add(_statusLabel);
        Controls.Add(pairCard);

        _advancedPanel = MakeCard(16, 310, 748, 150);
        _advancedPanel.Visible = false;
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 10),
            Size = new Size(400, 22),
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            Text = "高级（局域网调试）"
        });
        _advancedPanel.Controls.Add(Lbl("主机", 12, 40));
        _hostBox = new TextBox
        {
            Location = new Point(56, 40),
            Size = new Size(140, 26),
            Font = new Font("Consolas", 9.5F),
            Text = _settings.LastHost ?? ""
        };
        _hostBox.TextChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_hostBox);

        _advancedPanel.Controls.Add(Lbl("端口", 210, 40));
        _portBox = new TextBox
        {
            Location = new Point(250, 40),
            Size = new Size(70, 26),
            Font = new Font("Consolas", 9.5F),
            Text = (_settings.LastPort ?? ConnectDraftParser.DefaultPort).ToString()
        };
        _portBox.TextChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_portBox);

        _advancedPanel.Controls.Add(Lbl("权限", 340, 40));
        _permBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(380, 40),
            Size = new Size(100, 26)
        };
        _permBox.Items.AddRange(new object[] { "Assist", "Guest", "Admin" });
        _permBox.SelectedIndex = 0;
        _advancedPanel.Controls.Add(_permBox);

        _insecureBox = new CheckBox
        {
            Location = new Point(500, 42),
            Size = new Size(220, 24),
            Text = "明文调试（仅内网）",
            ForeColor = Color.FromArgb(0xB0, 0x60, 0x00)
        };
        _insecureBox.CheckedChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_insecureBox);

        _advancedPanel.Controls.Add(Lbl("TLS指纹", 12, 78));
        _fpBox = new TextBox
        {
            Location = new Point(86, 76),
            Size = new Size(640, 26),
            Font = new Font("Consolas", 8.5F),
            Text = _settings.LastFingerprint ?? ""
        };
        _fpBox.TextChanged += (_, _) => RefreshPreview();
        _advancedPanel.Controls.Add(_fpBox);

        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(12, 112),
            Size = new Size(70, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "中继"
        });
        _relayBox = new TextBox
        {
            Location = new Point(86, 110),
            Size = new Size(280, 26),
            Font = new Font("Consolas", 9.5F),
            Text = _settings.RelayEndpoint ?? RelayEndpoint.DefaultEndpoint
        };
        _advancedPanel.Controls.Add(_relayBox);
        var btnSaveRelay = MakeButton("保存中继", 376, 108, 90);
        btnSaveRelay.Click += (_, _) => SaveRelaySetting();
        _advancedPanel.Controls.Add(btnSaveRelay);
        _advancedPanel.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(480, 112),
            Size = new Size(250, 24),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = "默认 " + RelayEndpoint.DefaultPublic
        });
        Controls.Add(_advancedPanel);

        // R048：MCP 安装卡主屏常显（不必等连上）；不自动对外监听 MCP
        _mcpCard = MakeCard(16, 310, 748, 168);
        _mcpCard.Controls.Add(SectionTitle(McpInstallGuide.SectionTitle, 12, 10));
        _mcpCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(710, 64),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = McpInstallGuide.StepsBlock
        });
        var btnCopyMcp = MakeButton(McpInstallGuide.CopyConfigButton, 16, 116, 140);
        btnCopyMcp.FlatStyle = FlatStyle.Flat;
        btnCopyMcp.BackColor = Color.White;
        btnCopyMcp.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
        btnCopyMcp.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDC, 0xE0);
        btnCopyMcp.Click += (_, _) => CopyMcpConfigFromUi();
        _mcpCard.Controls.Add(btnCopyMcp);
        var btnCopySkill = MakeButton(McpInstallGuide.CopySkillButton, 166, 116, 140);
        btnCopySkill.FlatStyle = FlatStyle.Flat;
        btnCopySkill.BackColor = Color.White;
        btnCopySkill.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
        btnCopySkill.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDC, 0xE0);
        btnCopySkill.Click += (_, _) => CopySkillInstructions();
        _mcpCard.Controls.Add(btnCopySkill);
        Controls.Add(_mcpCard);

        _toolCard = MakeCard(16, 490, 748, 300);
        _toolCard.Controls.Add(SectionTitle("2. 排障操作（连上后可用）", 12, 10));

        var btnProcs = MakeButton("进程列表", 16, 48, 100);
        var btnLs = MakeButton("列目录", 126, 48, 90);
        var btnLogs = MakeButton("读日志尾", 226, 48, 90);
        var btnCat = MakeButton("读文件", 326, 48, 90);
        var btnRun = MakeButton("跑命令", 426, 48, 90);
        _toolButtons = new[] { btnProcs, btnLs, btnLogs, btnCat, btnRun };
        foreach (var b in _toolButtons)
            _toolCard.Controls.Add(b);

        _toolCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 88),
            Size = new Size(200, 20),
            Text = "路径 / 命令参数",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        _argBox = new TextBox
        {
            Location = new Point(16, 110),
            Size = new Size(710, 26),
            Font = new Font("Consolas", 9.5F)
        };
        _toolCard.Controls.Add(_argBox);

        _outputBox = new TextBox
        {
            Location = new Point(16, 146),
            Size = new Size(710, 130),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 8.5F),
            BackColor = Color.White
        };
        _toolCard.Controls.Add(_outputBox);

        btnProcs.Click += async (_, _) => await RunToolAsync(ToolKind.ListProcesses, new Dictionary<string, string?>()).ConfigureAwait(true);
        btnLs.Click += async (_, _) =>
        {
            var path = RequireArg("path");
            if (path is null) return;
            await RunToolAsync(ToolKind.ListDir, new Dictionary<string, string?> { ["path"] = path }).ConfigureAwait(true);
        };
        btnLogs.Click += async (_, _) =>
        {
            var path = RequireArg("path");
            if (path is null) return;
            await RunToolAsync(ToolKind.ReadLogTail, new Dictionary<string, string?>
            {
                ["path"] = path,
                ["lines"] = "80"
            }).ConfigureAwait(true);
        };
        btnCat.Click += async (_, _) =>
        {
            var path = RequireArg("path");
            if (path is null) return;
            await RunToolAsync(ToolKind.ReadFile, new Dictionary<string, string?> { ["path"] = path }).ConfigureAwait(true);
        };
        btnRun.Click += async (_, _) =>
        {
            var cmd = RequireArg("command");
            if (cmd is null) return;
            await RunToolAsync(ToolKind.RunCommand, new Dictionary<string, string?> { ["command"] = cmd }).ConfigureAwait(true);
        };
        Controls.Add(_toolCard);

        FormClosing += OnFormClosing;
        LayoutPanels();
        RefreshPreview();
        SetConnectedUi(false);
    }

    private void ToggleAdvanced()
    {
        _advancedOpen = !_advancedOpen;
        _btnAdvanced.Text = _advancedOpen ? "高级 ▾" : "高级 ▸";
        LayoutPanels();
    }

    private void LayoutPanels()
    {
        _advancedPanel.Visible = _advancedOpen;
        // pairCard ends ~300; MCP 卡常显在 310
        _mcpCard.Top = 310;
        var top = 310 + _mcpCard.Height + 10;
        if (_advancedOpen)
        {
            _advancedPanel.Top = top;
            top = top + _advancedPanel.Height + 10;
        }
        _toolCard.Top = top;
        var need = top + _toolCard.Height + 24;
        if (ClientSize.Height < need)
            ClientSize = new Size(ClientSize.Width, Math.Min(need, 980));
    }

    private static Panel MakeCard(int x, int y, int w, int h) => new()
    {
        Location = new Point(x, y),
        Size = new Size(w, h),
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    private static Label SectionTitle(string text, int x, int y) => new()
    {
        AutoSize = false,
        Location = new Point(x, y),
        Size = new Size(400, 24),
        Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold),
        Text = text
    };

    private static Label Lbl(string text, int x, int y) => new()
    {
        AutoSize = false,
        Location = new Point(x, y),
        Size = new Size(70, 26),
        TextAlign = ContentAlignment.MiddleLeft,
        Text = text
    };

    private static Button MakeButton(string text, int x, int y, int w) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(w, 28),
        FlatStyle = FlatStyle.System
    };


    private void CopyInstallInstructions()
    {
        try
        {
            Clipboard.SetText(WriteConfirmCopy.InstallInstructionsClipboard);
            _previewLabel.Text = WriteConfirmCopy.InstallCopiedHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            ClearFailHint();
            SetStatus(WriteConfirmCopy.InstallCopiedHint);
        }
        catch (Exception ex)
        {
            SetFailHint("复制失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
            SetStatus("复制失败");
        }
    }

    /// <summary>
    /// R048：复制可粘贴的 MCP JSON。必须完整安全载荷（含业务 fp）；禁止缺指纹降明文。
    /// 不启动 MCP、不对外监听。
    /// </summary>
    private void CopyMcpConfigFromUi()
    {
        try
        {
            if (!TryBuildOptions(out var opts, out _, out var err))
            {
                SetFailHint(string.IsNullOrEmpty(err)
                    ? McpInstallGuide.NeedCredentialHint
                    : err);
                _previewLabel.ForeColor = Color.FromArgb(0xC5, 0x22, 0x1F);
                return;
            }

            // 禁止 Insecure / 缺 fp 生成「可连」明文 MCP 配置
            if (opts.Insecure || !TlsCertUtil.IsWellFormedFingerprint(opts.Fingerprint))
            {
                SetFailHint(FriendConnectGuide.MissingSecurityInfoHint);
                _previewLabel.ForeColor = Color.FromArgb(0xC5, 0x22, 0x1F);
                SetStatus("无法复制 MCP 配置");
                return;
            }

            string payload;
            if (opts.ViaRelay)
            {
                var relay = string.IsNullOrWhiteSpace(opts.RelayEndpoint)
                    ? RelayEndpoint.DefaultEndpoint
                    : opts.RelayEndpoint!;
                var pwd = opts.Password ?? "";
                if (string.IsNullOrWhiteSpace(pwd))
                {
                    SetFailHint(McpInstallGuide.NeedCredentialHint);
                    return;
                }
                payload = PairQrPayload.BuildRelay(
                    relay, opts.Code, pwd, opts.Fingerprint, useTls: true);
            }
            else
            {
                payload = PairQrPayload.BuildLan(
                    opts.Host, opts.Port, opts.Code, opts.Fingerprint, useTls: true);
            }

            var exists = McpExeLocator.TryResolveExisting(out var exe);
            if (!exists)
                exe = McpExeLocator.Resolve();

            Clipboard.SetText(McpConfigSnippet.BuildClipboardBundle(exe, payload));
            // 反馈不回显完整载荷
            _previewLabel.Text = exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            ClearFailHint();
            SetStatus(exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint);
        }
        catch (Exception ex)
        {
            SetFailHint("复制 MCP 配置失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
            SetStatus("复制失败");
        }
    }

    private void CopySkillInstructions()
    {
        try
        {
            Clipboard.SetText(McpInstallGuide.SkillInstructionsClipboard);
            _previewLabel.Text = McpInstallGuide.SkillCopiedHint;
            _previewLabel.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            ClearFailHint();
            SetStatus(McpInstallGuide.SkillCopiedHint);
        }
        catch (Exception ex)
        {
            SetFailHint("复制 Skill 说明失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
            SetStatus("复制失败");
        }
    }

    private void PasteClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                SetStatus("剪贴板没有文本。");
                return;
            }
            var text = Clipboard.GetText()?.Trim() ?? "";
            if (text.Length == 0)
            {
                SetStatus("剪贴板为空。");
                return;
            }
            _inputBox.Text = text;
            RefreshPreview();
            SetStatus("已从剪贴板粘贴。");
        }
        catch (Exception ex)
        {
            SetStatus("读取剪贴板失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
        }
    }

    private void RefreshPreview()
    {
        if (!TryBuildOptions(out _, out var preview, out var err))
        {
            _previewLabel.Text = string.IsNullOrEmpty(err) ? FriendConnectGuide.PasteCredentialHint : err;
            _previewLabel.ForeColor = string.IsNullOrEmpty(err)
                ? Color.FromArgb(0x5F, 0x63, 0x68)
                : Color.FromArgb(0xC5, 0x22, 0x1F);
            return;
        }
        _previewLabel.Text = preview;
        _previewLabel.ForeColor = Color.FromArgb(0x20, 0x24, 0x27);
    }

    private bool TryBuildOptions(out AssistConnectOptions options, out string preview, out string error)
    {
        var preferRelay = string.IsNullOrWhiteSpace(_hostBox.Text);
        return ConnectDraftParser.TryBuildOptions(
            paste: _inputBox.Text,
            passwordBox: null,
            relayEndpoint: string.IsNullOrWhiteSpace(_relayBox.Text) ? RelayEndpoint.DefaultEndpoint : _relayBox.Text.Trim(),
            preferRelay: preferRelay || _settings.PreferRelay,
            lanHost: _hostBox.Text,
            lanPortText: _portBox.Text,
            fingerprint: _fpBox.Text,
            insecure: _insecureBox.Checked,
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

        if (!TryBuildOptions(out var opts, out var preview, out var err))
        {
            SetFailHint(string.IsNullOrEmpty(err)
                ? FriendConnectGuide.NeedSecurePayloadHint
                : err);
            SetStatus("无法连接");
            return;
        }

        _busy = true;
        _btnConnect.Enabled = false;
        ClearFailHint();
        SetStatus("正在连接… " + preview);
        try
        {
            // R043 安全纠错：禁止公网主路径无指纹时静默降明文。须载荷含 fp= 或高级填写指纹；
            // 显式勾选「明文调试」才走 --insecure。短凭证 {码}-{验证码} 不含指纹 → 安全阻断并人话引导。
            if (opts.ViaRelay && !opts.Insecure && !TlsCertUtil.IsWellFormedFingerprint(opts.Fingerprint))
            {
                SetStatus("无法连接");
                SetFailHint(
                    FriendConnectGuide.MissingSecurityInfoHint
                    + Environment.NewLine
                    + "（仅本机调试可勾选「明文调试」，公网请勿使用。）");
                return;
            }

            var client = await AssistClient.PairAsync(opts, "helper-ui").ConfigureAwait(true);
            _client = client;
            _settings.LastHost = opts.Host;
            _settings.LastPort = opts.Port;
            _settings.PreferRelay = true;
            if (!string.IsNullOrWhiteSpace(opts.Fingerprint))
                _settings.LastFingerprint = opts.Fingerprint;
            _settings.Save();
            SetConnectedUi(true);
            SetStatus("已连接 · " + preview);
            AppendOutput("PAIR_OK perm=" + client.Permission + "\r\n");
        }
        catch (Exception ex)
        {
            var friendly = FriendConnectGuide.FriendlyOrRaw(ex);
            SetStatus("连接失败");
            SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly));
            SetConnectedUi(false);
        }
        finally
        {
            _busy = false;
            if (_client is null)
                _btnConnect.Enabled = true;
        }
    }

    private void Disconnect()
    {
        try { _client?.Dispose(); } catch { /* ignore */ }
        _client = null;
        SetConnectedUi(false);
        SetStatus("状态：未连接 · 经默认中继");
        ClearFailHint();
    }

    private void SetFailHint(string text) => _failHintLabel.Text = text;
    private void ClearFailHint() => _failHintLabel.Text = "";

    private void SetConnectedUi(bool connected)
    {
        _btnConnect.Enabled = !connected;
        _btnDisconnect.Enabled = connected;
        foreach (var b in _toolButtons)
            b.Enabled = connected;
        _inputBox.Enabled = !connected;
        _hostBox.Enabled = !connected;
        _portBox.Enabled = !connected;
        _fpBox.Enabled = !connected;
        _permBox.Enabled = !connected;
        _insecureBox.Enabled = !connected;
        _relayBox.Enabled = !connected;
    }

    private string? RequireArg(string key)
    {
        var v = _argBox.Text?.Trim() ?? "";
        if (v.Length == 0)
        {
            MessageBox.Show(this, "请填写 " + key + "（上方「路径 / 命令参数」）", "缺少参数",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        return v;
    }

    private async Task RunToolAsync(ToolKind tool, Dictionary<string, string?> args)
    {
        if (_client is null || _busy) return;
        _busy = true;
        SetStatus("执行 " + tool + " …");
        try
        {
            var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(true);
            if (!resp.Ok)
            {
                var msg = resp.Error ?? "失败";
                if (resp.DeniedByUser)
                    msg = "对方拒绝：" + msg;
                SetStatus(msg);
                AppendOutput("[" + tool + "] FAIL: " + msg + "\r\n");
                return;
            }
            SetStatus(tool + " 成功");
            AppendOutput("[" + tool + "] OK\r\n" + resp.Result + "\r\n");
        }
        catch (Exception ex)
        {
            SetStatus("调用失败：" + FriendConnectGuide.FriendlyOrRaw(ex));
            AppendOutput("[" + tool + "] ERR: " + FriendConnectGuide.RedactSecureMaterial(ex.Message) + "\r\n");
        }
        finally
        {
            _busy = false;
        }
    }

    private void SaveRelaySetting()
    {
        var text = _relayBox.Text?.Trim() ?? "";
        if (text.Length == 0)
            text = RelayEndpoint.DefaultEndpoint;
        if (!RelayPlaceholder.TryParseEndpoint(text, out _, out _, out var err))
        {
            MessageBox.Show(this, err, "中继地址", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _settings.RelayEndpoint = text;
        _settings.PreferRelay = true;
        _settings.Save();
        SetStatus("已保存中继：" + text);
        RefreshPreview();
    }

    private void AppendOutput(string text)
    {
        _outputBox.AppendText(text);
    }

    private void SetStatus(string text) => _statusLabel.Text = text;

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_returningHome)
            return;
        if (_embeddedInShell && e.CloseReason == CloseReason.UserClosing)
        {
            // 嵌入单主窗口：不关会话；由外壳「返回首页」确认
            e.Cancel = true;
            return;
        }
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // 独立 Helper 关闭即退出
        }
        try { _client?.Dispose(); } catch { /* ignore */ }
    }
}
