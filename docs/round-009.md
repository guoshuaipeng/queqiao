# 第 9 轮 — Mac/Linux 被控 CLI + MCP + 本机确认（平台第一步）

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（跨平台被控 CLI / MCP / 发布小包；不改 `relay/`，不把 Windows WinForms 迁到 Avalonia，不重做 R006–R008）
> 状态：**wip**
> 依据：docs/PRODUCT.md「支持平台」第一步；docs/architecture.md §13（先 CLI/MCP + 确认，再补对等 GUI）；docs/roadmap.md 平台轨道

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
