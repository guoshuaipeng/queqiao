using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>亲友被控主窗：复制码+密码 / 信任等级 / 结束这次协助。R012 补高级三档。</summary>
internal sealed class MainWindow : Window
{
    private readonly AgentHost _host;
    private readonly bool _shotMode;
    private readonly bool _enableReturnHome;
    private readonly TextBlock _codeBlock;
    private readonly TextBlock _pwdBlock;
    private readonly TextBlock _status;
    private readonly TextBlock _hint;
    private readonly Border _trustTip;
    private readonly TextBlock _trustTipText;
    private readonly Button _trustTipAction;
    private readonly Button _btnAdvanced;
    private readonly Border _advancedPanel;
    private readonly RadioButton _radioAsk;
    private readonly RadioButton _radioRelaxed;
    private readonly RadioButton _radioFullTrust;
    private readonly ScrollViewer _scroll;
    private readonly ListBox _confirmedList;
    private readonly ListBox _auditList;
    private bool _advancedOpen;
    private bool _suppressTrustRadio;
    private bool _ending;
    private bool _allowClose;

    public MainWindow(AgentHost host, string? shotDir, bool enableReturnHome = false)
    {
        _host = host;
        _shotMode = shotDir is not null;
        _enableReturnHome = enableReturnHome;

        Title = "鹊桥远程协助";
        Width = 520;
        Height = 560;
        MinWidth = 460;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brush.Parse("#F5F6F8");

        _scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        var root = new StackPanel { Margin = new Thickness(20), Spacing = 10 };

        var topRow = new DockPanel();
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

        _trustTipText = new TextBlock
        {
            Name = "trustTipText",
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        _trustTipAction = new Button
        {
            Name = "trustTipAction",
            Padding = new Thickness(8, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        _trustTipAction.Click += (_, _) => OnTrustTipAction();
        var tipInner = new DockPanel { Margin = new Thickness(12, 8) };
        DockPanel.SetDock(_trustTipAction, Dock.Right);
        tipInner.Children.Add(_trustTipAction);
        tipInner.Children.Add(_trustTipText);
        _trustTip = new Border
        {
            Name = "trustTip",
            IsVisible = false,
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            Child = tipInner
        };
        root.Children.Add(_trustTip);

        _hint = new TextBlock
        {
            Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#5F6368")
        };
        root.Children.Add(_hint);

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

        _status = new TextBlock
        {
            Text = "正在连接网络…",
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        };
        root.Children.Add(_status);

        _btnAdvanced = new Button
        {
            Name = "btnAdvanced",
            Content = "高级 ▸",
            Width = 100,
            Height = 34,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _btnAdvanced.Click += (_, _) => ToggleAdvanced();
        root.Children.Add(_btnAdvanced);

        _advancedPanel = BuildAdvancedPanel(out _radioAsk, out _radioRelaxed, out _radioFullTrust, out _confirmedList, out _auditList);
        _advancedPanel.IsVisible = false;
        root.Children.Add(_advancedPanel);

        var endBtn = new Button
        {
            Content = "结束这次协助",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 40,
            Margin = new Thickness(0, 4, 0, 0)
        };
        endBtn.Click += async (_, _) => await EndAssistAsync(returnHome: _enableReturnHome);
        root.Children.Add(endBtn);

        _scroll.Content = root;
        Content = _scroll;

        Closing += OnClosing;

        if (!_shotMode)
        {
            _host.SessionChanged += () => Dispatcher.UIThread.Post(RefreshSessionUi);
            _host.RelayStatusChanged += () => Dispatcher.UIThread.Post(RefreshSessionUi);
            _host.PairedClientsChanged += () => Dispatcher.UIThread.Post(RefreshSessionUi);
            _host.TrustLevelChanged += () => Dispatcher.UIThread.Post(RefreshTrustUi);
            _host.Audit.EntryWritten += () => Dispatcher.UIThread.Post(() =>
            {
                RefreshConfirmedChanges();
                RefreshAudit();
            });
            Opened += (_, _) =>
            {
                RefreshSessionUi();
                RefreshTrustUi();
                RefreshConfirmedChanges();
                RefreshAudit();
            };
        }
        else
        {
            _codeBlock.Text = "会话码：AB12CD";
            _pwdBlock.Text = "验证密码：3471";
            _status.Text = "已连上网络，可以发给对方了";
            RefreshTrustUi();
        }
    }

    public void AllowCloseWithoutPrompt() => _allowClose = true;

    private Border BuildAdvancedPanel(
        out RadioButton radioAsk,
        out RadioButton radioRelaxed,
        out RadioButton radioFullTrust,
        out ListBox confirmedList,
        out ListBox auditList)
    {
        var stack = new StackPanel { Spacing = 8 };

        stack.Children.Add(new TextBlock
        {
            Name = "trustGroupTitle",
            Text = TrustLevelCopy.GroupTitle,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14
        });

        radioAsk = MakeTrustRadio("radioTrustAsk", TrustLevelCopy.AskEveryTimeName, checkedByDefault: true);
        radioRelaxed = MakeTrustRadio("radioTrustRelaxed", TrustLevelCopy.RelaxedName, checkedByDefault: false);
        radioFullTrust = MakeTrustRadio("radioTrustFullTrust", TrustLevelCopy.FullTrustName, checkedByDefault: false);

        stack.Children.Add(MakeOptionRow(radioAsk, TrustLevelCopy.AskEveryTimeBlurb, "trustAskBlurb", onceHint: null));
        stack.Children.Add(MakeOptionRow(radioRelaxed, TrustLevelCopy.RelaxedBlurb, "trustRelaxedBlurb", onceHint: null));
        stack.Children.Add(MakeOptionRow(radioFullTrust, TrustLevelCopy.FullTrustBlurb, "trustFullTrustBlurb", onceHint: TrustLevelCopy.FullTrustOnceHint));

        stack.Children.Add(new TextBlock
        {
            Name = "trustHighRiskNote",
            Text = TrustLevelCopy.HighRiskSupplement,
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new TextBlock
        {
            Name = "trustGuestCeiling",
            Text = TrustLevelCopy.GuestCeilingNote,
            FontSize = 12,
            Foreground = Brush.Parse("#80868B"),
            TextWrapping = TextWrapping.Wrap
        });

        radioAsk.IsCheckedChanged += OnTrustRadioChanged;
        radioRelaxed.IsCheckedChanged += OnTrustRadioChanged;
        radioFullTrust.IsCheckedChanged += OnTrustRadioChanged;

        stack.Children.Add(new TextBlock
        {
            Name = "confirmedChangesTitle",
            Text = TrustLevelCopy.ConfirmedChangesTitle,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 0)
        });
        confirmedList = new ListBox
        {
            Name = "confirmedChangesList",
            Height = 88,
            MinHeight = 72
        };
        stack.Children.Add(confirmedList);

        stack.Children.Add(new TextBlock
        {
            Name = "recentAuditTitle",
            Text = "最近审计",
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            Foreground = Brush.Parse("#5F6368"),
            Margin = new Thickness(0, 6, 0, 0)
        });
        auditList = new ListBox
        {
            Name = "recentAuditList",
            Height = 72,
            MinHeight = 56
        };
        stack.Children.Add(auditList);

        return new Border
        {
            Name = "advancedPanel",
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = stack
        };
    }

    private static RadioButton MakeTrustRadio(string name, string text, bool checkedByDefault) => new()
    {
        Name = name,
        Content = text,
        GroupName = "trustLevel",
        IsChecked = checkedByDefault,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Control MakeOptionRow(RadioButton radio, string blurb, string blurbName, string? onceHint)
    {
        var row = new StackPanel { Spacing = 2 };
        var head = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        head.Children.Add(radio);
        if (onceHint is not null)
        {
            head.Children.Add(new TextBlock
            {
                Name = "trustFullTrustOnceHint",
                Text = onceHint,
                FontSize = 12,
                Foreground = Brush.Parse("#B06000"),
                VerticalAlignment = VerticalAlignment.Center
            });
        }
        row.Children.Add(head);
        row.Children.Add(new TextBlock
        {
            Name = blurbName,
            Text = blurb,
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(28, 0, 0, 0)
        });
        return row;
    }

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
            Name = "btnRefreshSession",
            Content = FriendConnectGuide.RefreshSessionConfirmOk,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0),
            // 轻按钮：不抢「复制码+密码」实心主按钮权重
            Background = Brushes.Transparent
        };
        refreshBtn.Click += async (_, _) => await ConfirmAndRefreshSessionAsync();
        stack.Children.Add(refreshBtn);
        return stack;
    }

    private void ToggleAdvanced()
    {
        _advancedOpen = !_advancedOpen;
        _advancedPanel.IsVisible = _advancedOpen;
        _btnAdvanced.Content = _advancedOpen ? "高级 ▾" : "高级 ▸";
        if (_advancedOpen)
            Height = Math.Max(Height, 860);
    }

    private async void OnTrustRadioChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_suppressTrustRadio) return;
        if (sender is not RadioButton { IsChecked: true } rb) return;

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
            var ok = await ShowFullTrustConfirmAsync().ConfigureAwait(true);
            if (ok)
            {
                _host.EnableFullTrustAfterConfirm();
                RefreshTrustUi();
            }
            else
            {
                SyncTrustRadios();
                RefreshTrustUi();
            }
        }
    }

    private async Task<bool> ShowFullTrustConfirmAsync()
    {
        var dlg = new FullTrustConfirmWindow();
        var result = await dlg.ShowDialog<bool?>(this).ConfigureAwait(true);
        return dlg.Confirmed || result == true;
    }

    private void OnTrustTipAction()
    {
        if (_host.TrustLevelIsFullTrust)
            _host.EndFullTrustSession();
        else
            _host.RevertTrustToAskEveryTime();
        RefreshTrustUi();
    }

    private void SyncTrustRadios()
    {
        var level = _host.TrustLevelEffective;
        _suppressTrustRadio = true;
        try
        {
            _radioAsk.IsChecked = level == TrustLevelKind.AskEveryTime;
            _radioRelaxed.IsChecked = level == TrustLevelKind.Relaxed;
            _radioFullTrust.IsChecked = level == TrustLevelKind.FullTrust;
        }
        finally
        {
            _suppressTrustRadio = false;
        }
    }

    private void RefreshTrustUi()
    {
        var level = _host.TrustLevelEffective;

        _suppressTrustRadio = true;
        try
        {
            _radioAsk.IsChecked = level == TrustLevelKind.AskEveryTime;
            _radioRelaxed.IsChecked = level == TrustLevelKind.Relaxed;
            _radioFullTrust.IsChecked = level == TrustLevelKind.FullTrust;
        }
        finally
        {
            _suppressTrustRadio = false;
        }

        if (level == TrustLevelKind.AskEveryTime)
        {
            _trustTip.IsVisible = false;
            RefreshConfirmedChanges();
            RefreshAudit();
            return;
        }

        _trustTip.IsVisible = true;
        _trustTipText.Text = TrustLevelCopy.BannerOf(level);
        _trustTipAction.Content = TrustLevelCopy.TipActionOf(level);
        if (level == TrustLevelKind.Relaxed)
        {
            _trustTip.Background = Brush.Parse("#E8F0FE");
            _trustTip.BorderBrush = Brush.Parse("#AECBFA");
            _trustTipText.Foreground = Brush.Parse("#174EA6");
        }
        else
        {
            _trustTip.Background = Brush.Parse("#FEF7E0");
            _trustTip.BorderBrush = Brush.Parse("#FDD663");
            _trustTipText.Foreground = Brush.Parse("#B06000");
        }
        RefreshConfirmedChanges();
        RefreshAudit();
    }

    private async Task ConfirmAndRefreshSessionAsync()
    {
        if (_shotMode) return;
        var dlg = new RefreshSessionConfirmWindow();
        var result = await dlg.ShowDialog<bool?>(this).ConfigureAwait(true);
        if (!(dlg.Confirmed || result == true))
            return;
        _host.RefreshSessionCode();
        RefreshSessionUi();
        RefreshTrustUi();
        RefreshConfirmedChanges();
        RefreshAudit();
        _status.Text = "已换新的会话码和验证密码。之前发出去的码不能再用。";
    }

    private void RefreshConfirmedChanges()
    {
        try
        {
            var entries = _host.Audit.ReadRecent(80);
            var filtered = ConfirmedChangeList.Filter(entries.Select(e => (e.EventType, e.Detail)));
            var items = new List<string>();
            foreach (var (eventType, detail) in filtered.Reverse())
            {
                var matching = entries.LastOrDefault(e => e.EventType == eventType && e.Detail == detail);
                var ts = matching?.Timestamp;
                var line = ConfirmedChangeList.DisplayLine(
                    eventType, detail, ts is { Length: >= 19 } ? ts[..19] : ts);
                items.Add(line);
            }
            if (items.Count == 0)
                items.Add("（暂无已确认的改动）");
            _confirmedList.ItemsSource = null;
            _confirmedList.ItemsSource = items;
        }
        catch
        {
            _confirmedList.ItemsSource = new[] { "（暂无已确认的改动）" };
        }
    }

    private void RefreshAudit()
    {
        try
        {
            var entries = _host.Audit.ReadRecent(8);
            var items = new List<string>();
            foreach (var e in entries.Reverse())
            {
                items.Add(FormatAuditLine(e));
            }
            if (items.Count == 0)
                items.Add("（暂无审计记录）");
            _auditList.ItemsSource = null;
            _auditList.ItemsSource = items;
        }
        catch
        {
            _auditList.ItemsSource = new[] { "（暂无审计记录）" };
        }
    }

    private static string FormatAuditLine(AuditEntry e)
    {
        var ts = e.Timestamp.Length >= 19 ? e.Timestamp[..19] : e.Timestamp;
        var label = e.EventType switch
        {
            "tool_confirmed" => "已确认执行",
            "tool_denied_user" => WriteConfirmCopy.DeniedMessage,
            "tool_denied_perm" => "权限不足",
            "tool_auto_relaxed" => "宽松自动允许",
            "tool_auto_fulltrust" => "完全信任自动允许",
            "tool_admin_auto" => "已自动执行",
            "trust_level" => "信任等级",
            "session_refresh" => "刷新会话码",
            _ => e.EventType
        };
        // 人话摘要；剥掉 tool=/source= 等，避免亲友界面出现内部字段
        var detail = ConfirmedChangeList.HumanizeDetail(e.Detail);
        if (string.IsNullOrWhiteSpace(detail) &&
            !string.Equals(e.EventType, "trust_level", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(e.EventType, "session_refresh", StringComparison.OrdinalIgnoreCase))
        {
            detail = "";
        }
        else if (string.IsNullOrWhiteSpace(detail))
        {
            // 信任/刷新等保留短说明，但过滤技术泄漏
            detail = FriendConnectGuide.LooksTechnicalLeak(e.Detail) ? "" : (e.Detail.Length > 60 ? e.Detail[..57] + "…" : e.Detail);
        }
        if (FriendConnectGuide.LooksTechnicalLeak(detail))
            detail = "";
        return string.IsNullOrEmpty(detail) ? $"{ts}  {label}" : $"{ts}  {label}  ·  {detail}";
    }

    internal void SeedConfirmedForShot(string humanDetail)
    {
        _host.Audit.Write(
            "tool_confirmed",
            $"tool=RunCommand; source=guest; via=ui; result=approved; at={DateTimeOffset.UtcNow:o}; {humanDetail}");
        RefreshConfirmedChanges();
        RefreshAudit();
    }

    internal void SeedDeniedForShot(string humanDetail)
    {
        _host.Audit.Write(
            "tool_denied_user",
            $"tool=RunCommand; source=guest; via=ui; result=denied; at={DateTimeOffset.UtcNow:o}; {humanDetail}");
        RefreshConfirmedChanges();
        RefreshAudit();
    }

    internal RefreshSessionConfirmWindow ShowRefreshConfirmForShot()
    {
        var dlg = new RefreshSessionConfirmWindow();
        dlg.Show(this);
        return dlg;
    }

    internal string ApplyRefreshForShot()
    {
        var code = _host.RefreshSessionCode();
        _codeBlock.Text = "会话码：" + code;
        _pwdBlock.Text = "验证密码：" + _host.SessionPassword;
        RefreshTrustUi();
        RefreshConfirmedChanges();
        RefreshAudit();
        _status.Text = "已换新的会话码和验证密码。之前发出去的码对方将连不上。";
        return code;
    }

    internal void ScrollListsIntoViewForShot()
    {
        // 确保「本次协助已确认的改动 / 最近审计」进入视口（验收截图用）
        _scroll.Offset = new Vector(0, double.MaxValue);
        _confirmedList.InvalidateVisual();
        _auditList.InvalidateVisual();
        InvalidateVisual();
    }

    internal void OpenAdvancedForShot()
    {
        if (!_advancedOpen) ToggleAdvanced();
    }

    internal void CloseAdvancedForShot()
    {
        if (_advancedOpen) ToggleAdvanced();
    }

    internal void ApplyTrustForShot(TrustLevelKind level)
    {
        if (level == TrustLevelKind.FullTrust)
            _host.EnableFullTrustAfterConfirm();
        else
            _host.SetTrustLevelPreference(level);
        RefreshTrustUi();
    }

    internal FullTrustConfirmWindow ShowFullTrustConfirmForShot()
    {
        var dlg = new FullTrustConfirmWindow();
        dlg.Show(this);
        return dlg;
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
