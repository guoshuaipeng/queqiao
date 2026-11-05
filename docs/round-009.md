# 第 9 轮 — Mac/Linux 被控 CLI + MCP + 本机确认（平台第一步）

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（跨平台被控 CLI / MCP / 发布小包；不改 `relay/`，不把 Windows WinForms 迁到 Avalonia，不重做 R006–R008）
> 状态：**done**（产品已验收，2026-11-05 Asia/Shanghai）
> 依据：docs/PRODUCT.md「支持平台」第一步；docs/architecture.md §13（先 CLI/MCP + 确认，再补对等 GUI）；docs/roadmap.md 平台轨道
> CLI 展示与终端确认原文见 `docs/first-run-mac-linux.md` 第 3、4 节（直接用字，不扩大本轮范围）。

Windows P0（配对、确认、信任等级、MCP 提议闭环、同一安装包双模式）已在 R001–R008 收口。本轮开始平台轨道，不新开 Windows 功能面，也不把 Mac/Linux 一次做完。

## 本轮目标

1. **高频**：先出可在 Linux（验收基准）运行的 **被控 CLI + MCP**：经默认公网中继配对，协助方能做只读查询；写操作必须在被控本机确认，智能体不能绕过。本轮终端确认即可。
2. **打磨**：默认发 **依赖已安装 .NET 8 运行时的小包**（framework-dependent），记录实测体积；附人话安装说明。不要默认打出自包含大包。

## A. 被控 CLI + MCP（高频）

验收：

- 用 `net8.0`（非 `net8.0-windows`）做出被控端命令行，能在 **Linux** 上启动并连默认公网中继（`121.196.45.81:17891`，见 `docs/relay-deploy.md`）。
- 启动后展示 **会话码 + 验证密码**（终端打印即可），Windows 协助端用现有「去协助别人」能连上。
- 连上后至少能完成只读：查看正在运行的程序、查看日志。
- 写操作（包括经 MCP 提议的）必须在被控本机确认。本轮可用**终端提示**，规则与 Windows 相同：说明是「对方想在这台电脑执行操作」+ 人话（如「运行一条命令」）+ 命令/路径细节；「允许 / 拒绝」；超时不自动允许；拒绝后未做任何改动，并让人看到「已拒绝，未做任何改动」。
- 智能体 / MCP **不能绕过**该确认。不要用 osascript、zenity 等外部脚本当作正式确认方案。
- 同一套协议也要能发布 `osx-x64` / `osx-arm64` 小包；若本轮没有 macOS 真机，注明「已出包、未上真机」，不要把本轮写成 Mac 已验收。
- 信任等级本轮不新做 GUI；能沿用现有协议默认「每次询问」即可。不要重做 R007 的 WinForms 三档界面。

截图或可复核记录放到 `docs/screenshots/r009-*`（终端展示码+密码、写操作确认、拒绝未执行、协助端只读成功；文本记录可与截图并列，如 `r009-cli-session.txt`）。

## B. 框架依赖小包（打磨）

验收：

- 默认发布 **framework-dependent** 包，不把 .NET 8 运行时打进应用。目标尽量接近 architecture.md §13.3 里 Windows `app-net48-fd` 的约 1.4 MB 档；Avalonia 不在本轮，应力争个位数 MB。发布时写入实测数字。
- 附简短人话说明：需要先安装 .NET 8 运行时（Linux 用发行版/微软包；macOS 用官方安装包或 Homebrew），再运行本程序。不要只丢一句英文 Download runtime。
- 自包含包本轮不必做；若顺手打一份，体积上限约 72 MB，且不得替换默认小包。
- 不要用 .NET Framework 4.8 当作跨平台缩体积方案。

## 本轮不做

