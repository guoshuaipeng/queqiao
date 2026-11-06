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
    print("replaced", sig, "->", i, j)

# button
i = find_line("        _btnOwnConnect = new Button")
j = i
while not lines[j].strip().startswith("};"):
    j += 1
lines[i:j+1] = [f'''        _btnOwnConnect = new Button
        {{
            Name = {Q}ownConnectBtn{Q},
            Content = {Q}连接这台电脑{Q},
            Width = 180,
            Height = 36,
            IsEnabled = false,
            Background = Brush.Parse({Q}#E8EAED{Q}),
            Foreground = Brush.Parse({Q}#9AA0A6{Q})
        }};
''']
print("btn", i)
lines = "".join(lines).splitlines(keepends=True)

replace_brace_block("    private void SetConnectedUi(bool connected)", f'''    private void SetConnectedUi(bool connected)
    {{
        _btnConnect.IsEnabled = !connected;
        _pasteBox.IsEnabled = !connected;
        _toolCard.IsVisible = connected;
        _btnProcs.IsEnabled = connected;
        _btnLogs.IsEnabled = connected;
        _btnRun.IsEnabled = connected;
        if (connected)
        {{
            StyleToolButtonEnabled(_btnProcs);
            StyleToolButtonEnabled(_btnLogs);
            StyleToolButtonEnabled(_btnRun);
        }}
        UpdateOwnConnectUi();
    }}

    private static void StyleToolButtonEnabled(Button b)
    {{
        b.IsEnabled = true;
        b.Background = Brush.Parse({Q}#E8F0FE{Q});
        b.Foreground = Brush.Parse({Q}#1A73E8{Q});
        b.FontWeight = FontWeight.SemiBold;
    }}

''')
lines = "".join(lines).splitlines(keepends=True)

replace_brace_block("    private void UpdateOwnConnectUi()", f'''    private void UpdateOwnConnectUi()
    {{
        if (_deviceList.SelectedItem is null)
        {{
            ApplyOwnConnectStyle(enabled: false);
            _btnOwnConnect.Content = {Q}连接这台电脑{Q};
            _ownSelectHint.Text = {Q}请先点选一台自己的电脑{Q};
            return;
        }}

        if (_deviceList.SelectedItem is not DeviceListItem device)
        {{
            ApplyOwnConnectStyle(enabled: false);
            _btnOwnConnect.Content = {Q}连接这台电脑{Q};
            _ownSelectHint.Text = {Q}请先点选一台自己的电脑{Q};
            return;
        }}

        var name = string.IsNullOrWhiteSpace(device.Name) ? {Q}这台电脑{Q} : device.Name.Trim();
        if (!device.Online)
        {{
            ApplyOwnConnectStyle(enabled: false);
            _btnOwnConnect.Content = {Q}连接：{Q} + name;
            _ownSelectHint.Text = {Q}这台电脑现在不在线{Q};
            return;
        }}

        var canClick = _client is null;
        ApplyOwnConnectStyle(enabled: canClick);
        _btnOwnConnect.Content = {Q}连接：{Q} + name;
        _ownSelectHint.Text = {Q}将免密连接「{Q} + name + {Q}」{Q};
        }}

    private void ApplyOwnConnectStyle(bool enabled)
    {{
        _btnOwnConnect.IsEnabled = enabled;
        if (enabled)
        {{
            _btnOwnConnect.Background = Brush.Parse({Q}#0F9D58{Q});
            _btnOwnConnect.Foreground = Brushes.White;
            _btnOwnConnect.FontWeight = FontWeight.Bold;
        }}
        else
        {{
            _btnOwnConnect.Background = Brush.Parse({Q}#E8EAED{Q});
            _btnOwnConnect.Foreground = Brush.Parse({Q}#9AA0A6{Q});
            _btnOwnConnect.FontWeight = FontWeight.Normal;
        }}
    }}

''')
# BUG: extra closing brace in UpdateOwn - fix below after write check
lines = "".join(lines).splitlines(keepends=True)

i = find_line("            foreach (var d in list)")
if "Items.Add" not in lines[i+1]:
    raise SystemExit("bad foreach")
lines[i:i+2] = [f'''            foreach (var d in list)
            {{
                _deviceList.Items.Add(new DeviceListItem
                {{
                    Id = d.Id,
                    Name = string.IsNullOrWhiteSpace(d.Name) ? {Q}这台电脑{Q} : d.Name,
                    Online = d.Online
                }});
            }}
''']
print("refresh")
lines = "".join(lines).splitlines(keepends=True)

