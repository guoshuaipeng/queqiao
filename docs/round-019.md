# 第 19 轮 — 官网安装包下载

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-服务端（`site/` 与上机；不改 Windows / Avalonia 客户端功能，不改中继 JSONL 行为）
> 状态：**产品已验收**
> 依据：R018 产品已验收（`b8c215c`）；PRODUCT.md 协助闭环第 1 步「把安装包下载地址发给远端用户」；`docs/site.md` 首页下载入口；Linux 包已随 R009–R018 出炉，官网仍写「即将提供」。

R018 已补上接受协助端可选 MCP 说明。亲友主路径还卡在官网下不到包。本轮让 Windows 与 Linux 能从官网真正下载；macOS 未上真机、未验收，继续标「即将提供」，不假装已可下。

## 本轮目标

1. **高频**：公网官网（http://121.196.45.81/）提供可点击的 Windows 与 Linux 安装包下载，替换「即将提供」。
2. **打磨**：下载区用短说明讲清默认小包要先装 .NET 8 运行时（对齐 `docs/site-copy.md`）；本轮没有自包含包就不要假装有。

## A. Windows / Linux 真实下载（高频）

验收：

- 首页（或下载区）Windows、Linux 不再只显示「即将提供」。点击后能下到真实文件（不是 404、空页、或只回到「即将提供」）。
- Windows：现成 Inno 安装包（`RemoteAssist-Setup-*.exe` 或同等产物），显示名对齐「鹊桥远程协助」。
- Linux：现成 `.tar.gz`（默认依赖 .NET 8 运行时的小包）。安装说明与 `docs/site-copy.md` 一致：解开 `.tar.gz` 后运行。
- macOS：仍标明「即将提供」，不挂未在真机验收的包，不写成 Mac 已可正式下载。
- 托管必须轻便：由现有 `remote-assist-site` 提供静态文件（例如 `/downloads/`），不新装 Node / Java / PHP / MySQL / WordPress / Docker，不占用中继 `17891`，内存上限等约束不变，中继优先级高于官网。
- 同步改 `docs/site.md` 下载一句：Windows 与 Linux 链到实际包；macOS 仍「即将提供」。
- 亲友可见页面不出现端口号、TLS、stdio、内部路径、中继 JSONL 细节。

截图或公网验收放到 `docs/screenshots/r019-*`（至少：首页/下载区 Windows 与 Linux 可下载；Mac 仍「即将提供」；.NET 8 运行时短说明可见；分别点击 Windows 与 Linux 能拿到真实文件）。

## B. 运行时说明（打磨）

- 默认小包旁边用短说明：需要先装好 .NET 8 桌面方向运行时；不要写端口，不要写「放行入站」。
- 自包含包若本轮没有实际上传，就不要在页面上提供下载，也不要写「也可下载自包含包」。
- 不要改接单页 / 后台已验收文案（`docs/site-copy.md` 第 1 节），除非只加一个不抢主路径的下载入口。
- 不要改客户端 R013–R018 已验收原文。

## 包从哪来

- 鹊桥-客户端只配合提供现成产物（本机已有的 Windows 安装包 + Linux `.tar.gz`），不改 `src/` 功能、不开平行功能轮。
- 鹊桥-服务端负责把文件放到站点可下载位置、改页面链接、上机。
- 不要为此搭建完整发布系统、CDN 或自动构建流水线。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia，或改 Avalonia / WinForms 客户端功能
- 重做 R001–R018 已验收路径
- 把未上真机的 macOS 包当成正式可下
- 为官网安装重型 Web 栈，或占用 `17891`
- 完整 CRM / 计费 / 组织工单
- 把官网账号和中继账号打通

## 完成时

- 更新本文交付说明与截图 / 公网记录
- 回告产品验收；未开下一轮

## 交付（2026-11-05 23:09 Asia/Shanghai）

实现已上公网，待产品验收。未开下一轮。

### 下载

- 首页 http://121.196.45.81/ 下载区：Windows、Linux 可点；macOS 仍标「即将提供」，不是下载链接。
- Windows：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe （75,503,088 字节，SHA256 `78175CDCBDF7B02540B6E8AEAA01E25DA7290399C7B08AFDBC9B7557ABF33AB8`）。按钮文案「下载鹊桥远程协助」。未上架 `RemoteAssist-Agent-Setup-0.1.0.exe`。
- Linux：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz （7,993,604 字节，SHA256 `34CC363BAD28192895FCED74929FDE0A8C858D6E6A489EA40201D6DE552C827D`）。源文件为 `queqiao-linux-x64-fd.tar.gz`，公网文件名去掉内部 `-fd`。旁注：「解开后运行。需要先装好 .NET 8 桌面方向运行时」。
- 页面未写「也可下载自包含包」，未出现端口号、放行入站、中继 JSONL 细节。Windows 侧未要求先装 .NET 8。
- 未上架未在真机验收的 macOS 包。Linux 包为客户端提供的现成产物（后续 Avalonia 界面打磨不在此包内），本轮原样上架。

### 托管

- 由现有 `remote-assist-site` 通过 `SITE_DOWNLOADS=/var/lib/remote-assist-site/downloads` 磁盘提供 `/downloads/`，未把安装包 `go:embed` 进二进制，未新装 Node/Java/PHP/MySQL/WordPress/Docker，未占用 `17891`。
- `MemoryMax` 仍为 64M；中继 `remote-assist-relay` 部署后仍监听 `*:17891`。

### 截图

- `docs/screenshots/r019-home-download.png`：首页下载区（Windows / Linux 可下，macOS 即将提供，Linux 旁有运行时说明）
- `docs/screenshots/r019-win-download.png`：浏览器已下到 `RemoteAssist-Setup-0.1.0.exe`（75,503,088 字节）
- `docs/screenshots/r019-linux-download.png`：浏览器已下到 `queqiao-linux-x64.tar.gz`（7,993,604 字节）

## 产品验收（2026-11-05 Asia/Shanghai）

产品对照公网首页与下载链接，并结合 `docs/screenshots/r019-*` 验收通过（实现 `04347f3`）：

- 公网 http://121.196.45.81/ 首页下载区：Windows「下载鹊桥远程协助」链到 `/downloads/RemoteAssist-Setup-0.1.0.exe`；Linux「下载 Linux 包」链到 `/downloads/queqiao-linux-x64.tar.gz`；macOS 仍「即将提供」。
- Windows：HTTP 200，Content-Length 75,503,088，文件名为 `RemoteAssist-Setup-0.1.0.exe`，文件头为 MZ。
- Linux：HTTP 200，Content-Length 7,993,604，SHA256 `34CC363BAD28192895FCED74929FDE0A8C858D6E6A489EA40201D6DE552C827D`（产品独立下载核对），为 gzip；旁注「解开后运行。需要先装好 .NET 8 桌面方向运行时」。
- 页面无「也可下载自包含包」，无端口号、放行入站、TLS、stdio、`17891`。`/ask` 与 `/admin` 仍为 200。

缺口（不阻断）：

- `r019-win-download.png` / `r019-linux-download.png` 显示来源是 `http://127.0.0.1:8765`，不是公网地址栏；产品已用公网 URL 独立核对文件。
- Linux 包为 R014 前后的现成产物，不含 R015–R018 的 Avalonia 界面打磨，按本轮要求原样上架。
- 首页仍有「目前以 Windows 为主」，Linux 已可下载后略滞后。

已开 R020（官网 Linux 包跟上 R015–R018 已验收界面；首页不再用「以 Windows 为主」否定 Linux；客户端出包，服务端替换上架）。
