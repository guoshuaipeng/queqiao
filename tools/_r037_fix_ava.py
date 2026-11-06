from pathlib import Path
p = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
old = '''    private void RefreshPreview()
    {
        var raw = (_pasteBox.Text ?? "").Trim();
        if (raw.Length == 0)
        {
            _preview.Text = HelperSessionCopy.PasteThenConnectHint;
            return;
        }
'''
new = '''    private void RefreshPreview()
    {
        // R037：已连上不再刷回「粘贴后点「连接」」
        if (_client is not null || _uiShotConnected)
        {
            if (_preview.Text == HelperSessionCopy.PasteThenConnectHint)
                _preview.IsVisible = false;
            return;
        }

        _preview.IsVisible = true;
        var raw = (_pasteBox.Text ?? "").Trim();
        if (raw.Length == 0)
        {
            _preview.Text = HelperSessionCopy.PasteThenConnectHint;
            return;
        }
'''
if "已连上不再刷回" not in t:
    if old not in t:
        raise SystemExit("RefreshPreview block missing")
    t = t.replace(old, new, 1)

# Ensure SetConnectedUi has UpdatePasteThenConnectHintVisibility
if "UpdatePasteThenConnectHintVisibility" not in t:
    raise SystemExit("SetConnectedUi patch missing UpdatePaste")

# Shot helpers: after PreparePeerConnected ensure tip hidden; after ended ensure tip shown
# PreparePeerConnectedSessionForShot already SetConnectedUi(true) which calls UpdatePaste...
# PrepareIdleForShot sets preview text - ok

p.write_text(t, encoding="utf-8", newline="\n")
print("Ava RefreshPreview OK")
# Verify Win has UpdatePaste
assert "UpdatePasteThenConnectHintVisibility" in Path("src/RemoteAssist.Helper.UI/MainForm.cs").read_text(encoding="utf-8")
print("Win has UpdatePaste")
