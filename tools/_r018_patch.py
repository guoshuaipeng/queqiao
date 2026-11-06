from pathlib import Path

Q = chr(34)
path = Path(r"src/RemoteAssist.Agent.Avalonia/Views/MainWindow.cs")
text = path.read_text(encoding="utf-8")

# 1) Add fields
old_f = """    private TextBox _accountUserBox = null!;
    private TextBox _accountPassBox = null!;
    private TextBlock _accountBindStatus = null!;
    private readonly HelperUiSettings _settings;
"""
new_f = """    private TextBox _accountUserBox = null!;
    private TextBox _accountPassBox = null!;
    private TextBlock _accountBindStatus = null!;
    private TextBlock _mcpFeedback = null!;
    private readonly HelperUiSettings _settings;
"""
if old_f not in text:
    raise SystemExit("fields missing")
text = text.replace(old_f, new_f, 1)

# 2) Insert MCP section after accountBindStatus add, before confirmedChangesTitle
old_sec = """        stack.Children.Add(_accountBindStatus);

        stack.Children.Add(new TextBlock
        {
            Name = "confirmedChangesTitle",
            Text = TrustLevelCopy.ConfirmedChangesTitle,
"""
new_sec = f"""        stack.Children.Add(_accountBindStatus);

        // R018：可选 MCP 说明（高级内；不抢「复制码+密码」；不自动监听）
        stack.Children.Add(new TextBlock
        {{
            Name = {Q}mcpGuideTitle{Q},
            Text = McpInstallGuide.SectionTitle,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 0)
        }});
        stack.Children.Add(new TextBlock
        {{
            Name = {Q}mcpOptionalNote{Q},
            Text = {Q}可选。接受协助时不会自动对外开启 MCP；只有你主动复制配置给智能体时才用得上。{Q},
            FontSize = 12,
            Foreground = Brush.Parse({Q}#5F6368{Q}),
            TextWrapping = TextWrapping.Wrap
        }});
        stack.Children.Add(new TextBlock
        {{
            Name = {Q}mcpSteps{Q},
            Text = McpInstallGuide.StepsBlock,
            FontSize = 12,
            Foreground = Brush.Parse({Q}#3C4043{Q}),
            TextWrapping = TextWrapping.Wrap
        }});
        var mcpRow = new StackPanel {{ Orientation = Orientation.Horizontal, Spacing = 10 }};
        var btnCopyMcp = new Button
        {{
            Name = {Q}copyMcpConfigBtn{Q},
            Content = McpInstallGuide.CopyConfigButton,
            Width = 140,
            Height = 32,
            Background = Brushes.White,
            Foreground = Brush.Parse({Q}#1A73E8{Q}),
            BorderBrush = Brush.Parse({Q}#DADEE3{Q}),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.Normal
        }};
        btnCopyMcp.Click += async (_, _) => await CopyMcpConfigAsync();
        mcpRow.Children.Add(btnCopyMcp);
        mcpRow.Children.Add(new TextBlock
        {{
            Name = {Q}mcpSideNote{Q},
            Text = {Q}详细参数见产品文档；写操作仍须本机确认{Q},
            FontSize = 12,
            Foreground = Brush.Parse({Q}#5F6368{Q}),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        }});
        stack.Children.Add(mcpRow);
        _mcpFeedback = new TextBlock
        {{
            Name = {Q}mcpFeedback{Q},
            Text = {Q}{Q},
            FontSize = 12,
            Foreground = Brush.Parse({Q}#0F9D58{Q}),
            TextWrapping = TextWrapping.Wrap
        }};
        stack.Children.Add(_mcpFeedback);

        stack.Children.Add(new TextBlock
        {{
            Name = {Q}confirmedChangesTitle{Q},
            Text = TrustLevelCopy.ConfirmedChangesTitle,
"""
if old_sec not in text:
    raise SystemExit("account/confirmed section missing")
text = text.replace(old_sec, new_sec, 1)

# 3) Bump advanced open height a bit more for MCP block
text = text.replace(
    "            Height = Math.Max(Height, 860);",
    "            Height = Math.Max(Height, 980);",
    1,
)

