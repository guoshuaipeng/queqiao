# 第 36 轮 — 去协助端连上后「连接」改为已连接态

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**done（产品已验收）**
> 依据：R035 产品已验收。协助方已经连上时，Avalonia 会把「连接」改成灰色不可点；Windows 仍保持实心可点绿，看起来还能再连一次。连上之后，这一颗主按钮应该变成「已连接」。

R035 已经说清楚对方结束后发生了什么。接下来把「已经连上」的按钮态对齐：不要继续用一颗实心绿的「连接」来表示此刻还能再连。

## 本轮目标

1. **高频**：去协助端成功连上之后（访客粘贴码连上，或对等的已接通状态），主按钮「连接」改为不可点的「已连接」，不要继续保持实心可点绿。
2. **打磨**：对方结束这次协助、自己点断开、或还没连上时，按钮回到可点的实心「连接」。不要回退 R035 已验收的三句主状态。

## 文案（锁定）

已连接

（这是按钮上的字，不是再写一句主状态。主状态在已连上时仍用已有的「已连接对方电脑」。）

## 行为

- Windows Helper.UI 与 Avalonia HelperWindow 都改到，按钮文案共用同一句常量。
- 已连上：按钮文案「已连接」，禁用，不再是实心绿色。Avalonia 现在已是禁用的「连接」，本轮改成同一句「已连接」。
- 对方已结束这次协助、自己断开、尚未连上（含连接失败）：按钮回到可点的实心「连接」。
- 不要改账号区的「连接这台电脑」/ 未选设备时禁用 +「请先点选一台自己的电脑」；账号免密连上后仍按 R016：该按钮是灰色禁用的「已连接」。
- 不要改 `HelperSessionCopy.StatusPeerEndedAssist` / `StatusDisconnectedSelf` / `StatusConnectFailed`，不要改 R003 失败原因句。
- 不要改五个常用操作的显隐规则（已连上才出现，结束后收起）。

## 本轮不做

- 改中继 JSONL、改 AI-SSH-Pro、完整远程桌面
- 改 EndAssistCopy / R031 接受协助端主状态
- 改 InstallInstructions / Skill 文案 / trust-levels「才问你」
- 改官网 HTML、换公网安装包
- 把 macOS 写成已经可下

## 完成时

- 更新本文交付说明
- 截图放到 docs/screenshots/r036-*（至少：Windows 已连上时按钮为不可点的「已连接」、不是实心绿「连接」；Avalonia 同样；对方结束或自己断开后按钮回到实心「连接」，且 R035 主状态原文仍在）。可用 ui-shot，须标明。**勿覆盖** r022-*～r035-*。
- 回告产品验收；未开下一轮


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

亦复制到 `%USERPROFILE%\Downloads\r036-shots\`。

### 缺口 / 披露
- 截图为 ui-shot；Win 连上态用控件校验确认按钮文案/禁用/非绿色。
- Avalonia 对方结束 / 自己断开构图与 R035 对应张像素可能相同（断开后主按钮本就是实心「连接」）；本轮 r036-* 六张 MD5 互不相同，连上态因「已连接」文案已与 R035 区分。

## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r036-*` 与源码独立验收通过（`d24fb44`）。Protocol.Tests 190 通过。

- `HelperSessionCopy.ConnectedButtonText` =「已连接」；`ConnectButtonText` =「连接」；`SessionConnectButtonLabel` 切换。Windows `Helper.UI.MainForm` 与 Avalonia `HelperWindow` 都接到。
- 已连上：主按钮文案「已连接」，不可点，非实心绿；主状态仍是「已连接对方电脑」；Windows 五个常用操作仍可见；账号区仍是「连接这台电脑」。
- 对方结束 / 自己断开后按钮回到实心「连接」，R035 主状态未回退。这四张与对应 `r035-*` 像素相同（已披露），产品此前已看过那些图。
- 未改 EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」。

| 截图 | MD5 | 产品看到的 |
|------|-----|------------|
| `docs/screenshots/r036-helper-connected-btn.png` | A909CE39C491D7A168543148E4F9070D | Windows 灰色不可点「已连接」；「已连接对方电脑」；五键可见；账号区「连接这台电脑」 |
| `docs/screenshots/r036-helper-peer-ended-reconnect.png` | 4054F5B75E449DE032AF37C9CFB890E8 | 与 `r035-helper-peer-ended.png` 相同：「对方已结束这次协助。」+ 实心「连接」 |
| `docs/screenshots/r036-helper-self-disconnect-reconnect.png` | 46D5815D703A7AA50796A529B6EF2EAA | 与 `r035-helper-self-disconnect.png` 相同：「状态：未连接」+ 实心「连接」 |
| `docs/screenshots/r036-avalonia-helper-connected-btn.png` | E4150D9A3F19C451BD05B8B471A39925 | Avalonia 灰禁「已连接」（不再是禁用的「连接」）；「已连接对方电脑」；账号区「连接这台电脑」 |
| `docs/screenshots/r036-avalonia-helper-peer-ended-reconnect.png` | A5D5B996AC38C3AD417A2D453E5E5518 | 与 `r035-avalonia-helper-peer-ended.png` 相同：「对方已结束这次协助。」+ 实心「连接」 |
| `docs/screenshots/r036-avalonia-helper-self-disconnect-reconnect.png` | 991BF347449A1D5A885A0578509B2916 | 与 `r035-avalonia-helper-self-disconnect.png` 相同：「状态：未连接」+ 实心「连接」 |

缺口：六张均为 ui-shot；对方结束/自己断开四张与 R035 对应图 MD5 相同（已披露，未覆盖 r035 文件）；Avalonia 已连上图可视区域仍未收入五个常用操作。已连上时旁边还留着「粘贴后点「连接」」，与此时按钮「已连接」打架，交 R037。

已开 R037（去协助端已连上后不再提示「粘贴后点「连接」」；交给鹊桥-客户端；不改中继/官网/AI-SSH-Pro）。
