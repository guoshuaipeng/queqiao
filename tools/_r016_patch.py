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

def replace_brace_block(sig, new_body):
    global lines
    i = find_line(sig)
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
    lines[i:j] = [new_body]
    print("replaced", sig.strip(), "lines", i, j)

# ---- fields ----
old_fields = '''    private readonly Border _toolCard;
    private readonly TextBox _output;
    private readonly Button _btnProcs;
    private readonly Button _btnLogs;
    private readonly Button _btnRun;
'''
new_fields = '''    private readonly Border _toolCard;
    private readonly TextBox _output;
    private readonly TextBox _argBox;
    private readonly Button _btnProcs;
    private readonly Button _btnLs;
    private readonly Button _btnLogs;
    private readonly Button _btnCat;
    private readonly Button _btnRun;
'''
if old_fields not in "".join(lines):
    raise SystemExit("fields block missing")
text = "".join(lines).replace(old_fields, new_fields)
lines = text.splitlines(keepends=True)

# ---- tool card UI block from _toolCard = new Border through body.Children.Add(_toolCard); ----
i = find_line("        _toolCard = new Border")
j = find_line("        body.Children.Add(_toolCard);", i)
# include the Add line
j += 1
tool_ui = f'''        _toolCard = new Border
        {{
            Background = Brushes.White,
            BorderBrush = Brush.Parse({Q}#DADEE3{Q}),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            IsVisible = false
        }};
        var toolStack = new StackPanel {{ Spacing = 10 }};
        toolStack.Children.Add(new TextBlock
        {{
            Text = {Q}常用操作{Q},
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        }});
        toolStack.Children.Add(new TextBlock
        {{
            Text = {Q}建议先看正在运行的程序，再决定要不要改{Q},
            FontSize = 12,
            Foreground = Brush.Parse({Q}#5F6368{Q}),
            TextWrapping = TextWrapping.Wrap
        }});
        var tools = new WrapPanel {{ MaxWidth = 680 }};
        _btnProcs = MakeToolButton({Q}查看正在运行的程序{Q}, 180);
        _btnProcs.Click += async (_, _) => await RunToolAsync(ToolKind.ListProcesses, new Dictionary<string, string?>());
        _btnLs = MakeToolButton({Q}查看文件夹{Q}, 110);
        _btnLs.Click += async (_, _) =>
        {{
            var p = RequireArg({Q}path{Q});
            if (p is null) return;
            await RunToolAsync(ToolKind.ListDir, new Dictionary<string, string?> {{ [{Q}path{Q}] = p }});
        }};
        _btnLogs = MakeToolButton({Q}查看日志{Q}, 100);
        _btnLogs.Click += async (_, _) =>
        {{
            var p = RequireArg({Q}path{Q});
            if (p is null) return;
            await RunToolAsync(ToolKind.ReadLogTail, new Dictionary<string, string?>
            {{
                [{Q}path{Q}] = p,
                [{Q}lines{Q}] = {Q}80{Q}
            }});
        }};
        _btnCat = MakeToolButton({Q}查看文件{Q}, 100);
        _btnCat.Click += async (_, _) =>
        {{
            var p = RequireArg({Q}path{Q});
            if (p is null) return;
            await RunToolAsync(ToolKind.ReadFile, new Dictionary<string, string?> {{ [{Q}path{Q}] = p }});
        }};
        _btnRun = MakeToolButton({Q}提议一项改动{Q}, 120);
        _btnRun.Click += async (_, _) =>
        {{
            var c = RequireArg({Q}command{Q});
            if (c is null) return;
            await RunToolAsync(ToolKind.RunCommand, new Dictionary<string, string?> {{ [{Q}command{Q}] = c }});
        }};
        foreach (var b in new[] {{ _btnProcs, _btnLs, _btnLogs, _btnCat, _btnRun }})
        {{
            b.Margin = new Thickness(0, 0, 10, 8);
            tools.Children.Add(b);
        }}
        toolStack.Children.Add(tools);

        toolStack.Children.Add(new TextBlock
        {{
            Text = {Q}路径 / 命令参数{Q},
            FontSize = 12,
            Foreground = Brush.Parse({Q}#5F6368{Q})
        }});
        _argBox = new TextBox
        {{
            Watermark = OperatingSystem.IsWindows()
                ? {Q}例如 C:\\Users\\你的文件夹{Q}
                : {Q}例如 /home/你的文件夹{Q},
            MinHeight = 32,
            FontSize = 13
        }};
        toolStack.Children.Add(_argBox);

        _output = new TextBox
        {{
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 120,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily({Q}Consolas,Menlo,monospace{Q}),
            FontSize = 12
        }};
        toolStack.Children.Add(_output);
        _toolCard.Child = toolStack;
        body.Children.Add(_toolCard);
'''
lines[i:j] = [tool_ui]
print("tool ui", i, j)
lines = "".join(lines).splitlines(keepends=True)

