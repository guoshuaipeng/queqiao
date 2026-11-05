# 第 11 轮 — Mac/Linux Avalonia 双模式（去协助）

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（Avalonia 双模式 / 协助端；不改 `relay/`，不把 Windows WinForms 迁到 Avalonia，不重做 R006–R010）
> 状态：**done**（实现完成，待产品验收）
> 依据：docs/PRODUCT.md「支持平台」第二步；docs/architecture.md §13；docs/quality.md；被控窗文案仍见 `docs/first-run-mac-linux.md` 第 1、2 节。协助端文案对齐 Windows 已验收原文（一次粘贴、空态、默认中继、连上后白话操作），不要套 Windows「点允许连网」的话术。

R010 已产品验收 Linux Avalonia 被控窗（`30dd4fd`）。本轮补平台第二项：与 Windows 对等的双模式 GUI（接受协助 / 去协助别人）。同事运维仍可用 R009 CLI；亲友/客户被控主路径仍是 R010 的被控窗，不要把「打开终端复制码」写给他们。

## 本轮目标

1. **高频**：在 Linux（验收基准）跑通 Avalonia **双模式入口 + 去协助**：同一程序可选「接受协助」或「去协助别人」；协助端能经默认公网中继粘贴会话码+验证密码连上对方，完成只读；写操作仍到对方本机确认。
2. **打磨**：默认仍发 **framework-dependent** 包，记录双模式实测体积；附上与 `docs/first-run-mac-linux.md` 第 5 节一致的运行时人话。能顺手打出 Linux 可分发的 `.tar.gz`（内含运行说明）更好；做不到不挫主路径。不要默认打出自包含大包。不要把「压到个位数 MB」当硬指标（R010 已记录 Skia 占大半）。

## A. 双模式 + 去协助（高频）

验收：

- 同一 Avalonia 程序先出首页，可选「接受协助」或「去协助别人」，并能返回首页。首页卡片对齐 `docs/quality.md`：接受协助蓝、去协助别人绿；卡片本身可点。
- 「接受协助」进入已验收的 R010 被控窗，不要回退或改掉已有被控文案（「复制码+密码」、确认弹窗、「结束这次协助」、关窗提示）。
- 「去协助别人」：能一次粘贴会话码+验证密码（与 Windows 协助端相同的两行格式），经默认公网中继 `121.196.45.81:17891` 连上对方被控端。连上前不要出现可点的灰色操作按钮；连上后操作用白话，见 `docs/quality.md`。
- 连上后至少能完成只读：查看正在运行的程序、查看日志。写操作必须走对方本机确认（Linux 被控窗或 Windows 被控端均可作对端验收）；超时不自动允许；对方拒绝后协助端看到「已拒绝，未做任何改动」。MCP 不能绕过。
- 品牌「鹊桥」。亲友可见界面不出现 Assist、Admin、ownerLink、TLS、端口号。
- 连不上时的可执行引导对齐 Windows R003：错密码 / 已换新码 / 到不了中继 / 对方未开始接受协助；不要写端口，不要套「点允许连网」。
- 信任等级本轮不新做三档界面；默认「每次询问」。不要把 P2-7 拆进本轮。
- 同一套也要能发布 `osx-x64` / `osx-arm64`；若没有 macOS 真机，注明「已出包、未上真机」，不要把本轮写成 Mac 已验收。

截图放到 `docs/screenshots/r011-*`（至少包括：首页双模式入口、协助端连上前、协助端只读成功、对方拒绝写操作后的提示、从协助/被控能返回首页；若有发布产物，再加体积截图）。

## B. 框架依赖小包（打磨）

- 默认发布 framework-dependent 包，记录实测体积。不要默认打进 .NET 8 运行时。
- 附简短人话，与第 5 节一致：「这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。」
- 能顺手打出 Linux `.tar.gz`（内含运行说明.txt）更好；Mac 能出 `.app` 目录即可，`.dmg` 做不到就注明，不挫主验收。
- 自包含包本轮不必做；若顺手打一份，体积上限约 72 MB，且不得替换默认小包。
- 不要用 .NET Framework 4.8 当作跨平台缩体积方案。不要把压到个位数 MB 当本轮硬指标。

## 本轮不做

- 把 Windows 客户端迁到 Avalonia，或重做 R006–R010 的 WinForms / CLI / 被控窗
- 改中继、改 AI-SSH-Pro、完整远程桌面
- 官网「即将提供」改成真实下载地址
- 信任等级三档界面或 P2-7「结束完全信任」
- 把「打开终端复制码」写进亲友/客户主路径

## 完成时

- 更新本文交付说明、`docs/roadmap.md`（本轮标 done；平台「Mac/Linux 双模式 GUI」在验收通过后标 done）
- 截图放到 `docs/screenshots/r011-*`
- 回告产品验收；未开下一轮

## 交付说明（2026-11-05 Asia/Shanghai）

### 状态

**done**（Linux Avalonia 双模式入口 + 去协助别人；framework-dependent 小包 + Linux `.tar.gz`；未开 R012；osx 已出包、未上真机）

