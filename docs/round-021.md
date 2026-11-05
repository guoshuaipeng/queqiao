# 第 21 轮 — 复制安装说明带上官网下载地址

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（Windows WinForms 与 Linux Avalonia 去协助端；不改中继 / 官网站点实现）
> 状态：**ready-for-product**
> 依据：R020 产品已验收（上架 `8ce2bd9`）；PRODUCT.md 协助闭环第 1 步「把安装包下载地址或本机安装包目录发给远端用户」；官网已可下 Windows / Linux 包。

R020 已让官网能下到跟上已验收界面的 Linux 包。去协助端「复制安装说明」仍主要按「把安装包文件一起发出去」来写，亲友还要靠对方传一个大文件。本轮把官网下载地址写进说明，让对方自己下载也能装上。

## 本轮目标

1. **高频**：去协助端「复制安装说明」的剪贴板文案带上官网下载入口，覆盖 Windows 与 Linux（macOS 仍标即将提供，不要写成已经可下）。
2. **打磨**：复制后的人话反馈改成能看出说明里已带下载地址，不要静默，也不要把「复制安装说明」改回实心主按钮。

## A. 安装说明带上官网下载地址（高频）

验收：

- Windows WinForms 与 Linux Avalonia 去协助端都要改；两段人话保持同一意思，不要各写一套。
- 剪贴板文案须包含官网入口 http://121.196.45.81/ ，并写明：
  - Windows 可从官网下载安装包；
  - Linux 可从官网下载包，解开后运行，需要先装好 .NET 8 桌面方向运行时；
  - macOS 尚未提供正式下载，不要写成已经可下。
- 可以同时给出具体文件地址（Windows：`/downloads/RemoteAssist-Setup-0.1.0.exe`；Linux：`/downloads/queqiao-linux-x64.tar.gz`），也可以只写官网首页并引导到下载区。推荐直接写出可点的下载地址，减少对方再找一遍。
- 说明里不要把「必须把安装包文件一起发出去」写成唯一办法。可以保留「如果你手头有安装包，也可以一起发出去」，但主路径改为发给对方官网下载地址。
- 不要写端口号、`17891`、TLS、stdio、内部磁盘路径。
- 不要改「如何安装 MCP」三步、不要改「复制 MCP 配置」及其已验收反馈，不要改 R013–R016 已验收原文，不要改 `docs/trust-levels.md` 的「才问你」。
- 「连接」仍是去协助端唯一实心绿主按钮；「复制安装说明」保持轻按钮。

截图放到 `docs/screenshots/r021-*`（至少：去协助端能看到「复制安装说明」仍是轻按钮；复制后的人话反馈；剪贴板或预览里能看到官网下载地址，且 macOS 未写成已可下）。Windows 与 Linux 能各给一张更好；若本轮只展示 Avalonia，Windows 也必须改到同一文案并在交付说明里写明。

## B. 复制反馈（打磨）

- 复制后要有人话反馈，推荐：「说明里已带上官网下载地址，发给对方即可」。也可以用同等清楚的短句。
- 不要再只写「请把安装包和这段说明一起发出去」而不提官网下载。
- 反馈只出现一次，不要叠字（对齐 R017 已修过的「只出现一次」）。

## 本轮不做

- 改中继、改官网站点实现、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 重做 R001–R020 已验收路径（本轮只更新「复制安装说明」文案与反馈）
- 把未上真机的 macOS 包写成已经可下
- 改接单页 / 后台已验收文案
- 现场公网双机不是本轮必须

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- 共享常量 `RemoteAssist.Protocol.WriteConfirmCopy.InstallInstructions` / `InstallInstructionsClipboard` / `InstallCopiedHint` 已更新；**Windows WinForms（Helper.UI）与 Linux Avalonia（HelperWindow）共用同一常量**，两端人话一致，无各写一套。
- 剪贴板主路径改为发给对方官网下载地址（含 Windows / Linux 直链）；保留「如果你手头有安装包，也可以一起发出去」；**macOS 写「尚未提供正式下载」**，未写成已可下。
- 复制反馈改为「说明里已带上官网下载地址，发给对方即可」；只写预览行一次，不写状态行（对齐 R017 不叠字）。
- 「连接」仍是唯一实心绿主按钮；「复制安装说明」保持轻按钮（白底蓝字）。
- **未改**「如何安装 MCP」三步、「复制 MCP 配置」及反馈；未改 R013–R016 已验收原文；未改 `docs/trust-levels.md`「才问你」。

### 最终逐字文案

**InstallCopiedHint（复制反馈）**

> 说明里已带上官网下载地址，发给对方即可

**InstallInstructions / InstallInstructionsClipboard（剪贴板）**

```
请打开官网下载「鹊桥远程协助」：http://121.196.45.81/
Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）
macOS 尚未提供正式下载。
如果你手头有安装包，也可以一起发出去。
装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。
如果电脑问是否允许连网，点「允许」。
```

### 截图（`docs/screenshots/r021-*`，已复制 `%USERPROFILE%\Downloads\r021-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r021-install-entry.png` | 「复制安装说明」轻按钮；「连接」实心绿；ui-shot | 8ED52115F70AF18402F98BABF64CF47B |
| `r021-install-copied.png` | 复制后反馈「说明里已带上官网下载地址，发给对方即可」只出现一次；ui-shot | ED902C943BD0D4906580AA772E8E054A |
| `r021-clipboard-preview.png` | 预览区展示剪贴板正文：官网入口 + Win/Linux 直链；macOS 尚未提供正式下载；ui-shot | 57E0DA16C431DA7A6AF3E94EE36135FE |

三张 MD5 互异。坦白：`r021-install-entry.png` 与 `r017-install-entry.png` **字节/MD5 相同**（入口控件本轮未改视觉，仅剪贴板与反馈文案变了；入口图复用同一渲染结果）。

本轮截图为 **Avalonia ui-shot**；Windows 已改到同一共享常量，未单独拍 WinForms 窗（交付说明在此写明）。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release；安装说明断言已跟新文案）。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未把 Windows 迁到 Avalonia；未把 macOS 写成已可下
- 未改 R013–R016 已验收原文；未改 MCP 三步 /「复制 MCP 配置」反馈；未改 `docs/trust-levels.md`「才问你」
- 未写端口号、17891、TLS、stdio、内部磁盘路径
- 现场公网双机非本轮必须，未做

