from pathlib import Path

# MainForm: empty default status; use constant on close; shot helpers
p = Path("src/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = '''            Font = new Font(Font.FontFamily, 8.5F),
            Text = "就绪。关闭窗口后会继续在托盘运行。"
        };'''
new = '''            Font = new Font(Font.FontFamily, 8.5F),
            // R032：底栏默认空白，托盘说明只留启动气泡一处
            Text = ""
        };'''
if old not in t:
    raise SystemExit("default status Text missing")
t = t.replace(old, new, 1)

old2 = '        SetStatus("已隐藏到托盘。双击托盘图标可重新打开。");'
new2 = '        SetStatus(TrayStayCopy.HiddenToTrayFeedback);'
if old2 not in t:
    raise SystemExit("FormClosing SetStatus missing")
t = t.replace(old2, new2, 1)

# Shot helpers before PreparePeerIdleForShot
helpers = '''
    /// <summary>UiShot：接受协助就绪态——底栏空白，不含常驻托盘句。</summary>
    internal void PrepareTrayStayIdleForShot()
    {
        PreparePeerIdleForShot();
        SetStatus("");
    }

    /// <summary>UiShot：关窗藏托盘后的一次性底栏反馈（窗仍可见便于截图）。</summary>
    internal void PrepareHiddenToTrayFeedbackForShot()
    {
        PreparePeerIdleForShot();
        SetStatus(TrayStayCopy.HiddenToTrayFeedback);
    }

'''
if "PrepareTrayStayIdleForShot" not in t:
    anchor = "    /// <summary>UiShot：还没人连上——主状态可发给对方；列表（暂无）。</summary>"
    if anchor not in t:
        raise SystemExit("PreparePeerIdle anchor missing")
    t = t.replace(anchor, helpers + anchor, 1)

p.write_text(t, encoding="utf-8", newline="\n")
print("MainForm OK")

# AgentTrayContext balloons
p = Path("src/RemoteAssist.Agent/AgentTrayContext.cs")
t = p.read_text(encoding="utf-8")
# ensure using Protocol
if "using RemoteAssist.Protocol;" not in t:
    t = t.replace("using RemoteAssist.Agent;\n", "using RemoteAssist.Agent;\nusing RemoteAssist.Protocol;\n", 1)
    if "using RemoteAssist.Protocol;" not in t:
        # try after namespace usings
        t = "using RemoteAssist.Protocol;\n" + t

old = '''        _tray.ShowBalloonTip(2500, "鹊桥",
            enableReturnHome
                ? "被控模式已启动。可返回首页切换为控制端；关闭主窗口会留在托盘。"
                : "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。",
            ToolTipIcon.Info);'''
new = '''        _tray.ShowBalloonTip(2500, "鹊桥",
            TrayStayCopy.BalloonFor(enableReturnHome),
            ToolTipIcon.Info);'''
if old not in t:
    raise SystemExit("balloon block missing:\n" + "\n".join(line for line in t.splitlines() if "ShowBalloonTip(2500" in line or "关闭主窗口" in line))
t = t.replace(old, new, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("AgentTrayContext OK")
