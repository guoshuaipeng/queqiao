# 第 41 轮 — 「提议一项改动」等待/结果四态人话 + 连上后去掉 PAIR_OK

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**delivered**
> 依据：R040 产品已验收。场景 S004（`docs/scene-s004-propose-wait.md`）与体验质感续篇（`docs/quality-after-connect-status.md`）已收口。官网包已含 R039；本轮修连上之后协助方仍像控制台的两处人话。

## 本轮目标

1. **高频（S004）**：去协助端点「提议一项改动」后的四态人话（Windows + Avalonia）：
   - 等待：`正在等对方在本机确认…`（不要出现 `RunCommand`）
   - 允许成功：`对方已允许，改动已执行`（不要用「已提议一项改动」表示成功）
   - 拒绝：继续 `已拒绝，未做任何改动`
   - 超时：必须 `确认超时，未做任何改动`（`DeniedByUser` 时优先展示非空 `Error`，不要无条件盖成拒绝句）
2. **打磨**：配对刚成功、尚未点操作时，结果区不要显示 `PAIR_OK`；改为例如 `已连上。建议先点「查看正在运行的程序」。`；点操作后再显示可读结果。

## 行为要点

- 文案进共用常量，Windows Helper.UI 与 Avalonia HelperWindow 都接到。
- 只读四个常用操作不要套「正在等对方确认」等待句。
- 不新增中继/协议字段；不改被控端确认窗原文；不改五键文案与顺序、显隐；不改 R035–R039 已锁主状态/按钮。
- 不改 EndAssistCopy / InstallInstructions / Skill / trust-levels「才问你」。

## 本轮不做

- 改中继 JSONL、改 AI-SSH-Pro、完整远程桌面
- 换公网安装包、改官网 HTML
- 把 macOS 写成已经可下；买证书或上 HTTPS

## 完成时

- 更新本文交付说明
- 截图放到 `docs/screenshots/r041-*`（至少：刚连上无 PAIR_OK；等待确认句；允许成功句；拒绝句；超时句；Windows 与 Avalonia 都要覆盖四态与刚连上）。可用 ui-shot，须标明。**勿覆盖** r022-*～r040-*。
- 回告产品验收；未开下一轮


## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- 新增 `ProposeChangeCopy`：`WaitingStatus` / `AllowedStatus` / `ConnectedResultHint`；`FailureStatus(deniedByUser, error)`（DeniedByUser 时优先非空 Error，超时不再盖成拒绝句）
- Windows `Helper.UI.MainForm` 与 Avalonia `HelperWindow`：`RunToolAsync` 对 `RunCommand` 显示等待/允许人话；失败走 `FailureStatus`；刚连上结果区改为 `ConnectedResultHint`（去掉 `PAIR_OK`）
- 拒绝/超时原文仍用 `WriteConfirmCopy.DeniedMessage` / `TimedOutMessage`（未改字）
- 未改五键顺序/显隐、R035–R037、R031、EndAssistCopy / InstallInstructions / Skill / 「才问你」；未改中继/官网包；未开下一轮

### 常量
| 常量 | 取值 |
|------|------|
| `ProposeChangeCopy.WaitingStatus` | 正在等对方在本机确认… |
| `ProposeChangeCopy.AllowedStatus` | 对方已允许，改动已执行 |
| `WriteConfirmCopy.DeniedMessage` | 已拒绝，未做任何改动（未改） |
| `WriteConfirmCopy.TimedOutMessage` | 确认超时，未做任何改动（未改） |
| `ProposeChangeCopy.ConnectedResultHint` | 已连上。建议先点「查看正在运行的程序」。 |

### 测试
- `RemoteAssist.Protocol.Tests`：**196** 通过（+4 `ProposeChangeCopyTests`）

### 截图（均为 **ui-shot**，非真实本机确认流）
| 文件 | 平台 | 显示 | MD5 | 字节 |
|------|------|------|-----|------|
| `docs/screenshots/r041-avalonia-helper-connected-no-pair-ok.png` | Avalonia | 刚连上；结果区人话，无 PAIR_OK | A515E08D22ED7940EA2FBCE7728223B3 | 62026 |
| `docs/screenshots/r041-avalonia-helper-propose-allowed.png` | Avalonia | 对方已允许，改动已执行 | FD2C5579A3910AC1F3E846C2B74F2B27 | 61980 |
| `docs/screenshots/r041-avalonia-helper-propose-denied.png` | Avalonia | 已拒绝，未做任何改动 | 1E369647C6E3457E60538AB3FD938638 | 62483 |
| `docs/screenshots/r041-avalonia-helper-propose-timed-out.png` | Avalonia | 确认超时，未做任何改动 | 29B20F0C7B95BFD6E985D5BAF0DC1D24 | 63039 |
| `docs/screenshots/r041-avalonia-helper-propose-waiting.png` | Avalonia | 正在等对方在本机确认… | 40FB1E4E57CA616E5F2804F6286FCF47 | 62596 |
| `docs/screenshots/r041-helper-connected-no-pair-ok.png` | Win | 刚连上；结果区人话，无 PAIR_OK | 8864575CA5A6FFDD25866232074BC8DF | 46081 |
| `docs/screenshots/r041-helper-propose-allowed.png` | Win | 对方已允许，改动已执行 | 02F7F002A4AE399427F972AE5A805458 | 45910 |
| `docs/screenshots/r041-helper-propose-denied.png` | Win | 已拒绝，未做任何改动 | 5745D7D30CC5DEF7394EB4C238C582A5 | 46274 |
| `docs/screenshots/r041-helper-propose-timed-out.png` | Win | 确认超时，未做任何改动 | DCDBDC3D1FA2EEC280607931F72877A5 | 46610 |
| `docs/screenshots/r041-helper-propose-waiting.png` | Win | 正在等对方在本机确认… | 916D886955AE1027404EDEB8817E261E | 46306 |

亦复制到 `%USERPROFILE%\Downloads\r041-shots\`（`C:\Users\user\Downloads\r041-shots\`）。十张 MD5 互不相同；未覆盖 r022–r040。

### 缺口 / 披露
- 十张均为 ui-shot 注入状态，非真实中继配对与对方点允许/拒绝/超时的实拍。
- Windows 超时帧控件校验确认主状态为超时句且不含拒绝句。
