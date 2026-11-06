# -*- coding: utf-8 -*-
"""R015: human device list + solid connect button + offline hint + tools shot."""
from pathlib import Path

hw_path = Path(r"E:\IdeaProjects\remote-assist\src\RemoteAssist.Agent.Avalonia\Views\HelperWindow.cs")
t = hw_path.read_text(encoding="utf-8")

# 1) Add DeviceListItem nested type before closing of class - insert before last }
# Better: add after fields, and replace methods.

# Insert DeviceListItem class after namespace opening... actually as nested private sealed class at end before final }

device_item = '''
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

'''

if "private sealed class DeviceListItem" not in t:
    # insert before last closing brace of HelperWindow
    idx = t.rfind("\n}")
    if idx < 0:
        raise SystemExit("no closing brace")
    t = t[:idx] + "\n" + device_item + t[idx:]
    print("added DeviceListItem")

# 2) Replace UpdateOwnConnectUi
old_update = '''    private void UpdateOwnConnectUi()
    {
        if (_deviceList.SelectedItem is null)
        {
            _btnOwnConnect.IsEnabled = false;
            _btnOwnConnect.Content = "连接这台电脑";
            _ownSelectHint.Text = "请先点选一台自己的电脑";
            return;
        }

        var row = _deviceList.SelectedItem.ToString() ?? "";
        var parts = row.Split('\\t');
        var name = parts.Length > 1 ? parts[1] : parts[0];
        if (string.IsNullOrWhiteSpace(name)) name = "这台电脑";
        _btnOwnConnect.IsEnabled = _client is null;
        _btnOwnConnect.Content = "连接：" + name;
        _ownSelectHint.Text = "将免密连接「" + name + "」";
    }'''

new_update = '''    private void UpdateOwnConnectUi()
    {
        if (_deviceList.SelectedItem is null)
        {
            ApplyOwnConnectStyle(enabled: false);
            _btnOwnConnect.Content = "连接这台电脑";
            _ownSelectHint.Text = "请先点选一台自己的电脑";
            return;
        }

        if (_deviceList.SelectedItem is not DeviceListItem device)
        {
            ApplyOwnConnectStyle(enabled: false);
            _btnOwnConnect.Content = "连接这台电脑";
            _ownSelectHint.Text = "请先点选一台自己的电脑";
            return;
        }

        var name = string.IsNullOrWhiteSpace(device.Name) ? "这台电脑" : device.Name.Trim();
        if (!device.Online)
        {
            ApplyOwnConnectStyle(enabled: false);
            _btnOwnConnect.Content = "连接：" + name;
            _ownSelectHint.Text = "这台电脑现在不在线";
            return;
        }

        // 在线且尚未连接：实心绿可点（与上方会话码「连接」同级可点感）
        var canClick = _client is null;
        ApplyOwnConnectStyle(enabled: canClick);
        _btnOwnConnect.Content = "连接：" + name;
        _ownSelectHint.Text = "将免密连接「" + name + "」";
    }

    /// <summary>启用=实心绿；禁用=灰色。一眼能分，不只靠文案。</summary>
    private void ApplyOwnConnectStyle(bool enabled)
    {
        _btnOwnConnect.IsEnabled = enabled;
        if (enabled)
        {
            _btnOwnConnect.Background = Brush.Parse("#0F9D58");
            _btnOwnConnect.Foreground = Brushes.White;
            _btnOwnConnect.FontWeight = FontWeight.Bold;
        }
        else
        {
            _btnOwnConnect.Background = Brush.Parse("#E8EAED");
            _btnOwnConnect.Foreground = Brush.Parse("#9AA0A6");
            _btnOwnConnect.FontWeight = FontWeight.Normal;
        }
    }'''

if old_update not in t:
    raise SystemExit("UpdateOwnConnectUi block missing")
t = t.replace(old_update, new_update)
print("UpdateOwnConnectUi replaced")

# 3) SetConnectedUi also enable tool buttons
old_set = '''    private void SetConnectedUi(bool connected)
    {
        _btnConnect.IsEnabled = !connected;
        _pasteBox.IsEnabled = !connected;
        _toolCard.IsVisible = connected;
        UpdateOwnConnectUi();
    }'''
new_set = '''    private void SetConnectedUi(bool connected)
    {
        _btnConnect.IsEnabled = !connected;
        _pasteBox.IsEnabled = !connected;
        _toolCard.IsVisible = connected;
        // 连上后常用操作必须可用（不要仍呈禁用灰）
        _btnProcs.IsEnabled = connected;
        _btnLogs.IsEnabled = connected;
        _btnRun.IsEnabled = connected;
        if (connected)
        {
            StyleToolButtonEnabled(_btnProcs);
            StyleToolButtonEnabled(_btnLogs);
            StyleToolButtonEnabled(_btnRun);
        }
        UpdateOwnConnectUi();
    }

    private static void StyleToolButtonEnabled(Button b)
    {
        b.IsEnabled = true;
        b.Background = Brush.Parse("#E8F0FE");
        b.Foreground = Brush.Parse("#1A73E8");
        b.FontWeight = FontWeight.SemiBold;
    }'''
if old_set not in t:
    raise SystemExit("SetConnectedUi missing")
