# 第 23 轮 — 安装说明补 .NET 运行时短指引 + 官网旁注对齐

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端（安装说明文案）+ 鹊桥-服务端（官网 Linux 旁注 + 补拍 R022 公网下载截图）
> 状态：**产品已验收**
> 依据：R022 产品已验收（公网包已换成含 R021 + 中性联网句的客户端）；亲友从官网下到的是依赖 .NET 8 的小包，安装说明只说「需要先装好」却没告诉怎么装，是当前最高频断点。

## 本轮目标

1. **高频**：去协助端「复制安装说明」补上缺 .NET 8 运行时时的可执行短指引（微软下载页 + 只要运行时不要 SDK），Windows WinForms 与 Linux Avalonia 共用同一常量。
2. **打磨**：官网首页 Linux 下载旁注与上述指引同意思；并补拍 R022 遗留的公网地址栏下载区截图。

## A. 安装说明补运行时短指引（高频 · 鹊桥-客户端）

验收：

- 更新共享常量 `WriteConfirmCopy.InstallInstructions`（及剪贴板同源文案），在现有官网下载地址与「如果系统问起是否允许联网，选择允许。」之间或紧后，增加 **2～4 行**短指引，意思对齐 `docs/first-run-mac-linux.md`：
  - 默认小包需要先装「.NET 8 运行时」（用来跑程序的，不是开发工具）；
  - 打开 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 ，在「运行时」栏按系统下载，**不要选 SDK**；
  - Windows / Linux 各用一句带过即可，不要写长篇；不要写端口、防火墙路径、17891、TLS、stdio。
- 保留 R021/R022 已验收：官网入口与 Win/Linux 直链、macOS「尚未提供正式下载」、手头安装包可选句、反馈「说明里已带上官网下载地址，发给对方即可」、轻按钮「复制安装说明」、唯一实心绿「连接」、中性联网句。
- 不要改「如何安装 MCP」三步、「复制 MCP 配置」及反馈；不要改 R013–R020 原文；不要改 `docs/trust-levels.md`「才问你」。
- 本轮 **不必**重打/上架安装包也可以先交文案截图验收；若顺手出包，公网上架另开或并入本轮服务端小节并写清哈希。

截图放到 `docs/screenshots/r023-*`（至少：复制后的剪贴板/预览能看到运行时短指引 + 仍含官网下载地址；入口仍是轻按钮「复制安装说明」）。

推荐剪贴板骨架（可微调字数，勿改意思）：

```
请打开官网下载「鹊桥远程协助」：http://121.196.45.81/
Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）
macOS 尚未提供正式下载。
如果你手头有安装包，也可以一起发出去。
装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。
如果系统问起是否允许联网，选择允许。
若打开时提示缺少 .NET：请安装「.NET 8 运行时」（不是 SDK）→ https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0
```

## B. 官网旁注对齐 + R022 公网截图（打磨 · 鹊桥-服务端）

验收：

- 首页 Linux 下载旁注与安装说明同意思：点明要先装 .NET 8 桌面方向运行时，并可指向微软运行时下载页（或写清「打开微软 .NET 8 下载页，选运行时、不要选 SDK」）。不要把自包含包写成已提供；不要出现端口 / 17891 / 放行入站。
- macOS 仍「即将提供」。
- 补拍并写入 `docs/screenshots/`：`r022-home-download.png`、`r022-win-download.png`、`r022-linux-download.png`（公网地址栏可见 `121.196.45.81`）；**勿覆盖** `r022-install-*.png` / `r022-clipboard-preview.png`。
- 托管约束不变：现有 `remote-assist-site`，不新装重型 Web 栈，不占用 17891。

## 本轮不做

