from pathlib import Path
p = Path(r"src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
# Add PrepareDeviceListHumanForShot if missing
if "PrepareDeviceListHumanForShot" not in t:
    needle = "    /// <summary>UiShot：账号标签 + 设备列表；selected 控制是否点选在线设备。</summary>"
    insert = '''    /// <summary>UiShot：列表仅人话名称+在线/离线（与未选中截图文案区分）。</summary>
    internal void PrepareDeviceListHumanForShot()
    {
        _accountUserBox.Text = "demo";
        _accountPassBox.Text = "••••••";
        _accountStatus.Text = "设备 2 台";
        PrepareDevicesForShot(selectedOnline: false, selectedOffline: false);
    }

'''
    if needle not in t:
        raise SystemExit("needle missing")
    t = t.replace(needle, insert + needle)
    p.write_text(t, encoding="utf-8")
    print("added PrepareDeviceListHumanForShot")
else:
    print("already has PrepareDeviceListHumanForShot")