- Avalonia 被控状态窗 / 确认窗（下一步，R010 方向；§13「再补对等 GUI」）
- Mac/Linux 双模式 GUI（接受协助 / 去协助，路线图平台第二项）
- 把 Windows 客户端迁到 Avalonia，或重做 R006–R008 的 WinForms 界面
- 改中继、改 AI-SSH-Pro、完整远程桌面
- 官网「即将提供」改成真实下载地址（本轮不挂 Windows 安装包）
- 新发明信任等级或确认文案（沿用 PRODUCT.md / trust-levels.md 已有原文）

## 完成时

- 更新本文交付说明、`docs/roadmap.md`（本轮标 done；平台「Mac/Linux 被控端 + MCP」在 CLI 验收通过后标为部分完成 / 仍待 Avalonia，不要一次标成平台项全部 done）
- 截图或文本复核放到 `docs/screenshots/r009-*`
- 回告产品验收；未开下一轮


## 交付说明（2026-11-05 Asia/Shanghai）

### 状态

**done**（平台第一步：Linux 被控 CLI + MCP + 终端确认 + 框架依赖小包；未开 R010 / Avalonia）

### 代码

- 新增 `src/RemoteAssist.Agent.Cli`（`net8.0`）：跨平台被控 CLI，默认连公网中继 `121.196.45.81:17891`。
- CLI 展示 / 终端确认文案对齐 `docs/first-run-mac-linux.md` 第 3、4 节原文：
  - 启动：「鹊桥远程协助已启动，正在等待对方连接。」+「把下面两行发给要帮你的人：」+ `会话码：` / `验证密码：`；剪贴板成功「已复制，直接发给对方即可。」失败「用鼠标选中上面两行，复制后发给对方。」；已连上「已有人连上。对方可以查看进程、日志和文件；要改动时会再问你。」；刷新「已换新的会话码和验证密码。之前发出去的码不能再用。」
  - 写确认：标题「对方想在这台电脑执行操作」；人话 +「命令：…」；「请输入 允许 或 拒绝（也可以输入 y 或 n）。」；「若不操作，将在 60 秒后自动拒绝」；拒绝后「已拒绝，未做任何改动」。超时不自动允许；MCP / 中继不能绕过。
- `ConfirmPrompt` 在 `AGENT_CLI` 下仅控制台确认（无 WinForms）；`ToolExecutor.RunCommand` 在非 Windows 用 `/bin/sh -c`。
- 发布脚本 `scripts/publish-agent-cli.ps1`：默认 **framework-dependent** 小包 `linux-x64` / `osx-x64` / `osx-arm64`（Agent.Cli + MCP），附 `运行说明.txt`（「这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。」）。
- 不改 `relay/`；不重做 R006–R008 WinForms；无 Avalonia。

### 实测体积（framework-dependent，不含 pdb）

| 产出 | 合计 |
|------|------|
| `artifacts/agent-cli-linux-x64-fd` | 245.9 KB（约 0.24 MB） |
| `artifacts/agent-cli-osx-x64-fd` | 255.2 KB（约 0.25 MB） |
| `artifacts/agent-cli-osx-arm64-fd` | 278.3 KB（约 0.27 MB） |
| `artifacts/mcp-linux-x64-fd` | 200.2 KB |
| `artifacts/mcp-osx-x64-fd` | 209.5 KB |
| `artifacts/mcp-osx-arm64-fd` | 232.7 KB |

### 联调

- 在 Linux（本 bot 共享 box，`DOTNET_ROOT` + .NET 8 运行时）跑通 `RemoteAssist.Agent.Cli`，经默认公网中继展示会话码+验证密码。
- Windows 协助端经中继连上后完成只读：`ListProcesses`、`ReadLogTail`（`/tmp/r009-demo.log`）。
- 写操作 `RunCommand` 在 Linux 终端确认后选「拒绝」→ 协助端收到 `已拒绝，未做任何改动`；本机 `/tmp/r009-deny-marker` 不存在。
- 联调为对齐 Windows 协助端亲友粘贴「码+密码」（Helper.UI 无指纹时回落明文业务管道）使用了 `--insecure`；默认发布仍为 TLS。

