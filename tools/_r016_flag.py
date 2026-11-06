from pathlib import Path
p = Path(r"src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
if "_uiShotConnected" not in t:
    t = t.replace(
        "    private bool _busy;\n    private bool _allowClose;",
        "    private bool _busy;\n    private bool _allowClose;\n    private bool _uiShotConnected;",
    )
    t = t.replace(
        "        if (_client is not null)\n        {\n            // 已连接：账号区不再保持实心绿主权重",
        "        if (_client is not null || _uiShotConnected)\n        {\n            // 已连接：账号区不再保持实心绿主权重",
    )
    t = t.replace(
        "        _client = null;\n        SetConnectedUi(false);\n        _status.Text = \"状态：未连接\";",
        "        _client = null;\n        _uiShotConnected = false;\n        SetConnectedUi(false);\n        _status.Text = \"状态：未连接\";",
    )
    t = t.replace(
        """        // 截图态：标记已连接（免密），展开五键；账号区「已连接」轻态
        _client = null; // 先清，再靠 shot 旗标
        SetConnectedUi(true);
        // SetConnectedUi 在 _client==null 时会把账号钮设为可点绿；强制轻态
        ApplyOwnConnectStyle(OwnConnectVisual.ConnectedLight);
        _btnOwnConnect.Content = "已连接";
        _ownSelectHint.Text = "免密已连接「客厅电脑」";
""",
        """        _uiShotConnected = true;
        SetConnectedUi(true);
""",
    )
    p.write_text(t, encoding="utf-8")
    print("shot flag wired")
else:
    print("already has flag")
