using System.Drawing;
using System.Windows.Forms;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;
using RemoteAssist.Relay.Stub;

namespace RemoteAssist.Helper.UI;

/// <summary>
/// 协助端主界面：粘贴会话载荷 / 配对向导 / 常用排障操作 / 中继设置占位。
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

    private AssistClient? _client;
    private readonly HelperUiSettings _settings;
    private bool _busy;
    private readonly bool _enableReturnHome;
    private bool _returningHome;

    /// <summary>统一 App 中请求返回模式选择页。</summary>
    public event Action? HomeRequested;

    public MainForm(bool enableReturnHome = false)
    {
        _enableReturnHome = enableReturnHome;
        _settings = HelperUiSettings.Load();

        Text = "远端协助 · 协助端";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(780, 860);
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
            Text = "远端协助 · 协助端",
            BackColor = Color.Transparent
        });
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 36),
            Size = new Size(_enableReturnHome ? 620 : 720, 22),
            ForeColor = Color.FromArgb(0xE8, 0xF5, 0xE9),
            Font = new Font(Font.FontFamily, 9F),
            Text = "输入对方会话码+验证密码（经中继）或粘贴载荷；也可登录账号免密控制自己的机器",
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

        var pairCard = MakeCard(16, 80, 748, 220);
        pairCard.Controls.Add(SectionTitle("1. 配对", 12, 10));

        pairCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(520, 20),
            Text = "会话载荷 / 会话码（中继：再填验证密码；推荐整段 remoteassist:pair?...）",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        _inputBox = new TextBox
        {
            Location = new Point(16, 62),
            Size = new Size(600, 26),
            Font = new Font("Consolas", 9.5F)
        };
        _inputBox.TextChanged += (_, _) => OnInputChanged();
        pairCard.Controls.Add(_inputBox);

        var btnClip = MakeButton("从剪贴板", 626, 60, 100);
        btnClip.Click += (_, _) => PasteClipboard();
        pairCard.Controls.Add(btnClip);

        pairCard.Controls.Add(Lbl("主机", 16, 100));
        _hostBox = new TextBox
        {
            Location = new Point(56, 98),
            Size = new Size(150, 26),
            Font = new Font("Consolas", 9.5F),
            Text = _settings.LastHost ?? ""
        };
        _hostBox.TextChanged += (_, _) => RefreshPreview();
        pairCard.Controls.Add(_hostBox);

        pairCard.Controls.Add(Lbl("端口", 220, 100));
        _portBox = new TextBox
        {
            Location = new Point(260, 98),
            Size = new Size(70, 26),
            Font = new Font("Consolas", 9.5F),
            Text = (_settings.LastPort ?? ConnectDraftParser.DefaultPort).ToString()
        };
        _portBox.TextChanged += (_, _) => RefreshPreview();
        pairCard.Controls.Add(_portBox);

        pairCard.Controls.Add(Lbl("密码", 350, 100));
        _pwdBox = new TextBox
        {
            Location = new Point(390, 98),
            Size = new Size(100, 26),
            Font = new Font("Consolas", 9.5F)
        };
        _pwdBox.TextChanged += (_, _) => RefreshPreview();
        pairCard.Controls.Add(_pwdBox);

        pairCard.Controls.Add(Lbl("权限", 500, 100));
        _permBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(540, 98),
            Size = new Size(90, 26)
        };
        _permBox.Items.AddRange(new object[] { "Assist", "Guest", "Admin" });
        _permBox.SelectedIndex = 0;
        _permBox.SelectedIndexChanged += (_, _) => RefreshPreview();
        pairCard.Controls.Add(_permBox);

        _insecureBox = new CheckBox
        {
            Location = new Point(640, 100),
            Size = new Size(100, 24),
            Text = "明文",
            ForeColor = Color.FromArgb(0xB0, 0x60, 0x00)
        };
        _insecureBox.CheckedChanged += (_, _) => RefreshPreview();
        pairCard.Controls.Add(_insecureBox);

        pairCard.Controls.Add(Lbl("TLS指纹", 16, 136));
        _fpBox = new TextBox
        {
            Location = new Point(86, 134),
            Size = new Size(640, 26),
            Font = new Font("Consolas", 8.5F)
        };
        _fpBox.TextChanged += (_, _) => RefreshPreview();
        pairCard.Controls.Add(_fpBox);

        _previewLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 170),
            Size = new Size(540, 36),
            Font = new Font("Consolas", 9F),
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            Text = "将连接：—"
        };
        pairCard.Controls.Add(_previewLabel);

        _btnConnect = MakeButton("连接", 566, 172, 80);
        _btnConnect.BackColor = Color.FromArgb(0x0F, 0x9D, 0x58);
        _btnConnect.ForeColor = Color.White;
        _btnConnect.FlatStyle = FlatStyle.Flat;
        _btnConnect.Click += async (_, _) => await ConnectAsync().ConfigureAwait(true);
        pairCard.Controls.Add(_btnConnect);

        _btnDisconnect = MakeButton("断开", 654, 172, 70);
        _btnDisconnect.Enabled = false;
        _btnDisconnect.Click += (_, _) => Disconnect();
        pairCard.Controls.Add(_btnDisconnect);

        Controls.Add(pairCard);

        var toolCard = MakeCard(16, 316, 748, 290);
        toolCard.Controls.Add(SectionTitle("2. 常用操作（连接后）", 12, 10));

        _statusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(710, 22),
            Text = "状态：未连接",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        };
        toolCard.Controls.Add(_statusLabel);

        var btnProcs = MakeButton("列进程", 16, 70, 90);
        var btnLs = MakeButton("列目录", 114, 70, 90);
        var btnLogs = MakeButton("读日志尾", 212, 70, 90);
        var btnCat = MakeButton("读文件", 310, 70, 90);
        var btnRun = MakeButton("执行命令", 408, 70, 100);
        btnRun.BackColor = Color.FromArgb(0xFB, 0xBC, 0x04);
        _toolButtons = new[] { btnProcs, btnLs, btnLogs, btnCat, btnRun };
        foreach (var b in _toolButtons)
        {
            b.Enabled = false;
            toolCard.Controls.Add(b);
        }

        toolCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 108),
            Size = new Size(200, 20),
            Text = "路径 / 命令参数",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        _argBox = new TextBox
        {
            Location = new Point(16, 130),
            Size = new Size(710, 26),
            Font = new Font("Consolas", 9.5F)
        };
        toolCard.Controls.Add(_argBox);

        _outputBox = new TextBox
        {
            Location = new Point(16, 164),
            Size = new Size(710, 110),
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

        Controls.Add(toolCard);

        var settingsCard = MakeCard(16, 622, 748, 220);
        settingsCard.Controls.Add(SectionTitle("3. 中继与账号", 12, 10));
        settingsCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 40),
            Size = new Size(100, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "中继服务器"
        });
        _relayBox = new TextBox
        {
            Location = new Point(116, 40),
            Size = new Size(280, 26),
            Font = new Font("Consolas", 9.5F),
            Text = _settings.RelayEndpoint ?? "",
            BackColor = Color.FromArgb(0xEE, 0xF0, 0xF3)
        };
        settingsCard.Controls.Add(_relayBox);
        settingsCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 72),
            Size = new Size(700, 20),
            Text = "账号（可选）：登录后可在设备列表免密控制自己的被控机",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });
        _accountUserBox = new TextBox { Location = new Point(16, 96), Size = new Size(140, 26), Text = _settings.AccountUser ?? "" };
        _accountPassBox = new TextBox { Location = new Point(164, 96), Size = new Size(140, 26), UseSystemPasswordChar = true };
        settingsCard.Controls.Add(_accountUserBox);
        settingsCard.Controls.Add(_accountPassBox);
        var btnReg = MakeButton("注册", 314, 94, 60);
        btnReg.Click += async (_, _) => await AccountAuthAsync(register: true).ConfigureAwait(true);
        var btnLogin = MakeButton("登录", 380, 94, 60);
        btnLogin.Click += async (_, _) => await AccountAuthAsync(register: false).ConfigureAwait(true);
        var btnList = MakeButton("刷新设备", 446, 94, 80);
        btnList.Click += async (_, _) => await RefreshDevicesAsync().ConfigureAwait(true);
        var btnOwn = MakeButton("免密连接选中", 532, 94, 120);
        btnOwn.Click += async (_, _) => await ConnectOwnedDeviceAsync().ConfigureAwait(true);
        settingsCard.Controls.Add(btnReg);
        settingsCard.Controls.Add(btnLogin);
        settingsCard.Controls.Add(btnList);
        settingsCard.Controls.Add(btnOwn);
        _deviceList = new ListBox { Location = new Point(16, 128), Size = new Size(700, 70) };
        settingsCard.Controls.Add(_deviceList);


        var btnSaveRelay = MakeButton("保存", 406, 38, 70);
        btnSaveRelay.Click += (_, _) => SaveRelaySetting();
        settingsCard.Controls.Add(btnSaveRelay);

        settingsCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(490, 40),
            Size = new Size(240, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0xB0, 0x60, 0x00),
            Text = "即将支持（可用纯 IP）"
        });

        settingsCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 72),
            Size = new Size(710, 28),
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = RelayPlaceholder.UpcomingHint
        });

        Controls.Add(settingsCard);

        FormClosing += OnFormClosing;

        RefreshPreview();
        SetConnectedUi(false);
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
        Size = new Size(52, 26),
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
            OnInputChanged();
            SetStatus("已从剪贴板粘贴。");
        }
        catch (Exception ex)
        {
            SetStatus("读取剪贴板失败：" + ex.Message);
        }
    }

    private void OnInputChanged()
    {
        if (PairQrPayload.TryParseFull(_inputBox.Text, out var host, out var port, out _, out var fp, out var useTls, out var viaRelay, out var relayEp, out var pwd))
        {
            _hostBox.Text = host;
            _portBox.Text = port.ToString();
            if (!string.IsNullOrEmpty(fp))
                _fpBox.Text = fp;
            _insecureBox.Checked = !useTls;
                if (!string.IsNullOrWhiteSpace(pwd)) _pwdBox.Text = pwd;
                if (!string.IsNullOrWhiteSpace(relayEp)) _relayBox.Text = relayEp;
                if (viaRelay) { /* host shows relay host */ }
        }

        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (!TryBuildOptions(out _, out var preview, out var err))
        {
            _previewLabel.Text = string.IsNullOrEmpty(err) ? "将连接：—" : "无法连接：" + err;
            _previewLabel.ForeColor = Color.FromArgb(0xC5, 0x22, 0x1F);
            return;
        }

        _previewLabel.Text = preview;
        _previewLabel.ForeColor = Color.FromArgb(0x20, 0x24, 0x27);
    }

    private bool TryBuildOptions(out AssistConnectOptions options, out string preview, out string error)
    {
        var perm = Enum.TryParse<PermissionLevel>(_permBox.SelectedItem?.ToString(), true, out var p)
            ? p
            : PermissionLevel.Assist;

        return ConnectDraftParser.TryParse(
            _inputBox.Text,
            _hostBox.Text,
            _portBox.Text,
            _fpBox.Text,
            perm,
            _insecureBox.Checked,
            _pwdBox.Text,
            _relayBox.Text,
            preferRelay: _settings.PreferRelay,
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
            MessageBox.Show(this, err, "无法连接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _busy = true;
        _btnConnect.Enabled = false;
        SetStatus("正在连接… " + preview);
        try
        {
            var client = await AssistClient.PairAsync(opts, "helper-ui").ConfigureAwait(true);
            _client = client;
            _settings.LastHost = opts.Host;
            _settings.LastPort = opts.Port;
            _settings.Save();
            SetConnectedUi(true);
            var tokenPreview = client.Token is { Length: >= 8 } t ? t[..8] : (client.Token ?? "");
            SetStatus($"已连接 {opts.Host}:{opts.Port} · 权限={client.Permission} · token={tokenPreview}…");
            AppendOutput($"PAIR_OK perm={client.Permission}\r\n");
        }
        catch (Exception ex)
        {
            SetStatus("连接失败：" + ex.Message);
            MessageBox.Show(this, ex.Message, "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        SetStatus("状态：未连接");
    }

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
                if (resp.DeniedByUser)
                    msg = "对方拒绝：" + msg;
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
        finally
        {
            _busy = false;
        }
    }

    private void SaveRelaySetting()
    {
        var text = _relayBox.Text?.Trim() ?? "";
        if (text.Length > 0 && !RelayPlaceholder.TryParseEndpoint(text, out _, out _, out var err))
        {
            MessageBox.Show(this,
                err + "\r\n\r\n仍可留空；填写仅为以后公网中继预留。",
                "中继地址格式", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _settings.RelayEndpoint = string.IsNullOrEmpty(text) ? RelayEndpoint.DefaultEndpoint : text;
        _settings.PreferRelay = true;
        _settings.Save();
        SetStatus($"已保存中继：{_settings.RelayEndpoint}（默认经中继连接）");
    }


    private void RequestHome()
    {
        if (!_enableReturnHome) return;
        if (_client is not null)
        {
            var r = MessageBox.Show(this,
                "当前已连接被控端。返回首页将断开连接。是否继续？",
                "返回首页",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) return;
        }

        _returningHome = true;
        HomeRequested?.Invoke();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_enableReturnHome && !_returningHome && e.CloseReason == CloseReason.UserClosing)
        {
            // 统一 App：关窗 = 回首页，而不是退出进程
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
            MessageBox.Show(this, ex.Message, "账号失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            SetStatus($"设备 {list.Count} 台");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "设备列表失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ConnectOwnedDeviceAsync()
    {
        if (_client is not null) { SetStatus("已连接，请先断开"); return; }
        if (_deviceList.SelectedItem is null || string.IsNullOrWhiteSpace(_settings.AccountToken))
        {
            MessageBox.Show(this, "请先登录并选中一台设备", "免密连接", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        var insecure = _insecureBox.Checked;
        if (!insecure && string.IsNullOrWhiteSpace(fp))
        {
            MessageBox.Show(this, "免密连接需要 TLS 指纹（可先用 code+密码连一次自动回填），或勾选明文调试", "免密连接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
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
            _client = client;
            SetConnectedUi(true);
            SetStatus($"已免密连接设备 {deviceId} · 权限={client.Permission}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "免密连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
}

