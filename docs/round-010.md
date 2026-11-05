# 第 10 轮 — Mac/Linux Avalonia 被控窗

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（Avalonia 被控窗 / 发布小包；不改 `relay/`，不把 Windows WinForms 迁到 Avalonia，不重做 R006–R009）
> 状态：**done**（2026-11-05 Asia/Shanghai）
> 依据：docs/PRODUCT.md「支持平台」第一步的 GUI；docs/architecture.md §13（先 CLI/MCP + 确认，再补对等 GUI）；docs/mac-linux-scenes.md；被控窗文案见 `docs/first-run-mac-linux.md` 第 1、2 节（直接用字，不扩大本轮范围）。

R009 已产品验收 Linux 被控 CLI + MCP + 终端确认 + 框架依赖小包（`ef0948a`）。本轮只补亲友/客户的主路径：Avalonia 被控状态窗。同事运维仍可用 R009 的 CLI，不要把「打开终端复制码」写给亲友和客户。

## 本轮目标

1. **高频**：在 Linux（验收基准）跑通 Avalonia **被控窗**：经默认公网中继展示并复制会话码+验证密码；访客配对确认和写操作确认走弹窗（不再用终端输入当主路径）；窗口上有「结束这次协助」；关窗不等于结束协助。
2. **打磨**：默认仍发 **framework-dependent** 小包，记录 Avalonia 被控窗实测体积；附上与 `docs/first-run-mac-linux.md` 第 5 节一致的运行时人话。不要默认打出自包含大包。

## A. Avalonia 被控窗（高频）

验收（文案直接用 `docs/first-run-mac-linux.md` 第 1、2 节与 `docs/mac-linux-scenes.md`，不另起炉灶）：

- 用 Avalonia 做 **被控状态窗**（只做接受协助 / 被控一侧，不做「去协助别人」对等界面），能在 **Linux** 上启动并连默认公网中继（`121.196.45.81:17891`）。
- 主按钮是「复制码+密码」，复制内容与 Windows 相同：两行「会话码：… / 验证密码：…」。不要让亲友/客户去终端里手选。
- 品牌是「鹊桥」。亲友可见界面不出现 Assist、Admin、ownerLink、TLS、端口号。
- 访客配对确认沿用 Windows：「有人想协助这台电脑」；「允许协助 / 拒绝」；「若不操作，将在 30 秒后自动拒绝。」
- 写操作确认走弹窗（不是终端输入），与 Windows 已验收原文对齐：标题「对方想在这台电脑执行操作」；人话是主说明，命令原文放在「命令：」后面；按钮「允许 / 拒绝」；「若不操作，将在 60 秒后自动拒绝。」超时不自动允许；拒绝后「已拒绝，未做任何改动」。MCP 不能绕过。不要用 osascript、zenity 等外部脚本当作正式确认方案。
- 收尾：窗口上有明确的「结束这次协助」。关闭窗口时提示：「关闭窗口后，协助可能还在进行。要结束请点「结束这次协助」。」
- 刷新会话码后，旧码旧密失效。
- Windows 协助端用现有「去协助别人」经同一中继能连上，完成至少只读（查看正在运行的程序、查看日志）；写操作仍要在这个 Avalonia 窗里确认。
- 同一套也要能发布 `osx-x64` / `osx-arm64`；若没有 macOS 真机，注明「已出包、未上真机」，不要把本轮写成 Mac 已验收。
- 信任等级本轮不新做三档界面，默认「每次询问」即可。不要把 P2-7（「结束完全信任」）拆进本轮，也不要重做 R007 的 WinForms 三档界面。

截图放到 `docs/screenshots/r010-*`（至少包括：被控窗主界面与「复制码+密码」、配对确认、写操作确认、拒绝未执行、关窗提示或「结束这次协助」、协助端只读成功；若有发布产物，再加体积截图）。

## B. 框架依赖小包（打磨）

- 默认发布 framework-dependent 包，记录 Avalonia 被控窗实测体积。Avalonia 会比 R009 的 CLI 大，但仍争取个位数 MB，不要默认打进 .NET 8 运行时。
- 附简短人话，与第 5 节一致：「这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。」
- 自包含包本轮不必做；若顺手打一份，体积上限约 72 MB，且不得替换默认小包。
- 不要用 .NET Framework 4.8 当作跨平台缩体积方案。

## 本轮不做

- Mac/Linux 双模式 GUI（接受协助 / 去协助，路线图平台第二项，本轮之后）
- 把 Windows 客户端迁到 Avalonia，或重做 R006–R009 的 WinForms / CLI
- 改中继、改 AI-SSH-Pro、完整远程桌面
- 官网「即将提供」改成真实下载地址
- 信任等级三档界面或 P2-7「结束完全信任」（沿用默认每次询问；P2-7 仍是 Windows 文案跟进 backlog）
- 把「打开终端复制码」写进亲友/客户主路径

