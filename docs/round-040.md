# 第 40 轮 — 官网安装包跟上含 R039 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端出包；鹊桥-服务端替换上架
> 状态：**wip**
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
