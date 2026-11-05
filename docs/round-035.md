# 第 35 轮 — 去协助端对方结束协助后人话

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**delivered**
> 依据：R034 产品已验收。R033 让被协助的人能在窗口里点「结束这次协助」；协助方已经连上时，对方一停，目前更容易看起来像「连接失败」，而不是这次协助正常收尾。

R033 已经补上被协助端怎么停。接下来补另一半：协助方已经连上之后，这次协助结束了，主状态要说人话。

## 本轮目标

1. **高频**：去协助端在已经成功连上之后，对方结束了这次协助（点了「结束这次协助」，或对等地把本次协助停掉），主状态改为「对方已结束这次协助。」
2. **打磨**：这句话不要用在还没连上的失败原因上；协助方自己主动断开时也不要用这句。还没连上时，继续用 R003 已验收的原因句。

## 文案（锁定）

对方已结束这次协助。

## 行为

- 只在「已经成功连上、随后这次协助结束了」时使用上面这句。不要写成「连接失败」。
- 协助方自己点断开：继续用现有断开反馈，不要改成「对方已结束这次协助。」
- 还没连上就失败：不要改 R003 已验收的句子，包括「对方已刷新会话，旧码失效了。请对方重新复制并发给你。」和「对方还没打开「接受协助」。请对方在首页选择「接受协助」，再把码和密码发给你。」
- 结束后，常用操作五个按钮按 R016 规则重新藏起（未连接时不展示）；可以再次粘贴码或选设备重新连接。
- Windows 去协助端（Helper.UI）与 Avalonia 去协助端（HelperWindow）都改到，共用同一句常量。
- 本轮不要求新增中继协议字段。已经连上之后会话没了，对协助方来说就是这次协助结束了；网络中途断开也可以用这句，不必区分「对方点了按钮」还是「连接中断」。

## 本轮不做

- 改中继 JSONL、改 AI-SSH-Pro、完整远程桌面
- 改 EndAssistCopy / R033 接受协助端结束按钮与确认窗
- 改 R031 接受协助端主状态
- 改 InstallInstructions / Skill 文案 / trust-levels「才问你」
- 改官网 HTML、换公网安装包
- 把 macOS 写成已经可下

## 完成时

- 更新本文交付说明
- 截图放到 docs/screenshots/r035-*（至少：去协助端已连上；对方结束后主状态为「对方已结束这次协助。」且五个操作按钮已收起；协助方自己断开或尚未连上失败时仍是原有文案、不是这句）。Windows 与 Avalonia 都要。可用 ui-shot，须标明。**勿覆盖** r022-*～r034-*。
- 回告产品验收；未开下一轮


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

亦复制到 `%USERPROFILE%\Downloads\r035-shots\`。

### 缺口 / 披露
- 截图为 ui-shot 注入状态，非真连中继后对方点「结束这次协助」的实拍；运行时依赖 Socket Poll / 读失败探测对端关闭。
