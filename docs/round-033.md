# 第 33 轮 — Windows 接受协助端补「结束这次协助」

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**done（产品已验收）**
> 依据：R032 产品已验收（关窗会留在托盘，底栏不再重复说明）。Linux Avalonia 接受协助端已有「结束这次协助」；Windows 主界面只有「返回首页」，真正停掉协助要去托盘里找「退出」，亲友不容易把「关掉窗口」和「结束协助」分开。

关窗 = 藏到托盘，协助还在进行。这件事 R032 已经说清楚。接下来要补上另一半：想停的时候，窗口里就要有一处「结束这次协助」。

## 本轮目标

1. **高频**：Windows 接受协助端主界面增加一处「结束这次协助」（轻按钮，或放进「更多」）。点了之后结束本次协助：已连上的对方立刻连不上，也不再接受新的连接。
2. **打磨**：结束前先确认：「结束后对方将立刻连不上。」按钮：「结束这次协助 / 取消」。取消则什么都不做。

## A. 窗口里能结束（高频 · 鹊桥-客户端）

验收：

- Windows 接受协助端能看到「结束这次协助」，不必先打开托盘。
- 它不能做成实心主按钮。「复制码+密码」仍是该屏唯一实心主按钮。
- 关掉窗口仍然只是藏到托盘（R032 不变）：启动气泡原文不改，底栏默认仍空白，关窗后仍可出现一次性「已隐藏到托盘。双击托盘图标可重新打开。」
- 结束后对方连不上；如果当前窗口有「返回首页」，结束后回到首页（与 Avalonia 点「结束这次协助」同一意图）。不要只断开列表里某一位、却继续等人连上。
- 不要改 Avalonia 关窗提示「关闭窗口后，协助可能还在进行。要结束请点「结束这次协助」。」若 Avalonia 主界面已经有这颗按钮，不必重做，也不要改成实心主按钮。
- 不要改 R031 主状态与协助方列表、不要改 Skill / MCP / InstallInstructions、不要改 docs/trust-levels.md 的「才问你」、不要改 R032 已锁定的两句启动气泡。
- 截图放到 docs/screenshots/r033-*（至少：能看到「结束这次协助」且「复制码+密码」仍是唯一实心主按钮；确认窗原文；取消后仍在接受协助）。可用 ui-shot，须标明。**勿覆盖** r022-*～r032-*。

## 打磨：结束前确认

确认原文：

结束后对方将立刻连不上。

按钮：

结束这次协助
取消

超时不要自动结束。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 替换公网安装包
- 把未上真机的 macOS 包写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS
- 改信任等级「才问你」
- 改 R031 主状态、改 R032 托盘气泡与默认空底栏
- 改 Avalonia 关窗提示原文

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮


## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- 新增 RemoteAssist.Protocol.EndAssistCopy：ButtonText / ConfirmBody / ConfirmOk / ConfirmCancel
- Windows EndAssistConfirmDialog：正文「结束后对方将立刻连不上。」；按钮「结束这次协助 / 取消」；AcceptButton/CancelButton 均为取消（超时/回车不自动结束）
- Windows MainForm：轻按钮「结束这次协助」（白底描边，非实心）；确认后 DisconnectAllSessions + Stop；有返回首页则 HomeRequested，否则 ExitRequested
- AgentDesktopUi 接线 MainForm.ExitRequested
- 「复制码+密码」仍唯一实心主按钮；R032 关窗藏托盘 / 气泡 / 默认空底栏未改；未改 Avalonia 关窗提示与已有结束按钮；未改 R031 / Skill / MCP / InstallInstructions / trust-levels；未换公网包；未开下一轮

### 测试
- RemoteAssist.Protocol.Tests：**185** 通过

### 截图（均为 **ui-shot**）
| 文件 | 显示 | MD5 |
|------|------|-----|
| docs/screenshots/r033-accept-end-assist-entry.png | 轻按钮「结束这次协助」+ 实心「复制码+密码」 | AB25E54689FF5D95ACB5535C4736D045 |
| docs/screenshots/r033-accept-end-assist-confirm.png | 确认窗正文与两按钮（屏幕合成） | 202CD344237A3171A97D93AD112793D3 |
| docs/screenshots/r033-accept-end-assist-cancelled.png | 取消后仍在接受协助界面（会话码已刷新以区分 entry 像素） | AE8035C234B5DACC4B71EDBAC49AFD5A |

### 缺口 / 披露
- 确认窗截图为非模态 Show + 屏幕合成（便于同屏验收），真实交互为 ShowDialog。
- 取消构图与 entry 同属接受协助屏；为满足三张 MD5 不同，取消后刷新了会话码再拍。

## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 docs/screenshots/r033-* 与源码独立验收通过（3b01e7f）。

独立核对：

- Windows 接受协助端可见轻按钮「结束这次协助」（非实心）；「复制码+密码」仍是唯一实心主按钮（r033-accept-end-assist-entry.png）。
- 确认窗正文「结束后对方将立刻连不上。」；按钮「结束这次协助 / 取消」（r033-accept-end-assist-confirm.png）。取消后仍在接受协助界面（r033-accept-end-assist-cancelled.png）。
- EndAssistConfirmDialog：AcceptButton/CancelButton 均为取消，超时/回车不自动结束。确认后 DisconnectAllSessions + Stop，有返回首页则回首页。
- R031 主状态、R032 托盘气泡/默认空底栏、Avalonia 关窗提示、Skill/MCP/InstallInstructions/trust-levels「才问你」未改。未覆盖 r022-*～r032-*。

缺口：确认窗为屏幕合成非模态示意；取消帧刷新了会话码以区分像素。公网安装包仍为 R030（交 R034）。

说明：并行例行曾误把 R033 标题写成「官网安装包跟上」；以本文件为准——R033 是 Windows「结束这次协助」，官网换包开为 R034。

已开 R034（官网 Windows / Linux 安装包跟上含 R031–R033 的客户端；替换后首页旁注与导语不回退；不改中继/AI-SSH-Pro）。