c = find_line("    private async Task ConnectOwnedDeviceAsync()")
i = None
for k in range(c, c+50):
    if "_deviceList.SelectedItem is null" in lines[k]:
        i = k
        break
if i is None:
    raise SystemExit("no selected null")
j = i
while "var deviceId" not in lines[j]:
    j += 1
j += 1
lines[i:j] = [f'''        if (_deviceList.SelectedItem is not DeviceListItem device)
        {{
            _accountStatus.Text = {Q}请先点选一台自己的电脑{Q};
            return;
        }}
        if (!device.Online)
        {{
            _accountStatus.Text = {Q}这台电脑现在不在线{Q};
            return;
        }}

        var deviceId = device.Id;
''']
print("connect")
lines = "".join(lines).splitlines(keepends=True)

i = find_line("    /// <summary>UiShot：账号标签 + 设备列表")
k = find_line("    internal void PrepareOwnerConnectedForShot()")
j = k
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
lines[i:j] = [f'''    /// <summary>UiShot：账号标签 + 设备列表；selected 控制是否点选在线设备。</summary>
    internal void PrepareAccountForShot(bool selected)
    {{
        _accountUserBox.Text = {Q}demo{Q};
        _accountPassBox.Text = {Q}••••••{Q};
        _accountStatus.Text = {Q}已登录（截图）{Q};
        PrepareDevicesForShot(selectedOnline: selected, selectedOffline: false);
    }}

    internal void PrepareDevicesForShot(bool selectedOnline, bool selectedOffline = false)
    {{
        _deviceList.Items.Clear();
        _deviceList.Items.Add(new DeviceListItem {{ Id = {Q}dev-living{Q}, Name = {Q}客厅电脑{Q}, Online = true }});
        _deviceList.Items.Add(new DeviceListItem {{ Id = {Q}dev-office{Q}, Name = {Q}书房电脑{Q}, Online = false }});
        if (selectedOffline)
            _deviceList.SelectedIndex = 1;
        else if (selectedOnline)
            _deviceList.SelectedIndex = 0;
        else
            _deviceList.SelectedIndex = -1;
        UpdateOwnConnectUi();
    }}

    /// <summary>UiShot：选中离线设备。</summary>
    internal void PrepareOfflineSelectedForShot()
    {{
        _accountUserBox.Text = {Q}demo{Q};
        _accountPassBox.Text = {Q}••••••{Q};
        _accountStatus.Text = {Q}已登录（截图）{Q};
        PrepareDevicesForShot(selectedOnline: false, selectedOffline: true);
    }}

    internal void PrepareOwnerConnectedForShot()
    {{
        PrepareAccountForShot(selected: true);
        SetConnectedUi(true);
        _status.Text = {Q}已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）{Q};
        _accountStatus.Text = {Q}免密已连接（截图）{Q};
        _output.Text = {Q}{Q};
        Height = Math.Max(Height, 860);
        Width = Math.Max(Width, 720);
    }}

''']
print("shots")
text = "".join(lines)
text = text.replace("PrepareDevicesForShot(selected: false)", "PrepareDevicesForShot(selectedOnline: false)")
text = text.replace("PrepareDevicesForShot(selected: true)", "PrepareDevicesForShot(selectedOnline: true)")

# fix accidental extra brace in UpdateOwnConnectUi if present
bad = '''        _ownSelectHint.Text = "将免密连接「" + name + "」";
        }

    private void ApplyOwnConnectStyle'''
good = '''        _ownSelectHint.Text = "将免密连接「" + name + "」";
    }

    private void ApplyOwnConnectStyle'''
if bad in text:
    text = text.replace(bad, good)
    print("fixed extra brace")

if "class DeviceListItem" not in text:
    idx = text.rstrip().rfind("}")
    item = f'''
    /// <summary>列表展示用人话名称+在线/离线；内部 Id 不出现在 ToString。</summary>
    private sealed class DeviceListItem
    {{
        public required string Id {{ get; init; }}
        public required string Name {{ get; init; }}
        public required bool Online {{ get; init; }}

        public override string ToString()
        {{
            var name = string.IsNullOrWhiteSpace(Name) ? {Q}这台电脑{Q} : Name.Trim();
            return name + {Q}\\t{Q} + (Online ? {Q}在线{Q} : {Q}离线{Q});
        }}
    }}

'''
    text = text[:idx] + item + text[idx:]
    print("DeviceListItem added")

path.write_text(text, encoding="utf-8")
print("WROTE OK", len(text.splitlines()))
