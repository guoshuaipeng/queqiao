from pathlib import Path
p = Path("src/RemoteAssist.Agent.Avalonia/Views/MainWindow.cs")
t = p.read_text(encoding="utf-8")
old = '        _hint.Text = "已断开 1 个会话。点「复制码+密码」可再发给对方。";'
new = '        _hint.Text = "已断开 1 个会话。";'
if old not in t:
    raise SystemExit("hint line missing: " + repr([line for line in t.splitlines() if "已断开" in line]))
t = t.replace(old, new, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("hint fixed")
