# 第 40 轮 — 官网安装包跟上含 R039 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端出包；鹊桥-服务端替换上架
> 状态：**delivered**
> 依据：R039 产品已验收。官网当前仍是 R038 的包（含 R035–R037）。R039 Avalonia 去协助端连上后五键出现在首屏，还没进正式下载。该让官网跟上。

用户从官网下载到的 Linux Avalonia 包，连上之后仍会把五个常用操作挤到账号和 MCP 后面。R039 已经修好，包要换新的。

## 本轮目标

1. **高频（客户端）**：从包含 `5c276ec`（R039；祖先含 R037 `0405491`、R036 `d24fb44`、R035 `b707804`）的 HEAD 打出 Windows 安装包和 Linux 包，把路径、体积、SHA256 交给服务端。
2. **打磨（服务端）**：用新包替换公网两个下载文件；首页旁注不要改；补公网下载截图。

## 公网文件（锁定路径，只换内容）

- Windows：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
- Linux：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz

首页不要改：

- 导语继续是「Windows 和 Linux 都可下载使用」
- Linux 旁注（R023）继续是：解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。
- Windows 旁注（R025）继续是：若浏览器提示通常不会下载，选择保留即可。
- macOS 仍写即将提供
- 不要出现端口、17891、放行入站、自包含、TLS、证书、签名、SmartScreen 说明

## 本轮不做

- 改客户端 UI 文案或布局（含 Avalonia 五键折行）、改中继 JSONL、改 AI-SSH-Pro
- 改 InstallInstructions / Skill / EndAssistCopy / trust-levels「才问你」
- 把 macOS 写成已经可下
- 买证书或上 HTTPS
- 改首页其他文案

## 完成时

- 客户端：交付产物路径、体积、SHA256、出包所用 HEAD；把包交给服务端。不要自己上公网。
- 服务端：替换后回告；截图放到 `docs/screenshots/r040-home-download.png`、`r040-win-download.png`、`r040-linux-download.png`（须能看到地址栏是 121.196.45.81）。可用浏览器实拍。**勿覆盖** r022-*～r039-*。
- 产品对照公网独立下载并核对哈希后再验收；未开下一轮


## 交付说明（客户端出包 · 2026-11-06 Asia/Shanghai）

### 出包 HEAD
- `49a4292727b01e615e16a0a95835ec491410ffaa` — `docs: 收口 S004 为 R041 高频，PAIR_OK 为人话打磨`
- 祖先确认：`git merge-base --is-ancestor 5c276ec HEAD` / `0405491` / `d24fb44` / `b707804` 均为真（含 R039 功能及 R037/R036/R035）。

### Windows
| 项 | 值 |
|----|-----|
| 路径 | `E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe` |
| 字节 | 75,585,615 |
| SHA256 | `C403FA6D254CD8F9FD8AEB509A27757523A7C28E54A57444C704D8A0601E2104` |
| mtime | 2026-11-06 07:55:01 Asia/Shanghai |
| 命令 | `scripts\build-installer.ps1` |

### Linux（framework-dependent，非 SC）
| 项 | 值 |
|----|-----|
| 本地路径 | `E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `queqiao-linux-x64.tar.gz`（去掉 `-fd`） |
| 字节 | 8,005,149 |
| SHA256 | `A2F1E1874CD59B50D4EE96DCC0E01FA4167AC3E468234A0C0DF9853975AC2467` |
| mtime | 2026-11-06 07:55:04 Asia/Shanghai |
| 命令 | `scripts\publish-agent-avalonia.ps1 -Runtimes linux-x64` |

包内：`RemoteAssist.Agent.Avalonia` + `运行说明.txt`；`RemoteAssist.Agent.Avalonia.runtimeconfig.json` → `Microsoft.NETCore.App` 8.0.0；无 hostfxr/coreclr 等 SC 痕迹。

### 对照 R038（上一正式公网包）
| | R038 | 本轮 R040 |
|--|------|-----------|
| Windows 字节 | 75,585,944 | **75,585,615** |
| Windows SHA256 | `8B777AE3…6416` | `C403FA6D…2104` |
| Linux 字节 | 8,005,019 | **8,005,149** |
| Linux SHA256 | `AB2DF29F…ABD2` | `A2F1E187…2467` |

字节与 SHA256 **均不同**（代码已含 R039）。

### 未做 / 确认
- 未改业务代码 / UI / 文案；未改 InstallInstructions / Skill / EndAssistCopy / R031–R039 已验收文案 / trust-levels「才问你」
- 未打 Linux SC；未把 macOS 当正式可下；未改 site/ / relay/；未上生产机；未自己上公网；未开下一轮
- 包已就绪，交给服务端替换公网两个下载文件