### osx

**已出包、未上真机**（无 macOS 真机启动验收；见 `r009-osx-publish.png`）。不要把本轮写成 Mac 已验收。

### 测试

- 全量 `dotnet test`：通过 166 / 总计 167；`SpoofedOwnerLinkFlag_OnGuestChannel_StillRequiresConfirm` 在整包并行时偶发超时竞态失败，单独重跑 3 次均通过（非本轮引入；本轮未改该用例）。

### 截图 / 复核

| 文件 | 屏幕/终端可见原文（要点） |
|------|---------------------------|
| `docs/screenshots/r009-linux-cli-session.png` | 「鹊桥远程协助已启动，正在等待对方连接。」「会话码：…」「验证密码：…」「用鼠标选中上面两行，复制后发给对方。」 |
| `docs/screenshots/r009-helper-readonly.png` | Windows 协助端 `PROCS ok=True` / `LOGS ok=True`；日志含 `r009-demo` |
| `docs/screenshots/r009-write-confirm.png` | 「对方想在这台电脑执行操作」「运行一条命令」「请输入 允许 或 拒绝（也可以输入 y 或 n）。」「已拒绝，未做任何改动」 |
| `docs/screenshots/r009-write-denied.png` | 「已拒绝，未做任何改动」；`tool_denied_user`；标记文件未生成 |
| `docs/screenshots/r009-package-size.png` | 实测 KB +「需先安装「.NET 8 运行时」」人话 |
| `docs/screenshots/r009-osx-publish.png` | osx-x64 / osx-arm64 目录与体积；标明未上真机 |

文本复核：`r009-cli-session.txt`、`r009-write-confirm.txt`、`r009-helper-combo.txt`、`r009-linux-agent-full.txt`。父级取图：`%USERPROFILE%\Downloads\r009-shots\`。

### 本轮不做（已遵守）

- 未开 R010 / Avalonia 被控窗；未做 Mac/Linux 双模式 GUI；未把 Windows 迁到 Avalonia；未改中继；未改官网下载按钮；未重做 R006–R008。

## 产品验收（2026-11-05 Asia/Shanghai）

对照 `docs/screenshots/r009-*` 与 `docs/first-run-mac-linux.md` 第 3、4、5 节原文，**验收通过**。提交 `ef0948a`。

核对要点：

- Linux CLI 经默认公网中继 `121.196.45.81:17891` 打印「鹊桥远程协助已启动，正在等待对方连接。」「把下面两行发给要帮你的人：」以及两行「会话码：」「验证密码：」；剪贴板失败路径为「用鼠标选中上面两行，复制后发给对方。」品牌为「鹊桥」。
- Windows 协助端经同一中继连上后 `PROCS ok=True`（Linux 进程如 bamfdaemon）、`LOGS ok=True`（`/tmp/r009-demo.log`）。
- 写操作终端确认标题「对方想在这台电脑执行操作」、人话「运行一条命令」、命令详情、「请输入 允许 或 拒绝（也可以输入 y 或 n）。」、「若不操作，将在 60 秒后自动拒绝」；拒绝后「已拒绝，未做任何改动」；本机 `/tmp/r009-deny-marker` 不存在。
- 默认 framework-dependent 小包实测：linux-x64 245.9 KB、osx-x64 255.2 KB、osx-arm64 278.3 KB；人话「这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。」未默认自包含大包。
- osx-x64 / osx-arm64 **已出包、未上真机**，本轮不写成 Mac 已验收。
- 联调截图使用了 `--insecure`（与协助端未粘贴指纹的路径对齐）；默认发布仍走 TLS。
- `dotnet test` 166/167；`SpoofedOwnerLinkFlag_OnGuestChannel_StillRequiresConfirm` 整包偶发超时，单独重跑通过，非本轮引入，不阻断本轮。

Mac 真机验收与 Avalonia 被控窗不在本轮，见 `docs/round-010.md`。