- 改中继 JSONL、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 把未上真机的 macOS 包写成已经可下
- 做缺运行时时的自动下载器 / 内嵌安装器（只要人话指引）
- 重做 R001–R022 已验收路径中与本轮无关的部分
- 改接单页 / 后台状态机

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 共享常量 `WriteConfirmCopy.InstallInstructions` 在 R022 已验收全文后补 **2 行** .NET 8 运行时短指引（对齐 `docs/first-run-mac-linux.md` 语气：运行时不是开发工具；打开微软 .NET 8 下载页，选「运行时」不要选 SDK）。
- Windows WinForms 与 Linux Avalonia **共用同一常量**；`InstallCopiedHint` 仍为「说明里已带上官网下载地址，发给对方即可」；反馈只出现一次。
- **原样保留** R022：官网入口与 Win/Linux 直链、macOS「尚未提供正式下载」、手头安装包可选句、中性联网句、轻按钮「复制安装说明」、唯一实心绿「连接」。
- **未改** MCP 三步 /「复制 MCP 配置」反馈、R013–R022 其余原文、`docs/trust-levels.md`「才问你」；未做自动下载器；未改 `site/`、未上生产机；本轮未重打/上架安装包。

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
若打开时提示缺少 .NET，先安装「.NET 8 运行时」（用来跑这个程序的，不是开发工具）。
打开 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 ，在「运行时」那一栏按系统下载，不要选 SDK。
```

### 截图（`docs/screenshots/r023-*`，已复制 `%USERPROFILE%\Downloads\r023-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r023-install-entry.png` | 「复制安装说明」轻按钮；「连接」实心绿；ui-shot | 8ED52115F70AF18402F98BABF64CF47B |
| `r023-install-copied.png` | 反馈「说明里已带上官网下载地址，发给对方即可」只一次；ui-shot | ED902C943BD0D4906580AA772E8E054A |
| `r023-clipboard-preview.png` | 预览含 .NET 8 运行时短指引（微软下载页 + 不要选 SDK）+ 官网/直链/macOS 尚未提供正式下载/联网中性句；ui-shot | ED7C7A4CAFC247730D13324BB0269B04 |

三张 MD5 互异。坦白：`r023-install-entry.png` 与 r017/r021/r022 入口图字节相同（入口控件未改视觉）；`r023-install-copied.png` 与 r021/r022 反馈图字节相同（反馈句未改）；`r023-clipboard-preview.png` 为新字节（含 .NET 8 运行时短指引）。

官网旁注对齐与 R022 公网下载截图：**交鹊桥-服务端**（本交付仅为客户端 A 节）。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未做缺运行时时的自动下载器 / 内嵌安装器
- 未重打/上架安装包（本轮文案截图先行）
- 未改 MCP 三步 /「复制 MCP 配置」反馈；未改 trust-levels「才问你」
- 未把 macOS 写成已可下



## 交付说明（鹊桥-服务端 · 2026-11-06 Asia/Shanghai）

### 上架摘要

已更新官网首页 Linux 下载旁注并重建部署 `remote-assist-site` 二进制。未改接单页 / 后台 HTML，未替换 Windows / Linux 安装包（仍为 R022），未重启 `remote-assist-relay`，未占用 17891，未把安装包嵌进站点进程。

### 旁注最终原文（已上线）

> 解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。

公网 HTML 等价：

```html
<p class="note" style="margin-top:8px">解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 <a href="https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0">微软 .NET 8 下载页</a>，选运行时、不要选 SDK。</p>
```

### 部署

- 构建：`site/` 模块 `GOOS=linux GOARCH=amd64 CGO_ENABLED=0 go build -o ../artifacts/remote-assist-site-linux-amd64 ./cmd/site`
- 产物：`artifacts/remote-assist-site-linux-amd64`，16983100 字节
- 安装：`/usr/local/bin/remote-assist-site`（755）；仅 `systemctl restart remote-assist-site`
- 重启后：`remote-assist-site` MainPID **19184**（ActiveEnterTimestamp 2026-11-06 00:33:11 CST / Asia/Shanghai）
- `remote-assist-relay` 仍 MainPID **2114**（未重启），仍听 `*:17891`

### 公网抽查

- 首页导语仍含「Windows 和 Linux 都可下载使用」；**未**出现「目前以 Windows 为主」；macOS 仍「即将提供」
- 亲友可见首页无端口号 / 17891 / 放行入站 / 也可下载自包含包 / TLS / stdio
- 安装包仍为 R022：Windows 75,582,719 / `6AC3CE69…84400CD1`；Linux 7,999,909 / `B90E3B28…CD0AB1CE`；Content-Disposition 文件名无 `-fd`
- `/ask`、`/admin` 均为 HTTP 200

### 截图

已用本机 Windows Edge 打开公网 `http://121.196.45.81/#download` 补拍，并覆盖以下三张（未改客户端 `r022-install-*.png` / `r022-clipboard-preview.png`，也未改本轮 `r023-install-*.png` / `r023-clipboard-preview.png`）：

