# 第 37 轮 — 去协助端已连上后不再提示点「连接」

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**done（产品已验收）**
> 依据：R036 产品已验收。连上之后主按钮已经是不可点的「已连接」，但旁边仍写着「粘贴后点「连接」」，看起来还要再点一次。

按钮态已经对齐了。接下来把这句过时的操作提示收起来：已经连上，就不要再叫人去点「连接」。

## 本轮目标

1. **高频**：去协助端成功连上之后，不要再显示「粘贴后点「连接」」。
2. **打磨**：对方结束这次协助、自己点断开、或还没连上（含连接失败）时，这句提示再出现。不要另写一句新的主状态。

## 文案（锁定）

提示原文保持：「粘贴后点「连接」」。本轮只控制它何时出现，不改这几个字。

已连上时不显示这句。主状态继续用已有的「已连接对方电脑」；按钮继续用 R036 的「已连接」。

## 行为

- Windows Helper.UI 与 Avalonia HelperWindow 都改到。
- 已连上：隐藏「粘贴后点「连接」」。
- 对方已结束这次协助、自己断开、尚未连上（含连接失败）：再显示这句原文。
- 不要改 `ConnectedButtonText` / `ConnectButtonText` / R035 三句主状态 / R003 失败原因句。
- 不要改五个常用操作的显隐，不要改账号区「连接这台电脑」。

## 本轮不做

- 改中继 JSONL、改 AI-SSH-Pro、完整远程桌面
- 改 EndAssistCopy / R031 接受协助端主状态
- 改 InstallInstructions / Skill 文案 / trust-levels「才问你」
- 改官网 HTML、换公网安装包
- 把 macOS 写成已经可下

## 完成时

- 更新本文交付说明
- 截图放到 docs/screenshots/r037-*（至少：Windows 已连上时能看到「已连接」按钮且看不到「粘贴后点「连接」」；Avalonia 同样；对方结束或自己断开后这句提示重新出现，且 R035 主状态、R036 实心「连接」仍在）。可用 ui-shot，须标明。**勿覆盖** r022-*～r036-*。
- 回告产品验收；未开下一轮


## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- `HelperSessionCopy` 增补：`PasteThenConnectHint`=「粘贴后点「连接」」（原文锁定）、`ShowPasteThenConnectHint(sessionConnected)`（已连上 → false）
- Windows `Helper.UI.MainForm`：`SetConnectedUi` → `UpdatePasteThenConnectHintVisibility`；`RefreshPreview` 已连上（真连或 ui-shot「已连接」按钮）不再刷回该句
- Avalonia `HelperWindow`：同上；`RefreshPreview` 在 `_client` / `_uiShotConnected` 时不再刷回
- 未改 `ConnectedButtonText` / `ConnectButtonText` / R035 三句主状态 / R003 失败原因句
- 未改五个常用操作显隐、账号区「连接这台电脑」/ 免密灰「已连接」
- 未改 EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」；未改 relay/site；未换公网包；未开下一轮

### 粘贴提示显隐
| 情景 | 「粘贴后点「连接」」 | R035 主状态 | R036 主按钮 |
|------|-------------------|------------|------------|
| 已连上 | 隐藏 | 已连接对方电脑 | 不可点「已连接」 |
| 对方结束 | 再显示原文 | 对方已结束这次协助。 | 实心「连接」 |
| 自己断开 | 再显示原文 | 状态：未连接 | 实心「连接」 |
| 未连上 / 失败 | 显示原文 | （失败时「连接失败」等） | 实心「连接」 |

### 测试
- `RemoteAssist.Protocol.Tests`：**191** 通过（含 `PasteThenConnectHint_HiddenWhenConnected_ExactCopyUnchanged`）

