# 第 34 轮 — 官网安装包跟上含 R031–R033 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端（出包）/ 鹊桥-服务端（替换上架）
> 状态：**done（待产品验收）**
> 依据：R033 产品已验收。R031「对方已连上」、R032 托盘说明去重、R033 Windows「结束这次协助」已进仓库（HEAD 含 3b01e7f）；公网安装包仍是 R030（Windows 75,582,164 / Linux 8,001,712），从官网下载的人还拿不到这三轮改动。

## 本轮目标

1. **高频**：用包含 R031–R033 的客户端重新打 Windows / Linux 包，替换官网上的安装包。
2. **打磨**：替换后首页 Windows / Linux 旁注与导语不回退。

## A. 打出含 R031–R033 的新包（高频 · 鹊桥-客户端）

从包含 3b01e7f（R033）的 HEAD 打出：

- Windows：RemoteAssist-Setup-0.1.0.exe（scripts/build-installer.ps1）
- Linux：内部可保留 queqiao-linux-x64-fd.tar.gz，公网文件名必须是 queqiao-linux-x64.tar.gz（去掉 -fd）；scripts/publish-agent-avalonia.ps1 -Runtimes linux-x64

交付时写明：字节数、SHA256、本地路径、mtime（Asia/Shanghai）、出包所用 HEAD。新包字节或哈希应与当前公网 R030 包不同（Windows 现为 75,582,164；Linux 现为 8,001,712）。

不要改 InstallInstructions、不要改 Skill 文案常量、不要改 R031–R033 已验收文案、不要改 docs/trust-levels.md 的「才问你」。

## B. 替换公网上架（高频 · 鹊桥-服务端）

等客户端交付新包后，替换：

- http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
- http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz

打磨：首页 Windows 旁注仍是「若浏览器提示通常不会下载，选择保留即可。」Linux 旁注仍指向微软 .NET 8 下载页并写「选运行时、不要选 SDK」。导语仍是「Windows 和 Linux 都可下载使用」；macOS 仍「即将提供」。不要改 HTML 其它部分，不要把未上真机的 macOS 写成已经可下，不要占用 17891，能不重启中继就不要重启。

截图放到 docs/screenshots/r034-*（至少首页下载区、Windows 下载、Linux 下载）。**勿覆盖** r022-*～r033-*。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 改安装说明 / Skill 文案 / 信任等级「才问你」
- 改 R031 主状态、R032 托盘文案、R033 结束协助文案
- 把 macOS 写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮


## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 基于已推送 HEAD `7bf7924`（祖先含 R033 `3b01e7f`，因而也含 R031 `e40e70a` / R032 `b78fc1e`）打出 Windows Inno 与 Linux framework-dependent 包；**未改**业务代码与已验收 InstallInstructions / Skill 常量 / R031–R033 文案 / trust-levels「才问你」。
- Linux 包内已核对：`RemoteAssist.Agent.Avalonia` + `运行说明.txt`；`runtimeconfig.json` 声明 `Microsoft.NETCore.App` 8.0（**FD 非 SC**）；无 `libhostfxr`/coreclr 自包含痕迹。
- **未改** `site/`、`relay/`、未上生产机；公网上架交鹊桥-服务端。未把 macOS 当正式可下。

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe` | `E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,584,964 | 8,002,690 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-06 02:50:51 Asia/Shanghai | 2026-11-06 02:51:01 Asia/Shanghai |
| SHA256 | `64A3231617963B4D6100036E34FA8A74DAB6FFC6978AE6E5142FE82A8B5EBCD0` | `B20257AD6E7A6CFB4E5048BBD2099A8D0B5906DA96802C5C7BFFC60B8E789108` |
| 构建命令 | `scripts\build-installer.ps1` | `scripts\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

出包 HEAD：`7bf7924d012da7c68dba434f23a8e8f785a1eb3a` — `docs(R033): 补回验收文档里被吞掉的截图文件名`（`git merge-base --is-ancestor 3b01e7f HEAD` 为真）。

对照公网 R030：Windows 75,582,164 / `EEA41DA2…C1E5` → 新 75,584,964 / `64A32316…BCD0`；Linux 8,001,712 / `D659BF24…080D` → 新 8,002,690 / `B20257AD…9108` — **字节与 SHA256 均不同**。

公网替换与首页不回退核对：**交鹊桥-服务端（B 节）**。未开下一轮。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未改安装说明 / Skill 常量 / R031–R033 已验收文案 / trust-levels「才问你」；未改业务功能代码
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物；未联络服务端/产品


## 交付说明（鹊桥-服务端 · 2026-11-06 Asia/Shanghai）

### 上架摘要

已替换公网 Windows / Linux 安装包（客户端交付 HEAD `7bf7924`，祖先含 R033 `3b01e7f`、R031 `e40e70a`、R032 `b78fc1e`），中继与站点进程均未重启；首页旁注与导语未回退。本轮未改 HTML。

| 项 | Windows | Linux |
|------|---------|-------|
| 公网 URL | http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe | http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz |
| 字节 | 75,584,964 | 8,002,690 |
| SHA256 | `64A3231617963B4D6100036E34FA8A74DAB6FFC6978AE6E5142FE82A8B5EBCD0` | `B20257AD6E7A6CFB4E5048BBD2099A8D0B5906DA96802C5C7BFFC60B8E789108` |
| Last-Modified | Thu, 05 Nov 2026 18:55:23 GMT（2026-11-06 02:55:23 Asia/Shanghai） | 同左 |
| Content-Disposition | 正常 | 正常（公网文件名无内部 `-fd`） |

对照公网 R030：Windows 75,582,164 / `EEA41DA2…C1E5` → 新 `64A32316…BCD0`；Linux 8,001,712 / `D659BF24…080D` → 新 `B20257AD…9108`——**字节与 SHA256 均不同**。

进程：`remote-assist-site` MainPID 仍 **21336**（`*:80`）；中继 MainPID 仍 **2114**（`*:17891`）。`/ask`、`/admin` HTTP 200。

客户端文档提交：`763d9b3eb3223db13a11db1d500a96e82c87b43e`。

### 首页旁注（未回退）

Windows 旁注原文仍为：「若浏览器提示通常不会下载，选择保留即可。」

Linux 旁注原文仍为：「解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。」

导语仍含「Windows 和 Linux 都可下载使用」；无「目前以 Windows 为主」；macOS「即将提供」。首页无端口 / 17891 / 放行入站 / 也可下载自包含包 / TLS / stdio / 证书 / 签名 / SmartScreen。

### 截图

本机 Windows Edge 打开公网 `http://121.196.45.81/#download`（地址栏可见 `121.196.45.81`），窗口标题以「鹊桥远程协助」开头（未拍到运营后台主界面 / Cursor IDE 前台）。**未覆盖** `r022-*`～`r033-*` 已有截图。

- `docs/screenshots/r034-home-download.png`：首页下载区 + 地址栏；可见 Windows 旁注「若浏览器提示通常不会下载，选择保留即可。」、Linux 旁注（微软 .NET 8 下载页 + 选运行时不要选 SDK）与 macOS「即将提供」；未展开下载浮层。
- `docs/screenshots/r034-win-download.png`：同一页 + Edge 下载浮层，可见 `RemoteAssist-Setup-0.1.0.exe`。
- `docs/screenshots/r034-linux-download.png`：同一页 + 浮层，可见 `queqiao-linux-x64.tar.gz`（无 `-fd`）。

浮层里排在顶部两个鹊桥相关文件以下的其它本机下载记录已遮盖后再入库。

### 明确未做

- 未开下一轮；未重启 site / relay；未改 HTML；未改中继
- 未读/打印 `/etc/remote-assist-site.env`；未把帮助脚本加进仓库 `tools/`
