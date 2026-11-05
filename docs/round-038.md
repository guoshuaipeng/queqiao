# 第 38 轮 — 官网安装包跟上含 R035–R037 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端出包；鹊桥-服务端替换上架
> 状态：**ready**
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
