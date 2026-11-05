# 第 20 轮 — 官网 Linux 包跟上已验收界面

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（打出含 R015–R018 的 Linux 小包）+ 鹊桥-服务端（替换官网文件，并打磨首页「目前以 Windows 为主」）
> 状态：**wip**（客户端 Linux 包已打出，待服务端上架 / 首页打磨）
> 依据：R019 产品已验收（实现 `04347f3`）；当前官网 Linux 包为 R014 前后现成产物，不含后续 Avalonia 界面打磨。

R019 已让官网能下到 Windows 与 Linux 包。当前 Linux 包按当时要求原样上架，但不含 R015–R018 已验收的界面。本轮换上当前已验收的 Linux 小包。macOS 仍标「即将提供」，不假装已可下。

## 本轮目标

1. **高频**：官网 Linux 下载换成含 R015–R018 已验收界面的框架依赖小包。
2. **打磨**：首页「目前以 Windows 为主」改为同时覆盖已提供的 Windows / Linux（macOS 仍即将提供）。

## A. Linux 包更新（高频）

验收：

- 鹊桥-客户端：基于当前已验收代码打一份 Linux `.tar.gz`（默认依赖 .NET 8 运行时的小包，与现在官网同形态），包含 R015–R018 已验收的 Avalonia 去协助 / 接受协助界面（设备列表人话与连接态、连上后五键白话、去协助端 MCP / 复制安装说明、接受协助端可选 MCP 说明）。不要重做这些功能，不要改 R013–R018 已验收原文，不要改 `docs/trust-levels.md` 的「才问你」。
- Windows 安装包本轮默认不换（官网 Windows 包仍可用）。不要提供未在真机验收的 macOS 包当作正式可下。
- 不要做成自包含包来冒充本轮产物；若只打出了小包，就继续按小包上架。
- 鹊桥-服务端：用新文件替换 http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（或同等路径），公网文件名不要带内部 `-fd`；下载仍为真实 gzip，Content-Disposition 正常。旁注仍是「解开后运行。需要先装好 .NET 8 桌面方向运行时」。记下新包大小与 SHA256。
- 新包的大小或 SHA256 应与 R019 的 7,993,604 字节 / `34CC363BAD28192895FCED74929FDE0A8C858D6E6A489EA40201D6DE552C827D` 不同（除非碰巧相同，需说明原因）。
- 托管约束不变：现有 `remote-assist-site` 静态提供，不新装重型 Web 栈，不占用 `17891`，中继优先级高于官网。不要上架 macOS。
- 亲友可见页面不出现端口号、TLS、stdio、内部路径、中继 JSONL 细节。

截图或公网记录放到 `docs/screenshots/r020-*`（至少：新的 Linux 包可从官网下载；如有首页文案打磨，首页不再写「目前以 Windows 为主」来否定 Linux）。

## B. 首页表述（打磨）

- 把首页「目前以 Windows 为主」改成不否定 Linux 已可下载的表述，可以同时提到 Windows 和 Linux；macOS 仍标「即将提供」。
- 不要写成完整多平台营销，不要改接单页 / 后台已验收文案（`docs/site-copy.md` 第 1 节）。
- 不要写端口、不要写「放行入站」，也不要写「也可下载自包含包」（若仍未提供自包含包）。

## 包从哪来

- 鹊桥-客户端负责出包，并把文件路径交给鹊桥-服务端；不改 `src/` 功能、不开平行功能轮。
- 鹊桥-服务端负责替换上架、改首页表述、上机。不要搭建完整发布系统、CDN 或自动构建流水线。
- 客户端出包完成后，服务端再替换文件；首页表述打磨可以并行。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia，或改 Avalonia / WinForms 已验收功能
- 重做 R001–R019 已验收路径
- 把未上真机的 macOS 包当成正式可下
- 为官网安装重型 Web 栈，或占用 `17891`
- 完整 CRM / 计费 / 组织工单
- 把官网账号和中继账号打通

## 完成时

- 更新本文交付说明与截图 / 公网记录
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要（客户端出包）

- 基于当前已验收 HEAD `9217e4c`（含 R015 `e11a8a7`、R016 `f083f51`、R017 `e46b29f`/`5bcab34`、R018 `b8c215c`）用 `scripts/publish-agent-avalonia.ps1 -Runtimes linux-x64` 打出 **framework-dependent** Linux 小包（`--self-contained false`）。
- **未改** `src/` 业务逻辑；未改 R013–R018 已验收原文；未改 `docs/trust-levels.md`「才问你」。
- **未重打** Windows `RemoteAssist-Setup-*.exe`；**未把 macOS 包当正式可下**（本轮仅打 linux-x64）。
- **未改** `site/`、未上生产机；新包本机路径交鹊桥-服务端替换上架。

### 本机产物

| 项 | 值 |
|------|------|
| 绝对路径 | `E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz` |
| 字节 | 7,999,640 |
| MB | 7.63 |
| mtime | 2026-11-05 23:22:18 Asia/Shanghai |
| SHA256 | `EC1C7211E5B1045E7E945756035CCE426C5AB4F7CB102523ECA41BDB9DCE3650` |
| 出包 HEAD | `9217e4c4af582e1b89053d3c12e0320a73beb777` |
| 形态 | framework-dependent（非 self-contained）；约 7.63 MB gzip，远小于自包含体量 |
| 包内 | `鹊桥远程协助/RemoteAssist.Agent.Avalonia`、`.dll`、`.deps.json`、`.runtimeconfig.json`、`运行说明.txt`（脚本既有约定文案） |

对照 R019 旧包 7,993,604 字节 / SHA256 `34CC363BAD28192895FCED74929FDE0A8C858D6E6A489EA40201D6DE552C827D`：**大小与 SHA256 均不同**。

源码抽查仍含 R015 设备列表人话/连接态、R016 五键引导、R017「复制安装说明」、R018「接受协助时不会自动对外开启 MCP」。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做（客户端）

- 未开下一轮；未改 `relay/`、官网 `site/`、AI-SSH-Pro；未上生产机
- 未重做 R015–R018 功能；未改 R013–R018 已验收原文
- 未把 macOS 包当正式产物；未换 Windows 安装包
- 公网替换与首页「目前以 Windows 为主」文案打磨：**交鹊桥-服务端**（客户端只交出包路径）

