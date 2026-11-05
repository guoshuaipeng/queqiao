# 第 30 轮 — 官网安装包跟上含 R027–R029 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端（出包）/ 鹊桥-服务端（替换上架）
> 状态：**done（待产品验收）**
> 依据：R029 产品已验收（Windows 接受协助端菜单已能复制 Skill 说明）。R027–R029 已把「复制 Skill 说明」落到去协助端、Avalonia 接受协助端和 Windows 接受协助端；公网安装包仍是 R026，从官网下载的人还拿不到这些入口。

## 本轮目标

1. **高频**：用包含 R027–R029 的客户端重新打 Windows / Linux 包，替换官网上的安装包。
2. **打磨**：替换后首页 Windows / Linux 旁注与导语不回退。

## A. 打出含 Skill 入口的新包（高频 · 鹊桥-客户端）

从包含 `5909d99`（R029）的 HEAD 打出：

- Windows：`RemoteAssist-Setup-0.1.0.exe`（`scripts/build-installer.ps1`）
- Linux：内部可保留 `queqiao-linux-x64-fd.tar.gz`，公网文件名必须是 `queqiao-linux-x64.tar.gz`（去掉 `-fd`）；`scripts/publish-agent-avalonia.ps1 -Runtimes linux-x64`

交付时写明：字节数、SHA256、本地路径、mtime（Asia/Shanghai）、出包所用 HEAD。新包字节或哈希应与当前公网 R026 包不同（Windows 现为 75,580,750 / `2A8EE24D…B6F59E26`；Linux 现为 8,000,125 / `14B0C7EF…69832F76`）。

不要改 `InstallInstructions`、不要改 Skill 文案常量、不要改 `docs/trust-levels.md` 的「才问你」。

## B. 替换公网上架（高频 · 鹊桥-服务端）

等客户端交付新包后，替换：

- http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
- http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz

打磨：首页 Windows 旁注仍是「若浏览器提示通常不会下载，选择保留即可。」Linux 旁注仍指向微软 .NET 8 下载页并写「选运行时、不要选 SDK」。导语仍是「Windows 和 Linux 都可下载使用」；macOS 仍「即将提供」。不要改 HTML 其它部分，不要把未上真机的 macOS 写成已经可下，不要占用 17891，能不重启中继就不要重启。

截图放到 `docs/screenshots/r030-*`（至少首页下载区、Windows 下载、Linux 下载）。**勿覆盖** `r022-*`～`r029-*`。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 改安装说明 / Skill 文案 / 信任等级「才问你」
- 把 macOS 写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 基于已推送 HEAD `c7dc792`（含 R029 `5909d99`，覆盖 R027–R029 Skill 入口）打出 Windows Inno 与 Linux framework-dependent 包；**未改**业务代码与已验收安装说明 / Skill 常量 / trust-levels「才问你」。
- Linux 产物 DLL 已核对含 Skill 说明正文。
- **未改** `site/`、未上生产机；公网上架交鹊桥-服务端。未把 macOS 当正式可下；Linux 为 FD 非 SC。

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe` | `E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,582,164 | 8,001,712 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-06 02:09:18 Asia/Shanghai | 2026-11-06 02:08:54 Asia/Shanghai |
| SHA256 | `EEA41DA2474DED60C3DA5C72F718ADEFC2C56C5B0AACF53757E719F530E9C1E5` | `D659BF24602C3ACEF6F896071FA3A9B17EC7791D23B10C393E7B3A8CFB69080D` |
| 构建命令 | `scripts\build-installer.ps1` | `scripts\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

出包 HEAD：`c7dc792f8a8676759e85535512c70ae4fb36455e` — `docs(R029): 产品验收通过并开 R030（官网安装包跟上含 Skill 的客户端）`（祖先含 `5909d99`）。

对照 R026 旧包：Windows 75,580,750 / `2A8EE24D…9E26` → 新 `EEA41DA2…C1E5`；Linux 8,000,125 / `14B0C7EF…2F76` → 新 `D659BF24…080D` — **大小与 SHA256 均不同**。

公网替换与首页不回退核对：**交鹊桥-服务端**。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未改安装说明 / Skill 常量 / trust-levels「才问你」；未改业务功能
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物


## 交付说明（鹊桥-服务端 · 2026-11-06 Asia/Shanghai）

### 上架摘要

已替换公网 Windows / Linux 安装包（客户端交付 HEAD `c7dc792`，含祖先 R029 `5909d99`，覆盖 R027–R029 Skill 入口），中继与站点进程均未重启；首页旁注与导语未回退。本轮未改 HTML。

| 项 | Windows | Linux |
|------|---------|-------|
| 公网 URL | http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe | http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz |
| 字节 | 75,582,164 | 8,001,712 |
| SHA256 | `EEA41DA2474DED60C3DA5C72F718ADEFC2C56C5B0AACF53757E719F530E9C1E5` | `D659BF24602C3ACEF6F896071FA3A9B17EC7791D23B10C393E7B3A8CFB69080D` |
| Last-Modified | Thu, 05 Nov 2026 18:13:19 GMT（2026-11-06 02:13:19 Asia/Shanghai） | 同左 |
| Content-Disposition | 正常 | 正常（公网文件名无内部 `-fd`） |

对照 R026 旧公网：Windows 75,580,750 / `2A8EE24D…9E26` → 新 `EEA41DA2…C1E5`；Linux 8,000,125 / `14B0C7EF…2F76` → 新 `D659BF24…080D`——**字节与 SHA256 均不同**。

进程：`remote-assist-site` MainPID 仍 **21336**（`*:80`）；中继 MainPID 仍 **2114**（`*:17891`）。`/ask`、`/admin` HTTP 200。

客户端文档提交：`15e57ae3ba46bf0d2debf9fe020a32c83d8448d9`。

### 首页旁注（未回退）

Windows 旁注原文仍为：「若浏览器提示通常不会下载，选择保留即可。」

Linux 旁注原文仍为：「解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。」

导语仍含「Windows 和 Linux 都可下载使用」；无「目前以 Windows 为主」；macOS「即将提供」。首页无端口 / 17891 / 放行入站 / 也可下载自包含包 / TLS / stdio / 证书 / 签名 / SmartScreen。

### 截图

本机 Windows Edge 打开公网 `http://121.196.45.81/#download`（地址栏可见 `121.196.45.81`），窗口标题以「鹊桥远程协助」开头（未拍到运营后台主界面 / Cursor IDE 前台）。**未覆盖** `r022-*` / `r023-*` / `r024-*` / `r025-*` / `r026-*` / `r027-*` / `r028-*` / `r029-*` 已有截图。

- `docs/screenshots/r030-home-download.png`：首页下载区 + 地址栏；可见 Windows 旁注「若浏览器提示通常不会下载，选择保留即可。」、Linux 旁注（微软 .NET 8 下载页 + 选运行时不要选 SDK）与 macOS「即将提供」；未展开下载浮层。
- `docs/screenshots/r030-win-download.png`：同一页 + Edge 下载浮层，可见 `RemoteAssist-Setup-0.1.0.exe`。
- `docs/screenshots/r030-linux-download.png`：同一页 + 浮层，可见 `queqiao-linux-x64.tar.gz`（无 `-fd`）。

浮层里排在顶部两个鹊桥相关文件以下的其它本机下载记录已遮盖后再入库。

### 明确未做

- 未开下一轮；未重启 site / relay；未改 HTML；未改中继
- 未读/打印 `/etc/remote-assist-site.env`；未把帮助脚本加进仓库 `tools/`
