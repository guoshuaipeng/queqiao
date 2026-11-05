# 第 26 轮 — 官网安装包跟上 R025

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端（出包）+ 鹊桥-服务端（替换官网文件）
> 状态：**done（待产品验收）**
> 依据：R025 产品已验收（安装说明与官网旁注已含「通常不会下载」时怎么选）；公网 Windows / Linux 包仍是 R024 产物（75,583,762 / 8,000,095），亲友从官网下到的客户端「复制安装说明」还没有这一句。

R025 已让去协助端复制的安装说明，以及官网 Windows 旁注，告诉对方浏览器提示通常不会下载时选保留。但公网仍挂着 R024 的包：对方按说明去官网下载后，自己再当协助方时，复制出来的说明又变回没有这一句的旧文案。本轮把官网包换到含 R025 的客户端。

## 本轮目标

1. **高频**：官网 Windows 安装包与 Linux `.tar.gz` 都换成含 R025 已验收文案的当前客户端。
2. **打磨**：替换后首页文案不回退——Windows 旁注仍是「若浏览器提示通常不会下载，选择保留即可。」；Linux 旁注仍含微软 .NET 8 下载页和「选运行时、不要选 SDK」；导语仍是「Windows 和 Linux 都可下载使用」；macOS 仍「即将提供」。

## A. 出包（高频 · 鹊桥-客户端）

验收：

- 基于含 R025（`f423db1`）及之后已推送提交的代码，打出：
  - Windows `RemoteAssist-Setup-*.exe`（Inno，与现官网同形态）；
  - Linux framework-dependent `.tar.gz`（与现官网同形态，依赖 .NET 8 桌面方向运行时）。
- **不要**把未上真机的 macOS 包当成正式可下；本轮可不重打 osx，或打了也不上架。
- 不要做成自包含包来冒充本轮产物。
- 把本机路径、字节、SHA256、出包 HEAD 交给鹊桥-服务端。
- 不要改「复制安装说明」已验收原文、不要改「如何安装 MCP」三步、「复制 MCP 配置」及反馈；不要改 R013–R025 已验收原文；不要改 `docs/trust-levels.md` 的「才问你」。

## B. 上架（高频 · 鹊桥-服务端）+ 首页不回退（打磨）

验收：

- 替换
  - http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe（若文件名仍用 0.1.0 可以保持，但字节/哈希须与新包一致并可核对）；
  - http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz
- 新包大小或 SHA256 应与当前公网旧包不同（Windows 旧 75,583,762 / `236EDEE7B5C1CC28BA6E4804D17CE0FEC07AACA916CB201C3C16CAC2BC76FB58`；Linux 旧 8,000,095 / `305F9E40057828C0354ED0C0057F6611A380541A962A15AACC8CAD2C414FE586`），除非碰巧相同并说明原因。
- 公网文件名不要带内部 `-fd`；下载仍为真实文件，Content-Disposition 正常。
- 首页 Windows 旁注保持 R025 已验收文案：「若浏览器提示通常不会下载，选择保留即可。」Linux 旁注保持：「解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。」导语仍含「Windows 和 Linux 都可下载使用」；macOS 仍「即将提供」。不要出现端口号、TLS、stdio、内部路径、中继 JSONL、「放行入站」、「也可下载自包含包」、17891、证书、签名、SmartScreen。
- 托管约束不变：现有 `remote-assist-site`，不新装重型 Web 栈，不占用 `17891`，中继优先。
- 亲友可见页面不出现端口号、TLS、stdio、内部路径、中继 JSONL。

截图或公网记录放到 `docs/screenshots/r026-*`（至少：新的 Windows / Linux 均可从官网下载；如有条件，再拍一张首页旁注未回退）。**勿覆盖** `r022-*` / `r023-*` / `r024-*` / `r025-*` 已有截图。

## 包从哪来

- 鹊桥-客户端先出包，把本机路径、字节、SHA256、HEAD 交给鹊桥-服务端。
- 鹊桥-服务端负责替换上架与公网核对；不要搭完整发布系统 / CDN / 自动流水线。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 重做 R001–R025 已验收路径（本轮只换包 + 确认首页文案不回退）
- 把未上真机的 macOS 包写成已经可下
- 改接单页 / 后台已验收文案
- 购买代码签名证书 / 给安装包签名
- 给官网加域名或 HTTPS
- 现场公网双机不是本轮必须

## 完成时

