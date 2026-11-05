# 第 38 轮 — 官网安装包跟上含 R035–R037 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端出包；鹊桥-服务端替换上架
> 状态：**done（待产品验收）**
> 依据：R037 产品已验收。官网当前仍是 R034 的包（含 R031–R033）。R035 对方结束人话、R036 连上后「已连接」、R037 已连上后隐藏「粘贴后点「连接」」还没进正式下载。三轮 UI 已落地，该让官网跟上。

R034 之后又走了三轮去协助端会话体验。下载到的安装包应该带上这些改动，而不是还停在「结束这次协助」那一版。

## 本轮目标

1. **高频（客户端）**：从包含 `0405491`（R037；祖先含 R035 `b707804`、R036 `d24fb44`）的 HEAD 打出 Windows 安装包和 Linux 包，把路径、体积、SHA256 交给服务端。
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

- 改客户端 UI 文案、改中继 JSONL、改 AI-SSH-Pro
- 改 InstallInstructions / Skill / EndAssistCopy / trust-levels「才问你」
- 把 macOS 写成已经可下
- 买证书或上 HTTPS
- 改首页其他文案

## 完成时

- 客户端：交付产物路径、体积、SHA256、出包所用 HEAD；把包交给服务端。不要自己上公网。
- 服务端：替换后回告；截图放到 `docs/screenshots/r038-home-download.png`、`r038-win-download.png`、`r038-linux-download.png`（须能看到地址栏是 121.196.45.81）。可用浏览器实拍。**勿覆盖** r022-*～r037-*。
- 产品对照公网独立下载并核对哈希后再验收；未开下一轮


## 交付说明（客户端出包 · 2026-11-06 Asia/Shanghai）

### 出包 HEAD
- `0efc3c4f2e8e254f7bf8834f69252509c2dc970a` — `docs(R037): 产品验收通过并开 R038（官网安装包跟上含 R035-R037）`
- 祖先确认：`git merge-base --is-ancestor 0405491 HEAD` / `d24fb44` / `b707804` 均为真（含 R037 功能、R036、R035）。

### Windows
| 项 | 值 |
|----|-----|
| 路径 | `E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe` |
| 字节 | 75,585,944 |
| SHA256 | `8B777AE324AB2528A434BB407018A6048708F10BF5E9220F2C5281B1E6866416` |
| mtime | 2026-11-06 03:40:40 Asia/Shanghai |
| 命令 | `scripts\build-installer.ps1` |

