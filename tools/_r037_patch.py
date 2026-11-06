from pathlib import Path

# HelperSessionCopy
p = Path("src/RemoteAssist.Protocol/HelperSessionCopy.cs")
t = p.read_text(encoding="utf-8")
if "PasteThenConnectHint" not in t:
    old = '''    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;
}
'''
    new = '''    /// <summary>未连上时会话码区操作提示（原文锁定；R037 只控制显隐）。</summary>
    public const string PasteThenConnectHint = "粘贴后点「连接」";

    /// <summary>R037：已连上不显示粘贴提示；断开/结束/未连上再显示。</summary>
    public static bool ShowPasteThenConnectHint(bool sessionConnected) => !sessionConnected;

    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;
}
'''
    if old not in t:
        raise SystemExit("HelperSessionCopy end missing")
    t = t.replace(old, new, 1)
    t = t.replace("（R035/R036）", "（R035/R036/R037）", 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("HelperSessionCopy OK")

# tests
p = Path("tests/RemoteAssist.Protocol.Tests/HelperSessionCopyTests.cs")
t = p.read_text(encoding="utf-8")
if "ShowPasteThenConnectHint" not in t:
    marker = '''        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }
}
'''
    # append before final class close after last fact
    fact = '''
    [Fact]
    public void PasteThenConnectHint_HiddenWhenConnected_ExactCopyUnchanged()
    {
        Assert.Equal("粘贴后点「连接」", HelperSessionCopy.PasteThenConnectHint);
        Assert.False(HelperSessionCopy.ShowPasteThenConnectHint(sessionConnected: true));
        Assert.True(HelperSessionCopy.ShowPasteThenConnectHint(sessionConnected: false));
        // R035/R036 未回退
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("连接", HelperSessionCopy.ConnectButtonText);
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
    }
}
'''
    end = '''        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }
}
'''
    # find SessionConnectButton test ending
    if end not in t:
        # try CommonTools end was extended - look for SessionConnectButton fact end
        end2 = '''        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }
}
'''
        if end2 not in t:
            raise SystemExit("test end missing")
        t = t.replace(end2, end2[:-2] + fact.lstrip() + "\n", 1)
    else:
        t = t.replace(end, end[:-2] + fact.lstrip() + "\n", 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("tests OK")

# Win MainForm
p = Path("src/RemoteAssist.Helper.UI/MainForm.cs")
t = p.read_text(encoding="utf-8")
t = t.replace('Text = "粘贴后点「连接」"', 'Text = HelperSessionCopy.PasteThenConnectHint')
t = t.replace('_previewLabel.Text = "粘贴后点「连接」";', '_previewLabel.Text = HelperSessionCopy.PasteThenConnectHint;')

old_sc = '''    private void SetConnectedUi(bool connected)
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
new_sc = '''    private void SetConnectedUi(bool connected)
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
        UpdatePasteThenConnectHintVisibility(connected);
        UpdateOwnConnectUi();
    }

    /// <summary>R037：已连上隐藏「粘贴后点「连接」」；断开后再显示原文。</summary>
    private void UpdatePasteThenConnectHintVisibility(bool connected)
    {
        var show = HelperSessionCopy.ShowPasteThenConnectHint(connected);
        if (!show)
        {
            if (_previewLabel.Text == HelperSessionCopy.PasteThenConnectHint)
                _previewLabel.Visible = false;
            return;
        }

        _previewLabel.Visible = true;
        if (_previewLabel.Text == HelperSessionCopy.PasteThenConnectHint ||
            string.IsNullOrWhiteSpace(_previewLabel.Text))
            RefreshPreview();
    }
'''
if "UpdatePasteThenConnectHintVisibility" not in t:
    if old_sc not in t:
        raise SystemExit("Win SetConnectedUi missing")
    t = t.replace(old_sc, new_sc, 1)

# RefreshPreview: when connected, don't re-show tip
old_rp = '''    private void RefreshPreview()
    {
        // 空态：未点连接前不报「无法连接」
        var raw = (_inputBox.Text ?? "").Trim();
        if (raw.Length == 0 && string.IsNullOrWhiteSpace(_pwdBox.Text))
        {
            _previewLabel.Text = HelperSessionCopy.PasteThenConnectHint;
            _previewLabel.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
            return;
        }
'''
new_rp = '''    private void RefreshPreview()
    {
        // R037：已连上不再把预览刷回「粘贴后点「连接」」
        if (_client is not null)
        {
            if (_previewLabel.Text == HelperSessionCopy.PasteThenConnectHint)
                _previewLabel.Visible = false;
            return;
        }

        _previewLabel.Visible = true;
        // 空态：未点连接前不报「无法连接」
        var raw = (_inputBox.Text ?? "").Trim();
        if (raw.Length == 0 && string.IsNullOrWhiteSpace(_pwdBox.Text))
        {
            _previewLabel.Text = HelperSessionCopy.PasteThenConnectHint;
            _previewLabel.ForeColor = Color.FromArgb(0x5F, 0x63, 0x68);
            return;
        }
'''
if "已连上不再把预览刷回" not in t:
    if old_rp not in t:
        raise SystemExit("Win RefreshPreview missing")
    t = t.replace(old_rp, new_rp, 1)

p.write_text(t, encoding="utf-8", newline="\n")
print("Win OK")

# Avalonia
p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
t = t.replace('Text = "粘贴后点「连接」",', 'Text = HelperSessionCopy.PasteThenConnectHint,')
t = t.replace('_preview.Text = "粘贴后点「连接」";', '_preview.Text = HelperSessionCopy.PasteThenConnectHint;')

old_sc = '''    private void SetConnectedUi(bool connected)
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
        foreach (var b in new[] { _btnProcs, _btnLs, _btnLogs, _btnCat, _btnRun })
        {
            b.IsEnabled = connected;
            if (connected)
                StyleToolButtonEnabled(b);
        }
        UpdateOwnConnectUi();
    }
'''
new_sc = '''    private void SetConnectedUi(bool connected)
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
        foreach (var b in new[] { _btnProcs, _btnLs, _btnLogs, _btnCat, _btnRun })
        {
            b.IsEnabled = connected;
            if (connected)
                StyleToolButtonEnabled(b);
        }
        UpdatePasteThenConnectHintVisibility(connected);
        UpdateOwnConnectUi();
    }

    /// <summary>R037：已连上隐藏「粘贴后点「连接」」；断开后再显示原文。</summary>
    private void UpdatePasteThenConnectHintVisibility(bool connected)
    {
        var show = HelperSessionCopy.ShowPasteThenConnectHint(connected);
        if (!show)
        {
            if (_preview.Text == HelperSessionCopy.PasteThenConnectHint)
                _preview.IsVisible = false;
            return;
        }

        // 仅当仍是操作提示原文时恢复显示；MCP/Skill 预览等其它用途保持现状
        if (_preview.Text == HelperSessionCopy.PasteThenConnectHint ||
            string.IsNullOrWhiteSpace(_preview.Text))
        {
            _preview.Text = HelperSessionCopy.PasteThenConnectHint;
            _preview.Foreground = Brush.Parse("#5F6368");
            _preview.IsVisible = true;
        }
        else if (!_preview.IsVisible)
        {
            _preview.IsVisible = true;
        }
    }
'''
if "UpdatePasteThenConnectHintVisibility" not in t:
    if old_sc not in t:
        raise SystemExit("Ava SetConnectedUi missing")
    t = t.replace(old_sc, new_sc, 1)

# Avalonia RefreshPreview
old_rp = '''    private void RefreshPreview()
    {
'''
# need more context - read exact
if "已连上不再把预览刷回" not in t:
    # find RefreshPreview body start
    idx = t.find("    private void RefreshPreview()\n    {\n")
    if idx < 0:
        raise SystemExit("Ava RefreshPreview missing")
    insert = '''    private void RefreshPreview()
    {
        if (_client is not null || _uiShotConnected)
        {
            if (_preview.Text == HelperSessionCopy.PasteThenConnectHint)
                _preview.IsVisible = false;
            return;
        }

'''
    # replace only the opening - find empty-state assignment block
    old_block = '''    private void RefreshPreview()
    {
        var raw = (_pasteBox.Text ?? "").Trim();
        if (raw.Length == 0)
        {
            _preview.Text = HelperSessionCopy.PasteThenConnectHint;
'''
    # maybe different - check file
    pass

p.write_text(t, encoding="utf-8", newline="\n")
print("Avalonia partial write; fix RefreshPreview next")