t = t.replace(old_set, new_set)
print("SetConnectedUi replaced")

# 4) RefreshDevicesAsync use DeviceListItem
old_refresh_add = '''            _deviceList.Items.Clear();
            foreach (var d in list)
                _deviceList.Items.Add($"{d.Id}\\t{d.Name}\\t{(d.Online ? "在线" : "离线")}");
            UpdateOwnConnectUi();'''
new_refresh_add = '''            _deviceList.Items.Clear();
            foreach (var d in list)
            {
                _deviceList.Items.Add(new DeviceListItem
                {
                    Id = d.Id,
                    Name = string.IsNullOrWhiteSpace(d.Name) ? "这台电脑" : d.Name,
                    Online = d.Online
                });
            }
            UpdateOwnConnectUi();'''
if old_refresh_add not in t:
    raise SystemExit("RefreshDevices add missing")
t = t.replace(old_refresh_add, new_refresh_add)
print("RefreshDevices humanized")

# 5) ConnectOwnedDeviceAsync get id from DeviceListItem; reject offline
old_conn = '''        if (_deviceList.SelectedItem is null)
        {
            _accountStatus.Text = "请先点选一台自己的电脑";
            return;
        }

        var row = _deviceList.SelectedItem.ToString() ?? "";
        var deviceId = row.Split('\\t')[0];
        if (!RelayEndpoint.TryParse(RelayEndpointText, out var host, out var port, out var err))'''
new_conn = '''        if (_deviceList.SelectedItem is not DeviceListItem device)
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
        if (!RelayEndpoint.TryParse(RelayEndpointText, out var host, out var port, out var err))'''
if old_conn not in t:
    raise SystemExit("ConnectOwned parse missing")
t = t.replace(old_conn, new_conn)
print("ConnectOwnedDeviceAsync fixed")

# 6) Initial own button style disabled gray
old_btn = '''        _btnOwnConnect = new Button
        {
            Name = "ownConnectBtn",
            Content = "连接这台电脑",
            Width = 160,
            Height = 36,
            IsEnabled = false
        };
        _btnOwnConnect.Click += async (_, _) => await ConnectOwnedDeviceAsync();'''
new_btn = '''        _btnOwnConnect = new Button
        {
            Name = "ownConnectBtn",
            Content = "连接这台电脑",
            Width = 180,
            Height = 36,
            IsEnabled = false,
            Background = Brush.Parse("#E8EAED"),
            Foreground = Brush.Parse("#9AA0A6")
        };
        _btnOwnConnect.Click += async (_, _) => await ConnectOwnedDeviceAsync();'''
if old_btn not in t:
    raise SystemExit("own btn create missing")
t = t.replace(old_btn, new_btn)
print("own btn initial style")

# 7) Shot helpers
old_shots = '''    internal void PrepareDevicesForShot(bool selected)
    {
        _deviceList.Items.Clear();
        _deviceList.Items.Add("dev-living\\t客厅电脑\\t在线");
        _deviceList.Items.Add("dev-office\\t书房电脑\\t离线");
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
}'''
new_shots = '''    internal void PrepareDevicesForShot(bool selectedOnline, bool selectedOffline = false)
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
        SetConnectedUi(true);
        _status.Text = "已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）";
        _accountStatus.Text = "免密已连接（截图）";
        _output.Text = "";
        // 抬高窗口，确保常用操作三按钮入镜
        Height = Math.Max(Height, 860);
        Width = Math.Max(Width, 720);
    }
}
'''
# PrepareAccountForShot still calls PrepareDevicesForShot(selected) - need to keep bool selected as selectedOnline
old_prep_acc = '''    internal void PrepareAccountForShot(bool selected)
    {
        _accountUserBox.Text = "demo";
        _accountPassBox.Text = "••••••";
        _accountStatus.Text = "已登录（截图）";
        PrepareDevicesForShot(selected);
    }'''
new_prep_acc = '''    internal void PrepareAccountForShot(bool selected)
    {
        _accountUserBox.Text = "demo";
        _accountPassBox.Text = "••••••";
        _accountStatus.Text = "已登录（截图）";
        PrepareDevicesForShot(selectedOnline: selected, selectedOffline: false);
    }'''

if old_prep_acc not in t:
    raise SystemExit("PrepareAccountForShot missing")
t = t.replace(old_prep_acc, new_prep_acc)

if old_shots not in t:
    # maybe already partially changed - show nearby
    raise SystemExit("shot helpers block missing:\n" + t[t.find("PrepareDevicesForShot"):t.find("PrepareDevicesForShot")+500])
t = t.replace(old_shots, new_shots)
print("shot helpers updated")

# Also fix AccountAuthAsync shot path PrepareDevicesForShot(selected: false) - still valid with named? 
# It calls PrepareDevicesForShot(selected: false) - our new signature is (bool selectedOnline, bool selectedOffline = false)
# named selected: won't work! Fix call sites.
t = t.replace("PrepareDevicesForShot(selected: false)", "PrepareDevicesForShot(selectedOnline: false)")
t = t.replace("PrepareDevicesForShot(selected: true)", "PrepareDevicesForShot(selectedOnline: true)")
print("call sites fixed")

hw_path.write_text(t, encoding="utf-8")
print("HelperWindow written OK")