# ---- SetConnectedUi ----
replace_brace_block("    private void SetConnectedUi(bool connected)", f'''    private void SetConnectedUi(bool connected)
    {{
        _btnConnect.IsEnabled = !connected;
        _pasteBox.IsEnabled = !connected;
        if (connected)
        {{
            _btnConnect.Background = Brush.Parse({Q}#E8EAED{Q});
            _btnConnect.Foreground = Brush.Parse({Q}#9AA0A6{Q});
        }}
        else
        {{
            _btnConnect.Background = Brush.Parse({Q}#0F9D58{Q});
            _btnConnect.Foreground = Brushes.White;
        }}

        _toolCard.IsVisible = connected;
        foreach (var b in new[] {{ _btnProcs, _btnLs, _btnLogs, _btnCat, _btnRun }})
        {{
            b.IsEnabled = connected;
            if (connected)
                StyleToolButtonEnabled(b);
        }}
        UpdateOwnConnectUi();
    }}

''')
lines = "".join(lines).splitlines(keepends=True)

# ---- UpdateOwnConnectUi ----
replace_brace_block("    private void UpdateOwnConnectUi()", f'''    private void UpdateOwnConnectUi()
    {{
        if (_deviceList.SelectedItem is null)
        {{
            ApplyOwnConnectStyle(OwnConnectVisual.Disabled);
            _btnOwnConnect.Content = {Q}连接这台电脑{Q};
            _ownSelectHint.Text = {Q}请先点选一台自己的电脑{Q};
            return;
        }}

        if (_deviceList.SelectedItem is not DeviceListItem device)
        {{
            ApplyOwnConnectStyle(OwnConnectVisual.Disabled);
            _btnOwnConnect.Content = {Q}连接这台电脑{Q};
            _ownSelectHint.Text = {Q}请先点选一台自己的电脑{Q};
            return;
        }}

        var name = string.IsNullOrWhiteSpace(device.Name) ? {Q}这台电脑{Q} : device.Name.Trim();
        if (!device.Online)
        {{
            ApplyOwnConnectStyle(OwnConnectVisual.Disabled);
            _btnOwnConnect.Content = {Q}连接：{Q} + name;
            _ownSelectHint.Text = {Q}这台电脑现在不在线{Q};
            return;
        }}

        if (_client is not null)
        {{
            // 已连接：账号区不再保持实心绿主权重
            ApplyOwnConnectStyle(OwnConnectVisual.ConnectedLight);
            _btnOwnConnect.Content = {Q}已连接{Q};
            _ownSelectHint.Text = {Q}免密已连接「{Q} + name + {Q}」{Q};
            return;
        }}

        ApplyOwnConnectStyle(OwnConnectVisual.Clickable);
        _btnOwnConnect.Content = {Q}连接：{Q} + name;
        _ownSelectHint.Text = {Q}将免密连接「{Q} + name + {Q}」{Q};
    }}

''')
lines = "".join(lines).splitlines(keepends=True)

# ---- ApplyOwnConnectStyle ----
replace_brace_block("    private void ApplyOwnConnectStyle(bool enabled)", f'''    private void ApplyOwnConnectStyle(OwnConnectVisual visual)
    {{
        switch (visual)
        {{
            case OwnConnectVisual.Clickable:
                _btnOwnConnect.IsEnabled = true;
                _btnOwnConnect.Background = Brush.Parse({Q}#0F9D58{Q});
                _btnOwnConnect.Foreground = Brushes.White;
                _btnOwnConnect.FontWeight = FontWeight.Bold;
                break;
            case OwnConnectVisual.ConnectedLight:
                _btnOwnConnect.IsEnabled = false;
                _btnOwnConnect.Background = Brush.Parse({Q}#F1F3F4{Q});
                _btnOwnConnect.Foreground = Brush.Parse({Q}#5F6368{Q});
                _btnOwnConnect.FontWeight = FontWeight.SemiBold;
                break;
            default:
                _btnOwnConnect.IsEnabled = false;
                _btnOwnConnect.Background = Brush.Parse({Q}#E8EAED{Q});
                _btnOwnConnect.Foreground = Brush.Parse({Q}#9AA0A6{Q});
                _btnOwnConnect.FontWeight = FontWeight.Normal;
                break;
        }}
    }}

''')
lines = "".join(lines).splitlines(keepends=True)