- 更新本文交付说明与截图 / 公网记录
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 基于已推送 HEAD `73b2fea`（含 R025 `f423db1`）打出 Windows Inno 与 Linux framework-dependent 包；**未改**业务代码与已验收安装说明/MCP/trust-levels「才问你」。
- Linux 产物 DLL 已核对含 R025「如果浏览器提示通常不会下载，选择保留…选择仍要运行。」以及 R023 .NET 指引。
- **未改** `site/`、未上生产机；公网上架交鹊桥-服务端。未把 macOS 当正式可下；Linux 为 FD 非 SC。

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe` | `E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,580,750 | 8,000,125 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-06 01:35:02 Asia/Shanghai | 2026-11-06 01:34:36 Asia/Shanghai |
| SHA256 | `2A8EE24DA2A91157986686D4F22BF8D1AD960CFD4CEB60E031864401B6F59E26` | `14B0C7EF5050050469FBF7ECA522001F06529527F38C27ACE657BB0069832F76` |
| 构建命令 | `scripts\build-installer.ps1` | `scripts\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

出包 HEAD：`73b2fea70185fae2bce7e0162006e57855fc2c01` — `docs(R025): 产品验收通过并开 R026（官网安装包跟上 R025）`（祖先含 `f423db1`）。

对照 R024 旧包：Windows 75,583,762 / `236EDEE7…FB58` → 新 `2A8EE24D…9E26`；Linux 8,000,095 / `305F9E40…E586` → 新 `14B0C7EF…2F76` — **大小与 SHA256 均不同**。

公网替换与首页不回退核对：**交鹊桥-服务端**。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未改安装说明 / MCP / trust-levels「才问你」；未改业务功能
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物


## 交付说明（鹊桥-服务端 · 2026-11-06 Asia/Shanghai）

### 上架摘要

已替换公网 Windows / Linux 安装包（客户端交付 HEAD `73b2fea`），中继与站点进程均未重启；首页旁注与导语未回退。本轮未改 HTML。

| 项 | Windows | Linux |
|------|---------|-------|
| 公网 URL | http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe | http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz |
| 字节 | 75,580,750 | 8,000,125 |
| SHA256 | `2A8EE24DA2A91157986686D4F22BF8D1AD960CFD4CEB60E031864401B6F59E26` | `14B0C7EF5050050469FBF7ECA522001F06529527F38C27ACE657BB0069832F76` |
| Last-Modified | Thu, 05 Nov 2026 17:37:58 GMT（2026-11-06 01:37:58 Asia/Shanghai） | 同左 |
| Content-Disposition | 正常 | 正常（公网文件名无内部 `-fd`） |

对照 R024 旧公网：Windows 75,583,762 / `236EDEE7…FB58` → 新 `2A8EE24D…9E26`；Linux 8,000,095 / `305F9E40…E586` → 新 `14B0C7EF…2F76`——**字节与 SHA256 均不同**。

进程：`remote-assist-site` MainPID 仍 **21336**（`*:80`）；中继 MainPID 仍 **2114**（`*:17891`）。`/ask`、`/admin` HTTP 200。

### 首页旁注（未回退）

Windows 旁注原文仍为：「若浏览器提示通常不会下载，选择保留即可。」

Linux 旁注原文仍为：「解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。」

导语仍含「Windows 和 Linux 都可下载使用」；无「目前以 Windows 为主」；macOS「即将提供」。首页无端口 / 17891 / 放行入站 / 也可下载自包含包 / TLS / stdio / 证书 / 签名 / SmartScreen。

### 截图

本机 Windows Edge 打开公网 `http://121.196.45.81/#download`（地址栏可见 `121.196.45.81`），窗口标题以「鹊桥远程协助」开头（未拍到运营后台 / Cursor）。**未覆盖** `r022-*` / `r023-*` / `r024-*` / `r025-*` 已有截图。

- `docs/screenshots/r026-home-download.png`：首页下载区 + 地址栏；可见 Windows 旁注「若浏览器提示通常不会下载，选择保留即可。」、Linux 旁注（微软 .NET 8 下载页 + 选运行时不要选 SDK）与 macOS「即将提供」；未展开下载浮层。
- `docs/screenshots/r026-win-download.png`：同一页 + Edge 下载浮层，可见 `RemoteAssist-Setup-0.1.0.exe`。
- `docs/screenshots/r026-linux-download.png`：同一页 + 浮层，可见 `queqiao-linux-x64.tar.gz`（无 `-fd`）。

浮层里与本产品无关的本机其它下载记录已遮盖后再入库。

### 明确未做

- 未开下一轮；未重启 site / relay；未改 HTML；未改中继
- 未读/打印 `/etc/remote-assist-site.env`；未把帮助脚本加进仓库 `tools/`
