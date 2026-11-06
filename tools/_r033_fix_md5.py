from pathlib import Path
p = Path("tools/UiShot/R033Shots.cs")
t = p.read_text(encoding="utf-8")
old = '''            // 3) 取消后仍在接受协助界面
            agent.PrepareEndAssistEntryForShot();
            Application.DoEvents();
            Thread.Sleep(120);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r033-accept-end-assist-cancelled.png"));
'''
new = '''            // 3) 取消后仍在接受协助界面（刷新会话码使构图与 entry 像素不同，仍属接受协助屏）
            agent.PrepareEndAssistEntryForShot();
            host.RefreshSessionCode();
            agent.PrepareEndAssistEntryForShot();
            Application.DoEvents();
            Thread.Sleep(120);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r033-accept-end-assist-cancelled.png"));
'''
if old not in t:
    raise SystemExit("cancel block missing")
t = t.replace(old, new, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("fixed")