# ---- RequireArg before RunToolAsync ----
i = find_line("    private async Task RunToolAsync(ToolKind tool")
req = f'''    private string? RequireArg(string key)
    {{
        var v = _argBox.Text?.Trim() ?? {Q}{Q};
        if (v.Length == 0)
        {{
            var hint = OperatingSystem.IsWindows()
                ? {Q}例如 C:\\Users\\你的文件夹{Q}
                : {Q}例如 /home/你的文件夹{Q};
            _status.Text = key == {Q}command{Q}
                ? {Q}请先在上方填写要提议的改动（人话说明或命令）{Q}
                : {Q}请先在上方填写路径（{Q} + hint + {Q}）{Q};
            return null;
        }}
        return v;
    }}

'''
lines[i:i] = [req]
print("RequireArg inserted")
lines = "".join(lines).splitlines(keepends=True)

# ---- status messages for more tools ----
old_status = '''            _status.Text = tool switch
            {
                ToolKind.ListProcesses => "已查看正在运行的程序",
                ToolKind.ReadLogTail => "已查看日志",
                _ => "完成"
            };'''
new_status = '''            _status.Text = tool switch
            {
                ToolKind.ListProcesses => "已查看正在运行的程序",
                ToolKind.ListDir => "已查看文件夹",
                ToolKind.ReadLogTail => "已查看日志",
                ToolKind.ReadFile => "已查看文件",
                ToolKind.RunCommand => "已提议一项改动",
                _ => "完成"
            };'''
text = "".join(lines)
if old_status not in text:
    raise SystemExit("status switch missing")
text = text.replace(old_status, new_status)
lines = text.splitlines(keepends=True)

# ---- PrepareOwnerConnectedForShot + PrepareLoggedInUnconnected + PrepareDenied ----
# Replace PrepareOwnerConnectedForShot
replace_brace_block("    internal void PrepareOwnerConnectedForShot()", f'''    internal void PrepareOwnerConnectedForShot()
    {{
        PrepareAccountForShot(selected: true);
        // 截图态：标记已连接（免密），展开五键；账号区「已连接」轻态
        _client = null; // 先清，再靠 shot 旗标
        SetConnectedUi(true);
        // SetConnectedUi 在 _client==null 时会把账号钮设为可点绿；强制轻态
        ApplyOwnConnectStyle(OwnConnectVisual.ConnectedLight);
        _btnOwnConnect.Content = {Q}已连接{Q};
        _ownSelectHint.Text = {Q}免密已连接「客厅电脑」{Q};
        _status.Text = {Q}已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）{Q};
        _accountStatus.Text = {Q}免密已连接（截图）{Q};
        _argBox.Text = {Q}{Q};
        _argBox.Watermark = OperatingSystem.IsWindows()
            ? {Q}例如 C:\\Users\\你的文件夹{Q}
            : {Q}例如 /home/你的文件夹{Q};
        _output.Text = {Q}{Q};
        Height = Math.Max(Height, 920);
        Width = Math.Max(Width, 720);
    }}

    /// <summary>UiShot：仅登录未免密连上——常用操作整块不可见。</summary>
    internal void PrepareLoggedInUnconnectedForShot()
    {{
        PrepareAccountForShot(selected: true);
        SetConnectedUi(false);
        _status.Text = {Q}状态：未连接{Q};
        _accountStatus.Text = {Q}已登录（截图）{Q};
        _output.Text = {Q}{Q};
    }}

''')
lines = "".join(lines).splitlines(keepends=True)

# Update PrepareDeniedForShot to ensure tools visible + human output
replace_brace_block("    internal void PrepareDeniedForShot()", f'''    internal void PrepareDeniedForShot()
    {{
        PrepareOwnerConnectedForShot();
        _status.Text = WriteConfirmCopy.DeniedMessage;
        _failHint.Text = {Q}{Q};
        _output.Text = {Q}[提议一项改动] FAIL: {Q} + WriteConfirmCopy.DeniedMessage + {Q}\n{Q};
        _argBox.Text = {Q}echo r016-should-not-run{Q};
    }}

''')
lines = "".join(lines).splitlines(keepends=True)

# Add enum before DeviceListItem
if "enum OwnConnectVisual" not in "".join(lines):
    i = find_line("    /// <summary>列表展示用人话名称+在线/离线")
    enum_block = f'''    private enum OwnConnectVisual
    {{
        Disabled,
        Clickable,
        ConnectedLight
    }}

'''
    lines[i:i] = [enum_block]
    print("enum added")

# Fix PrepareOwnerConnectedForShot: SetConnectedUi(true) with _client null makes UpdateOwnConnectUi set Clickable.
# We already force ConnectedLight after. But SetConnectedUi also enables tools - good.
# Problem: _toolCard visible requires connected=true - good.
# Another problem: we need a fake "connected" for UpdateOwnConnectUi to take connected branch.
# Currently we force styles after SetConnectedUi - OK for shot.
# For real connect, _client is set before SetConnectedUi(true) - UpdateOwnConnectUi sees _client != null - OK.

path.write_text("".join(lines), encoding="utf-8")
print("WROTE", len("".join(lines).splitlines()))
