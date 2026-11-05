# 第 32 轮 — 关闭窗口后留在托盘的说明只留一处

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**delivered**
> 依据：R031 产品已验收。`docs/quality.md` 仍有效的统一规则：被协助端「关闭窗口后继续在托盘运行」如果提示框和底栏各写一次，只留一处。

Windows 接受协助端现在会把同一件事说两遍：启动时托盘气泡说「关闭主窗口会留在托盘 / 关闭主窗口不会退出」；窗口底栏默认又写「就绪。关闭窗口后会继续在托盘运行。」亲友开屏先看到一句常驻提醒，过两秒气泡再重复一遍。

## 本轮目标

1. **高频**：Windows 接受协助端「关窗后还会在托盘里」只留一处。产品拍板：启动时的托盘气泡保留；窗口底栏默认不再显示「就绪。关闭窗口后会继续在托盘运行。」
2. **打磨**：用户自己点关闭、窗口藏到托盘之后，底栏可以出现一次「已隐藏到托盘。双击托盘图标可重新打开。」这是刚发生的操作反馈，不是开机默认重复。

## A. 底栏默认不再重复托盘说明（高频 · 鹊桥-客户端）

验收：

- Windows 接受协助端打开后，底栏默认空白，或不写「关闭窗口后会继续在托盘运行」这类常驻句。
- 启动时托盘气泡仍可保留一句，推荐沿用已有口径：「已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。」（有「返回首页」时，也可以继续用现在的「关闭主窗口会留在托盘。」）不要新造第三种说法。
- 不要改 Avalonia 关窗提示「关闭窗口后，协助可能还在进行。要结束请点「结束这次协助」。」（`first-run-mac-linux.md` 第 2 节原文；Linux 关窗语义不同，不是本轮要收的重复句）。
- 不要改 R031 已验收的主状态（「对方已连上。改动这台电脑前会先问你。」/「已连上网络，可以发给对方了」）、不要改 Skill / MCP / `InstallInstructions`、不要改 `docs/trust-levels.md` 的「才问你」。
- 「复制码+密码」仍是该屏唯一实心主按钮。
- 截图放到 `docs/screenshots/r032-*`（至少：Windows 刚打开时底栏不再出现那句常驻说明；点关闭后底栏出现一次性「已隐藏到托盘…」——若关窗即藏到托盘、无法同屏展示，用打开后的底栏空态 + 文字说明即可）。可用 ui-shot，须标明。**勿覆盖** `r022-*`～`r031-*`。

## 打磨：关掉窗口后的一次性反馈

用户点窗口关闭、界面藏到托盘后，底栏可以出现一次：

已隐藏到托盘。双击托盘图标可重新打开。

不要在默认底栏里预写这句。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 替换公网安装包
- 把未上真机的 macOS 包写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS
- 改信任等级「才问你」
- 改 R031 主状态与协助方列表
- 改 Avalonia 关窗提示原文

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮


## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- 新增 `RemoteAssist.Protocol.TrayStayCopy`：
  - `BalloonDefault` / `BalloonWithReturnHome`（沿用原有两句，无第三种说法）
  - `HiddenToTrayFeedback` =「已隐藏到托盘。双击托盘图标可重新打开。」
  - `RemovedDefaultStatusBar` = 原底栏常驻句（仅回归对照，不再作默认底栏）
- Windows `MainForm`：底栏默认 `Text = ""`，不再写「就绪。关闭窗口后会继续在托盘运行。」
- `OnFormClosing` 藏托盘后 `SetStatus(TrayStayCopy.HiddenToTrayFeedback)`（一次性反馈）
- `AgentTrayContext` / `AgentDesktopUi` 启动气泡改为 `TrayStayCopy.BalloonFor(enableReturnHome)`
- 未改 Avalonia 关窗提示；未改 R031 主状态/协助方列表；未改 Skill/MCP/`InstallInstructions`/`trust-levels`；未换公网包；未开下一轮

### 测试
- `RemoteAssist.Protocol.Tests`：**183** 通过（含 TrayStayCopy 3 项）

### 截图（均为 **ui-shot**）
| 文件 | 显示 | MD5 |
|------|------|-----|
| `docs/screenshots/r032-accept-statusbar-empty.png` | 接受协助就绪态；底栏空白；无「就绪。关闭窗口后会继续在托盘运行。」；「复制码+密码」仍是唯一实心主按钮 | EFD52E073BDC2A2DD0881E5E6ADD9A12 |
| `docs/screenshots/r032-accept-hidden-to-tray-feedback.png` | 底栏一次性「已隐藏到托盘。双击托盘图标可重新打开。」（窗仍可见便于截图；非默认底栏） | 07B44EC33CBA1C49D205B4BD7CD4DD9C |

### 启动气泡（未能真拍 NotifyIcon）
ui-shot / DrawToBitmap 无法截取系统托盘气泡。证明方式：
1. 气泡文案常量即原句：`TrayStayCopy.BalloonDefault`、`BalloonWithReturnHome`（测试锁死原文）
2. `AgentTrayContext` 启动时调用 `TrayStayCopy.BalloonFor(enableReturnHome)`，未另造第三种说法
3. 底栏已去掉与气泡重复的常驻句（见上表第一张）

### 缺口 / 披露
- 未实拍启动气泡本体；以常量+代码接线证明未改气泡口径。
- 「已隐藏到托盘…」反馈图为关窗逻辑同款文案的 ui-shot 注入（窗未真正 Hide），便于同屏验收底栏。
