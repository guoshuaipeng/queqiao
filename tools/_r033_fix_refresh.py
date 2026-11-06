from pathlib import Path
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = '''    internal void PrepareEndAssistEntryForShot()
    {
        PreparePeerIdleForShot();
        SetStatus("");
        if (_advancedOpen) ToggleAdvanced();
    }
'''
new = '''    internal void PrepareEndAssistEntryForShot()
    {
        RefreshFromHost();
        PreparePeerIdleForShot();
        SetStatus("");
        if (_advancedOpen) ToggleAdvanced();
    }
'''
if old not in t:
    raise SystemExit("PrepareEndAssistEntryForShot missing")
t = t.replace(old, new, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("refresh OK")
