using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>亲友被控主窗：复制码+密码 / 结束这次协助；关窗提示见 first-run §2。R011 可返回首页。</summary>
internal sealed class MainWindow : Window
{
    private readonly AgentHost _host;
    private readonly bool _shotMode;
    private readonly bool _enableReturnHome;
    private readonly TextBlock _codeBlock;
    private readonly TextBlock _pwdBlock;
    private readonly TextBlock _status;
    private readonly TextBlock _hint;
    private bool _ending;
    private bool _allowClose;

    public MainWindow(AgentHost host, string? shotDir, bool enableReturnHome = false)
    {
        _host = host;
        _shotMode = shotDir is not null;
        _enableReturnHome = enableReturnHome;

        Title = "鹊桥远程协助";
        Width = 480;
        Height = 440;
        MinWidth = 420;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brush.Parse("#F5F6F8");

        var root = new DockPanel { Margin = new Thickness(20) };

        var topRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(topRow, Dock.Top);
        var title = new TextBlock
        {
            Text = "鹊桥远程协助",
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        topRow.Children.Add(title);
        if (_enableReturnHome)
        {
            var homeBtn = new Button
            {
                Content = "返回首页",
                Width = 96,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(homeBtn, Dock.Right);
            homeBtn.Click += async (_, _) => await ReturnHomeAsync();
            topRow.Children.Add(homeBtn);
        }
        root.Children.Add(topRow);

        _hint = new TextBlock
        {
            Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#5F6368"),
            Margin = new Thickness(0, 0, 0, 16)
        };
        DockPanel.SetDock(_hint, Dock.Top);
        root.Children.Add(_hint);

        var endBtn = new Button
        {
            Content = "结束这次协助",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 40,
            Margin = new Thickness(0, 12, 0, 0)
        };
        endBtn.Click += async (_, _) => await EndAssistAsync(returnHome: _enableReturnHome);
        DockPanel.SetDock(endBtn, Dock.Bottom);
        root.Children.Add(endBtn);

        _status = new TextBlock
        {
            Text = "正在连接网络…",
            Foreground = Brush.Parse("#5F6368"),
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        DockPanel.SetDock(_status, Dock.Bottom);
        root.Children.Add(_status);

        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = BuildSessionCard(out _codeBlock, out _pwdBlock)
        };
        root.Children.Add(card);

        Content = root;

        Closing += OnClosing;

        if (!_shotMode)
        {
            _host.SessionChanged += () => Dispatcher.UIThread.Post(RefreshSessionUi);
            _host.RelayStatusChanged += () => Dispatcher.UIThread.Post(RefreshSessionUi);
            _host.PairedClientsChanged += () => Dispatcher.UIThread.Post(RefreshSessionUi);
            Opened += (_, _) => RefreshSessionUi();
        }
        else
        {
            // 截图模式：填演示凭证，不连中继（与 R010 一致；真实联调以协助端码为准）
            _codeBlock.Text = "会话码：AB12CD";
            _pwdBlock.Text = "验证密码：3471";
            _status.Text = "已连上网络，可以发给对方了";
        }
    }

    public void AllowCloseWithoutPrompt() => _allowClose = true;

    private Control BuildSessionCard(out TextBlock codeBlock, out TextBlock pwdBlock)
    {
        var stack = new StackPanel { Spacing = 10 };

        stack.Children.Add(new TextBlock
        {
            Text = "发给对方的凭证",
            FontWeight = FontWeight.SemiBold,
            FontSize = 14
        });

        codeBlock = new TextBlock
        {
            Text = "会话码：……",
            FontSize = 18,
            FontWeight = FontWeight.Bold
        };
        pwdBlock = new TextBlock
        {
            Text = "验证密码：……",
            FontSize = 18,
            FontWeight = FontWeight.Bold
        };
        stack.Children.Add(codeBlock);
        stack.Children.Add(pwdBlock);

        var copyBtn = new Button
        {
            Content = "复制码+密码",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 44,
            FontWeight = FontWeight.Bold,
            Background = Brush.Parse("#1A73E8"),
            Foreground = Brushes.White,
            Margin = new Thickness(0, 8, 0, 0)
        };
        copyBtn.Click += async (_, _) => await CopyShareAsync();
        stack.Children.Add(copyBtn);

        var refreshBtn = new Button
        {
            Content = "刷新会话码",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0)
        };
        refreshBtn.Click += (_, _) =>
        {
            if (_shotMode) return;
            _host.RefreshSessionCode();
            RefreshSessionUi();
            _status.Text = "已换新的会话码和验证密码。之前发出去的码不能再用。";
        };
        stack.Children.Add(refreshBtn);

        return stack;
    }

    private async Task CopyShareAsync()
    {
        try
        {
            var text = _shotMode
                ? FriendConnectGuide.FormatShareClipboard("AB12CD", "3471")
                : FriendConnectGuide.FormatShareClipboard(_host.SessionCode, _host.SessionPassword);
            var clip = Clipboard;
            if (clip is not null)
            {
                await clip.SetTextAsync(text).ConfigureAwait(true);
                _status.Text = "已复制会话码和验证密码。";
            }
            else
            {
                _status.Text = "无法访问剪贴板，请手动选中会话码和验证密码。";
            }
        }
        catch (Exception ex)
        {
            _status.Text = "复制失败：" + ex.Message;
        }
    }

    private void RefreshSessionUi()
    {
        _codeBlock.Text = "会话码：" + _host.SessionCode;
        _pwdBlock.Text = "验证密码：" + _host.SessionPassword;
        var phase = _host.RelayPhase;
        var paired = _host.ListPairedClients().Count;
        if (paired > 0 || phase == "paired")
        {
            _hint.Text = "已有人连上。对方可以查看进程、日志和文件；要改动时会再问你。";
            _status.Text = FriendConnectGuide.StatusWaitingPeer;
            if (paired > 0)
                _status.Text = $"已有 {paired} 位协助方连着。";
        }
        else if (phase is "ready" or "waiting")
        {
            _status.Text = FriendConnectGuide.StatusReadyToShare;
        }
        else if (!string.IsNullOrWhiteSpace(_host.LastRelayError))
        {
            _status.Text = FriendConnectGuide.FailRelayUnreachable;
        }
        else
        {
            _status.Text = "正在连接网络…";
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || _ending || _shotMode)
            return;

        e.Cancel = true;
        var tip = new CloseWarnWindow();
        var result = await tip.ShowDialog<string?>(this).ConfigureAwait(true);
        if (result == "end")
            await EndAssistAsync(returnHome: _enableReturnHome).ConfigureAwait(true);
        // 「返回」或关闭提示：继续留在主窗
    }

    private async Task ReturnHomeAsync()
    {
        await EndAssistAsync(returnHome: true).ConfigureAwait(true);
    }

    private async Task EndAssistAsync(bool returnHome)
    {
        if (_ending) return;
        _ending = true;
        try
        {
            if (!_shotMode)
            {
                _host.DisconnectAllSessions();
                _host.Stop();
            }
            _status.Text = "已结束这次协助。";
            _allowClose = true;
            if (returnHome)
            {
                AppNavigator.ShowHome();
                try { Close(); } catch { /* ignore */ }
            }
            else
            {
                Close();
                if (Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.Shutdown();
            }
        }
        catch (Exception ex)
        {
            _ending = false;
            _status.Text = "结束失败：" + ex.Message;
        }
        await Task.CompletedTask;
    }
}