# 4) Insert CopyMcpConfigAsync before CopyShareAsync
needle = "    private async Task CopyShareAsync()"
method = f'''    private async Task CopyMcpConfigAsync()
    {{
        try
        {{
            var payload = _shotMode
                ? PairQrPayload.BuildRelay(RelayEndpoint.DefaultEndpoint, {Q}AB12CD{Q}, {Q}3471{Q}, fingerprint: null, useTls: false)
                : _host.QrPayload;
            if (string.IsNullOrWhiteSpace(payload))
            {{
                _mcpFeedback.Text = {Q}当前没有可用的会话，请稍后再试{Q};
                _mcpFeedback.Foreground = Brush.Parse({Q}#D93025{Q});
                return;
            }}

            var exists = McpExeLocator.TryResolveExisting(out var exe);
            if (!exists)
                exe = McpExeLocator.Resolve();

            var clip = Clipboard;
            if (clip is null)
            {{
                _mcpFeedback.Text = {Q}复制失败：剪贴板不可用{Q};
                _mcpFeedback.Foreground = Brush.Parse({Q}#D93025{Q});
                return;
            }}

            await clip.SetTextAsync(McpConfigSnippet.BuildClipboardBundle(exe, payload)).ConfigureAwait(true);
            // 只写 MCP 反馈行一次，不写进状态行，避免重复（R017 教训）
            _mcpFeedback.Text = exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint;
            _mcpFeedback.Foreground = Brush.Parse({Q}#0F9D58{Q});
        }}
        catch (Exception ex)
        {{
            _mcpFeedback.Text = {Q}复制 MCP 配置失败：{Q} + ex.Message;
            _mcpFeedback.Foreground = Brush.Parse({Q}#D93025{Q});
        }}
    }}

'''
if needle not in text:
    raise SystemExit("CopyShareAsync missing")
text = text.replace(needle, method + needle, 1)

# 5) Shot helpers after OpenAdvancedForShot
old_open = """    internal void OpenAdvancedForShot()
    {
        if (!_advancedOpen) ToggleAdvanced();
    }
"""
new_open = f"""    internal void OpenAdvancedForShot()
    {{
        if (!_advancedOpen) ToggleAdvanced();
    }}

    /// <summary>UiShot：高级内 MCP 入口 + 可选/不自动开启说明；「复制码+密码」仍为主按钮。</summary>
    internal void PrepareMcpEntryForShot()
    {{
        OpenAdvancedForShot();
        _mcpFeedback.Text = {Q}{Q};
        _status.Text = {Q}已连上网络，可以发给对方了{Q};
        Height = Math.Max(Height, 1040);
        Width = Math.Max(Width, 520);
        // 滚到 MCP 区附近，同时保留上方主按钮在构图中
        _scroll.Offset = new Vector(0, 220);
    }}

    /// <summary>UiShot：复制 MCP 配置后人话反馈（只出现一次）。</summary>
    internal void PrepareMcpCopiedForShot()
    {{
        PrepareMcpEntryForShot();
        _mcpFeedback.Text = McpInstallGuide.CopiedOkHint;
        _mcpFeedback.Foreground = Brush.Parse({Q}#0F9D58{Q});
        // 故意不改 _status，避免与反馈句重复
        _scroll.Offset = new Vector(0, 280);
    }}

    /// <summary>UiShot：强调「复制码+密码」实心主按钮 vs「复制 MCP 配置」轻按钮。</summary>
    internal void PrepareMcpPrimaryContrastForShot()
    {{
        PrepareMcpEntryForShot();
        _hint.Text = {Q}点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。{Q};
        _status.Text = {Q}「复制码+密码」是本屏主按钮；MCP 配置在高级里，可选。{Q};
        _scroll.Offset = new Vector(0, 0);
    }}

"""
if old_open not in text:
    raise SystemExit("OpenAdvancedForShot missing")
text = text.replace(old_open, new_open, 1)

path.write_text(text, encoding="utf-8")
print("MainWindow patched", len(text.splitlines()))
