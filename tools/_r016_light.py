from pathlib import Path
p = Path(r"src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
if "PrepareConnectedButtonLightForShot" not in t:
    needle = "    /// <summary>UiShot：仅登录未免密连上——常用操作整块不可见。</summary>"
    insert = '''    /// <summary>UiShot：强调已连接后账号区「已连接」轻态 + 会话码「连接」灰禁。</summary>
    internal void PrepareConnectedButtonLightForShot()
    {
        PrepareOwnerConnectedForShot();
        _output.Text = "（截图）账号区为「已连接」轻态；上方会话码「连接」已灰禁用。";
        _accountStatus.Text = "免密已连接 · 按钮轻态（截图）";
    }

'''
    if needle not in t:
        raise SystemExit("needle missing for light shot")
    t = t.replace(needle, insert + needle)
    p.write_text(t, encoding="utf-8")
    print("added light shot helper")
else:
    print("light helper exists")
