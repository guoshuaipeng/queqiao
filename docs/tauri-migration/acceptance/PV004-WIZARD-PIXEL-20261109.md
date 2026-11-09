# PV-004 向导可视像素补证 · 2026-11-09

## 判定

**像素补证（中文信息第一页 / 显式 LANG）：PASS（父代理已目视）**

- 候选 PNG 已经 CopyToBox 到 box：`/workspace/PV004-WIZARD-zh-CN-20261109-screen.png`（经 `C:\Users\user\agent-tools\` 中转；直读 `E:\IdeaProjects\...` 曾被 local-exec 根目录拒绝）。
- 父代理 2026-11-09 08:59 Asia/Shanghai 亲自 Read 目视：标题「安装 - 鹊桥远程协助 0.1.0 (net48)」；页眉「信息」；正文「请在继续安装前阅读以下重要信息。」「准备好继续安装后，点击“下一步”。」；信息区含 .NET Framework 4.8 / RemoteAssist.Net48.exe / RemoteAssist.Mcp.exe 说明；按钮「下一步(N)」「取消」可读；非黑图、非锁屏 Spotlight。
- 同刻 UIA 与画面一致（见 `PV004-WIZARD-zh-CN-20261109-uia.txt`）。
- PrintWindow flags=0/2 黑图仍无效，仅作反例；有效证据为 CopyFromScreen 屏矩形图。

**未宣称**：系统语言自动跟随（本次为显式 `/LANG=chinesesimplified`，UICulture=en-US）；英文向导页；点「下一步」后的后续页；完整安装流程。UIA 语言项历史 PASS 不因本文件重开。

## 环境与包

- 捕获时间：2026-11-09 08:34:54 Asia/Shanghai（UTC+8）；目视复核：2026-11-09 08:59 Asia/Shanghai。
- 机器：DESKTOP-J9F0EKT，machineId 48ac8334-4d7a-4bfd-8f6c-f2820d793124；connected=true。
- 输入桌面：Default；`query user`：console / Active；LogonUI 未列出；LockApp 仍存在，但不单独判锁屏。
- UICulture：en-US；启动参数：`/LANG=chinesesimplified`。
- 权威包：`E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Net48-Setup-0.1.0.exe`
- SHA256：`71CE3908300A804697502D80361B4FF9476E35F0F0B04A2286E70FA58DF39239`（与历史一致）。

## 操作范围

仅启动向导、停在信息第一页、捕获后 WM_CLOSE；未点下一步、未安装、未改 iss / 业务码、未 push、未上公网。

## 自检

| 项 | 状态 |
| --- | --- |
| 可辨认向导 PNG（非黑/非锁屏） | PASS（父代理目视） |
| 中文信息第一页文案可读 | PASS |
| 同刻 UIA 交叉 | PASS |
| 伪完整 PV-004 / 自动语言 | 否（明确未宣称） |

保留 `PV004-WIZARD-PIXEL-20261107.md` 锁屏阻塞历史，勿回滚为缺口。
