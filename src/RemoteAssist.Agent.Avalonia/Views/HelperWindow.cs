using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Agent;

/// <summary>
/// 协助端（R011）：一次粘贴会话码+验证密码，经默认公网中继连接；
/// 连上前不出现可点灰按钮；连上后白话只读；写操作对方确认，拒绝见 DeniedMessage。
/// </summary>
internal sealed class HelperWindow : Window
{
    private readonly bool _shotMode;
    private readonly bool _enableReturnHome;
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
    private AssistClient? _client;
    private bool _busy;
    private bool _allowClose;

    public HelperWindow(string? shotDir, bool enableReturnHome = true)
    {
        _shotMode = shotDir is not null;
        _enableReturnHome = enableReturnHome;

        Title = "鹊桥远程协助";
        Width = 720;
        Height = 560;
        MinWidth = 640;
        MinHeight = 480;
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
                    Text = "粘贴对方发来的会话码和验证密码，连接后查看进程 / 日志",
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
            var path = OperatingSystem.IsWindows() ? @"C:\Windows\WindowsUpdate.log" : "/var/log/syslog";
            // 联调 / 截图用短日志；真实路径失败时仍显示结果
            if (!OperatingSystem.IsWindows())
                path = File.Exists("/tmp/r011-demo.log") ? "/tmp/r011-demo.log" : "/tmp/r011-demo.log";
            await RunToolAsync(ToolKind.ReadLogTail, new Dictionary<string, string?>
            {
                ["path"] = path,
                ["lines"] = "80"
            });
        };
        _btnRun = MakeToolButton("运行一条命令", 120);
        _btnRun.Click += async (_, _) =>
        {
            var cmd = OperatingSystem.IsWindows()
                ? "echo r011-should-not-run"
                : "echo r011-should-not-run >/tmp/r011-deny-marker";
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
            MinHeight = 140,
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
        {
            // 已连接：直接断开后回首页（与 Windows「返回首页将断开」一致，简化无二次确认）
            Disconnect();
        }
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
        // 关协助窗 = 回首页，不退出应用
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

        // 亲友可见：只用 FriendConnectionPreview，不展示中继 IP / 端口 / Assist / 明文
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
            insecure: true, // 亲友无指纹：对齐 Windows 协助端中继默认明文业务管道
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

        // 中继访客：无指纹时强制明文（与 Windows Helper.UI 一致）
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
                // 截图不真连
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
    }

    private void Disconnect()
    {
        try { _client?.Dispose(); } catch { /* ignore */ }
        _client = null;
        SetConnectedUi(false);
        _status.Text = "状态：未连接";
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
}

