# 第 25 轮 — 下载或打开时提示不受信任怎么选

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端（复制安装说明）+ 鹊桥-服务端（官网 Windows 旁注）
> 状态：**ready-for-product**（客户端 A 节已交付，待服务端 B 节）
> 依据：R024 产品已验收（公网包已含 R023 运行时指引）；亲友从官网用浏览器下载 Windows 安装包时，会看到「通常不会下载…请在打开前确保信任」。安装说明和官网目前只讲了缺 .NET 怎么装、系统问起联网怎么选，没讲这一步。

R024 验收时，产品在公网下载截图里看到 Edge 对 `RemoteAssist-Setup-0.1.0.exe` 提示「通常不会下载」。这是 Windows 亲友从官网下载后的下一个台阶：不知道该点保留还是该停手。本轮用一句白话补上，不谈证书、签名或 HTTP。

## 本轮目标

1. **高频**：去协助端「复制安装说明」补一句——浏览器提示通常不会下载时选保留；打开时系统提示不认识这个程序时选仍要运行。
2. **打磨**：官网 Windows 下载按钮旁加一句旁注（立刻对正在下载的人生效）：「若浏览器提示通常不会下载，选择保留即可。」

本轮 **不替换** 公网安装包（与 R023 相同：先改文案和旁注；换包交后续轮）。

## A. 复制安装说明（高频 · 鹊桥-客户端）

在已验收的 `InstallInstructions` / `InstallInstructionsClipboard` 全文末尾（.NET 运行时两句之后）追加一句：

如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。

验收：

- 去协助端「复制安装说明」仍是轻按钮；复制反馈仍是已验收原文「说明里已带上官网下载地址，发给对方即可」。
- 剪贴板含 R023 已验收全文（官网地址、Win/Linux 直链、macOS 尚未提供、联网句、.NET 8 运行时两句）**加上**上面这句。
- 不要提 SmartScreen、证书、签名、HTTP、「不安全」、端口、17891、自包含。
- 不要改「如何安装 MCP」三步 /「复制 MCP 配置」及反馈；不要改 `docs/trust-levels.md` 的「才问你」；不要改 R013–R024 已验收其它原文。
- 截图放到 `docs/screenshots/r025-*`（至少：入口仍在、复制反馈未变、剪贴板预览能读到新句）。**勿覆盖** `r022-*` / `r023-*` / `r024-*`。

## B. 官网 Windows 旁注（打磨 · 鹊桥-服务端）

在首页 Windows 下载按钮旁（Linux 旁注保持不动）增加一句：

若浏览器提示通常不会下载，选择保留即可。

验收：

- Linux 旁注仍是 R023 已验收原文：「解开后运行。需要先装好 .NET 8 桌面方向运行时。打开 [微软 .NET 8 下载页](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)，选运行时、不要选 SDK。」
- 导语仍是「Windows 和 Linux 都可下载使用」；macOS 仍「即将提供」。
- 不要出现端口号、TLS、stdio、内部路径、中继 JSONL、「放行入站」、「也可下载自包含包」、17891、证书、签名。
- 不替换公网安装包（Windows 仍 75,583,762 / `236EDEE7B5C1CC28BA6E4804D17CE0FEC07AACA916CB201C3C16CAC2BC76FB58`；Linux 仍 8,000,095 / `305F9E40057828C0354ED0C0057F6611A380541A962A15AACC8CAD2C414FE586`），除非本轮必须动文件并说明原因。
- `/`、`/ask`、`/admin` 仍 HTTP 200；不占用 17891；不新装重型 Web 栈。
- 截图放到 `docs/screenshots/r025-*`（至少：公网首页 Windows 旁注可见，地址栏可见 `121.196.45.81`）。**勿覆盖** `r022-*` / `r023-*` / `r024-*`。
- 若 `docs/site-copy.md` 写有下载区文案，一并改成与首页一致。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 购买代码签名证书 / 给安装包签名
- 给官网加域名或 HTTPS（另议）
- 替换公网安装包
- 把未上真机的 macOS 包写成已经可下
- 改接单页 / 后台已验收文案
- 改信任等级「才问你」

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 在已验收 `WriteConfirmCopy.InstallInstructions` 全文末尾（R023 .NET 两句之后）**仅追加一句**：「如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。」
- Windows WinForms 与 Linux Avalonia **共用同一常量**；`InstallCopiedHint` 仍为「说明里已带上官网下载地址，发给对方即可」；反馈只出现一次。
- **未改** MCP 三步 /「复制 MCP 配置」反馈、trust-levels「才问你」、R013–R024 其余原文；**未换包**；未改 `site/`。

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
如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。
```

### 截图（`docs/screenshots/r025-*`，已复制 `%USERPROFILE%\Downloads\r025-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r025-install-entry.png` | 「复制安装说明」轻按钮；「连接」实心绿；ui-shot | 8ED52115F70AF18402F98BABF64CF47B |
| `r025-install-copied.png` | 反馈「说明里已带上官网下载地址，发给对方即可」只一次；ui-shot | ED902C943BD0D4906580AA772E8E054A |
| `r025-clipboard-preview.png` | 预览含新句 + 官网/直链/macOS 尚未提供正式下载/联网中性句/.NET 8 两行；ui-shot | 26B79CAE2D06123B450AC735AE2839A0 |

三张 MD5 互异。坦白：`r025-install-entry.png` / `r025-install-copied.png` 分别与 r023（及更早同源）入口图、反馈图**字节相同**；`r025-clipboard-preview.png` 为新字节（含不受信任怎么选那句），且未覆盖 r022/r023/r024 已有截图。

官网 Windows 旁注：**交鹊桥-服务端**（本交付仅为客户端 A 节）。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开下一轮；未换公网安装包；未改 `relay/`、`site/`、AI-SSH-Pro
- 未改 MCP / InstallCopiedHint / trust-levels「才问你」
- 未提 SmartScreen、证书、签名、HTTP、「不安全」、端口、17891

