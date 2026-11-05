# 第 17 轮 — Avalonia MCP 安装说明

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（Avalonia；不改 `relay/`，不迁 Windows 到 Avalonia，不重做 R006–R016）
> 状态：**产品已验收**
> 依据：R016 产品已验收（f083f51）；Windows 已验收的 MCP / 「复制安装说明」入口（R005，quality.md §3）；PRODUCT.md「可选 MCP 给 Agent；仅接受协助时不自动对外监听 MCP」。

R016 已产品验收 Avalonia 协助端连上后五键白话与已连接按钮轻态。Linux Avalonia 界面上还缺与 Windows 同等的 MCP 安装说明入口。

## 本轮目标

1. **高频**：Linux Avalonia 界面补上与 Windows 同等的 MCP 安装说明入口（「复制安装说明」或对等）。
2. **打磨**：复制后要有人话反馈（如「已复制安装说明」），不要静默。

## A. MCP 安装说明（高频）

验收：
- 对照 Windows 已验收的 MCP / 「复制安装说明」入口与文案，在 Linux Avalonia 做出同等能力，不要另起一套说法。放置位置对齐 Windows。
- 用户能在界面上看到并复制安装说明。
- 该入口是轻按钮，不要和会话码「连接」或账号区「连接：{名称}」抢主点击。
- 仅接受协助时不自动对外监听 MCP。
- 亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号、内部 id。品牌「鹊桥」。
- osx 能出包即可；未上真机就注明，不写成 Mac 已验收。

截图放到 `docs/screenshots/r017-*`（至少：MCP 安装说明入口可见且是轻按钮；说明内容或复制后的人话反馈）。

## B. 复制后人话反馈（打磨）

- 点「复制安装说明」后，要有人话反馈（如「已复制安装说明」或 Windows 已验收的同等文案），不要静默。
- 不要改 R016 已验收的五键文案与顺序、引导「建议先看正在运行的程序，再决定要不要改」、已连接后账号区「已连接」轻态、免密成功文案。
- 不要改 R013–R015 已验收原文。

## 本轮不做

- 改中继、改官网下载、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 重做 R006–R016 已验收路径
- 把「打开终端复制码」写给亲友主路径
- 改 trust-levels.md 里宽松一行说明的「才问你」
- 不要把现场公网双机结对当成本轮必须

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- **「复制安装说明」**：协助端「连接对方」卡内，与 Windows 同位置语义——「先让对方装好「鹊桥远程协助」」旁轻按钮（白底蓝字，非实心绿）。复制 `WriteConfirmCopy.InstallInstructionsClipboard`，反馈 `WriteConfirmCopy.InstallCopiedHint`（「请把安装包和这段说明一起发出去」）。
- **MCP 安装说明卡**：`McpInstallGuide.SectionTitle` / `StepsBlock` / 「复制 MCP 配置」轻按钮 +「详细参数见产品文档；写操作仍须对方本机确认」。复制走 `McpConfigSnippet.BuildClipboardBundle` + `McpExeLocator`，反馈 `CopiedOkHint` / `CopiedPlaceholderHint`。缺会话码时提示 `NeedCodePasswordHint`。
- **未改** R013–R016 已验收原文；未在接受协助路径自动对外监听 MCP（MainWindow 无 MCP 监听启动）。
- **osx**：与 Linux 同树可出包；本轮未重跑 publish、**未上真机**，不写成 Mac 已验收。

### 截图（`docs/screenshots/r017-*`，已复制 `%USERPROFILE%\Downloads\r017-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r017-install-entry.png` | 「复制安装说明」轻按钮 +「如何安装 MCP」卡与「复制 MCP 配置」轻按钮可见；ui-shot | 8ED52115F70AF18402F98BABF64CF47B |
| `r017-install-copied.png` | 复制后反馈「请把安装包和这段说明一起发出去」；ui-shot | E8B50E41736D1349263FAE50E1BD663F |

截图为 **ui-shot**。MD5 互异，且不同于 r015/r016。亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号、deviceId、offline。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开 R018 / 下一轮
- 未改 `relay/`、官网下载、AI-SSH-Pro
- 未把 Windows 迁到 Avalonia；未重做 R006–R016
- 未改 R013–R016 已验收文案；未改 `docs/trust-levels.md`「才问你」
- 仅接受协助时不自动对外监听 MCP（本轮未新增监听）
- 现场公网双机非本轮必须，未做

## 产品验收（2026-11-05 Asia/Shanghai）

产品对照截图验收通过，提交 `5bcab34`（`5bcab346b7d5de32eb27ce317eb71bca1c38adce`；实现 `e46b29f`，随后修了反馈重复）。交付说明记 `dotnet test` 169 通过 / 0 失败。

所见：
- `r017-install-entry.png`：品牌「鹊桥远程协助 · 去协助别人」；「先让对方装好「鹊桥远程协助」」旁轻按钮「复制安装说明」（白底蓝字，非实心绿）；会话码「连接」仍是实心绿主按钮；「从剪贴板」为次要灰按钮。账号「账号」「密码」可见标签仍在；「连接这台电脑」灰色禁用 + 「请先点选一台自己的电脑」。
  「如何安装 MCP」三步可见：1. 点「复制 MCP 配置」；2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存；3. 刷新或重启后再用智能体协助；对方电脑上要改动的操作仍会弹确认，智能体不能绕过。轻按钮「复制 MCP 配置」+「详细参数见产品文档；写操作仍须对方本机确认」。未见 Assist、Admin、ownerLink、TLS、端口号。
- `r017-install-copied.png`：复制后绿色提示「请把安装包和这段说明一起发出去」只出现一次；「状态: 未连接」仍在。此为 Windows 已验收的 InstallCopiedHint 文案，不是另起的「已复制安装说明」。

缺口（不挡本轮）：
- 截图为 ui-shot（模拟复制反馈，非真机剪贴板验收）。
- 未单独拍「复制 MCP 配置」后的反馈（本轮打磨要求的是「复制安装说明」反馈，MCP 三步说明本身已可见）。
- 「仅接受协助时不自动对外监听 MCP」为代码侧说明（MainWindow 无监听启动），本轮截图是去协助界面，未在接受协助窗上直接取证。
- 本轮未重跑 osx publish；未上真机，不写「Mac 已验收」。

下一轮见 `docs/round-018.md`。
