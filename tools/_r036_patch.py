from pathlib import Path

# --- HelperSessionCopy ---
p = Path("src/RemoteAssist.Protocol/HelperSessionCopy.cs")
t = p.read_text(encoding="utf-8")
if "ConnectedButtonText" not in t:
    old = '''    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;
}
'''
    new = '''    /// <summary>会话码区主按钮：未连上时可点实心绿。</summary>
    public const string ConnectButtonText = "连接";

    /// <summary>会话码区主按钮：已连上后不可点（非实心绿）。账号区免密连上后的文案也可复用此句。</summary>
    public const string ConnectedButtonText = "已连接";

    /// <summary>会话码主按钮文案：连上 →「已连接」；否则 →「连接」。</summary>
    public static string SessionConnectButtonLabel(bool connected) =>
        connected ? ConnectedButtonText : ConnectButtonText;

    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;
}
'''
    if old not in t:
        raise SystemExit("HelperSessionCopy anchor missing")
    t = t.replace(old, new, 1)
    # update class summary
    t = t.replace(
        "去协助端会话主状态（R035）：对方结束 / 自己断开 / 未连上失败 三类分场景。",
        "去协助端会话主状态与会话码主按钮文案（R035/R036）。",
        1,
    )
    p.write_text(t, encoding="utf-8", newline="\n")
    print("HelperSessionCopy OK")