- `docs/screenshots/r022-home-download.png`：地址栏可见 `121.196.45.81/#download`；导语「Windows 和 Linux 都可下载使用」；下载区 Linux 旁注含「微软 .NET 8 下载页」与「选运行时、不要选 SDK」；macOS「即将提供」；无下载浮层。
- `docs/screenshots/r022-win-download.png`、`r022-linux-download.png`：同一公网页 + Edge 下载浮层。Linux 文件名 `queqiao-linux-x64.tar.gz`（无 `-fd`，已完成「打开文件」）；Windows 文件名 `RemoteAssist-Setup-0.1.0.exe`（Edge SmartScreen 提示「通常不会下载 … 请在打开前确保信任」）。浮层里与本产品无关的本机其它下载记录已遮盖后再入库。

### 明确未做

- 未开下一轮；未改接单页 / 后台；未替换安装包；未重启中继
- 未读/打印 `/etc/remote-assist-site.env`

## 产品验收（鹊桥-产品 · 2026-11-06 00:50 Asia/Shanghai）

对照本轮交付说明、`docs/screenshots/r023-*`、公网首页与 `docs/screenshots/r022-*-download.png` 验收 **通过**。

### 核对

- 客户端 A：实现 `a5eb2df`。共享常量 `WriteConfirmCopy.InstallInstructions` 在 R022 已验收文案末尾补了 2 行：
  - 若打开时提示缺少 .NET，先安装「.NET 8 运行时」（用来跑这个程序的，不是开发工具）。
  - 打开 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 ，在「运行时」那一栏按系统下载，不要选 SDK。
  Windows WinForms 与 Linux Avalonia 共用同一常量。`InstallCopiedHint` 仍为「说明里已带上官网下载地址，发给对方即可」。`r023-clipboard-preview.png`（MD5 `ED7C7A4CAFC247730D13324BB0269B04`）含上述新句，以及官网入口/Win/Linux 直链、macOS「尚未提供正式下载」、联网中性句。
- 服务端 B：实现 `3a0d53b`。公网首页 Linux 旁注现为：「解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。」导语仍含「Windows 和 Linux 都可下载使用」；macOS 仍「即将提供」；亲友首页未见端口 / 17891 / 放行入站 / 自包含 / TLS / stdio。`/` `/ask` `/admin` 均为 HTTP 200。
- 安装包本轮未替换，仍为 R022：公网 Windows `Content-Length` 75,582,719；Linux 7,999,909、`Content-Disposition` 文件名 `queqiao-linux-x64.tar.gz`（无内部 `-fd`），Last-Modified 仍为 2026-11-05 16:06:49 GMT。
- R022 遗留公网下载截图已补：`r022-home-download.png` / `r022-win-download.png` / `r022-linux-download.png` 地址栏可见 `121.196.45.81/#download`；下载浮层可见 `RemoteAssist-Setup-0.1.0.exe` 与 `queqiao-linux-x64.tar.gz`。未覆盖客户端 `r022-install-*.png` / `r022-clipboard-preview.png`，也未覆盖 `r023-install-*.png` / `r023-clipboard-preview.png`（MD5 已核对仍为原值）。

### 缺口（不阻断）

- `r023-install-entry.png` / `r023-install-copied.png` 与 R017/R021/R022 入口/反馈图字节相同（本轮未改其视觉/反馈句）；ui-shot。
- `r022-win-download.png` 与 `r022-linux-download.png` 几乎是同一下载浮层（同时露出 Win/Linux 文件名）；产品已独立用公网 HEAD 核过两包字节。
- 本轮未出新安装包（方案允许）。因此官网下到的助手端「复制安装说明」仍是 R022 文案，交 R024 换包。
- 未把 macOS 写成可下；未改接单页/后台已验收文案。

出包/文案 HEAD：`a5eb2df`。旁注上架：`3a0d53b`。已开 R024。

