from pathlib import Path

p = Path("docs/round-032.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**ready**", "> 状态：**delivered**", 1)
delivery = """

## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- 新增 `RemoteAssist.Protocol.TrayStayCopy`：
  - `BalloonDefault` / `BalloonWithReturnHome`（沿用原有两句，无第三种说法）
  - `HiddenToTrayFeedback` =「已隐藏到托盘。双击托盘图标可重新打开。」
  - `RemovedDefaultStatusBar` = 原底栏常驻句（仅回归对照，不再作默认底栏）
- Windows `MainForm`：底栏默认 `Text = ""`，不再写「就绪。关闭窗口后会继续在托盘运行。」
- `OnFormClosing` 藏托盘后 `SetStatus(TrayStayCopy.HiddenToTrayFeedback)`（一次性反馈）
- `AgentTrayContext` / `AgentDesktopUi` 启动气泡改为 `TrayStayCopy.BalloonFor(enableReturnHome)`
- 未改 Avalonia 关窗提示；未改 R031 主状态/协助方列表；未改 Skill/MCP/`InstallInstructions`/`trust-levels`；未换公网包；未开下一轮

### 测试
- `RemoteAssist.Protocol.Tests`：**183** 通过（含 TrayStayCopy 3 项）

### 截图（均为 **ui-shot**）
| 文件 | 显示 | MD5 |
|------|------|-----|
| `docs/screenshots/r032-accept-statusbar-empty.png` | 接受协助就绪态；底栏空白；无「就绪。关闭窗口后会继续在托盘运行。」；「复制码+密码」仍是唯一实心主按钮 | EFD52E073BDC2A2DD0881E5E6ADD9A12 |
| `docs/screenshots/r032-accept-hidden-to-tray-feedback.png` | 底栏一次性「已隐藏到托盘。双击托盘图标可重新打开。」（窗仍可见便于截图；非默认底栏） | 07B44EC33CBA1C49D205B4BD7CD4DD9C |

### 启动气泡（未能真拍 NotifyIcon）
ui-shot / DrawToBitmap 无法截取系统托盘气泡。证明方式：
1. 气泡文案常量即原句：`TrayStayCopy.BalloonDefault`、`BalloonWithReturnHome`（测试锁死原文）
2. `AgentTrayContext` 启动时调用 `TrayStayCopy.BalloonFor(enableReturnHome)`，未另造第三种说法
3. 底栏已去掉与气泡重复的常驻句（见上表第一张）

### 缺口 / 披露
- 未实拍启动气泡本体；以常量+代码接线证明未改气泡口径。
- 「已隐藏到托盘…」反馈图为关窗逻辑同款文案的 ui-shot 注入（窗未真正 Hide），便于同屏验收底栏。
"""
if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-032 OK")

rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
rt2 = rt.replace(
    "| R032 | 高频：关闭窗口后留在托盘的说明只留一处；打磨：关窗后底栏一次性反馈 | 鹊桥-客户端 | **ready** | `docs/round-032.md` |",
    "| R032 | 高频：关闭窗口后留在托盘的说明只留一处；打磨：关窗后底栏一次性反馈 | 鹊桥-客户端 | **delivered** | `docs/round-032.md` |",
    1,
)
old = "下一步 R032：关闭窗口后留在托盘的说明只留一处；未开再下一轮。"
new = "R032 已交付（关闭窗口后留在托盘的说明只留一处，见 `docs/round-032.md`）；未开下一轮。"
if old in rt2:
    rt2 = rt2.replace(old, new, 1)
rp.write_text(rt2, encoding="utf-8", newline="\n")
print("roadmap OK")
