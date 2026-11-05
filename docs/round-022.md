# 第 22 轮 — 官网安装包跟上 R021 + 安装说明连网话术中性化

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（出包 + 安装说明小改）+ 鹊桥-服务端（替换官网文件）
> 状态：**产品已验收**
> 依据：R021 产品已验收（`632928a`）；官网 Windows / Linux 包仍是 R021 之前产物，亲友从官网下到的客户端「复制安装说明」还没有带下载地址。

R021 已让去协助端复制的安装说明带上官网下载地址。但公网仍挂着旧包：对方按说明去官网下载后，自己再当协助方时，复制出来的说明又变回旧文案。本轮把官网包换到含 R021 的客户端，并顺手把安装说明里偏 Windows 防火墙的「点允许连网」改成跨平台中性说法。

## 本轮目标

1. **高频**：官网 Windows 安装包与 Linux `.tar.gz` 都换成含 R021 已验收文案的当前客户端。
2. **打磨**：安装说明里「如果电脑问是否允许连网，点「允许」」改为跨平台中性措辞（不要写 Windows 防火墙路径，也不要暗示 Linux / macOS 也有同一套弹窗）。

## A. 官网安装包跟上（高频）

验收：

- 鹊桥-客户端：基于含 R021（`632928a`）及本轮打磨后的代码，打出：
  - Windows `RemoteAssist-Setup-*.exe`（Inno，与现官网同形态）；
  - Linux framework-dependent `.tar.gz`（与现官网同形态，依赖 .NET 8 桌面方向运行时）。
- **不要**把未上真机的 macOS 包当成正式可下；本轮可不重打 osx，或打了也不上架。
- 不要做成自包含包来冒充本轮产物。
- 鹊桥-服务端：替换
  - http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe（若文件名仍用 0.1.0 可以保持，但字节/哈希须与新包一致并可核对）；
  - http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz
- 新包大小或 SHA256 应与当前公网旧包不同（Windows 旧约 75,503,088；Linux 旧 7,999,640 / `EC1C7211…9DCE3650`），除非碰巧相同并说明原因。
- 公网文件名不要带内部 `-fd`；下载仍为真实文件，Content-Disposition 正常。Linux 旁注仍是「解开后运行。需要先装好 .NET 8 桌面方向运行时」。
- 托管约束不变：现有 `remote-assist-site`，不新装重型 Web 栈，不占用 `17891`，中继优先。
- 亲友可见页面不出现端口号、TLS、stdio、内部路径、中继 JSONL。

截图或公网记录放到 `docs/screenshots/r022-*`（至少：新的 Windows / Linux 均可从官网下载；如有条件，新包内去协助端复制安装说明仍含官网下载地址）。

## B. 安装说明连网话术（打磨）

- 更新共享常量 `WriteConfirmCopy.InstallInstructions`（及剪贴板同源文案）：把「如果电脑问是否允许连网，点「允许」」改成跨平台中性短句，推荐：「如果系统问起是否允许联网，选择允许。」
- 不要写「Windows 防火墙」「设置 → …」路径；不要针对 Linux / macOS 各写一套长说明。
- Windows WinForms 与 Linux Avalonia 继续共用同一常量。
- 不要改「如何安装 MCP」三步、不要改「复制 MCP 配置」及反馈；不要改 R013–R020 已验收原文；不要改 `docs/trust-levels.md` 的「才问你」。
- 「连接」仍是去协助端唯一实心绿主按钮；「复制安装说明」保持轻按钮；复制反馈仍用 R021 已验收句「说明里已带上官网下载地址，发给对方即可」。

## 包从哪来

- 鹊桥-客户端先改打磨文案并出包，把本机路径、字节、SHA256、HEAD 交给鹊桥-服务端。
- 鹊桥-服务端负责替换上架与公网核对；不要搭完整发布系统 / CDN / 自动流水线。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 重做 R001–R021 已验收路径（本轮只换包 + 一句连网话术）
- 把未上真机的 macOS 包写成已经可下
- 改接单页 / 后台已验收文案
- 现场公网双机不是本轮必须

## 完成时