### Linux（framework-dependent，非 SC）
| 项 | 值 |
|----|-----|
| 本地路径 | `E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `queqiao-linux-x64.tar.gz`（去掉 `-fd`） |
| 字节 | 8,005,019 |
| SHA256 | `AB2DF29FE1606D94A270D98D358B11F086B636BD649E47BDE5C6999D81C2ABD2` |
| mtime | 2026-11-06 03:40:53 Asia/Shanghai |
| 命令 | `scripts\publish-agent-avalonia.ps1 -Runtimes linux-x64` |

包内：`RemoteAssist.Agent.Avalonia` + `运行说明.txt`；`RemoteAssist.Agent.Avalonia.runtimeconfig.json` → `Microsoft.NETCore.App` 8.0.0；无 hostfxr/coreclr 等 SC 痕迹。

### 对照 R034（当前公网）
| | R034 | 本轮 R038 |
|--|------|-----------|
| Windows 字节 | 75,584,964 | **75,585,944** |
| Windows SHA256 | `64A32316…BCD0` | `8B777AE3…6416` |
| Linux 字节 | 8,002,690 | **8,005,019** |
| Linux SHA256 | `B20257AD…9108` | `AB2DF29F…ABD2` |

字节与 SHA256 **均不同**（代码已含 R035–R037）。

### 未做 / 确认
- 未改业务代码；未改 InstallInstructions / Skill / EndAssistCopy / R031–R037 已验收文案 / trust-levels「才问你」
- 未打 Linux SC；未把 macOS 当正式可下；未改 site/ / relay/；未上生产机；未自己上公网；未开下一轮
- 包已就绪，交给服务端替换公网两个下载文件


## 交付说明（鹊桥-服务端 · 2026-11-06 Asia/Shanghai）

### 上架摘要

已替换公网 Windows / Linux 安装包（客户端交付出包 HEAD `0efc3c4`，祖先含 R037 `0405491`、R036 `d24fb44`、R035 `b707804`；客户端文档提交 `9859186`），中继与站点进程均未重启；首页旁注与导语未回退。本轮未改 HTML。

| 项 | Windows | Linux |
|------|---------|-------|
| 公网 URL | http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe | http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz |
| 字节 | 75,585,944 | 8,005,019 |
| SHA256 | `8B777AE324AB2528A434BB407018A6048708F10BF5E9220F2C5281B1E6866416` | `AB2DF29FE1606D94A270D98D358B11F086B636BD649E47BDE5C6999D81C2ABD2` |
| Last-Modified | Thu, 05 Nov 2026 19:46:37 GMT（2026-11-06 03:46:37 Asia/Shanghai） | 同左 |
| Content-Type | application/octet-stream | application/gzip |
| Content-Disposition | `filename="RemoteAssist-Setup-0.1.0.exe"` | `filename="queqiao-linux-x64.tar.gz"`（无内部 `-fd`） |

对照公网 R034：Windows 75,584,964 / `64A32316…BCD0` → 新 75,585,944 / `8B777AE3…6416`；Linux 8,002,690 / `B20257AD…9108` → 新 8,005,019 / `AB2DF29F…ABD2`——**字节与 SHA256 均不同**。本机独立复核：Windows MZ `4D 5A`，Linux gzip `1F 8B`；scp 后 `/tmp` 与 `install` 后 `/var/lib/remote-assist-site/downloads/` 的 sha256sum 均与上表一致；属主 `rasite:rasite`，模式 `644`。

进程：替换前/后 `remote-assist-site` MainPID 仍 **21336**（ActiveEnterTimestamp 2026-11-06 01:28:31 Asia/Shanghai，`*:80`）；中继 MainPID 仍 **2114**（ActiveEnterTimestamp 2026-11-05 16:50:25 Asia/Shanghai，`*:17891`）。`/`、`/ask`、`/admin` HTTP 200。

### 首页旁注（未回退）

导语原文仍含：「鹊桥帮你连上对方的电脑：查进程、看日志、读文件、在对方确认后执行命令。Windows 和 Linux 都可下载使用」

Windows 旁注原文仍为：「若浏览器提示通常不会下载，选择保留即可。」

Linux 旁注原文仍为：「解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。」

macOS「即将提供」。首页无端口 / 17891 / 放行入站 / 也可下载自包含包 / TLS / stdio / 证书 / 签名 / SmartScreen。

### 截图

本机 Windows Edge 打开公网 `http://121.196.45.81/#download`（地址栏可见 `121.196.45.81`），窗口标题以「鹊桥远程协助」开头（未拍到运营后台主界面 / Cursor IDE 前台）。**未覆盖** `r022-*`～`r037-*` 已有截图。

- `docs/screenshots/r038-home-download.png`：首页下载区 + 地址栏；可见 Windows 旁注「若浏览器提示通常不会下载，选择保留即可。」、Linux 旁注（微软 .NET 8 下载页 + 选运行时不要选 SDK）与 macOS「即将提供」；未展开下载浮层。
- `docs/screenshots/r038-win-download.png`：同一页 + Edge 下载浮层，可见 `RemoteAssist-Setup-0.1.0.exe`（约 72.1 MB）。
- `docs/screenshots/r038-linux-download.png`：同一页 + 浮层，可见 `queqiao-linux-x64.tar.gz`（约 7.6 MB；无 `-fd`）。

浮层里排在顶部两个鹊桥相关文件以下的其它本机下载记录已遮盖后再入库。

### 明确未做

- 未开下一轮；未重启 site / relay；未改 HTML；未改中继；未改 AI-SSH-Pro
- 未读/打印 `/etc/remote-assist-site.env`；未把帮助脚本加进仓库 `tools/`