### 截图（均为 **ui-shot**）
| 文件 | 平台 | 显示 | MD5 | 字节 |
|------|------|------|-----|------|
| `docs/screenshots/r037-helper-connected-no-paste-hint.png` | Win | 「已连接」灰禁 +「已连接对方电脑」；**不见**「粘贴后点「连接」」 | 4823519DDC78D758059472703A2A6ABD | 45077 |
| `docs/screenshots/r037-helper-peer-ended-hint-back.png` | Win | 「对方已结束这次协助。」+ 实心「连接」+ 粘贴提示再出现 | 4054F5B75E449DE032AF37C9CFB890E8 | 41071 |
| `docs/screenshots/r037-helper-self-disconnect-hint-back.png` | Win | 「状态：未连接」+ 实心「连接」+ 粘贴提示 | 46D5815D703A7AA50796A529B6EF2EAA | 40760 |
| `docs/screenshots/r037-avalonia-helper-connected-no-paste-hint.png` | Avalonia | 「已连接」灰禁；**不见**粘贴提示 | E9BA7E3CCCF466F10CB0FF60844408FC | 57587 |
| `docs/screenshots/r037-avalonia-helper-peer-ended-hint-back.png` | Avalonia | 对方结束句 + 实心「连接」+ 粘贴提示 | A5D5B996AC38C3AD417A2D453E5E5518 | 58388 |
| `docs/screenshots/r037-avalonia-helper-self-disconnect-hint-back.png` | Avalonia | 未连接句 + 实心「连接」+ 粘贴提示 | 991BF347449A1D5A885A0578509B2916 | 57641 |

亦复制到 `%USERPROFILE%\Downloads\r037-shots\`（`C:\Users\user\Downloads\r037-shots\`）。

### 缺口 / 披露
- 六张均为 **ui-shot**（非真连中继实拍）；Win 连上态用控件校验：按钮「已连接」/禁用/非绿，且可见文本不含粘贴提示原文。
- 对方结束 / 自己断开四张与对应 `r036-*-reconnect.png` **MD5 相同**（那些图里粘贴提示本来就在；本轮连上态两张 MD5 已与 r036 区分）。未覆盖任何 r022–r036 截图文件。
- r037 六张 MD5 互不相同。

## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r037-*` 与源码独立验收通过（功能 `0405491`，文档 `60b7ac3`）。Protocol.Tests 191 通过。

- `HelperSessionCopy.PasteThenConnectHint` 仍是「粘贴后点「连接」」；显隐用 `ShowPasteThenConnectHint`（已连上为 false）。Windows `Helper.UI.MainForm` 与 Avalonia `HelperWindow` 都接到。
- 已连上：不见「粘贴后点「连接」」；保留灰色不可点「已连接」与主状态「已连接对方电脑」。Windows 五个常用操作仍可见；账号区仍是「连接这台电脑」。
- 对方结束后提示重新出现，且「对方已结束这次协助。」与实心「连接」仍在。自己断开四张与对应 `r036-*` / `r035-*` 像素相同（已披露）。
- 未改 ConnectedButtonText / ConnectButtonText / R035 三句主状态 / R003 / EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」。

| 截图 | MD5 | 产品看到的 |
|------|-----|------------|
| `docs/screenshots/r037-helper-connected-no-paste-hint.png` | 4823519DDC78D758059472703A2A6ABD | Windows 灰色不可点「已连接」；「已连接对方电脑」；不见「粘贴后点「连接」」；五键可见；账号区「连接这台电脑」 |
| `docs/screenshots/r037-helper-peer-ended-hint-back.png` | 4054F5B75E449DE032AF37C9CFB890E8 | 与 `r035-helper-peer-ended.png` / `r036-helper-peer-ended-reconnect.png` 相同：「对方已结束这次协助。」+ 实心「连接」+ 「粘贴后点「连接」」在 |
| `docs/screenshots/r037-helper-self-disconnect-hint-back.png` | 46D5815D703A7AA50796A529B6EF2EAA | 与对应 R035/R036 图相同：「状态：未连接」+ 实心「连接」+ 提示在 |
| `docs/screenshots/r037-avalonia-helper-connected-no-paste-hint.png` | E9BA7E3CCCF466F10CB0FF60844408FC | Avalonia 灰禁「已连接」；「已连接对方电脑」；不见「粘贴后点「连接」」；账号区「连接这台电脑」 |
| `docs/screenshots/r037-avalonia-helper-peer-ended-hint-back.png` | A5D5B996AC38C3AD417A2D453E5E5518 | 与对应 R035/R036 图相同：「对方已结束这次协助。」+ 实心「连接」+ 提示在 |
| `docs/screenshots/r037-avalonia-helper-self-disconnect-hint-back.png` | 991BF347449A1D5A885A0578509B2916 | 与对应 R035/R036 图相同：「状态：未连接」+ 实心「连接」+ 提示在 |

缺口：六张均为 ui-shot；对方结束/自己断开四张与 R036 对应图 MD5 相同（已披露，未覆盖旧文件）；Avalonia 已连上图可视区域仍未收入五个常用操作。

已开 R038（官网安装包跟上含 R035–R037 的客户端；客户端出包、服务端替换上架；不改首页旁注、不改 AI-SSH-Pro）。
