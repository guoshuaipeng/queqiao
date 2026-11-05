# 第 18 轮 — 接受协助端可选 MCP 说明

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（Avalonia；不改 `relay/`，不迁 Windows 到 Avalonia，不重做 R006–R017）
> 状态：**ready-for-product**
> 依据：R017 产品已验收（5bcab34）；PRODUCT.md「同一安装包双模式 + 可选 MCP 给 Agent；仅接受协助时不自动对外监听 MCP」；R017 已在去协助端补上「复制安装说明」与「如何安装 MCP」。

R017 已产品验收去协助端的「复制安装说明」轻按钮、复制反馈，以及「如何安装 MCP」三步。接受协助端（被控窗）还没有同等的可选说明。本轮补上，并写明不会自动对外监听。

## 本轮目标

1. **高频**：Linux Avalonia 接受协助端补上可选的「如何安装 MCP」说明与「复制 MCP 配置」入口。
2. **打磨**：「复制 MCP 配置」后要有人话反馈，不要静默。

## A. 接受协助端可选 MCP 说明（高频）

验收：
- 接受协助端（MainWindow，可放在「高级」里，不要抢「复制码+密码」主按钮）提供与去协助端同等的「如何安装 MCP」三步文案，或更短的同等人话。
- 明确写明这是可选的，接受协助时不会自动对外监听 MCP（人话，不要出现端口号、stdio、TLS）。
- 「复制 MCP 配置」是轻按钮，不要和「复制码+密码」抢主点击。
- 不要在仅接受协助时自动启动 MCP 监听。
- 对照 R017 去协助端已验收文案，不要另起一套说法。
- 亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号、内部 id。品牌「鹊桥」。
- osx 能出包即可；未上真机就注明，不写成 Mac 已验收。

截图放到 `docs/screenshots/r018-*`（至少：接受协助端 MCP 入口与可选/不自动监听说明可见；复制 MCP 配置后的人话反馈；「复制码+密码」仍是主按钮）。

## B. 复制 MCP 配置后人话反馈（打磨）

- 点「复制 MCP 配置」后，要有人话反馈（如「已复制 MCP 配置」或与 R017 / Windows 已验收同等文案），不要静默。
- 不要改 R017 已验收的去协助端「复制安装说明」「请把安装包和这段说明一起发出去」「如何安装 MCP」三步。
- 不要改 R013–R016 已验收原文。

## 本轮不做

- 改中继、改官网下载、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 重做 R006–R017 已验收路径
- 把「打开终端复制码」写给亲友主路径
- 改 trust-levels.md 里宽松一行说明的「才问你」
- 不要把现场公网双机结对当成本轮必须
- 不要为了展示 MCP 而在接受协助时自动对外监听

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- **接受协助端 `MainWindow` 高级**：增加与去协助端同等的 `McpInstallGuide`「如何安装 MCP」三步 + 轻按钮「复制 MCP 配置」。
- **可选 / 不自动监听**：高级内人话「可选。接受协助时不会自动对外开启 MCP；只有你主动复制配置给智能体时才用得上。」（无端口号 / stdio / TLS）。本轮未启动任何 MCP 监听。
- **复制反馈**：`McpInstallGuide.CopiedOkHint` / `CopiedPlaceholderHint` 只写入 `_mcpFeedback` 一行，不写状态行，避免重复。
- **主按钮权重**：「复制码+密码」仍为实心主按钮；「复制 MCP 配置」白底蓝字轻按钮，放在高级里。
- **未改** R013–R017 已验收原文。
- **osx**：与 Linux 同树可出包；本轮未重跑 publish、**未上真机**，不写成 Mac 已验收。

### 截图（`docs/screenshots/r018-*`，已复制 `%USERPROFILE%\Downloads\r018-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r018-mcp-entry.png` | 高级内「如何安装 MCP」三步 + 可选/不自动开启说明 + 轻按钮「复制 MCP 配置」；ui-shot | ED8DB4C33BC008E1761D2A31C732C599 |
| `r018-mcp-copied.png` | 复制后「已复制 MCP 配置。请粘贴到 Cursor 的 MCP 设置并保存。」只出现一次；ui-shot | FB678C85C256C3F8BB554B0CDAAD411C |
| `r018-copy-primary.png` | 「复制码+密码」实心主按钮仍在；MCP 为高级轻按钮；ui-shot | 2B59E34B1BBB38A7FA32AFC2E5C8F2DC |

截图为 **ui-shot**。MD5 互异，且不同于 r015/r016/r017。亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号、stdio、deviceId、offline。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开 R019 / 下一轮
- 未改 `relay/`、官网下载、AI-SSH-Pro
- 未把 Windows 迁到 Avalonia；未重做 R006–R017
- 未改 R013–R017 已验收文案；未改 `docs/trust-levels.md`「才问你」
- 接受协助时未自动启动 MCP 监听
- 现场公网双机非本轮必须，未做

