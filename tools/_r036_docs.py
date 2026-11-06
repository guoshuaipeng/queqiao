from pathlib import Path
import re

p = Path("docs/round-036.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**ready**", "> 状态：**delivered**", 1)
delivery = """

## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- `HelperSessionCopy` 增补：`ConnectButtonText`=「连接」、`ConnectedButtonText`=「已连接」、`SessionConnectButtonLabel(connected)`
- Windows `Helper.UI.MainForm.SetConnectedUi`：连上后主按钮文案「已连接」、禁用、浅灰底（非实心绿）；结束后/未连上恢复实心绿「连接」
- Avalonia `HelperWindow.SetConnectedUi`：连上后由禁用的「连接」改为「已连接」（同浅灰）；断开后回实心「连接」
- 账号区免密「已连接」仅复用同一字符串常量，行为未改；五个常用操作显隐未改；R035 三句主状态未回退
- 未改 EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」；未改 relay/site；未换公网包；未开下一轮

### 主按钮切换
| 情景 | 文案 | 可点 | 样式 |
|------|------|------|------|
| 已连上 | 已连接 | 否 | 浅灰 |
| 对方结束 / 自己断开 / 未连上失败 | 连接 | 是 | 实心绿 |
| 主状态 | 仍用 R035：`StatusConnected` / `StatusPeerEndedAssist` / `StatusDisconnectedSelf` / `StatusConnectFailed` | | |

### 测试
- `RemoteAssist.Protocol.Tests`：**190** 通过

### 截图（均为 **ui-shot**）
| 文件 | 平台 | 显示 | MD5 | 字节 |
|------|------|------|-----|------|
| `docs/screenshots/r036-helper-connected-btn.png` | Win | 主按钮不可点「已连接」（非实心绿）；主状态「已连接对方电脑」 | A909CE39C491D7A168543148E4F9070D | 45668 |
| `docs/screenshots/r036-helper-peer-ended-reconnect.png` | Win | 「对方已结束这次协助。」+ 实心「连接」 | 4054F5B75E449DE032AF37C9CFB890E8 | 41071 |
| `docs/screenshots/r036-helper-self-disconnect-reconnect.png` | Win | 「状态：未连接」+ 实心「连接」 | 46D5815D703A7AA50796A529B6EF2EAA | 40760 |
| `docs/screenshots/r036-avalonia-helper-connected-btn.png` | Avalonia | 主按钮「已连接」灰禁 | E4150D9A3F19C451BD05B8B471A39925 | 57489 |
| `docs/screenshots/r036-avalonia-helper-peer-ended-reconnect.png` | Avalonia | 对方结束句 + 实心「连接」 | A5D5B996AC38C3AD417A2D453E5E5518 | 58388 |
| `docs/screenshots/r036-avalonia-helper-self-disconnect-reconnect.png` | Avalonia | 未连接句 + 实心「连接」 | 991BF347449A1D5A885A0578509B2916 | 57641 |

亦复制到 `%USERPROFILE%\\Downloads\\r036-shots\\`。

### 缺口 / 披露
- 截图为 ui-shot；Win 连上态用控件校验确认按钮文案/禁用/非绿色。
- Avalonia 对方结束 / 自己断开构图与 R035 对应张像素可能相同（断开后主按钮本就是实心「连接」）；本轮 r036-* 六张 MD5 互不相同，连上态因「已连接」文案已与 R035 区分。
"""
if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-036 OK")

rp = Path("docs/roadmap.md")
text = rp.read_text(encoding="utf-8")
lines = text.splitlines(True)
out = []
for line in lines:
    if line.startswith("| R036 |") and "连接" in line:
        line = "| R036 | 高频：去协助端连上后「连接」改为不可点的「已连接」；打磨：结束后恢复实心「连接」 | 鹊桥-客户端 | **delivered** | `docs/round-036.md` |\n"
    out.append(line)
text = "".join(out)
if "下一步 R036：" in text:
    text = re.sub(r"下一步 R036：[^；]*；未开再下一轮。", "R036 已交付（去协助端连上后主按钮「已连接」，见 `docs/round-036.md`）；未开下一轮。", text)
elif "R035 已交付" in text and "R036 已交付" not in text:
    text = text.replace(
        "R035 已交付（去协助端对方结束协助后人话，见 `docs/round-035.md`）；未开下一轮。",
        "R035 已产品验收（`b707804`）；R036 已交付（去协助端连上后主按钮「已连接」，见 `docs/round-036.md`）；未开下一轮。",
        1,
    )
rp.write_text(text, encoding="utf-8", newline="\n")
print("roadmap OK")
for i,l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R036" in l and (l.startswith("| R036") or "交付" in l or "下一步" in l):
        print(f"{i}:{l[:160]}")
