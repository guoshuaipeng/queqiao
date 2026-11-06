# 第 42 轮 — 官网安装包跟上含 R041 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端出包；鹊桥-服务端替换上架
> 状态：**done（待产品验收）**
> 依据：R041 产品已验收。官网当前仍是 R040 的包（含 R039）。R041「提议一项改动」四态人话与连上后去掉 PAIR_OK 还没进正式下载。用户明确以稳定性和用户体验为重点，这批人话应尽快上架。

## 本轮目标

1. **高频（客户端）**：从包含 `aeafeeb`（R041）的 HEAD 打出 Windows 安装包和 Linux 包，把路径、体积、SHA256 交给服务端。
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
- 把 macOS 写成已经可下；买证书或上 HTTPS
- 改首页其他文案

## 完成时

- 客户端：交付产物路径、体积、SHA256、出包所用 HEAD；把包交给服务端。不要自己上公网。
- 服务端：替换后回告；截图放到 `docs/screenshots/r042-home-download.png`、`r042-win-download.png`、`r042-linux-download.png`（须能看到地址栏是 121.196.45.81）。可用浏览器实拍。**勿覆盖** r022-*～r041-*。
- 产品对照公网独立下载并核对哈希后再验收；未开下一轮


## 交付说明（客户端出包 · 2026-11-06 Asia/Shanghai）

### 出包 HEAD
- `8d61398d6393015e96bb721c4be344e072b0ddb9` — `docs(R041): 产品验收通过并开 R042（官网包跟上含 R041）`
- 祖先确认：`git merge-base --is-ancestor aeafeeb HEAD` / `5c276ec` 均为真（含 R041 功能及 R039）。

### Windows
| 项 | 值 |
|----|-----|
| 路径 | `E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe` |
| 字节 | 75,587,213 |
| SHA256 | `63360A839B346F41B09778D7F4A0C0898337F4EE994BCC033A3FAE701EC53783` |
| mtime | 2026-11-06 08:19:43 Asia/Shanghai |
| 命令 | `scripts\build-installer.ps1` |

### Linux（framework-dependent，非 SC）
| 项 | 值 |
|----|-----|
| 本地路径 | `E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `queqiao-linux-x64.tar.gz`（去掉 `-fd`） |
| 字节 | 8,005,849 |
| SHA256 | `2E16A11284E7E021C3F30DC9C6C9B806F2B7254034F2B0F5DACB79BEF45485E1` |
| mtime | 2026-11-06 08:19:46 Asia/Shanghai |
| 命令 | `scripts\publish-agent-avalonia.ps1 -Runtimes linux-x64` |

包内：`RemoteAssist.Agent.Avalonia` + `运行说明.txt`；`RemoteAssist.Agent.Avalonia.runtimeconfig.json` → `Microsoft.NETCore.App` 8.0.0；无 hostfxr/coreclr 等 SC 痕迹。

### 对照 R040（上一正式公网包）
| | R040 | 本轮 R042 |
|--|------|-----------|
| Windows 字节 | 75,585,615 | **75,587,213** |
| Windows SHA256 | `C403FA6D…2104` | `63360A83…3783` |
| Linux 字节 | 8,005,149 | **8,005,849** |
| Linux SHA256 | `A2F1E187…2467` | `2E16A112…85E1` |

字节与 SHA256 **均不同**（代码已含 R041）。

### 未做 / 确认
- 未改业务代码 / UI / 文案；未改 InstallInstructions / Skill / EndAssistCopy / R031–R041 已验收文案 / trust-levels「才问你」
- 未打 Linux SC；未把 macOS 当正式可下；未改 site/ / relay/；未上生产机；未自己上公网；未开下一轮
- 包已就绪，交给服务端替换公网两个下载文件


## 交付说明（鹊桥-服务端 · 2026-11-06 Asia/Shanghai）

### 上架摘要

已替换公网 Windows / Linux 安装包（客户端交付出包 HEAD `8d61398`，祖先含 R041 `aeafeeb`、R039 `5c276ec`；客户端文档提交 `fd14a24`），中继与站点进程均未重启；首页旁注与导语未回退。本轮未改 HTML。

| 项 | Windows | Linux |
|------|---------|-------|
| 公网 URL | http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe | http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz |
| 字节 | 75,587,213 | 8,005,849 |
| SHA256 | `63360A839B346F41B09778D7F4A0C0898337F4EE994BCC033A3FAE701EC53783` | `2E16A11284E7E021C3F30DC9C6C9B806F2B7254034F2B0F5DACB79BEF45485E1` |
| Last-Modified | Fri, 06 Nov 2026 00:22:33 GMT（2026-11-06 08:22:33 Asia/Shanghai） | 同左 |
| Content-Type | application/octet-stream | application/gzip |
| Content-Disposition | `filename="RemoteAssist-Setup-0.1.0.exe"` | `filename="queqiao-linux-x64.tar.gz"`（无内部 `-fd`） |

对照公网 R040：Windows 75,585,615 / `C403FA6D…2104` → 新 75,587,213 / `63360A83…3783`；Linux 8,005,149 / `A2F1E187…2467` → 新 8,005,849 / `2E16A112…85E1`——**字节与 SHA256 均不同**。本机独立复核：Windows MZ `4D 5A`，Linux gzip `1F 8B`；scp 后 `/tmp` 与 `install` 后 `/var/lib/remote-assist-site/downloads/` 的 sha256sum 均与上表一致；属主 `rasite:rasite`，模式 `644`。

进程：替换前/后 `remote-assist-site` MainPID 仍 **21336**（ActiveEnterTimestamp 2026-11-06 01:28:31 Asia/Shanghai，`*:80`）；中继 MainPID 仍 **2114**（ActiveEnterTimestamp 2026-11-05 16:50:25 Asia/Shanghai，`*:17891`）。`/`、`/ask`、`/admin` HTTP 200。

### 首页旁注（未回退）

导语原文仍含：「鹊桥帮你连上对方的电脑：查进程、看日志、读文件、在对方确认后执行命令。Windows 和 Linux 都可下载使用」

Windows 旁注原文仍为：「若浏览器提示通常不会下载，选择保留即可。」

Linux 旁注原文仍为：「解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。」

macOS「即将提供」。首页无端口 / 17891 / 放行入站 / 也可下载自包含包 / TLS / stdio / 证书 / 签名 / SmartScreen。

### 截图

本机 Windows Edge 打开公网 `http://121.196.45.81/#download`（地址栏可见 `121.196.45.81`），窗口标题以「鹊桥远程协助」开头（未拍到运营后台主界面 / Cursor IDE 前台）。**未覆盖** `r022-*`～`r041-*` 已有截图。

- `docs/screenshots/r042-home-download.png`：首页下载区 + 地址栏；可见 Windows 旁注「若浏览器提示通常不会下载，选择保留即可。」、Linux 旁注（微软 .NET 8 下载页 + 选运行时不要选 SDK）与 macOS「即将提供」；未展开下载浮层。
- `docs/screenshots/r042-win-download.png`：同一页 + Edge 下载浮层，可见 `RemoteAssist-Setup-0.1.0.exe`（约 72.1 MB）。
- `docs/screenshots/r042-linux-download.png`：同一页 + 浮层，可见 `queqiao-linux-x64.tar.gz`（约 7.6 MB；无 `-fd`）。

浮层里排在顶部两个鹊桥相关文件以下的其它本机下载记录已遮盖后再入库。

### 明确未做

- 未开下一轮；未重启 site / relay；未改 HTML；未改中继；未改 AI-SSH-Pro
- 未读/打印 `/etc/remote-assist-site.env`
