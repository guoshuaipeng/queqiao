from pathlib import Path

Q = chr(34)
path = Path(r"src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
text = path.read_text(encoding="utf-8")
lines = text.splitlines(keepends=True)

def find_line(s, start=0):
    for i, l in enumerate(lines[start:], start):
        if s in l:
            return i
    raise SystemExit("not found: " + s)

# 1) After "连接对方" title, insert Windows-aligned hint row + 复制安装说明 light button
# Replace the current paste hint block start
old = '''        pairStack.Children.Add(new TextBlock
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
'''
new = f'''        pairStack.Children.Add(new TextBlock
        {{
            Text = {Q}连接对方{Q},
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        }});
        var installRow = new StackPanel
        {{
            Orientation = Orientation.Horizontal,
            Spacing = 12
        }};
        installRow.Children.Add(new TextBlock
        {{
            Text = {Q}先让对方装好「鹊桥远程协助」{Q},
            Foreground = Brush.Parse({Q}#5F6368{Q}),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        }});
        _btnInstallGuide = new Button
        {{
            Name = {Q}copyInstallGuideBtn{Q},
            Content = {Q}复制安装说明{Q},
            Width = 140,
            Height = 28,
            Background = Brushes.White,
            Foreground = Brush.Parse({Q}#1A73E8{Q}),
            BorderBrush = Brush.Parse({Q}#DADEE3{Q}),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.Normal
        }};
        _btnInstallGuide.Click += async (_, _) => await CopyInstallInstructionsAsync();
        installRow.Children.Add(_btnInstallGuide);
        pairStack.Children.Add(installRow);
        pairStack.Children.Add(new TextBlock
        {{
            Text = {Q}粘贴对方发来的会话码和验证密码{Q},
            Foreground = Brush.Parse({Q}#5F6368{Q})
        }});
'''
joined = "".join(lines)
if old not in joined:
    raise SystemExit("pair title block missing")
joined = joined.replace(old, new, 1)
lines = joined.splitlines(keepends=True)
print("install row ok")

# 2) Add field for _btnInstallGuide and _mcpHint
old_f = '''    private readonly Button _btnOwnConnect;
    private readonly TextBlock _ownSelectHint;
    private readonly TextBlock _accountStatus;
'''
new_f = '''    private readonly Button _btnOwnConnect;
    private readonly TextBlock _ownSelectHint;
    private readonly TextBlock _accountStatus;
    private readonly Button _btnInstallGuide;
    private readonly Button _btnCopyMcp;
    private readonly TextBlock _mcpHint;
'''
joined = "".join(lines)
if old_f not in joined:
    raise SystemExit("fields missing")
joined = joined.replace(old_f, new_f, 1)
lines = joined.splitlines(keepends=True)

# 3) Insert MCP card after accountCard add, before _toolCard
i = find_line("        body.Children.Add(accountCard);")
mcp = f'''        body.Children.Add(accountCard);

        // ---- R017：MCP 安装说明（与 Windows Helper 同文案/轻按钮；始终可见，不因未连接隐藏）----
        var mcpCard = new Border
        {{
            Background = Brushes.White,
            BorderBrush = Brush.Parse({Q}#DADEE3{Q}),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        }};
        var mcpStack = new StackPanel {{ Spacing = 10 }};
        mcpStack.Children.Add(new TextBlock
        {{
            Text = McpInstallGuide.SectionTitle,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        }});
        mcpStack.Children.Add(new TextBlock
        {{
            Text = McpInstallGuide.StepsBlock,
            Foreground = Brush.Parse({Q}#3C4043{Q}),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13
        }});
        var mcpRow = new StackPanel {{ Orientation = Orientation.Horizontal, Spacing = 12 }};
        _btnCopyMcp = new Button
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
        _btnCopyMcp.Click += async (_, _) => await CopyMcpConfigFromUiAsync();
        mcpRow.Children.Add(_btnCopyMcp);
        mcpRow.Children.Add(new TextBlock
        {{
            Text = {Q}详细参数见产品文档；写操作仍须对方本机确认{Q},
            Foreground = Brush.Parse({Q}#5F6368{Q}),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        }});
        mcpStack.Children.Add(mcpRow);
        _mcpHint = new TextBlock
        {{
            Text = {Q}{Q},
            Foreground = Brush.Parse({Q}#0F9D58{Q}),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        }};
        mcpStack.Children.Add(_mcpHint);
        mcpCard.Child = mcpStack;
        body.Children.Add(mcpCard);

'''
# replace the single Add(accountCard) line with mcp block that includes Add(accountCard)
if lines[i].strip() != "body.Children.Add(accountCard);":
    raise SystemExit("unexpected account add line: " + repr(lines[i]))
lines[i:i+1] = [mcp]
print("mcp card ok")
lines = "".join(lines).splitlines(keepends=True)

# 4) Insert CopyInstallInstructionsAsync + CopyMcpConfigFromUiAsync before PasteFromClipboardAsync or after Disconnect
i = find_line("    private async Task PasteFromClipboardAsync()")
methods = f'''    private async Task CopyInstallInstructionsAsync()
    {{
        try
        {{
            var clip = Clipboard;
            if (clip is null)
            {{
                _failHint.Text = {Q}复制失败：剪贴板不可用{Q};
                return;
            }}
            await clip.SetTextAsync(WriteConfirmCopy.InstallInstructionsClipboard).ConfigureAwait(true);
            _preview.Text = WriteConfirmCopy.InstallCopiedHint;
            _preview.Foreground = Brush.Parse({Q}#0F9D58{Q});
            _mcpHint.Text = {Q}{Q};
            _failHint.Text = {Q}{Q};
            _status.Text = WriteConfirmCopy.InstallCopiedHint;
        }}
        catch (Exception ex)
        {{
            _failHint.Text = {Q}复制失败：{Q} + ex.Message;
        }}
    }}

    private async Task CopyMcpConfigFromUiAsync()
    {{
        try
        {{
            if (!TryBuildOptions(out var opts, out _, out _))
            {{
                _failHint.Text = McpInstallGuide.NeedCodePasswordHint;
                _preview.Foreground = Brush.Parse({Q}#C5221F{Q});
                _mcpHint.Text = {Q}{Q};
                return;
            }}

            var useTls = !opts.Insecure && !string.IsNullOrWhiteSpace(opts.Fingerprint);
            string payload;
            if (opts.ViaRelay)
            {{
                var relay = string.IsNullOrWhiteSpace(opts.RelayEndpoint)
                    ? RelayEndpoint.DefaultEndpoint
                    : opts.RelayEndpoint!;
                var pwd = opts.Password ?? {Q}{Q};
                if (!useTls)
                    payload = PairQrPayload.BuildRelay(relay, opts.Code, pwd, fingerprint: null, useTls: false);
                else
                    payload = PairQrPayload.BuildRelay(relay, opts.Code, pwd, opts.Fingerprint, useTls: true);
            }}
            else
            {{
                if (!useTls)
                    payload = PairQrPayload.BuildLan(opts.Host, opts.Port, opts.Code, fingerprint: null, useTls: false);
                else
                    payload = PairQrPayload.BuildLan(opts.Host, opts.Port, opts.Code, opts.Fingerprint, useTls: true);
            }}

            var exists = McpExeLocator.TryResolveExisting(out var exe);
            if (!exists)
                exe = McpExeLocator.Resolve();

            var clip = Clipboard;
            if (clip is null)
            {{
                _failHint.Text = {Q}复制失败：剪贴板不可用{Q};
                return;
            }}
            await clip.SetTextAsync(McpConfigSnippet.BuildClipboardBundle(exe, payload)).ConfigureAwait(true);
            var hint = exists ? McpInstallGuide.CopiedOkHint : McpInstallGuide.CopiedPlaceholderHint;
            _preview.Text = hint;
            _preview.Foreground = Brush.Parse({Q}#0F9D58{Q});
            _mcpHint.Text = hint;
            _failHint.Text = {Q}{Q};
            _status.Text = hint;
        }}
        catch (Exception ex)
        {{
            _failHint.Text = {Q}复制 MCP 配置失败：{Q} + ex.Message;
        }}
    }}

'''
lines[i:i] = [methods]
print("methods ok")
lines = "".join(lines).splitlines(keepends=True)

# 5) Shot helpers before PrepareClose or near other Prepare*
# Find PrepareLoggedInUnconnectedForShot end and add after PrepareDenied or at end of shot helpers
i = find_line("    internal void PrepareLoggedInUnconnectedForShot()")
# find end of that method
j = i
brace = 0
started = False
while j < len(lines):
    brace += lines[j].count("{")
    brace -= lines[j].count("}")
    if "{" in lines[j]:
        started = True
    j += 1
    if started and brace == 0:
        break
shots = f'''
    /// <summary>UiShot：MCP / 复制安装说明入口可见（轻按钮，非实心绿）。</summary>
    internal void PrepareInstallGuideEntryForShot()
    {{
        PrepareIdleForShot();
        _pasteBox.Text = {Q}{Q};
        _preview.Text = {Q}粘贴后点「连接」{Q};
        _preview.Foreground = Brush.Parse({Q}#5F6368{Q});
        _mcpHint.Text = {Q}{Q};
        _failHint.Text = {Q}{Q};
        _status.Text = {Q}状态：未连接{Q};
        Height = Math.Max(Height, 900);
    }}

    /// <summary>UiShot：已点「复制安装说明」后人话反馈（Windows 同等文案）。</summary>
    internal void PrepareInstallCopiedForShot()
    {{
        PrepareInstallGuideEntryForShot();
        _preview.Text = WriteConfirmCopy.InstallCopiedHint;
        _preview.Foreground = Brush.Parse({Q}#0F9D58{Q});
        _status.Text = WriteConfirmCopy.InstallCopiedHint;
        _mcpHint.Text = {Q}{Q};
    }}

'''
lines[j:j] = [shots]
print("shots ok")

path.write_text("".join(lines), encoding="utf-8")
print("WROTE", len("".join(lines).splitlines()))
