# 第 31 轮 — 接受协助端对方连上后人话

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**delivered**
> 依据：R030 产品已验收（官网包已含 R027–R029 的 Skill 入口）。接受协助端在还没人连上时，运行状态是「已连上网络，可以发给对方了」，这句没问题；可是对方真正连上之后，主状态仍容易停在这句“等待发出去”的话，亲友不容易一眼看出已经有人连上了。

官网下载与 Skill 入口这条线（R019–R030）已经闭环。本轮回到被协助的人正在等待对方时：有人连上，就要把主状态改成直白的「对方已连上」。

## 本轮目标

1. **高频**：协助方真正连上这台电脑时，接受协助端主状态改为「对方已连上。改动这台电脑前会先问你。」
2. **打磨**：对方断开后，主状态回到「已连上网络，可以发给对方了」。

## A. 对方连上后的主状态（高频 · 鹊桥-客户端）

当协助方已经连上（不是只连上了中继、还在等人），接受协助端主状态（Windows「运行状态」、Avalonia 对应那句）显示：

对方已连上。改动这台电脑前会先问你。

验收：

- Windows 接受协助端与 Linux Avalonia 接受协助端都要改。
- 还没人连上时，保持已验收的「已连上网络，可以发给对方了」，不要提前写成「对方已连上」。
- 「已连接的协助方」列表：有人时显示对方的称呼（没有称呼就写「协助方」），不要用一串内部编号；没人时可以继续是「（暂无）」。
- 「复制码+密码」仍是该屏唯一实心主按钮。
- 不要改 Skill / MCP 入口与文案、不要改「复制安装说明」/ `InstallInstructions`、不要改 `docs/trust-levels.md` 的「才问你」。
- 截图放到 `docs/screenshots/r031-*`（至少：还没人连上、已经有人连上、对方断开后）。可用 ui-shot，须标明。**勿覆盖** `r022-*`～`r030-*`。

## 打磨：断开后回到等待句

对方断开后，主状态回到：

已连上网络，可以发给对方了

不要留下过期的「对方已连上」。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 替换公网安装包
- 把未上真机的 macOS 包写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS
- 改信任等级「才问你」
- 改 R027–R029 已验收的 Skill 入口与文案

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮


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
