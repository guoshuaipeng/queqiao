# 第 33 轮 — 官网安装包跟上含 R031–R032 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端（出包）/ 鹊桥-服务端（替换上架）
> 状态：**ready**
> 依据：R032 产品已验收。R031 接受协助端「对方已连上」主状态与 R032 托盘说明去重已进仓库（HEAD 含 `b78fc1e`）；公网安装包仍是 R030（Windows 75,582,164 / Linux 8,001,712），从官网下载的人还拿不到这两轮改动。

## 本轮目标

1. **高频**：用包含 R031–R032 的客户端重新打 Windows / Linux 包，替换官网上的安装包。
2. **打磨**：替换后首页 Windows / Linux 旁注与导语不回退。

## A. 打出含 R031–R032 的新包（高频 · 鹊桥-客户端）

从包含 `b78fc1e`（R032）的 HEAD 打出：

- Windows：`RemoteAssist-Setup-0.1.0.exe`（`scripts/build-installer.ps1`）
- Linux：内部可保留 `queqiao-linux-x64-fd.tar.gz`，公网文件名必须是 `queqiao-linux-x64.tar.gz`（去掉 `-fd`）；`scripts/publish-agent-avalonia.ps1 -Runtimes linux-x64`

交付时写明：字节数、SHA256、本地路径、mtime（Asia/Shanghai）、出包所用 HEAD。新包字节或哈希应与当前公网 R030 包不同（Windows 现为 75,582,164；Linux 现为 8,001,712）。

不要改 `InstallInstructions`、不要改 Skill 文案常量、不要改 R031/R032 已验收文案、不要改 `docs/trust-levels.md` 的「才问你」。

## B. 替换公网上架（高频 · 鹊桥-服务端）

等客户端交付新包后，替换：

- http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
- http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz

打磨：首页 Windows 旁注仍是「若浏览器提示通常不会下载，选择保留即可。」Linux 旁注仍指向微软 .NET 8 下载页并写「选运行时、不要选 SDK」。导语仍是「Windows 和 Linux 都可下载使用」；macOS 仍「即将提供」。不要改 HTML 其它部分，不要把未上真机的 macOS 写成已经可下，不要占用 17891，能不重启中继就不要重启。

截图放到 `docs/screenshots/r033-*`（至少首页下载区、Windows 下载、Linux 下载）。**勿覆盖** `r022-*`～`r032-*`。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 改安装说明 / Skill 文案 / 信任等级「才问你」
- 改 R031 主状态或 R032 托盘文案
- 把 macOS 写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮
