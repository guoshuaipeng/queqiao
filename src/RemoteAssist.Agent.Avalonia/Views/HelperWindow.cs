using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Agent;

/// <summary>
/// 协助端（R011 / R014）：粘贴会话码连接；或账号登录 → 自有设备列表 → 免密连接。
/// 亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号。
/// </summary>
internal sealed class HelperWindow : Window
{
    private readonly bool _shotMode;
    private readonly bool _enableReturnHome;
    private readonly HelperUiSettings _settings;
    private readonly TextBox _pasteBox;
    private readonly Button _btnConnect;
    private readonly TextBlock _status;
    private readonly TextBlock _failHint;
    private readonly TextBlock _preview;
    private readonly Border _toolCard;
    private readonly TextBox _output;
    private readonly Button _btnProcs;
    private readonly Button _btnLogs;
    private readonly Button _btnRun;

    private readonly TextBox _accountUserBox;
    private readonly TextBox _accountPassBox;
    private readonly ListBox _deviceList;
    private readonly Button _btnOwnConnect;
    private readonly TextBlock _ownSelectHint;
    private readonly TextBlock _accountStatus;

    private AssistClient? _client;
    private bool _busy;
    private bool _allowClose;

    public HelperWindow(string? shotDir, bool enableReturnHome = true)
    {
        _shotMode = shotDir is not null;
        _enableReturnHome = enableReturnHome;
        _settings = HelperUiSettings.Load();

        Title = "鹊桥远程协助";
        Width = 720;
        Height = 720;
        MinWidth = 640;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brush.Parse("#F7F8FA");

        var root = new DockPanel();

        var header = new Border
        {
            Background = Brush.Parse("#0F9D58"),
            Height = 64,
            Padding = new Thickness(20, 12, 16, 12)
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        headerGrid.Children.Add(new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "鹊桥远程协助 · 去协助别人",
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = "粘贴对方发来的会话码和验证密码，或登录账号免密控制自己的机器",
                    FontSize = 12,
                    Foreground = Brush.Parse("#E6F4EA")
                }
            }
        });
        if (_enableReturnHome)
        {
            var homeBtn = new Button
            {
                Content = "返回首页",
                Width = 100,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brush.Parse("#0B7A45"),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };
            homeBtn.Click += (_, _) => RequestHome();
            Grid.SetColumn(homeBtn, 1);
            headerGrid.Children.Add(homeBtn);
        }
        header.Child = headerGrid;
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var body = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12
        };

        var pairCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };
        var pairStack = new StackPanel { Spacing = 10 };
        pairStack.Children.Add(new TextBlock
        {
            Text = "连接对方",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        });
        pairStack.Children.Add(new TextBlock
        {
            Text = "粘贴对方发来的会话码和验证密码",
            Foreground = Brush.Parse("#5F6368")
        });

        _pasteBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 72,
            Watermark = "会话码：……\n验证密码：……"
        };
        _pasteBox.TextChanged += (_, _) => RefreshPreview();
        pairStack.Children.Add(_pasteBox);

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };
        var clipBtn = new Button { Content = "从剪贴板", Width = 100, Height = 36 };
        clipBtn.Click += async (_, _) => await PasteFromClipboardAsync();
        _btnConnect = new Button
        {
            Content = "连接",
            Width = 120,
            Height = 36,
            FontWeight = FontWeight.Bold,
            Background = Brush.Parse("#0F9D58"),
            Foreground = Brushes.White
        };
        _btnConnect.Click += async (_, _) => await ConnectAsync();
        row.Children.Add(clipBtn);
        row.Children.Add(_btnConnect);
        pairStack.Children.Add(row);

        _preview = new TextBlock
        {
            Text = "粘贴后点「连接」",
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        };
        pairStack.Children.Add(_preview);

        _status = new TextBlock
        {
            Text = "状态：未连接",
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        };
        pairStack.Children.Add(_status);

        _failHint = new TextBlock
        {
            Text = "",
            Foreground = Brush.Parse("#D93025"),
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.SemiBold
        };
        pairStack.Children.Add(_failHint);

        pairCard.Child = pairStack;
        body.Children.Add(pairCard);

        // ---- R014 账号（可选）：权重低于粘贴区 ----
        var accountCard = new Border
        {
            Name = "accountCard",
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };
        var accountStack = new StackPanel { Spacing = 8 };
        accountStack.Children.Add(new TextBlock
        {
            Name = "accountSectionTitle",
            Text = "账号（可选）",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        });
        accountStack.Children.Add(new TextBlock
        {
            Name = "accountSectionBlurb",
            Text = "登录后免密连自己的机器（无需再确认配对；写操作仍须对方本机确认）",
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        });

        var credRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        credRow.Children.Add(new TextBlock
        {
            Name = "accountUserLabel",
            Text = "账号",
            VerticalAlignment = VerticalAlignment.Center
        });
        _accountUserBox = new TextBox
        {
            Name = "accountUserBox",
            Text = _settings.AccountUser ?? "",
            Width = 140
        };
        credRow.Children.Add(_accountUserBox);
        credRow.Children.Add(new TextBlock
        {
            Name = "accountPassLabel",
            Text = "密码",
            VerticalAlignment = VerticalAlignment.Center
        });
        _accountPassBox = new TextBox
        {
            Name = "accountPassBox",
            PasswordChar = '●',
            Width = 140
        };
        credRow.Children.Add(_accountPassBox);
        var btnReg = new Button { Name = "accountRegisterBtn", Content = "注册", Width = 60, Height = 32 };
        btnReg.Click += async (_, _) => await AccountAuthAsync(register: true);
        credRow.Children.Add(btnReg);
        var btnLogin = new Button { Name = "accountLoginBtn", Content = "登录", Width = 60, Height = 32 };
        btnLogin.Click += async (_, _) => await AccountAuthAsync(register: false);
        credRow.Children.Add(btnLogin);
        var btnList = new Button { Name = "accountRefreshDevicesBtn", Content = "刷新设备", Width = 80, Height = 32 };
        btnList.Click += async (_, _) => await RefreshDevicesAsync();
        credRow.Children.Add(btnList);
        accountStack.Children.Add(credRow);

        _deviceList = new ListBox
        {
            Name = "ownDeviceList",
            Height = 88,
            MinHeight = 72
        };
        _deviceList.SelectionChanged += (_, _) => UpdateOwnConnectUi();
        accountStack.Children.Add(_deviceList);

        var ownRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        _btnOwnConnect = new Button
        {
            Name = "ownConnectBtn",
            Content = "连接这台电脑",
            Width = 160,
            Height = 36,
            IsEnabled = false
        };
        _btnOwnConnect.Click += async (_, _) => await ConnectOwnedDeviceAsync();
        ownRow.Children.Add(_btnOwnConnect);
        _ownSelectHint = new TextBlock
        {
            Name = "ownSelectHint",
            Text = "请先点选一台自己的电脑",
            Foreground = Brush.Parse("#5F6368"),
            VerticalAlignment = VerticalAlignment.Center
        };
        ownRow.Children.Add(_ownSelectHint);
        accountStack.Children.Add(ownRow);

        _accountStatus = new TextBlock
        {
            Name = "accountStatus",
            Text = string.IsNullOrWhiteSpace(_settings.AccountToken) ? "尚未登录账号" : "已保存登录，可刷新设备",
            Foreground = Brush.Parse("#5F6368"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        accountStack.Children.Add(_accountStatus);

        accountCard.Child = accountStack;
        body.Children.Add(accountCard);

        _toolCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            IsVisible = false
        };
        var toolStack = new StackPanel { Spacing = 10 };
        toolStack.Children.Add(new TextBlock
        {
            Text = "常用操作",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        });
        var tools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        _btnProcs = MakeToolButton("查看正在运行的程序", 180);
        _btnProcs.Click += async (_, _) => await RunToolAsync(ToolKind.ListProcesses, new Dictionary<string, string?>());
        _btnLogs = MakeToolButton("查看日志", 100);
        _btnLogs.Click += async (_, _) =>
        {
            var pathLog = OperatingSystem.IsWindows() ? @"C:\Windows\WindowsUpdate.log" : "/tmp/r011-demo.log";
            await RunToolAsync(ToolKind.ReadLogTail, new Dictionary<string, string?>
            {
                ["path"] = pathLog,
                ["lines"] = "80"
            });
        };
        _btnRun = MakeToolButton("运行一条命令", 120);
        _btnRun.Click += async (_, _) =>
        {
            var cmd = OperatingSystem.IsWindows()
                ? "echo r014-should-not-run"
                : "echo r014-should-not-run >/tmp/r014-deny-marker";
            await RunToolAsync(ToolKind.RunCommand, new Dictionary<string, string?> { ["command"] = cmd });
        };
        tools.Children.Add(_btnProcs);
        tools.Children.Add(_btnLogs);
        tools.Children.Add(_btnRun);
        toolStack.Children.Add(tools);

        _output = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 120,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas,Menlo,monospace"),
            FontSize = 12
        };
        toolStack.Children.Add(_output);
        _toolCard.Child = toolStack;
        body.Children.Add(_toolCard);

        root.Children.Add(new ScrollViewer { Content = body });
        Content = root;

        Closing += OnClosing;
        UpdateOwnConnectUi();
    }

    private static Button MakeToolButton(string text, double width) => new()
    {
        Content = text,
        Width = width,
        Height = 36
    };

    public void PrepareClose()
    {
        _allowClose = true;
        Disconnect();
    }

    private void RequestHome()
    {
        if (_client is not null)
            Disconnect();
        _allowClose = true;
        AppNavigator.ShowHome();
        try { Close(); } catch { /* ignore */ }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || _shotMode)
        {
            Disconnect();
            return;
        }
        e.Cancel = true;
        RequestHome();
        await Task.CompletedTask;
    }

    private async Task PasteFromClipboardAsync()
    {
        try
        {
            var clip = Clipboard;
            string? text = null;
            if (clip is not null) text = await clip.GetTextAsync().ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(text))
            {
                _failHint.Text = "请粘贴会话码和验证密码";
                return;
            }
            _pasteBox.Text = text.Trim();
            _status.Text = "已粘贴会话信息。";
            _failHint.Text = "";
            RefreshPreview();
        }
        catch
        {
            _failHint.Text = "请粘贴会话码和验证密码";
        }
    }

    private void RefreshPreview()
    {
        var raw = (_pasteBox.Text ?? "").Trim();
        if (raw.Length == 0)
        {
            _preview.Text = "粘贴后点「连接」";
            return;
        }

        if (!TryBuildOptions(out var opts, out _, out _))
        {
            _preview.Text = "请粘贴会话码和验证密码";
            return;
        }

        _preview.Text = FriendConnectGuide.FriendConnectionPreview(opts.Code);
    }

    private bool TryBuildOptions(out AssistConnectOptions options, out string preview, out string error)
    {
        options = null!;
        preview = "";
        error = "";
        var raw = (_pasteBox.Text ?? "").Trim();
        if (raw.Length == 0)
        {
            error = "请粘贴会话码和验证密码";
            return false;
        }

        string code;
        string? password = null;
        if (FriendConnectGuide.TryParseShareText(raw, out code, out password))
        {
            // ok
        }
        else if (raw.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ConnectDraftParser.TryParse(
                raw, hostField: null, portField: null, fingerprintField: null,
                PermissionLevel.Assist, insecure: false,
                passwordField: null, relayField: RelayEndpoint.DefaultEndpoint, preferRelay: true,
                out options, out preview, out error);
        }
        else
        {
            error = "请粘贴会话码和验证密码";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            error = "请粘贴会话码和验证密码";
            return false;
        }

        return ConnectDraftParser.TryParse(
            code,
            hostField: null,
            portField: null,
            fingerprintField: null,
            PermissionLevel.Assist,
            insecure: true,
            passwordField: password,
            relayField: RelayEndpoint.DefaultEndpoint,
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
            _status.Text = "已连接，请先断开。";
            return;
        }

        _failHint.Text = "";
        var raw = (_pasteBox.Text ?? "").Trim();
        if (raw.Length == 0)
        {
            _failHint.Text = "请粘贴会话码和验证密码";
            return;
        }

        if (!TryBuildOptions(out var opts, out var preview, out _))
        {
            _failHint.Text = "请粘贴会话码和验证密码";
            return;
        }

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

        _busy = true;
        _btnConnect.IsEnabled = false;
        _status.Text = "正在连接…";
        try
        {
            if (_shotMode)
            {
                SetConnectedUi(true);
                _status.Text = "已连接对方电脑";
                _output.Text = "";
                return;
            }

            var client = await AssistClient.PairAsync(opts, "helper-avalonia").ConfigureAwait(true);
            _client = client;
            SetConnectedUi(true);
            _status.Text = "已连接对方电脑";
            _failHint.Text = "";
            AppendOutput("PAIR_OK\n");
            _ = preview;
        }
        catch (Exception ex)
        {
            var friendly = FriendConnectGuide.FriendlyOrRaw(ex);
            _status.Text = "连接失败";
            _failHint.Text = friendly;
            SetConnectedUi(false);
        }
        finally
        {
            _busy = false;
            if (_client is null)
                _btnConnect.IsEnabled = true;
        }
    }

    private void SetConnectedUi(bool connected)
    {
        _btnConnect.IsEnabled = !connected;
        _pasteBox.IsEnabled = !connected;
        _toolCard.IsVisible = connected;
        UpdateOwnConnectUi();
    }

    private void Disconnect()
    {
        try { _client?.Dispose(); } catch { /* ignore */ }
        _client = null;
        SetConnectedUi(false);
        _status.Text = "状态：未连接";
    }

    private void UpdateOwnConnectUi()
    {
        if (_deviceList.SelectedItem is null)
        {
            _btnOwnConnect.IsEnabled = false;
            _btnOwnConnect.Content = "连接这台电脑";
            _ownSelectHint.Text = "请先点选一台自己的电脑";
            return;
        }

        var row = _deviceList.SelectedItem.ToString() ?? "";
        var parts = row.Split('\t');
        var name = parts.Length > 1 ? parts[1] : parts[0];
        if (string.IsNullOrWhiteSpace(name)) name = "这台电脑";
        _btnOwnConnect.IsEnabled = _client is null;
        _btnOwnConnect.Content = "连接：" + name;
        _ownSelectHint.Text = "将免密连接「" + name + "」";
    }

    private string RelayEndpointText =>
        string.IsNullOrWhiteSpace(_settings.RelayEndpoint)
            ? RelayEndpoint.DefaultEndpoint
            : _settings.RelayEndpoint!;

    private async Task AccountAuthAsync(bool register)
    {
        if (!RelayEndpoint.TryParse(RelayEndpointText, out var host, out var port, out var err))
        {
            _accountStatus.Text = err;
            return;
        }
        var user = _accountUserBox.Text?.Trim() ?? "";
        var pass = _accountPassBox.Text ?? "";
        if (user.Length == 0 || pass.Length == 0)
        {
            _accountStatus.Text = "请填写用户名和密码";
            return;
        }

        if (_shotMode)
        {
            _settings.AccountUser = user;
            _settings.AccountToken = "shot-token";
            _settings.Save();
            _accountStatus.Text = (register ? "注册" : "登录") + "成功（截图）";
            PrepareDevicesForShot(selected: false);
            return;
        }

        try
        {
            var tok = await RelayClient.AuthAsync(host, port, register, user, pass).ConfigureAwait(true);
            _settings.AccountUser = user;
            _settings.AccountToken = tok;
            _settings.RelayEndpoint = $"{host}:{port}";
            _settings.Save();
            _accountStatus.Text = (register ? "注册" : "登录") + "成功，已保存";
            // 被控端若已开，可 BindAccount；此处写 settings，接受协助创建时也会带上
            try { Program.SharedHost?.BindAccount(tok); } catch { /* ignore */ }
            await RefreshDevicesAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _accountStatus.Text = FriendConnectGuide.FriendlyOrRaw(ex);
        }
    }

    private async Task RefreshDevicesAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.AccountToken))
        {
            _accountStatus.Text = "请先登录账号";
            return;
        }
        if (!RelayEndpoint.TryParse(RelayEndpointText, out var host, out var port, out var err))
        {
            _accountStatus.Text = err;
            return;
        }

        if (_shotMode)
        {
            PrepareDevicesForShot(selected: false);
            return;
        }

        try
        {
            var list = await RelayClient.ListDevicesAsync(host, port, _settings.AccountToken!).ConfigureAwait(true);
            _deviceList.Items.Clear();
            foreach (var d in list)
                _deviceList.Items.Add($"{d.Id}\t{d.Name}\t{(d.Online ? "在线" : "离线")}");
            UpdateOwnConnectUi();
            _accountStatus.Text = $"设备 {list.Count} 台";
        }
        catch (Exception ex)
        {
            _accountStatus.Text = FriendConnectGuide.FriendlyOrRaw(ex);
        }
    }

    private async Task ConnectOwnedDeviceAsync()
    {
        if (_client is not null)
        {
            _status.Text = "已连接，请先断开";
            return;
        }
        if (string.IsNullOrWhiteSpace(_settings.AccountToken))
        {
            _accountStatus.Text = "请先登录账号";
            return;
        }
        if (_deviceList.SelectedItem is null)
        {
            _accountStatus.Text = "请先点选一台自己的电脑";
            return;
        }

        var row = _deviceList.SelectedItem.ToString() ?? "";
        var deviceId = row.Split('\t')[0];
        if (!RelayEndpoint.TryParse(RelayEndpointText, out var host, out var port, out var err))
        {
            _accountStatus.Text = err;
            return;
        }

        if (_shotMode)
        {
            SetConnectedUi(true);
            _status.Text = "已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）";
            _accountStatus.Text = "免密连接（截图）";
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
                Fingerprint = null,
                Insecure = true,
                ViaRelay = true,
                RelayEndpoint = $"{host}:{port}",
                OwnerLink = true,
                AccountToken = _settings.AccountToken,
                DeviceId = deviceId
            };
            var client = await AssistClient.PairAsync(opts, "helper-avalonia-owner").ConfigureAwait(true);
            _client = client;
            SetConnectedUi(true);
            _status.Text = "已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）";
            _failHint.Text = "";
        }
        catch (Exception ex)
        {
            _status.Text = "连接失败";
            _failHint.Text = FriendConnectGuide.FriendlyOrRaw(ex);
            SetConnectedUi(false);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RunToolAsync(ToolKind tool, Dictionary<string, string?> args)
    {
        if (_busy) return;
        if (_client is null)
        {
            _status.Text = "状态：未连接";
            return;
        }

        _busy = true;
        try
        {
            var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(true);
            if (!resp.Ok)
            {
                var msg = resp.Error ?? "失败";
                if (resp.DeniedByUser)
                    msg = WriteConfirmCopy.DeniedMessage;
                _status.Text = msg;
                AppendOutput($"[{tool}] FAIL: {msg}\n");
                return;
            }

            _status.Text = tool switch
            {
                ToolKind.ListProcesses => "已查看正在运行的程序",
                ToolKind.ReadLogTail => "已查看日志",
                _ => "完成"
            };
            AppendOutput($"[{tool}] OK\n{resp.Result}\n");
        }
        catch (Exception ex)
        {
            _status.Text = "调用失败：" + ex.Message;
            AppendOutput($"[{tool}] ERR: {ex.Message}\n");
        }
        finally
        {
            _busy = false;
        }
    }

    private void AppendOutput(string text)
    {
        _output.Text = (_output.Text ?? "") + text;
        if ((_output.Text?.Length ?? 0) > 200_000)
            _output.Text = _output.Text![^100_000..];
    }

    /// <summary>UiShot：未连接空态。</summary>
    internal void PrepareIdleForShot()
    {
        _pasteBox.Text = "";
        SetConnectedUi(false);
        _status.Text = "状态：未连接";
        _preview.Text = "粘贴后点「连接」";
        _failHint.Text = "";
    }

    /// <summary>UiShot：填入粘贴文案。</summary>
    internal void PreparePasteForShot(string shareText)
    {
        _pasteBox.Text = shareText;
        RefreshPreview();
        SetConnectedUi(false);
        _status.Text = "状态：未连接";
        _failHint.Text = "";
    }

    /// <summary>UiShot：连不上引导（真实控件上的失败原文）。</summary>
    internal void PrepareFailForShot(string failMessage)
    {
        SetConnectedUi(false);
        _status.Text = "连接失败";
        _failHint.Text = failMessage;
    }

    /// <summary>UiShot：已连接 + 只读结果。</summary>
    internal void PrepareConnectedReadonlyForShot(string output)
    {
        SetConnectedUi(true);
        _status.Text = "已连接对方电脑";
        _failHint.Text = "";
        _output.Text = output;
    }

    /// <summary>UiShot：对方拒绝写操作后的协助端提示。</summary>
    internal void PrepareDeniedForShot()
    {
        SetConnectedUi(true);
        _status.Text = WriteConfirmCopy.DeniedMessage;
        _failHint.Text = "";
        AppendOutput($"[RunCommand] FAIL: {WriteConfirmCopy.DeniedMessage}\n");
    }


    /// <summary>UiShot：仅展示账号/密码可见标签（设备列表空，便于与未选中截图区分）。</summary>
    internal void PrepareAccountLabelsOnlyForShot()
    {
        _accountUserBox.Text = "";
        _accountPassBox.Text = "";
        _accountStatus.Text = "请填写账号和密码后登录";
        _deviceList.Items.Clear();
        _deviceList.SelectedIndex = -1;
        UpdateOwnConnectUi();
    }

    /// <summary>UiShot：账号标签 + 设备列表；selected 控制是否点选。</summary>
    internal void PrepareAccountForShot(bool selected)
    {
        _accountUserBox.Text = "demo";
        _accountPassBox.Text = "••••••";
        _accountStatus.Text = "已登录（截图）";
        PrepareDevicesForShot(selected);
    }

    internal void PrepareDevicesForShot(bool selected)
    {
        _deviceList.Items.Clear();
        _deviceList.Items.Add("dev-living\t客厅电脑\t在线");
        _deviceList.Items.Add("dev-office\t书房电脑\t离线");
        if (selected)
            _deviceList.SelectedIndex = 0;
        else
            _deviceList.SelectedIndex = -1;
        UpdateOwnConnectUi();
    }

    internal void PrepareOwnerConnectedForShot()
    {
        PrepareAccountForShot(selected: true);
        SetConnectedUi(true);
        _status.Text = "已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）";
        _accountStatus.Text = "免密已连接（截图）";
        _output.Text = "";
    }
}

