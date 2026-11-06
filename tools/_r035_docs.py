from pathlib import Path

p = Path("docs/round-035.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**ready**", "> 状态：**delivered**", 1)
delivery = """

## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- 新增 `RemoteAssist.Protocol.HelperSessionCopy`：
  - `StatusPeerEndedAssist` =「对方已结束这次协助。」
  - `StatusConnected` / `StatusDisconnectedSelf` / `StatusConnectFailed`
  - `StatusAfterConnectedSessionEnd(selfInitiated)`、`ShowCommonTools`
- `AssistClient`：配对后后台探测底层 Socket；对端关闭触发 `ConnectionLost`（自己 `Dispose` 不触发）；工具读失败也会 Raise
- Windows `Helper.UI.MainForm` / Avalonia `HelperWindow`：订阅 `ConnectionLost` → 对方结束句 + `SetConnectedUi(false)`（五键按 R016 收起）；自己点断开仍「状态：未连接」；未连上失败仍「连接失败」+ R003 原因句
- 未改 `EndAssistCopy`、R031 `StatusPeerConnected`、InstallInstructions / Skill、trust-levels「才问你」；未改 relay/site；未换公网包；未开下一轮

### 三类情景区分
1. **已连上后会话消失**：`AssistClient.ConnectionLost` 且非 `_selfDisconnect` → `StatusPeerEndedAssist`
2. **自己点断开**：先置 `_selfDisconnect` 再 Dispose → `StatusDisconnectedSelf`（不订阅触发对方结束句）
3. **尚未连上失败**：`PairAsync` catch → `StatusConnectFailed` + `FriendConnectGuide.FriendlyOrRaw`（R003 原文不变）

### 测试
- `RemoteAssist.Protocol.Tests`：**189** 通过（含 HelperSessionCopy 4 项）

### 截图（均为 **ui-shot**）
| 文件 | 平台 | 显示 | MD5 | 字节 |
|------|------|------|-----|------|
| `docs/screenshots/r035-helper-connected.png` | Win | 已连接对方电脑；常用操作可见 | 3764D392920978D65E141DCFD1371689 | 45661 |
| `docs/screenshots/r035-helper-peer-ended.png` | Win | 对方已结束这次协助。；五键收起 | 4054F5B75E449DE032AF37C9CFB890E8 | 41071 |
| `docs/screenshots/r035-helper-self-disconnect.png` | Win | 状态：未连接（非对方结束句） | 46D5815D703A7AA50796A529B6EF2EAA | 40760 |
| `docs/screenshots/r035-helper-connect-failed.png` | Win | 连接失败 + FailPeerNotAccepting | 3DB1E3AE8ADD0FD321CB108ECE0376F6 | 43131 |
| `docs/screenshots/r035-avalonia-helper-connected.png` | Avalonia | 已连上 | F5EA9CDAA2EFA259955663900BB072C0 | 57267 |
| `docs/screenshots/r035-avalonia-helper-peer-ended.png` | Avalonia | 对方已结束这次协助。 | A5D5B996AC38C3AD417A2D453E5E5518 | 58388 |
| `docs/screenshots/r035-avalonia-helper-self-disconnect.png` | Avalonia | 状态：未连接 | 991BF347449A1D5A885A0578509B2916 | 57641 |
| `docs/screenshots/r035-avalonia-helper-connect-failed.png` | Avalonia | 连接失败 + FailStaleCode | 7C237EF27B4C282BAE0365996D437677 | 67784 |

亦复制到 `%USERPROFILE%\\Downloads\\r035-shots\\`。

### 缺口 / 披露
- 截图为 ui-shot 注入状态，非真连中继后对方点「结束这次协助」的实拍；运行时依赖 Socket Poll / 读失败探测对端关闭。
"""
if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-035 OK")

rp = Path("docs/roadmap.md")
lines = rp.read_text(encoding="utf-8").splitlines(True)
out = []
for line in lines:
    if line.startswith("| R035 |") and "对方结束协助" in line:
        line = "| R035 | 高频：去协助端对方结束协助后主状态改为「对方已结束这次协助。」；打磨：未连上失败与自己断开仍用原文 | 鹊桥-客户端 | **delivered** | `docs/round-035.md` |\n"
    if "下一步 R035" in line or ("R034" in line and "未开下一轮" in line and "R035" not in line and "已交付" not in line[:20]):
        # append R035 delivered into the long narrative if it ends with 未开下一轮 about R034/R035
        pass
    if "下一步 R035：" in line:
        line = line.replace(
            line[line.index("下一步 R035："):],
            "R035 已交付（去协助端对方结束协助后人话，见 `docs/round-035.md`）；未开下一轮。\n",
        )
    # common pattern at end of long paragraph
    if "下一步 R035：去协助端对方结束协助后人话；未开再下一轮。" in line:
        line = line.replace(
            "下一步 R035：去协助端对方结束协助后人话；未开再下一轮。",
            "R035 已交付（去协助端对方结束协助后人话，见 `docs/round-035.md`）；未开下一轮。",
        )
    out.append(line)
text = "".join(out)
# if narrative still says 下一步 R035
if "下一步 R035" in text:
    import re
    text = re.sub(r"下一步 R035：[^；]*；未开再下一轮。", "R035 已交付（去协助端对方结束协助后人话，见 `docs/round-035.md`）；未开下一轮。", text)
if "R034 A 已出包" in text and "R035 已交付" not in text.split("R034 A 已出包")[-1][:200]:
    text = text.replace(
        "待服务端上架；未开下一轮。",
        "待服务端上架；R035 已交付（见 `docs/round-035.md`）；未开下一轮。",
        1,
    )
rp.write_text(text, encoding="utf-8", newline="\n")
print("roadmap touched")
for i,l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R035" in l and (l.startswith("| R035") or "交付" in l or "下一步" in l):
        print(f"{i}:{l[:160]}")
