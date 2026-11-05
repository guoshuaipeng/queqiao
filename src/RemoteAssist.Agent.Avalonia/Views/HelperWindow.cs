using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
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
    private readonly TextBox _argBox;
    private readonly Button _btnProcs;
    private readonly Button _btnLs;
    private readonly Button _btnLogs;
    private readonly Button _btnCat;
    private readonly Button _btnRun;

    private readonly TextBox _accountUserBox;
    private readonly TextBox _accountPassBox;
    private readonly ListBox _deviceList;
    private readonly Button _btnOwnConnect;
    private readonly TextBlock _ownSelectHint;
    private readonly TextBlock _accountStatus;
    private readonly Button _btnInstallGuide;
    private readonly Button _btnCopyMcp;
    private readonly Button _btnCopySkill;
    private readonly TextBlock _mcpHint;

    private AssistClient? _client;
    private bool _busy;
    private bool _allowClose;
    private bool _uiShotConnected;
    private bool _selfDisconnect;

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
        var installRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };
        installRow.Children.Add(new TextBlock
        {
            Text = "先让对方装好「鹊桥远程协助」",
            Foreground = Brush.Parse("#5F6368"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        _btnInstallGuide = new Button
        {
            Name = "copyInstallGuideBtn",
            Content = "复制安装说明",
            Width = 140,
            Height = 28,
            Background = Brushes.White,
            Foreground = Brush.Parse("#1A73E8"),
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.Normal
        };
        _btnInstallGuide.Click += async (_, _) => await CopyInstallInstructionsAsync();
        installRow.Children.Add(_btnInstallGuide);
        pairStack.Children.Add(installRow);
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
            Width = 180,
            Height = 36,
            IsEnabled = false,
            Background = Brush.Parse("#E8EAED"),
            Foreground = Brush.Parse("#9AA0A6")
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

        // ---- R017：MCP 安装说明（与 Windows Helper 同文案/轻按钮；始终可见，不因未连接隐藏）----
        var mcpCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };
        var mcpStack = new StackPanel { Spacing = 10 };
        mcpStack.Children.Add(new TextBlock
        {
            Text = McpInstallGuide.SectionTitle,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        });
        mcpStack.Children.Add(new TextBlock
        {
            Text = McpInstallGuide.StepsBlock,
            Foreground = Brush.Parse("#3C4043"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13
        });
        var mcpRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        _btnCopyMcp = new Button
        {
            Name = "copyMcpConfigBtn",
            Content = McpInstallGuide.CopyConfigButton,
            Width = 140,
            Height = 32,
            Background = Brushes.White,
            Foreground = Brush.Parse("#1A73E8"),
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.Normal
        };
        _btnCopyMcp.Click += async (_, _) => await CopyMcpConfigFromUiAsync();
        mcpRow.Children.Add(_btnCopyMcp);
        _btnCopySkill = new Button
        {
            Name = "copySkillGuideBtn",
            Content = McpInstallGuide.CopySkillButton,
            Width = 140,
            Height = 32,
            Background = Brushes.White,
            Foreground = Brush.Parse("#1A73E8"),
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.Normal
        };
        _btnCopySkill.Click += async (_, _) => await CopySkillInstructionsAsync();
        mcpRow.Children.Add(_btnCopySkill);
        mcpStack.Children.Add(mcpRow);
        mcpStack.Children.Add(new TextBlock
        {
            Text = "详细参数见产品文档；写操作仍须对方本机确认",
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });
        _mcpHint = new TextBlock
        {
            Text = "",
            Foreground = Brush.Parse("#0F9D58"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };
        mcpStack.Children.Add(_mcpHint);
        mcpCard.Child = mcpStack;
        body.Children.Add(mcpCard);


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
        toolStack.Children.Add(new TextBlock
        {
            Text = "建议先看正在运行的程序，再决定要不要改",
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });
        var tools = new WrapPanel { MaxWidth = 680 };
        _btnProcs = MakeToolButton("查看正在运行的程序", 180);
        _btnProcs.Click += async (_, _) => await RunToolAsync(ToolKind.ListProcesses, new Dictionary<string, string?>());
        _btnLs = MakeToolButton("查看文件夹", 110);
        _btnLs.Click += async (_, _) =>
        {
            var p = RequireArg("path");
            if (p is null) return;
            await RunToolAsync(ToolKind.ListDir, new Dictionary<string, string?> { ["path"] = p });
        };
        _btnLogs = MakeToolButton("查看日志", 100);
        _btnLogs.Click += async (_, _) =>
        {
            var p = RequireArg("path");
            if (p is null) return;
            await RunToolAsync(ToolKind.ReadLogTail, new Dictionary<string, string?>
            {
                ["path"] = p,
                ["lines"] = "80"
            });
        };
        _btnCat = MakeToolButton("查看文件", 100);
        _btnCat.Click += async (_, _) =>
        {
            var p = RequireArg("path");
            if (p is null) return;
            await RunToolAsync(ToolKind.ReadFile, new Dictionary<string, string?> { ["path"] = p });
        };
        _btnRun = MakeToolButton("提议一项改动", 120);
        _btnRun.Click += async (_, _) =>
        {
            var c = RequireArg("command");
            if (c is null) return;
            await RunToolAsync(ToolKind.RunCommand, new Dictionary<string, string?> { ["command"] = c });
        };
        foreach (var b in new[] { _btnProcs, _btnLs, _btnLogs, _btnCat, _btnRun })
        {
            b.Margin = new Thickness(0, 0, 10, 8);
            tools.Children.Add(b);
        }
        toolStack.Children.Add(tools);

        toolStack.Children.Add(new TextBlock
        {
            Text = "路径 / 命令参数",
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368")
        });
        _argBox = new TextBox
        {
            Watermark = OperatingSystem.IsWindows()
                ? @"例如 C:\Users\你的文件夹"
                : "例如 /home/你的文件夹",
            MinHeight = 32,
            FontSize = 13
        };
        toolStack.Children.Add(_argBox);

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

    private async Task CopyInstallInstructionsAsync()
    {
        try
        {
            var clip = Clipboard;
            if (clip is null)
            {
                _failHint.Text = "复制失败：剪贴板不可用";
                return;
            }
            await clip.SetTextAsync(WriteConfirmCopy.InstallInstructionsClipboard).ConfigureAwait(true);
            // 与 Windows 一致：InstallCopiedHint 只写预览行一次，不写状态行
            _preview.Text = WriteConfirmCopy.InstallCopiedHint;
            _preview.Foreground = Brush.Parse("#0F9D58");
            _mcpHint.Text = "";
            _failHint.Text = "";
        }
        catch (Exception ex)
        {
            _failHint.Text = "复制失败：" + ex.Message;
        }
    }

    private async Task CopyMcpConfigFromUiAsync()
    {
        try
        {
            if (!TryBuildOptions(out var opts, out _, out _))
            {
                _failHint.Text = McpInstallGuide.NeedCodePasswordHint;
                _preview.Foreground = Brush.Parse("#C5221F");
                _mcpHint.Text = "";
                return;
            }

            var useTls = !opts.Insecure && !string.IsNullOrWhiteSpace(opts.Fingerprint);
            string payload;
            if (opts.ViaRelay)
            {
                var relay = string.IsNullOrWhiteSpace(opts.RelayEndpoint)
                    ? RelayEndpoint.DefaultEndpoint
                    : opts.RelayEndpoint!;
                var pwd = opts.Password ?? "";
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

            var clip = Clipboard;
            if (clip is null)
            {
                _failHint.Text = "复制失败：剪贴板不可用";
                return;
            }
            await clip.SetTextAsync(McpConfigSnippet.BuildClipboardBundle(exe, payload)).ConfigureAwait(true);
            var hint = exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint;
            _preview.Text = hint;
            _preview.Foreground = Brush.Parse("#0F9D58");
            _mcpHint.Text = hint;
            _failHint.Text = "";
            _status.Text = hint;
        }
        catch (Exception ex)
        {
            _failHint.Text = "复制 MCP 配置失败：" + ex.Message;
        }
    }


    private async Task CopySkillInstructionsAsync()
    {
        try
        {
            var clip = Clipboard;
            if (clip is null)
            {
                _failHint.Text = "复制失败：剪贴板不可用";
                return;
            }
            await clip.SetTextAsync(McpInstallGuide.SkillInstructionsClipboard).ConfigureAwait(true);
            // 反馈只写 MCP 卡内提示一次，不写预览行/状态行（对齐 R017 不叠字）
            _mcpHint.Text = McpInstallGuide.SkillCopiedHint;
            _mcpHint.Foreground = Brush.Parse("#0F9D58");
            _failHint.Text = "";
        }
        catch (Exception ex)
        {
            _failHint.Text = "复制 Skill 说明失败：" + ex.Message;
        }
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
                _status.Text = HelperSessionCopy.StatusConnected;
                _output.Text = "";
                return;
            }

            var client = await AssistClient.PairAsync(opts, "helper-avalonia").ConfigureAwait(true);
            AttachClient(client);
            SetConnectedUi(true);
            _status.Text = HelperSessionCopy.StatusConnected;
            _failHint.Text = "";
            AppendOutput("PAIR_OK\n");
            _ = preview;
        }
        catch (Exception ex)
        {
            var friendly = FriendConnectGuide.FriendlyOrRaw(ex);
            _status.Text = HelperSessionCopy.StatusConnectFailed;
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
        if (connected)
        {
            _btnConnect.Background = Brush.Parse("#E8EAED");
            _btnConnect.Foreground = Brush.Parse("#9AA0A6");
        }
        else
        {
            _btnConnect.Background = Brush.Parse("#0F9D58");
            _btnConnect.Foreground = Brushes.White;
        }

        _toolCard.IsVisible = connected;
        foreach (var b in new[] { _btnProcs, _btnLs, _btnLogs, _btnCat, _btnRun })
        {
            b.IsEnabled = connected;
            if (connected)
                StyleToolButtonEnabled(b);
        }
        UpdateOwnConnectUi();
    }


    private static void StyleToolButtonEnabled(Button b)
    {
        b.IsEnabled = true;
        b.Background = Brush.Parse("#E8F0FE");
        b.Foreground = Brush.Parse("#1A73E8");
        b.FontWeight = FontWeight.SemiBold;
    }


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
        _uiShotConnected = false;
        _selfDisconnect = false;
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
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
        if (_selfDisconnect) return;
        void Apply()
        {
            if (_selfDisconnect) return;
            if (_client is null) return;
            try { _client.ConnectionLost -= OnClientConnectionLost; } catch { /* ignore */ }
            try { _client.Dispose(); } catch { /* ignore */ }
            _client = null;
            _uiShotConnected = false;
            SetConnectedUi(false);
            _status.Text = HelperSessionCopy.StatusPeerEndedAssist;
            _failHint.Text = "";
        }
        if (Dispatcher.UIThread.CheckAccess())
            Apply();
        else
            Dispatcher.UIThread.Post(Apply);
    }

    private void UpdateOwnConnectUi()
    {
        if (_deviceList.SelectedItem is null)
        {
            ApplyOwnConnectStyle(OwnConnectVisual.Disabled);
            _btnOwnConnect.Content = "连接这台电脑";
            _ownSelectHint.Text = "请先点选一台自己的电脑";
            return;
        }

        if (_deviceList.SelectedItem is not DeviceListItem device)
        {
            ApplyOwnConnectStyle(OwnConnectVisual.Disabled);
            _btnOwnConnect.Content = "连接这台电脑";
            _ownSelectHint.Text = "请先点选一台自己的电脑";
            return;
        }

        var name = string.IsNullOrWhiteSpace(device.Name) ? "这台电脑" : device.Name.Trim();
        if (!device.Online)
        {
            ApplyOwnConnectStyle(OwnConnectVisual.Disabled);
            _btnOwnConnect.Content = "连接：" + name;
            _ownSelectHint.Text = "这台电脑现在不在线";
            return;
        }

        if (_client is not null || _uiShotConnected)
        {
            // 已连接：账号区不再保持实心绿主权重
            ApplyOwnConnectStyle(OwnConnectVisual.ConnectedLight);
            _btnOwnConnect.Content = "已连接";
            _ownSelectHint.Text = "免密已连接「" + name + "」";
            return;
        }

        ApplyOwnConnectStyle(OwnConnectVisual.Clickable);
        _btnOwnConnect.Content = "连接：" + name;
        _ownSelectHint.Text = "将免密连接「" + name + "」";
    }


    private void ApplyOwnConnectStyle(OwnConnectVisual visual)
    {
        switch (visual)
        {
            case OwnConnectVisual.Clickable:
                _btnOwnConnect.IsEnabled = true;
                _btnOwnConnect.Background = Brush.Parse("#0F9D58");
                _btnOwnConnect.Foreground = Brushes.White;
                _btnOwnConnect.FontWeight = FontWeight.Bold;
                break;
            case OwnConnectVisual.ConnectedLight:
                _btnOwnConnect.IsEnabled = false;
                _btnOwnConnect.Background = Brush.Parse("#F1F3F4");
                _btnOwnConnect.Foreground = Brush.Parse("#5F6368");
                _btnOwnConnect.FontWeight = FontWeight.SemiBold;
                break;
            default:
                _btnOwnConnect.IsEnabled = false;
                _btnOwnConnect.Background = Brush.Parse("#E8EAED");
                _btnOwnConnect.Foreground = Brush.Parse("#9AA0A6");
                _btnOwnConnect.FontWeight = FontWeight.Normal;
                break;
        }
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
            PrepareDevicesForShot(selectedOnline: false);
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
            PrepareDevicesForShot(selectedOnline: false);
            return;
        }

        try
        {
            var list = await RelayClient.ListDevicesAsync(host, port, _settings.AccountToken!).ConfigureAwait(true);
            _deviceList.Items.Clear();
            foreach (var d in list)
            {
                _deviceList.Items.Add(new DeviceListItem
                {
                    Id = d.Id,
                    Name = string.IsNullOrWhiteSpace(d.Name) ? "这台电脑" : d.Name,
                    Online = d.Online
                });
            }
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
        if (_deviceList.SelectedItem is not DeviceListItem device)
        {
            _accountStatus.Text = "请先点选一台自己的电脑";
            return;
        }
        if (!device.Online)
        {
            _accountStatus.Text = "这台电脑现在不在线";
            return;
        }

        var deviceId = device.Id;
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
            AttachClient(client);
            SetConnectedUi(true);
            _status.Text = "已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）";
            _failHint.Text = "";
        }
        catch (Exception ex)
        {
            _status.Text = HelperSessionCopy.StatusConnectFailed;
            _failHint.Text = FriendConnectGuide.FriendlyOrRaw(ex);
            SetConnectedUi(false);
        }
        finally
        {
            _busy = false;
        }
    }

    private string? RequireArg(string key)
    {
        var v = _argBox.Text?.Trim() ?? "";
        if (v.Length == 0)
        {
            var hint = OperatingSystem.IsWindows()
                ? @"例如 C:\Users\你的文件夹"
                : "例如 /home/你的文件夹";
            _status.Text = key == "command"
                ? "请先在上方填写要提议的改动（人话说明或命令）"
                : "请先在上方填写路径（" + hint + "）";
            return null;
        }
        return v;
    }

    private async Task RunToolAsync(ToolKind tool, Dictionary<string, string?> args)
    {
        if (_busy) return;
        if (_client is null)
        {
            _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
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
                ToolKind.ListDir => "已查看文件夹",
                ToolKind.ReadLogTail => "已查看日志",
                ToolKind.ReadFile => "已查看文件",
                ToolKind.RunCommand => "已提议一项改动",
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


    /// <summary>UiShot：已连上 + 常用操作可见。</summary>
    internal void PreparePeerConnectedSessionForShot()
    {
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = HelperSessionCopy.StatusConnected;
        _failHint.Text = "";
        _output.Text = "PAIR_OK\n";
    }

    /// <summary>UiShot：对方结束后主状态 + 五键收起。</summary>
    internal void PreparePeerEndedAssistForShot()
    {
        _uiShotConnected = false;
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusPeerEndedAssist;
        _failHint.Text = "";
    }

    /// <summary>UiShot：自己断开原文。</summary>
    internal void PrepareSelfDisconnectedForShot()
    {
        _uiShotConnected = false;
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
        _failHint.Text = "";
    }

    /// <summary>UiShot：未连接空态。</summary>
    internal void PrepareIdleForShot()
    {
        _pasteBox.Text = "";
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
        _preview.Text = "粘贴后点「连接」";
        _failHint.Text = "";
    }

    /// <summary>UiShot：填入粘贴文案。</summary>
    internal void PreparePasteForShot(string shareText)
    {
        _pasteBox.Text = shareText;
        RefreshPreview();
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
        _failHint.Text = "";
    }

    /// <summary>UiShot：连不上引导（真实控件上的失败原文）。</summary>
    internal void PrepareFailForShot(string failMessage)
    {
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusConnectFailed;
        _failHint.Text = failMessage;
    }

    /// <summary>UiShot：已连接 + 只读结果。</summary>
    internal void PrepareConnectedReadonlyForShot(string output)
    {
        SetConnectedUi(true);
        _status.Text = HelperSessionCopy.StatusConnected;
        _failHint.Text = "";
        _output.Text = output;
    }

    /// <summary>UiShot：对方拒绝写操作后的协助端提示。</summary>
    internal void PrepareDeniedForShot()
    {
        PrepareOwnerConnectedForShot();
        _status.Text = WriteConfirmCopy.DeniedMessage;
        _failHint.Text = "";
        _output.Text = "[提议一项改动] FAIL: " + WriteConfirmCopy.DeniedMessage + "\n";
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

    /// <summary>UiShot：列表仅人话名称+在线/离线（与未选中截图文案区分）。</summary>
    internal void PrepareDeviceListHumanForShot()
    {
        _accountUserBox.Text = "demo";
        _accountPassBox.Text = "••••••";
        _accountStatus.Text = "设备 2 台";
        PrepareDevicesForShot(selectedOnline: false, selectedOffline: false);
    }

    /// <summary>UiShot：账号标签 + 设备列表；selected 控制是否点选在线设备。</summary>
    internal void PrepareAccountForShot(bool selected)
    {
        _accountUserBox.Text = "demo";
        _accountPassBox.Text = "••••••";
        _accountStatus.Text = "已登录（截图）";
        PrepareDevicesForShot(selectedOnline: selected, selectedOffline: false);
    }

    internal void PrepareDevicesForShot(bool selectedOnline, bool selectedOffline = false)
    {
        _deviceList.Items.Clear();
        _deviceList.Items.Add(new DeviceListItem { Id = "dev-living", Name = "客厅电脑", Online = true });
        _deviceList.Items.Add(new DeviceListItem { Id = "dev-office", Name = "书房电脑", Online = false });
        if (selectedOffline)
            _deviceList.SelectedIndex = 1;
        else if (selectedOnline)
            _deviceList.SelectedIndex = 0;
        else
            _deviceList.SelectedIndex = -1;
        UpdateOwnConnectUi();
    }

    /// <summary>UiShot：选中离线设备。</summary>
    internal void PrepareOfflineSelectedForShot()
    {
        _accountUserBox.Text = "demo";
        _accountPassBox.Text = "••••••";
        _accountStatus.Text = "已登录（截图）";
        PrepareDevicesForShot(selectedOnline: false, selectedOffline: true);
    }

    internal void PrepareOwnerConnectedForShot()
    {
        PrepareAccountForShot(selected: true);
        _uiShotConnected = true;
        SetConnectedUi(true);
        _status.Text = "已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）";
        _accountStatus.Text = "免密已连接（截图）";
        _argBox.Text = "";
        _argBox.Watermark = OperatingSystem.IsWindows()
            ? @"例如 C:\Users\你的文件夹"
            : "例如 /home/你的文件夹";
        _output.Text = "";
        Height = Math.Max(Height, 920);
        Width = Math.Max(Width, 720);
    }

    /// <summary>UiShot：强调已连接后账号区「已连接」轻态 + 会话码「连接」灰禁。</summary>
    internal void PrepareConnectedButtonLightForShot()
    {
        PrepareOwnerConnectedForShot();
        _output.Text = "（截图）账号区为「已连接」轻态；上方会话码「连接」已灰禁用。";
        _accountStatus.Text = "免密已连接 · 按钮轻态（截图）";
    }

    /// <summary>UiShot：仅登录未免密连上——常用操作整块不可见。</summary>
    internal void PrepareLoggedInUnconnectedForShot()
    {
        PrepareAccountForShot(selected: true);
        SetConnectedUi(false);
        _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
        _accountStatus.Text = "已登录（截图）";
        _output.Text = "";
    }


    /// <summary>UiShot：MCP 卡可见「复制 Skill 说明」轻按钮（与「复制 MCP 配置」并列）。</summary>
    internal void PrepareSkillGuideEntryForShot()
    {
        PrepareInstallGuideEntryForShot();
        _mcpHint.Text = "";
        _failHint.Text = "";
        Height = Math.Max(Height, 980);
    }

    /// <summary>UiShot：已点「复制 Skill 说明」后人话反馈（只出现一次）。</summary>
    internal void PrepareSkillCopiedForShot()
    {
        PrepareSkillGuideEntryForShot();
        _mcpHint.Text = McpInstallGuide.SkillCopiedHint;
        _mcpHint.Foreground = Brush.Parse("#0F9D58");
        _failHint.Text = "";
        // 预览/状态不写同一句，避免叠字
    }

    /// <summary>UiShot：预览区展示 Skill 说明剪贴板正文（与常量同一份）。</summary>
    internal void PrepareSkillClipboardPreviewForShot()
    {
        PrepareSkillGuideEntryForShot();
        _preview.Text = McpInstallGuide.SkillInstructionsClipboard;
        _preview.Foreground = Brush.Parse("#202124");
        _mcpHint.Text = "";
        _failHint.Text = "";
        Height = Math.Max(Height, 1020);
    }

    /// <summary>UiShot：MCP / 复制安装说明入口可见（轻按钮，非实心绿）。</summary>
    internal void PrepareInstallGuideEntryForShot()
    {
        PrepareIdleForShot();
        _pasteBox.Text = "";
        _preview.Text = "粘贴后点「连接」";
        _preview.Foreground = Brush.Parse("#5F6368");
        _mcpHint.Text = "";
        _failHint.Text = "";
        _status.Text = HelperSessionCopy.StatusDisconnectedSelf;
        Height = Math.Max(Height, 900);
    }

    /// <summary>UiShot：已点「复制安装说明」后人话反馈（Windows 同等文案；只出现一次）。</summary>
    internal void PrepareInstallCopiedForShot()
    {
        PrepareInstallGuideEntryForShot();
        _preview.Text = WriteConfirmCopy.InstallCopiedHint;
        _preview.Foreground = Brush.Parse("#0F9D58");
        // 状态行保持「状态：未连接」，避免与预览行重复同一句反馈
        _mcpHint.Text = "";
        _failHint.Text = "";
    }

    /// <summary>UiShot：预览区展示将复制到剪贴板的安装说明正文（与常量同一份；不含反馈叠字）。</summary>
    internal void PrepareInstallClipboardPreviewForShot()
    {
        PrepareInstallGuideEntryForShot();
        _preview.Text = WriteConfirmCopy.InstallInstructionsClipboard;
        _preview.Foreground = Brush.Parse("#202124");
        _mcpHint.Text = "";
        _failHint.Text = "";
        Height = Math.Max(Height, 980);
    }




    private enum OwnConnectVisual
    {
        Disabled,
        Clickable,
        ConnectedLight
    }

    /// <summary>列表展示用人话名称+在线/离线；内部 Id 不出现在 ToString。</summary>
    private sealed class DeviceListItem
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required bool Online { get; init; }

        public override string ToString()
        {
            var name = string.IsNullOrWhiteSpace(Name) ? "这台电脑" : Name.Trim();
            return name + "\t" + (Online ? "在线" : "离线");
        }
    }

}

