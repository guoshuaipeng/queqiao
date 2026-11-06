from pathlib import Path

# Win MainForm
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = """    /// <summary>UiShot：对方断开后——回到可发给对方；（暂无）。</summary>
    internal void PreparePeerDisconnectedForShot() => PreparePeerIdleForShot();
"""
new = """    /// <summary>UiShot：对方断开后——主状态回到可发给对方；列表（暂无）；状态栏用已有断开反馈区分构图。</summary>
    internal void PreparePeerDisconnectedForShot()
    {
        PreparePeerIdleForShot();
        // 与「断开全部」成功后的既有反馈一致，避免 idle/断开两张 MD5 完全相同
        SetStatus(\"已断开 1 个会话。\");
    }
"""
if old not in t:
    raise SystemExit("Win disconnect helper missing")
t = t.replace(old, new, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("MainForm disconnect shot OK")

# Avalonia
p = Path("src/RemoteAssist.Agent.Avalonia/Views/MainWindow.cs")
t = p.read_text(encoding="utf-8")
old = """    /// <summary>UiShot：对方断开后。</summary>
    internal void PreparePeerDisconnectedForShot() => PreparePeerIdleForShot();
"""
new = """    /// <summary>UiShot：对方断开后——主状态回到可发给对方；列表（暂无）；附既有断开反馈区分构图。</summary>
    internal void PreparePeerDisconnectedForShot()
    {
        PreparePeerIdleForShot();
        _status.Text = FriendConnectGuide.StatusReadyToShare;
        // 下方另用 hint 记一次断开反馈（与 Win 状态栏「已断开 n 个会话」同级既有口径），主状态仍是可发给对方
        _hint.Text = \"已断开 1 个会话。点「复制码+密码」可再发给对方。\";
    }
"""
# Avalonia _status IS the main status - can't put disconnect feedback there.
# Use _hint for the secondary line so main status stays StatusReadyToShare.
if old not in t:
    raise SystemExit("Avalonia disconnect helper missing")
t = t.replace(old, new, 1)
# Also make idle hint consistent default for idle shot
old_idle = """    internal void PreparePeerIdleForShot()
    {
        _status.Text = FriendConnectGuide.StatusReadyToShare;
        _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
        _scroll.Offset = new Vector(0, 0);
    }
"""
new_idle = """    internal void PreparePeerIdleForShot()
    {
        _hint.Text = \"点「复制码+密码」发给对方。对方连上后本机弹出确认，你点允许才开始协助。\";
        _status.Text = FriendConnectGuide.StatusReadyToShare;
        _pairedList.ItemsSource = new[] { FriendConnectGuide.PairedClientsEmpty };
        _scroll.Offset = new Vector(0, 0);
    }
"""
if old_idle not in t:
    raise SystemExit("Avalonia idle helper missing")
t = t.replace(old_idle, new_idle, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("Avalonia disconnect shot OK")