# --- tests ---
p = Path("tests/RemoteAssist.Protocol.Tests/HelperSessionCopyTests.cs")
t = p.read_text(encoding="utf-8")
if "SessionConnectButtonLabel" not in t:
    insert = '''
    [Fact]
    public void SessionConnectButton_ConnectedLabel_NotSolidConnect()
    {
        Assert.Equal("连接", HelperSessionCopy.ConnectButtonText);
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("已连接", HelperSessionCopy.SessionConnectButtonLabel(connected: true));
        Assert.Equal("连接", HelperSessionCopy.SessionConnectButtonLabel(connected: false));
        Assert.NotEqual(HelperSessionCopy.ConnectButtonText, HelperSessionCopy.ConnectedButtonText);
        // R035 主状态未回退
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }
}
'''
    if not t.rstrip().endswith("}"):
        raise SystemExit("tests end weird")
    # replace final closing of class
    idx = t.rfind("    }\n}\n")
    if idx < 0:
        idx = t.rfind("    }\n}")
    # append before last class closing brace
    # find last Fact's closing then class close
    marker = "            connected: HelperSessionCopy.ShowCommonTools(false))); // after peer end → false\n    }\n}\n"
    if marker in t:
        t = t.replace(marker, marker[:-3] + insert.lstrip() + "\n", 1)
        # oops that may duplicate - do cleaner
    t = p.read_text(encoding="utf-8")
    # rewrite end
    end_marker = '''    public void CommonTools_HiddenWhenNotConnected()
    {
        Assert.True(HelperSessionCopy.ShowCommonTools(connected: true));
        Assert.False(HelperSessionCopy.ShowCommonTools(connected: false));
        Assert.False(HelperSessionCopy.ShowCommonTools(
            connected: HelperSessionCopy.ShowCommonTools(false))); // after peer end → false
    }
}
'''
    new_end = '''    public void CommonTools_HiddenWhenNotConnected()
    {
        Assert.True(HelperSessionCopy.ShowCommonTools(connected: true));
        Assert.False(HelperSessionCopy.ShowCommonTools(connected: false));
        Assert.False(HelperSessionCopy.ShowCommonTools(
            connected: HelperSessionCopy.ShowCommonTools(false))); // after peer end → false
    }

    [Fact]
    public void SessionConnectButton_ConnectedLabel_NotSolidConnect()
    {
        Assert.Equal("连接", HelperSessionCopy.ConnectButtonText);
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("已连接", HelperSessionCopy.SessionConnectButtonLabel(connected: true));
        Assert.Equal("连接", HelperSessionCopy.SessionConnectButtonLabel(connected: false));
        Assert.NotEqual(HelperSessionCopy.ConnectButtonText, HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }
}
'''
    if end_marker not in t:
        raise SystemExit("test end marker missing")
    t = t.replace(end_marker, new_end, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("tests OK")

# --- Win MainForm SetConnectedUi ---
p = Path("src/RemoteAssist.Helper.UI/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = '''    private void SetConnectedUi(bool connected)
    {
        _btnConnect.Enabled = !connected;
        _btnDisconnect.Enabled = connected;
        foreach (var b in _toolButtons) b.Enabled = connected;
        _inputBox.Enabled = !connected;
        if (_toolCard is not null)
        {
            _toolCard.Visible = connected;
            LayoutForAdvanced();
        }
        UpdateOwnConnectUi();
    }
'''
new = '''    private void SetConnectedUi(bool connected)
    {
        _btnConnect.Enabled = !connected;
        _btnConnect.Text = HelperSessionCopy.SessionConnectButtonLabel(connected);
        if (connected)
        {
            // R036：已连上 → 不可点「已连接」，非实心绿
            _btnConnect.BackColor = Color.FromArgb(0xE8, 0xEA, 0xED);
            _btnConnect.ForeColor = Color.FromArgb(0x9A, 0xA0, 0xA6);
            _btnConnect.Cursor = Cursors.Default;
        }
        else
        {
            _btnConnect.BackColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            _btnConnect.ForeColor = Color.White;
            _btnConnect.Cursor = Cursors.Hand;
        }
        _btnDisconnect.Enabled = connected;
        foreach (var b in _toolButtons) b.Enabled = connected;
        _inputBox.Enabled = !connected;
        if (_toolCard is not null)
        {
            _toolCard.Visible = connected;
            LayoutForAdvanced();
        }
        UpdateOwnConnectUi();
    }
'''
if old not in t:
    raise SystemExit("Win SetConnectedUi missing")
t = t.replace(old, new, 1)
# initial button text from constant
t = t.replace('_btnConnect = MakeButton("连接", 450, 160, 120);',
              '_btnConnect = MakeButton(HelperSessionCopy.ConnectButtonText, 450, 160, 120);', 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("Win OK")

# --- Avalonia HelperWindow SetConnectedUi ---
p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
old = '''    private void SetConnectedUi(bool connected)
    {
        _btnConnect.IsEnabled = !connected;
        _pasteBox.IsEnabled = !connected;
        if (connected)
        {
            _btnConnect.Background = Brush.Parse("#E8EAED");
            _btnConnect.Foreground = Brush.Parse("#9AA0A6");
        }
        else
        {
            _btnConnect.Background = Brush.Parse("#0F9D58");
            _btnConnect.Foreground = Brushes.White;
        }

        _toolCard.IsVisible = connected;
'''
new = '''    private void SetConnectedUi(bool connected)
    {
        _btnConnect.IsEnabled = !connected;
        _btnConnect.Content = HelperSessionCopy.SessionConnectButtonLabel(connected);
        _pasteBox.IsEnabled = !connected;
        if (connected)
        {
            // R036：已连上 →「已连接」浅灰不可点（不再保留禁用的「连接」）
            _btnConnect.Background = Brush.Parse("#E8EAED");
            _btnConnect.Foreground = Brush.Parse("#9AA0A6");
        }
        else
        {
            _btnConnect.Background = Brush.Parse("#0F9D58");
            _btnConnect.Foreground = Brushes.White;
        }

        _toolCard.IsVisible = connected;
'''
if old not in t:
    raise SystemExit("Ava SetConnectedUi missing")
t = t.replace(old, new, 1)
# initial Content
t = t.replace('Content = "连接",', 'Content = HelperSessionCopy.ConnectButtonText,', 1)
# Account area: reuse same string constant only (behavior unchanged)
# Only replace the ConnectedLight Content assignment for own connect
t = t.replace(
    '_btnOwnConnect.Content = "已连接";',
    '_btnOwnConnect.Content = HelperSessionCopy.ConnectedButtonText;',
    1,
)
# Update shot helper comment/text that mentions 会话码「连接」灰禁
t = t.replace(
    '（截图）账号区为「已连接」轻态；上方会话码「连接」已灰禁用。',
    '（截图）账号区为「已连接」轻态；上方会话码主按钮为不可点「已连接」。',
    1,
)
t = t.replace(
    '/// <summary>UiShot：强调已连接后账号区「已连接」轻态 + 会话码「连接」灰禁。</summary>',
    '/// <summary>UiShot：强调已连接后账号区「已连接」轻态 + 会话码主按钮「已连接」灰禁。</summary>',
    1,
)
p.write_text(t, encoding="utf-8", newline="\n")
print("Avalonia OK")
