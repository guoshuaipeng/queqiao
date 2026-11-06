from pathlib import Path
from datetime import datetime

# round-031 delivery
p = Path("docs/round-031.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**ready**", "> 状态：**delivered**", 1)
delivery = """

## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- 共享常量（`RemoteAssist.Protocol.FriendConnectGuide`）：
  - `StatusPeerConnected` =「对方已连上。改动这台电脑前会先问你。」
  - `PairedClientFallbackName` =「协助方」、`PairedClientsEmpty` =「（暂无）」
  - `FormatPairedClientLabel`：有称呼用称呼；空/默认 `helper` 写「协助方」；账号免密可读前缀
- Windows `MainForm`：运行状态以 `ListPairedClients().Count > 0` 切换（不再把中继 `phase=paired` 当成已连上）；列表去掉内部 token 截断，改人话称呼
- Avalonia `MainWindow`：同上主状态；新增「已连接的协助方」列表（此前只有状态句）
- 断开后主状态回到 `StatusReadyToShare`；列表回到「（暂无）」
- 「复制码+密码」仍是唯一实心主按钮；未改 Skill/MCP/`InstallInstructions`/`trust-levels`「才问你」；未换公网包；未开下一轮

### 测试
- `RemoteAssist.Protocol.Tests`：180 通过（含 StatusPeerConnected / FormatPairedClientLabel 新测）

### 截图（均为 **ui-shot**，非手绘）
| 文件 | 平台 | 显示 | MD5 |
|------|------|------|-----|
| `docs/screenshots/r031-accept-idle.png` | Windows | 主状态「已连上网络，可以发给对方了」；列表「（暂无）」；「复制码+密码」实心主按钮 | FEEFE46778963A33AF1B2E673B478120 |
| `docs/screenshots/r031-accept-peer-connected.png` | Windows | 主状态「对方已连上。改动这台电脑前会先问你。」；列表「小明」 | 319EC6B84B3C4194ECAB59D044275A0E |
| `docs/screenshots/r031-accept-peer-disconnected.png` | Windows | 主状态回到可发给对方；列表「（暂无）」；状态栏「已断开 1 个会话。」（既有断开反馈，区分 idle MD5） | F9CD9F3BC03CE5EAE30B16C2F06977F1 |
| `docs/screenshots/r031-avalonia-idle.png` | Avalonia | 同上 idle | 7AB0845C696EE96FAD8F051E17290D2C |
| `docs/screenshots/r031-avalonia-peer-connected.png` | Avalonia | 同上已连上 + 列表「小明」 | AAB7F8DF41EE4176E3B2C4C1E1DFB522 |
| `docs/screenshots/r031-avalonia-peer-disconnected.png` | Avalonia | 主状态回到可发给对方；列表「（暂无）」；hint「已断开 1 个会话。」 | AA3F2AEA181FFDFF4FF2ADCB4B3B1161 |

两端均改代码且均有截图；文案共用 Protocol 常量。

### 缺口 / 披露
- 截图为 ui-shot 注入配对态（非真连中继后的实拍）；主状态与列表控件为真实 UI。
- 断开构图用既有「已断开 1 个会话。」反馈区分与 idle 的像素，主状态本身已回到「已连上网络，可以发给对方了」。
"""
if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-031 delivery OK")
else:
    print("delivery already present")

# roadmap
rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
rt2 = rt.replace(
    "| R031 | 高频：接受协助端对方连上后主状态改为「对方已连上」；打磨：断开后回到等待句 | 鹊桥-客户端 | **ready** | `docs/round-031.md` |",
    "| R031 | 高频：接受协助端对方连上后主状态改为「对方已连上」；打磨：断开后回到等待句 | 鹊桥-客户端 | **delivered** | `docs/round-031.md` |",
    1,
)
old_tail = "下一步 R031：接受协助端对方连上后人话；未开再下一轮。"
new_tail = "R031 已交付（接受协助端对方连上后人话，见 `docs/round-031.md`）；未开下一轮。"
if old_tail in rt2:
    rt2 = rt2.replace(old_tail, new_tail, 1)
if rt2 != rt:
    rp.write_text(rt2, encoding="utf-8", newline="\n")
    print("roadmap OK")
else:
    print("roadmap unchanged?")
