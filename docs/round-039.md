# 第 39 轮 — Avalonia 去协助端连上后五个常用操作出现在首屏

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**done（产品已验收）**
> 依据：R038 产品已验收。R035–R037 的 Avalonia 已连上截图里，五个常用操作都不在可视区域；Windows 同一状态下五键就在主状态下面。连上之后，人最先要做的是看正在运行的程序，不应该先翻过账号和 MCP。

官网包已经跟上。接下来补这块连上之后的首屏：Avalonia 去协助端也要把五个常用操作放在眼前。

## 本轮目标

1. **高频**：Avalonia `HelperWindow` 成功连上后，五个常用操作要紧挨主状态「已连接对方电脑」下面出现，不用往下翻。顺序和文案保持：查看正在运行的程序 / 查看文件夹 / 查看日志 / 查看文件 / 提议一项改动。
2. **打磨**：未连上、对方结束这次协助、或自己点断开时，这五键仍然隐藏（R016 规则不变）。不要改按钮文案，不要改账号区，不要改 Windows 已经能看见五键的布局。

## 行为

- 只改 Avalonia 去协助端把五键放到主状态与账号区之间（与 Windows 一致），连上后首屏可见。
- 显隐继续用已有的 `ShowCommonTools`：已连上才显示，否则隐藏。
- 不要改 R035 三句主状态、R036「已连接」按钮、R037「粘贴后点「连接」」的显隐。
- 不要改 Windows Helper.UI 的现有布局。

## 本轮不做

- 改中继 JSONL、改 AI-SSH-Pro、完整远程桌面
- 改 EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」
- 改官网 HTML、换公网安装包
- 把 macOS 写成已经可下

## 完成时

- 更新本文交付说明
- 截图放到 docs/screenshots/r039-*（至少：Avalonia 已连上时能在同一屏看到「已连接对方电脑」、灰色「已连接」和五个常用操作；Avalonia 对方结束或自己断开后五键重新隐藏；Windows 已连上时五键仍可见）。可用 ui-shot，须标明。**勿覆盖** r022-*～r038-*。
- 回告产品验收；未开下一轮


## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- Avalonia `HelperWindow`：`body` 子控件顺序改为 `pairCard` → `_toolCard` → `accountCard` → `mcpCard`（R039：常用操作紧挨主状态下方，账号 / MCP 之前）
- `_toolCard.IsVisible` 改为显式调用 `HelperSessionCopy.ShowCommonTools(connected)`（规则不变）
- 五键文案改为读 `HelperSessionCopy.CommonToolButtonLabels`（顺序/原文锁定）；未改 Windows Helper.UI 布局与账号区
- 未改 R035 三句主状态 / R036 按钮文案 / R037 粘贴提示原文与显隐；未改 EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」；未改 relay/site；未换公网包；未开下一轮

### 测试
- `RemoteAssist.Protocol.Tests`：**192** 通过（新增 `CommonToolButtonLabels_OrderLocked`）

### 截图（均为 **ui-shot**，非真连中继配对）
| 文件 | 平台 | 显示 | MD5 | 字节 |
|------|------|------|-----|------|
| `docs/screenshots/r039-avalonia-helper-connected-tools-first-screen.png` | Avalonia | 同屏「已连接对方电脑」+ 灰禁「已连接」+ 五个常用操作 | 2B8444538D7307AEEE79BFD255F84E6B | 60202 |
| `docs/screenshots/r039-avalonia-helper-peer-ended-tools-hidden.png` | Avalonia | 「对方已结束这次协助。」；五键隐藏 | A5D5B996AC38C3AD417A2D453E5E5518 | 58388 |
| `docs/screenshots/r039-avalonia-helper-self-disconnect-tools-hidden.png` | Avalonia | 「状态：未连接」；五键隐藏 | 991BF347449A1D5A885A0578509B2916 | 57641 |
| `docs/screenshots/r039-helper-connected-tools-visible.png` | Win | 已连上五键仍可见（确认未回归） | 4823519DDC78D758059472703A2A6ABD | 45077 |

亦复制到 `%USERPROFILE%\Downloads\r039-shots\`（`C:\Users\user\Downloads\r039-shots\`）。

### 缺口 / 披露
- 四张均为 ui-shot。
- Avalonia 对方结束 / 自己断开两张与对应 `r037-avalonia-*-hint-back.png` **MD5 相同**（那些帧五键本就隐藏）。
- Windows 已连上张与 `r037-helper-connected-no-paste-hint.png` **MD5 相同**（Win 布局未改，五键本就在首屏）。
- Avalonia 已连上张 MD5 **不同于** r037（可证五键进入首屏）。r039 四张 MD5 互不相同。未覆盖 r022–r038。

## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r039-*` 与源码独立验收通过（功能 `5c276ec`，文档 `aedb7ff`）。Protocol.Tests 192 通过。

- Avalonia `HelperWindow` 的 body 顺序改为 pairCard → `_toolCard` → accountCard → mcpCard。连上后五键紧挨「已连接对方电脑」出现在首屏。
- 五键文案与顺序仍是 `CommonToolButtonLabels`：查看正在运行的程序 / 查看文件夹 / 查看日志 / 查看文件 / 提议一项改动。显隐仍用 `ShowCommonTools`。
- 未改 Windows Helper.UI 布局。未改账号区、R035 三句主状态、R036「已连接」按钮、R037「粘贴后点「连接」」显隐、EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」。

| 截图 | MD5 | 产品看到的 |
|------|-----|------------|
| `docs/screenshots/r039-avalonia-helper-connected-tools-first-screen.png` | 2B8444538D7307AEEE79BFD255F84E6B | 同屏可见灰色「已连接」、「已连接对方电脑」、常用操作五键（建议先看正在运行的程序…） |
| `docs/screenshots/r039-avalonia-helper-peer-ended-tools-hidden.png` | A5D5B996AC38C3AD417A2D453E5E5518 | 与 r037 Avalonia 对方结束图相同：五键本就隐藏 |
| `docs/screenshots/r039-avalonia-helper-self-disconnect-tools-hidden.png` | 991BF347449A1D5A885A0578509B2916 | 与 r037 Avalonia 自己断开图相同：五键本就隐藏 |
| `docs/screenshots/r039-helper-connected-tools-visible.png` | 4823519DDC78D758059472703A2A6ABD | 与 r037 Windows 已连上图相同：五键仍可见，布局未改 |

缺口：四张均为 ui-shot；后三张与对应 r037 图 MD5 相同（已披露，未覆盖旧文件）；已连上图里「提议一项改动」因宽度折到第二行，五键仍都在首屏。未开下一轮。