## 完成时

- 更新本文交付说明、`docs/roadmap.md`（本轮标 done；平台「Mac/Linux 被控端 + MCP」在 Avalonia 被控窗验收通过后才可记为该项完成，双模式 GUI 仍 todo）
- 截图放到 `docs/screenshots/r010-*`
- 回告产品验收；未开下一轮


## 交付说明（2026-11-05 Asia/Shanghai）

### 状态

**done**（Linux Avalonia 被控窗 + 弹窗确认 + framework-dependent 小包；未开 R011 / 双模式 GUI）

### 代码

- 新增 `src/RemoteAssist.Agent.Avalonia`（`net8.0` + Avalonia 11.3）：只做接受协助 / 被控一侧。
- 主按钮「复制码+密码」；窗上「结束这次协助」；关窗提示原文：「关闭窗口后，协助可能还在进行。要结束请点「结束这次协助」。」
- 访客配对弹窗：「有人想协助这台电脑」/「允许协助」/「拒绝」/「若不操作，将在 30 秒后自动拒绝」。
- 写操作弹窗：标题「对方想在这台电脑执行操作」；人话 +「命令：…」；「允许」/「拒绝」；「若不操作，将在 60 秒后自动拒绝。」；拒绝后「已拒绝，未做任何改动」。超时不自动允许；MCP 不能绕过（仍走 ConfirmPrompt 队列）。
- 文案对齐 `docs/first-run-mac-linux.md` 第 1、2 节与 `docs/mac-linux-scenes.md`；品牌「鹊桥」。亲友主路径不写「打开终端复制码」。
- 发布脚本 `scripts/publish-agent-avalonia.ps1`：默认 framework-dependent `linux-x64` / `osx-x64` / `osx-arm64`，附 `运行说明.txt`（第 5 节人话）。
- 联调钩子（非正式产品路径）：`--test-allow-pair` / `--test-deny-write`。
- 不改 `relay/`；不迁 Windows WinForms；不重做 R006–R009；无 Avalonia「去协助」；无三档信任 / P2-7。

### 实测体积（framework-dependent，不含 pdb）

| 产出 | 合计 |
|------|------|
| `artifacts/agent-avalonia-linux-x64-fd` | 18.88 MB |
| `artifacts/agent-avalonia-osx-x64-fd` | 26.37 MB |
| `artifacts/agent-avalonia-osx-arm64-fd` | 26.38 MB |

（Avalonia + Skia 原生库占大半；仍小于自包含约 72 MB 档。人话：「这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。」）

### 联调

- Linux box（DISPLAY）跑通 Avalonia 被控窗，经默认公网中继 `121.196.45.81:17891`。
- Windows 协助端只读：`ListProcesses`、`ReadLogTail`（`/tmp/r010-demo.log`）成功。
- 写操作 `RunCommand` 拒绝 → 协助端 `已拒绝，未做任何改动`；本机 `/tmp/r010-deny-marker` 不存在。
- 联调使用 `--insecure`（对齐协助端无指纹粘贴）+ 联调钩子；默认发布仍为 TLS；正式确认路径是 Avalonia 弹窗（见 ui-shot 截图）。

### osx

**已出包、未上真机**（见 `r010-osx-publish.png`）。不要把本轮写成 Mac 已验收。

### 测试

- 全量 `dotnet test`：通过 167 / 总计 167。

### 截图 / 复核

| 文件 | 屏幕上可见原文（要点） |
|------|------------------------|
| `r010-agent-main.png` | 「鹊桥远程协助」；「复制码+密码」；「结束这次协助」；会话码/验证密码 |
| `r010-pair-confirm.png` | 「有人想协助这台电脑」；「允许协助」/「拒绝」；「若不操作，将在 30 秒后自动拒绝」 |
| `r010-write-confirm.png` | 「对方想在这台电脑执行操作」；「允许」/「拒绝」；「若不操作，将在 60 秒后自动拒绝」 |
| `r010-write-denied.png` | 「已拒绝，未做任何改动」 |
| `r010-close-warn.png` | 「关闭窗口后，协助可能还在进行。要结束请点「结束这次协助」。」 |
| `r010-helper-readonly.png` | PROCS/LOGS ok；RUN denied=`已拒绝，未做任何改动` |
| `r010-package-size.png` | 实测 MB + .NET 8 运行时人话 |
| `r010-osx-publish.png` | osx 包体积；标明未上真机 |

父级取图：`%USERPROFILE%\Downloads\r010-shots\`。

### 本轮不做（已遵守）

- 未开双模式 GUI / R011；未迁 Windows；未改中继/官网/AI-SSH-Pro；未做三档信任或 P2-7；未把「打开终端复制码」写给亲友。
