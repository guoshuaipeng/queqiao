using System.Linq;
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
    private readonly TextBlock _pairedHeading;
    private readonly ListBox _pairedList;
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
    private TextBox _accountUserBox = null!;
    private TextBox _accountPassBox = null!;
    private TextBlock _accountBindStatus = null!;
    private TextBlock _mcpFeedback = null!;
    private readonly HelperUiSettings _settings;
    private bool _advancedOpen;
    private bool _suppressTrustRadio;
    private bool _ending;
    private bool _allowClose;

    public MainWindow(AgentHost host, string? shotDir, bool enableReturnHome = false)
    {
        _host = host;
        _shotMode = shotDir is not null;
        _enableReturnHome = enableReturnHome;
        _settings = HelperUiSettings.Load();

        Title = "鹊桥远程协助";
        Width = 520;
        Height = 620;
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

        _pairedHeading = new TextBlock
        {
            Text = "已连接的协助方",
            Foreground = Brush.Parse("#5F6368"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        root.Children.Add(_pairedHeading);
        _pairedList = new ListBox
        {
            Name = "pairedClients",
            Height = 56,
            MinHeight = 48
        };
        _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
        root.Children.Add(_pairedList);

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
            _status.Text = FriendConnectGuide.StatusReadyToShare;
            _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
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

        // R014：被控登录绑定账号 → 设备上线（与 Windows HelperUiSettings + AgentHost 约定一致）
        stack.Children.Add(new TextBlock
        {
            Name = "accountBindTitle",
            Text = "账号（可选）",
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 0)
        });
        stack.Children.Add(new TextBlock
        {
            Name = "accountBindBlurb",
            Text = "登录后，这台电脑会出现在该账号的设备列表里，方便你从另一台免密连过来。",
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });
        var accRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        accRow.Children.Add(new TextBlock
        {
            Name = "accountUserLabel",
            Text = "账号",
            VerticalAlignment = VerticalAlignment.Center
        });
        _accountUserBox = new TextBox
        {
            Name = "accountUserBox",
            Width = 120,
            Text = _settings.AccountUser ?? ""
        };
        accRow.Children.Add(_accountUserBox);
        accRow.Children.Add(new TextBlock
        {
            Name = "accountPassLabel",
            Text = "密码",
            VerticalAlignment = VerticalAlignment.Center
        });
        _accountPassBox = new TextBox
        {
            Name = "accountPassBox",
            Width = 120,
            PasswordChar = '●'
        };
        accRow.Children.Add(_accountPassBox);
        var btnAccLogin = new Button { Name = "accountBindLoginBtn", Content = "登录", Width = 60, Height = 32 };
        btnAccLogin.Click += async (_, _) => await BindAccountAsync(register: false);
        accRow.Children.Add(btnAccLogin);
        var btnAccReg = new Button { Name = "accountBindRegisterBtn", Content = "注册", Width = 60, Height = 32 };
        btnAccReg.Click += async (_, _) => await BindAccountAsync(register: true);
        accRow.Children.Add(btnAccReg);
        stack.Children.Add(accRow);
        _accountBindStatus = new TextBlock
        {
            Name = "accountBindStatus",
            Text = _host.AccountBound ? "已绑定账号，本机可出现在设备列表" : "尚未绑定账号",
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(_accountBindStatus);

        // R018：可选 MCP 说明（高级内；不抢「复制码+密码」；不自动监听）
        stack.Children.Add(new TextBlock
        {
            Name = "mcpGuideTitle",
            Text = McpInstallGuide.SectionTitle,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 0)
        });
        stack.Children.Add(new TextBlock
        {
            Name = "mcpOptionalNote",
            Text = "可选。接受协助时不会自动对外开启 MCP；只有你主动复制配置给智能体时才用得上。",
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new TextBlock
        {
            Name = "mcpSteps",
            Text = McpInstallGuide.StepsBlock,
            FontSize = 12,
            Foreground = Brush.Parse("#3C4043"),
            TextWrapping = TextWrapping.Wrap
        });
        var mcpRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var btnCopyMcp = new Button
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
        btnCopyMcp.Click += async (_, _) => await CopyMcpConfigAsync();
        mcpRow.Children.Add(btnCopyMcp);
        var btnCopySkill = new Button
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
        btnCopySkill.Click += async (_, _) => await CopySkillInstructionsAsync();
        mcpRow.Children.Add(btnCopySkill);
        stack.Children.Add(mcpRow);
        stack.Children.Add(new TextBlock
        {
            Name = "mcpSideNote",
            Text = "详细参数见产品文档；写操作仍须本机确认",
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });
        _mcpFeedback = new TextBlock
        {
            Name = "mcpFeedback",
            Text = "",
            FontSize = 12,
            Foreground = Brush.Parse("#0F9D58"),
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(_mcpFeedback);

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
            Height = Math.Max(Height, 980);
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


    private async Task BindAccountAsync(bool register)
    {
        var user = _accountUserBox.Text?.Trim() ?? "";
        var pass = _accountPassBox.Text ?? "";
        if (user.Length == 0 || pass.Length == 0)
        {
            _accountBindStatus.Text = "请填写用户名和密码";
            return;
        }

        var ep = string.IsNullOrWhiteSpace(_settings.RelayEndpoint)
            ? RelayEndpoint.DefaultEndpoint
            : _settings.RelayEndpoint!;
        if (!RelayEndpoint.TryParse(ep, out var host, out var port, out var err))
        {
            _accountBindStatus.Text = err;
            return;
        }

        if (_shotMode)
        {
            _settings.AccountUser = user;
            _settings.AccountToken = "shot-token";
            _settings.DeviceDisplayName = Environment.MachineName;
            _settings.Save();
            try { _host.BindAccount("shot-token"); } catch { /* ignore */ }
            _accountBindStatus.Text = "已绑定账号（截图）";
            return;
        }

        try
        {
            var tok = await RelayClient.AuthAsync(host, port, register, user, pass).ConfigureAwait(true);
            _settings.AccountUser = user;
            _settings.AccountToken = tok;
            _settings.RelayEndpoint = $"{host}:{port}";
            if (string.IsNullOrWhiteSpace(_settings.DeviceDisplayName))
                _settings.DeviceDisplayName = Environment.MachineName;
            _settings.Save();
            _host.BindAccount(tok);
            _accountBindStatus.Text = (register ? "注册" : "登录") + "成功，本机将出现在设备列表";
        }
        catch (Exception ex)
        {
            _accountBindStatus.Text = FriendConnectGuide.FriendlyOrRaw(ex);
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
            "account_bound" => "账号已绑定",
            "pair_owner_auto" => "账号免密自动配对",
            _ => e.EventType
        };

        string detail;
        if (string.Equals(e.EventType, "trust_level", StringComparison.OrdinalIgnoreCase))
        {
            // R014：信任等级变更只显示「每次询问」/「宽松」/「完全信任」等人话
            detail = TrustLevelCopy.HumanizeAuditDetail(e.Detail);
        }
        else
        {
            // 人话摘要；剥掉 tool=/source= 等，避免亲友界面出现内部字段
            detail = ConfirmedChangeList.HumanizeDetail(e.Detail);
            if (string.IsNullOrWhiteSpace(detail) &&
                string.Equals(e.EventType, "session_refresh", StringComparison.OrdinalIgnoreCase))
            {
                detail = FriendConnectGuide.LooksTechnicalLeak(e.Detail)
                    ? ""
                    : (e.Detail.Length > 60 ? e.Detail[..57] + "…" : e.Detail);
            }
            else if (string.IsNullOrWhiteSpace(detail))
            {
                detail = "";
            }
        }

        if (FriendConnectGuide.LooksTechnicalLeak(detail))
            detail = "";
        // 亲友界面禁止 preference= / AskEveryTime 等内部名
        if (detail.Contains("preference=", StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("AskEveryTime", StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("ownerLink", StringComparison.OrdinalIgnoreCase))
            detail = TrustLevelCopy.HumanizeAuditDetail(e.Detail);

        return string.IsNullOrEmpty(detail) ? $"{ts}  {label}" : $"{ts}  {label}  ·  {detail}";
    }


    internal void SeedTrustAuditForShot()
    {
        // 截图用：清空本机审计文件，避免混入历史 R013 条目；写入内部 detail，界面须人话
        try { System.IO.File.WriteAllText(_host.Audit.FilePath, ""); } catch { /* ignore */ }
        _host.Audit.Write("trust_level", "preference=AskEveryTime");
        _host.Audit.Write("trust_level", "preference=Relaxed");
        _host.Audit.Write("trust_level", "fulltrust=enabled");
        _host.Audit.Write("trust_level", "preference=AskEveryTime; reverted=1");
        _host.Audit.Write("trust_level", "fulltrust=ended");
        RefreshConfirmedChanges();
        RefreshAudit();
        // 抬高最近审计列表，使人话三档与改回/结束同屏可见
        _auditList.Height = 140;
        _auditList.MinHeight = 120;
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



    /// <summary>UiShot：还没人连上。</summary>
    internal void PreparePeerIdleForShot()
    {
        _hint.Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。";
        _status.Text = FriendConnectGuide.StatusReadyToShare;
        _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
        _scroll.Offset = new Vector(0, 0);
    }

    /// <summary>UiShot：对方已连上。</summary>
    internal void PreparePeerConnectedForShot(string? displayName = null)
    {
        _status.Text = FriendConnectGuide.StatusPeerConnected;
        _pairedList.ItemsSource = new[] { FriendConnectGuide.FormatPairedClientLabel(displayName) };
        _scroll.Offset = new Vector(0, 0);
    }

    /// <summary>UiShot：对方断开后——主状态回到可发给对方；列表（暂无）；附既有断开反馈区分构图。</summary>
    internal void PreparePeerDisconnectedForShot()
    {
        PreparePeerIdleForShot();
        _status.Text = FriendConnectGuide.StatusReadyToShare;
        // 下方另用 hint 记一次断开反馈（与 Win 状态栏「已断开 n 个会话」同级既有口径），主状态仍是可发给对方
        _hint.Text = "已断开 1 个会话。";
    }

    /// <summary>UiShot：高级内 MCP +「复制 Skill 说明」轻按钮；R018 导语仍在。</summary>
    internal void PrepareSkillEntryForShot()
    {
        PrepareMcpEntryForShot();
        _mcpFeedback.Text = "";
        _scroll.Offset = new Vector(0, 260);
    }

    /// <summary>UiShot：复制 Skill 说明后人话反馈（只出现一次）。</summary>
    internal void PrepareSkillCopiedForShot()
    {
        PrepareSkillEntryForShot();
        _mcpFeedback.Text = McpInstallGuide.SkillCopiedHint;
        _mcpFeedback.Foreground = Brush.Parse("#0F9D58");
        // 故意不改 _status，避免与反馈句重复
        _scroll.Offset = new Vector(0, 300);
    }

    /// <summary>UiShot：MCP 反馈区展示 Skill 说明剪贴板正文（与 R027 常量同一份）。</summary>
    internal void PrepareSkillClipboardPreviewForShot()
    {
        PrepareSkillEntryForShot();
        _mcpFeedback.Text = McpInstallGuide.SkillInstructionsClipboard;
        _mcpFeedback.Foreground = Brush.Parse("#202124");
        Height = Math.Max(Height, 1120);
        _scroll.Offset = new Vector(0, 240);
    }

    /// <summary>UiShot：高级内 MCP 入口 + 可选/不自动开启说明；「复制码+密码」仍为主按钮。</summary>
    internal void PrepareMcpEntryForShot()
    {
        OpenAdvancedForShot();
        _mcpFeedback.Text = "";
        _status.Text = "已连上网络，可以发给对方了";
        Height = Math.Max(Height, 1040);
        Width = Math.Max(Width, 520);
        // 滚到 MCP 区附近，同时保留上方主按钮在构图中
        _scroll.Offset = new Vector(0, 220);
    }

    /// <summary>UiShot：复制 MCP 配置后人话反馈（只出现一次）。</summary>
    internal void PrepareMcpCopiedForShot()
    {
        PrepareMcpEntryForShot();
        _mcpFeedback.Text = McpInstallGuide.CopiedOkHint;
        _mcpFeedback.Foreground = Brush.Parse("#0F9D58");
        // 故意不改 _status，避免与反馈句重复
        _scroll.Offset = new Vector(0, 280);
    }

    /// <summary>UiShot：强调「复制码+密码」实心主按钮 vs「复制 MCP 配置」轻按钮。</summary>
    internal void PrepareMcpPrimaryContrastForShot()
    {
        PrepareMcpEntryForShot();
        _hint.Text = "点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。";
        _status.Text = "「复制码+密码」是本屏主按钮；MCP 配置在高级里，可选。";
        _scroll.Offset = new Vector(0, 0);
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

    private async Task CopyMcpConfigAsync()
    {
        try
        {
            var payload = _shotMode
                ? PairQrPayload.BuildRelay(RelayEndpoint.DefaultEndpoint, "AB12CD", "3471", fingerprint: null, useTls: false)
                : _host.QrPayload;
            if (string.IsNullOrWhiteSpace(payload))
            {
                _mcpFeedback.Text = "当前没有可用的会话，请稍后再试";
                _mcpFeedback.Foreground = Brush.Parse("#D93025");
                return;
            }

            var exists = McpExeLocator.TryResolveExisting(out var exe);
            if (!exists)
                exe = McpExeLocator.Resolve();

            var clip = Clipboard;
            if (clip is null)
            {
                _mcpFeedback.Text = "复制失败：剪贴板不可用";
                _mcpFeedback.Foreground = Brush.Parse("#D93025");
                return;
            }

            await clip.SetTextAsync(McpConfigSnippet.BuildClipboardBundle(exe, payload)).ConfigureAwait(true);
            // 只写 MCP 反馈行一次，不写进状态行，避免重复（R017 教训）
            _mcpFeedback.Text = exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint;
            _mcpFeedback.Foreground = Brush.Parse("#0F9D58");
        }
        catch (Exception ex)
        {
            _mcpFeedback.Text = "复制 MCP 配置失败：" + ex.Message;
            _mcpFeedback.Foreground = Brush.Parse("#D93025");
        }
    }


    private async Task CopySkillInstructionsAsync()
    {
        try
        {
            var clip = Clipboard;
            if (clip is null)
            {
                _mcpFeedback.Text = "复制失败：剪贴板不可用";
                _mcpFeedback.Foreground = Brush.Parse("#D93025");
                return;
            }
            await clip.SetTextAsync(McpInstallGuide.SkillInstructionsClipboard).ConfigureAwait(true);
            // 只写 MCP 反馈行一次，不写进状态行，避免重复（R017/R018 教训）
            _mcpFeedback.Text = McpInstallGuide.SkillCopiedHint;
            _mcpFeedback.Foreground = Brush.Parse("#0F9D58");
        }
        catch (Exception ex)
        {
            _mcpFeedback.Text = "复制 Skill 说明失败：" + ex.Message;
            _mcpFeedback.Foreground = Brush.Parse("#D93025");
        }
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
        var clients = _host.ListPairedClients();
        RefreshPairedList(clients);
        // R031：以真正配对进本机的协助方为准
        if (clients.Count > 0)
        {
            _status.Text = FriendConnectGuide.StatusPeerConnected;
        }
        else if (phase is "ready" or "waiting" or "paired")
        {
            _status.Text = FriendConnectGuide.StatusReadyToShare;
        }
        else if (!string.IsNullOrWhiteSpace(_host.LastRelayError) || phase == "error")
        {
            _status.Text = FriendConnectGuide.FailRelayUnreachable;
        }
        else
        {
            _status.Text = "正在连接网络…";
        }
    }

    private void RefreshPairedList(System.Collections.Generic.IReadOnlyList<PairedClientInfo> clients)
    {
        if (clients.Count == 0)
        {
            _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
            return;
        }
        _pairedList.ItemsSource = clients
            .Select(c => FriendConnectGuide.FormatPairedClientLabel(c.ClientName, c.OwnerLink))
            .ToArray();
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