### 代码

- 在已有 `src/RemoteAssist.Agent.Avalonia`（R010 被控窗）上扩展：`HomeWindow` 双模式首页、`HelperWindow` 协助端、`AppNavigator` 导航。
- 首页：「一个应用，两种角色 — 先选你要做什么」；接受协助蓝 / 去协助别人绿；整卡可点；「进入 ›」；页脚「选上面一项即可。」
- 「接受协助」进入未改文案的 R010 被控窗：「复制码+密码」「结束这次协助」「刷新会话码」；新增「返回首页」。关窗提示原文保留。
- 「去协助别人」：一次粘贴会话码+验证密码、「从剪贴板」「连接」；连上前无操作按钮；连上后「查看正在运行的程序」「查看日志」「运行一条命令」。
- 默认公网中继 `121.196.45.81:17891`；无指纹时明文业务管道（对齐 Windows 协助端）。连不上引导用 `FriendConnectGuide`（错密码 / 已换新码 / 到不了中继 / 对方未开始接受协助）；不套「点允许连网」。
- 写操作仍走对方本机确认；对方拒绝后协助端状态「已拒绝，未做任何改动」。预览只用「将通过网络连接 · 码 …」，亲友界面不出现 Assist / 端口 / 中继 IP。
- 发布脚本 `scripts/publish-agent-avalonia.ps1`：FD `linux-x64` / `osx-x64` / `osx-arm64` + Linux `artifacts/queqiao-linux-x64-fd.tar.gz`（内含运行说明.txt）。
- 不改 `relay/`；不迁 Windows；不重做 R006–R010；无三档信任 / P2-7。

### 实测体积（framework-dependent，不含 pdb）

| 产出 | 合计 |
|------|------|
| `artifacts/agent-avalonia-linux-x64-fd` | 18.90 MB |
| `artifacts/agent-avalonia-osx-x64-fd` | 26.38 MB |
| `artifacts/agent-avalonia-osx-arm64-fd` | 26.40 MB |
| `artifacts/queqiao-linux-x64-fd.tar.gz` | 7.61 MB（压缩包；内含运行说明.txt） |

人话：「这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。」

### 联调

- Linux box（DISPLAY）跑通 Avalonia 双模式；`--accept --insecure --test-allow-pair --test-deny-write` 经默认公网中继。
- 协助端（Windows `AssistClient` 经中继）会话码 `U6G722`：`PROCS ok`、`LOGS ok`（`/tmp/r011-demo.log`）；`RUN` denied →「已拒绝，未做任何改动」；`/tmp/r011-deny-marker` 不存在。见 `docs/screenshots/r011-helper-combo.txt`。
- 联调使用 `--insecure` + 测试钩子；默认发布仍为 TLS；正式确认路径是 Avalonia 弹窗。

### osx

**已出包、未上真机**（见 `r011-osx-publish.png`）。不要把本轮写成 Mac 已验收。

### 测试

- 全量 `dotnet test`：通过 167 / 总计 167。

### 截图 / 复核

| 文件 | 屏幕上可见原文（要点） |
|------|------------------------|
| `r011-home.png` | 「鹊桥远程协助 · 智能体」；「一个应用，两种角色 — 先选你要做什么」；蓝「接受协助」/ 绿「去协助别人」；「进入 ›」；「选上面一项即可。」 |
| `r011-accept-main.png` | 「鹊桥远程协助」；「复制码+密码」；「结束这次协助」；「返回首页」；「已连上网络，可以发给对方了」 |
| `r011-close-warn.png` | 「关闭窗口后，协助可能还在进行。要结束请点「结束这次协助」。」 |
| `r011-helper-idle.png` | 「连接对方」；「粘贴后点「连接」」；「状态：未连接」；无操作按钮 |
| `r011-helper-paste.png` | 一次粘贴「会话码 / 验证密码」；「从剪贴板」「连接」；「将通过网络连接 · 码 …」 |
| `r011-helper-fail-guide.png` | 「连接失败」；「对方还没打开「接受协助」。请对方在首页选择「接受协助」，再把码和密码发给你。」 |
| `r011-helper-readonly.png` | 「常用操作」；「查看正在运行的程序」「查看日志」；进程/日志 OK 输出 |
| `r011-helper-denied.png` | 状态「已拒绝，未做任何改动」 |
| `r011-write-confirm.png` | 「对方想在这台电脑执行操作」；「允许」「拒绝」 |
| `r011-package-size.png` | FD 实测 MB + .NET 8 运行时人话 |
| `r011-osx-publish.png` | osx 体积；标明未上真机 |
| `r011-tarball.png` | Linux `.tar.gz` 路径与体积 |

父级取图：`%USERPROFILE%\Downloads\r011-shots\`。

### 本轮不做（已遵守）

- 未开 R012；未迁 Windows；未改中继/官网/AI-SSH-Pro；未做三档信任或 P2-7；未把「打开终端复制码」写给亲友。