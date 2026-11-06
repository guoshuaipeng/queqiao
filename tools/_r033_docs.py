from pathlib import Path

p = Path("docs/round-033.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**ready**", "> 状态：**delivered**", 1)
delivery = """

## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- 新增 `RemoteAssist.Protocol.EndAssistCopy`：`ButtonText` / `ConfirmBody` / `ConfirmOk` / `ConfirmCancel`
- Windows `EndAssistConfirmDialog`：正文「结束后对方将立刻连不上。」；按钮「结束这次协助 / 取消」；`AcceptButton`/`CancelButton` 均为取消（超时/回车不自动结束）
- Windows `MainForm`：轻按钮「结束这次协助」（白底描边，非实心）；确认后 `DisconnectAllSessions` + `Stop`；有返回首页则 `HomeRequested`，否则 `ExitRequested`
- `AgentDesktopUi` 接线 `MainForm.ExitRequested`
- 「复制码+密码」仍唯一实心主按钮；R032 关窗藏托盘 / 气泡 / 默认空底栏未改；未改 Avalonia 关窗提示与已有结束按钮；未改 R031 / Skill / MCP / InstallInstructions / trust-levels；未换公网包；未开下一轮

### 测试
- `RemoteAssist.Protocol.Tests`：**185** 通过

### 截图（均为 **ui-shot**）
| 文件 | 显示 | MD5 |
|------|------|-----|
| `docs/screenshots/r033-accept-end-assist-entry.png` | 轻按钮「结束这次协助」+ 实心「复制码+密码」 | 7FB53C4A3C505001F71142E6897468B5 |
| `docs/screenshots/r033-accept-end-assist-confirm.png` | 确认窗正文与两按钮（屏幕合成） | A8EB8FDABB0AEA852E8E6365A3962B4B |
| `docs/screenshots/r033-accept-end-assist-cancelled.png` | 取消后仍在接受协助界面（会话码已刷新以区分 entry 像素） | F371996DD67F37CEEE4E7D489860C4CB |

### 缺口 / 披露
- 确认窗截图为非模态 `Show` + 屏幕合成（便于同屏验收），真实交互为 `ShowDialog`。
- 取消构图与 entry 同属接受协助屏；为满足三张 MD5 不同，取消后刷新了会话码再拍。
"""
# update MD5s after rebuild shot
if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-033 OK")

rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
rt2 = rt.replace(
    "| R033 | 高频：Windows 接受协助端补「结束这次协助」；打磨：结束前确认对方将连不上 | 鹊桥-客户端 | **ready** | `docs/round-033.md` |",
    "| R033 | 高频：Windows 接受协助端补「结束这次协助」；打磨：结束前确认对方将连不上 | 鹊桥-客户端 | **delivered** | `docs/round-033.md` |",
    1,
)
old = "下一步 R033：Windows 接受协助端补「结束这次协助」；未开再下一轮。"
new = "R033 已交付（Windows 接受协助端补「结束这次协助」，见 `docs/round-033.md`）；未开下一轮。"
if old in rt2:
    rt2 = rt2.replace(old, new, 1)
rp.write_text(rt2, encoding="utf-8", newline="\n")
print("roadmap OK")