- 更新本文交付说明与截图 / 公网记录
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- 打磨：共享常量 `WriteConfirmCopy.InstallInstructions` 末句由「如果电脑问是否允许连网，点「允许」」改为「如果系统问起是否允许联网，选择允许。」；Windows WinForms 与 Linux Avalonia **共用同一常量**。
- 保留 R021 已验收：官网入口与 Win/Linux 直链、macOS「尚未提供正式下载」、手头安装包可选句、反馈「说明里已带上官网下载地址，发给对方即可」、轻按钮「复制安装说明」、唯一实心绿「连接」。
- 高频出包：Windows Inno `RemoteAssist-Setup-0.1.0.exe` + Linux FD `queqiao-linux-x64-fd.tar.gz`（上架公网文件名去掉 `-fd` → `queqiao-linux-x64.tar.gz`）。
- **未改** MCP 三步 /「复制 MCP 配置」反馈、R013–R020 原文、`docs/trust-levels.md`「才问你」；未改 `site/`、未上生产机；未把 macOS 当正式产物；Linux 为 FD 非 SC。

### 最终逐字文案

**InstallCopiedHint**

> 说明里已带上官网下载地址，发给对方即可

**InstallInstructions / InstallInstructionsClipboard**

```
请打开官网下载「鹊桥远程协助」：http://121.196.45.81/
Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）
macOS 尚未提供正式下载。
如果你手头有安装包，也可以一起发出去。
装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。
如果系统问起是否允许联网，选择允许。
```

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe` | `E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,582,719 | 7,999,909 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-05 23:58:02 Asia/Shanghai | 2026-11-05 23:57:35 Asia/Shanghai |
| SHA256 | `6AC3CE69AD0FB40C245CF2071D7E8E3EA454E97637595F6D68F4B20484400CD1` | `B90E3B28F323174F3C1CB35D3CACDB017C5C47F368D10CFF8D5571DECD0AB1CE` |
| 构建命令 | `scripts\build-installer.ps1` | `scripts\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

对照旧包：Windows 旧 75,503,088 / `78175CDC…ABF33AB8`；Linux 旧 7,999,640 / `EC1C7211…9DCE3650` — **大小与 SHA256 均不同**。出包前源码/产物 DLL 已核对含新联网句、不含旧句。

出包 HEAD：`71893d223d5c6894864fa17f49e30dd03973e7fd`（含 R021 `632928a` + 本轮连网话术；包由该提交对应源码打出）。

### 截图（`docs/screenshots/r022-*`，已复制 `%USERPROFILE%\Downloads\r022-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r022-install-entry.png` | 「复制安装说明」轻按钮；「连接」实心绿；ui-shot | 8ED52115F70AF18402F98BABF64CF47B |
| `r022-install-copied.png` | 反馈「说明里已带上官网下载地址，发给对方即可」只一次；ui-shot | ED902C943BD0D4906580AA772E8E054A |
| `r022-clipboard-preview.png` | 预览含官网下载地址 + 新句「如果系统问起是否允许联网，选择允许。」；macOS 尚未提供正式下载；ui-shot | 1AB69573818494B172CE7E67518247DE |

三张 MD5 互异。坦白：`r022-install-entry.png` 与 r017/r021 入口图字节相同；`r022-install-copied.png` 与 r021 反馈图字节相同（入口与反馈句本轮未改视觉/文案）；**`r022-clipboard-preview.png` 为新字节**（含新联网句）。

公网替换与下载页核对：**交鹊桥-服务端**（客户端只交出包路径）。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物
- 未重打 `RemoteAssist-Agent-Setup-0.1.0.exe` 当官网主包
- 未改 MCP 三步 /「复制 MCP 配置」反馈；未改 R013–R020 原文；未改 trust-levels「才问你」



## 交付说明（鹊桥-服务端 · 2026-11-06 Asia/Shanghai）

### 上架摘要

已将客户端交出的 R022 包替换到公网 `remote-assist-site` 下载目录（`/var/lib/remote-assist-site/downloads`），未改 `site/` HTML，未重启 `remote-assist-site` / `remote-assist-relay`，未上架 macOS，未改接单页与后台已验收文案，未占用 17891，未把安装包嵌进站点进程。

### 公网文件

| 项 | Windows | Linux |
|------|---------|-------|
| URL | http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe | http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz |
| Content-Disposition 文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**无**内部 `-fd`） |
| Content-Length / 字节 | 75,582,719 | 7,999,909 |
| SHA256（公网 GET 正文） | `6AC3CE69AD0FB40C245CF2071D7E8E3EA454E97637595F6D68F4B20484400CD1` | `B90E3B28F323174F3C1CB35D3CACDB017C5C47F368D10CFF8D5571DECD0AB1CE` |
| Content-Type | application/octet-stream | application/gzip |
| Last-Modified | 2026-11-05 16:06:49 GMT → **2026-11-06 00:06:49 Asia/Shanghai** | 同左 |
| 文件头 | MZ (`4d5a`) | gzip (`1f8b08`) |

与客户端交付表一致；相对旧包（Windows 75,503,088 / `78175CDC…`；Linux 7,999,640 / `EC1C7211…`）字节与 SHA256 均不同。服务器侧 `sha256sum` 与公网 GET 一致。

### 首页与路由抽查

- 首页导语仍含「Windows 和 Linux 都可下载使用」，**未**出现「目前以 Windows 为主」
- Linux 旁注仍是「解开后运行。需要先装好 .NET 8 桌面方向运行时」
- macOS 仍「即将提供」
- 亲友可见首页未写端口号、「放行入站」、「也可下载自包含包」、TLS、stdio、17891
- `/ask`、`/admin` 均为 HTTP 200
- `remote-assist-site`、`remote-assist-relay` 均为 active；relay 仍在听 `*:17891`（仅检查，未重启）

### 截图

下载页公网截图（如 `r022-home-download.png` / `r022-win-download.png` / `r022-linux-download.png`）由服务端接着补拍后写入 `docs/screenshots/`；**勿覆盖**客户端已有的 `r022-install-entry.png`、`r022-install-copied.png`、`r022-clipboard-preview.png`。

### 明确未做

- 未 git commit / 推送（截图补齐后再由父代理一并提交）
- 未开下一轮；未改 `site/` HTML；未上架 macOS
- 未重启中继或站点；未读/打印 `/etc/remote-assist-site.env`


## 产品验收（鹊桥-产品 · 2026-11-06 00:20 Asia/Shanghai）

对照本轮交付说明、`docs/screenshots/r022-*`、公网 HEAD/GET 验收 **通过**。

### 核对

- 公网 Windows：`Content-Length` 75,582,719；`Content-Disposition` 文件名 `RemoteAssist-Setup-0.1.0.exe`；与客户端出包字节一致（本机产物 SHA256 `6AC3CE69AD0FB40C245CF2071D7E8E3EA454E97637595F6D68F4B20484400CD1`）。相对 R021 旧包 75,503,088 已变化。
- 公网 Linux：GET 正文 7,999,909 字节，SHA256 `B90E3B28F323174F3C1CB35D3CACDB017C5C47F368D10CFF8D5571DECD0AB1CE`（产品经公网下载核对）；文件名无内部 `-fd`；文件头 gzip；相对旧包 `EC1C7211…9DCE3650` 已变化。
- 首页导语「Windows 和 Linux 都可下载使用」；无「目前以 Windows 为主」；Linux 旁注仍提 .NET 8 桌面方向运行时；macOS「即将提供」；亲友首页未见端口 / 17891 / 放行入站 / stdio / TLS。
- `/` `/ask` `/admin` 均为 HTTP 200。
- 客户端截图：`r022-clipboard-preview.png` 含官网入口、Win/Linux 直链、macOS 尚未提供正式下载、新句「如果系统问起是否允许联网，选择允许。」；`r022-install-entry.png` / `r022-install-copied.png` 仍为轻按钮「复制安装说明」+ 唯一实心绿「连接」+ 反馈「说明里已带上官网下载地址，发给对方即可」。

### 缺口（不阻断）

- 服务端尚未补公网地址栏下载区截图（`r022-home-download.png` 等）；交 R023 打磨一并补上，**勿覆盖**客户端已有三张。
- 入口/反馈两图与 R017/R021 字节相同（本轮未改其视觉）；ui-shot，非现场公网双机。
- 本轮未把 macOS 写成可下；未改接单页/后台已验收文案。

出包 HEAD：`71893d2`。已开 R023。
