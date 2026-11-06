# 鹊桥 — 路线图（产品优先级）

> 以 `docs/PRODUCT.md` 为准。改优先级先改本文档再 @ 实现 bot。  
> 状态：`done` / `wip` / `todo`（中继侧进度经本机 `go test ./internal/relay` 核对，2026-11-05）。  
> **持续迭代**：每轮见 `docs/round-NNN.md`；复盘见 `docs/retro.md`。产品 bot 驱动选题与验收，实现 bot 写代码。  
> 实现分工：`鹊桥-客户端`（src / UI / MCP / 安装包）、`鹊桥-服务端`（relay / 部署）。

## 当前迭代

| 轮次 | 项 | 负责 | 状态 | 文档 |
|------|----|------|------|------|
| R001 | 被控「一键复制码+密码」+ 品牌文案「鹊桥」 | 鹊桥-客户端 | **done** | `docs/round-001.md` |
| R002 | 高频：ownerLink 专项验收；打磨：品牌扫尾 + 首页文案（体验 1/2/4） | 鹊桥-客户端 | **done** | `docs/round-002.md` |
| R003 | 高频：连不上引导 + 联网允许；打磨：首屏减负、一次粘贴、协助端空态/默认中继、访客配对窗白话 | 鹊桥-客户端 | **done** | `docs/round-003.md` |
| R004 | 高频：写操作确认白话（P0 收尾）；打磨：一键复制安装说明（P2-1） | 鹊桥-客户端 | **done** | `docs/round-004.md` |
| R005 | 高频：界面上的 MCP 安装指引；打磨：已连网收起允许横幅、避免「设置」折行 | 鹊桥-客户端 | **done** | `docs/round-005.md` |
| R006 | 质感：刷新会话码不抢点击 → 账号标签与免密选设备 → 连上后白话操作 → 首页卡片（见 quality.md） | 鹊桥-客户端 | **done**（产品已验收） | `docs/quality.md`、`docs/round-006.md` |
| R007 | 被协助端信任等级（每次询问 / 宽松 / 完全信任）+ 打磨本机已确认改动回看 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-007.md`、`docs/trust-levels.md` |
| R008 | 高频：MCP 提议命令闭环；打磨：双模式安装包扫尾 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-008.md` |
| R009 | 高频：Mac/Linux 被控 CLI + MCP + 本机确认；打磨：框架依赖小包 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-009.md` |
| R010 | 高频：Mac/Linux Avalonia 被控窗；打磨：Avalonia 框架依赖小包实测 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-010.md` |
| R011 | 高频：Mac/Linux Avalonia 双模式（去协助）；打磨：框架依赖小包实测（顺手 Linux .tar.gz） | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-011.md` |
| R012 | 高频：Avalonia 被控端信任等级；打磨：Windows P2-7「结束完全信任」文案 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-012.md` |
| R013 | 高频：Avalonia「本次协助已确认的改动」；打磨：刷新会话码先确认 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-013.md` |
| R014 | 高频：Avalonia 账号登录免密连自有设备；打磨：最近审计人话 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-014.md` |
| R015 | 高频：Avalonia 设备列表人话（隐藏内部 id）+ 免密连接可点击态；打磨：免密连上后常用操作可用 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-015.md` |
| R016 | 高频：Avalonia 协助端连上后五键白话 + 引导 + 已连接按钮权重；打磨：文件夹/文件路径轻量输入 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-016.md` |
| R017 | 高频：Avalonia MCP 安装说明；打磨：复制后人话反馈 | 鹊桥-客户端 | **done**（产品已验收） | `docs/round-017.md` |
| R018 | 高频：接受协助端可选 MCP 说明；打磨：复制 MCP 配置后人话反馈 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-018.md` |
| R019 | 高频：官网 Windows / Linux 安装包下载；打磨：默认小包的 .NET 8 运行时短说明 | 鹊桥-服务端 | **done（产品已验收）** | `docs/round-019.md` |
| R020 | 高频：官网 Linux 包跟上 R015–R018；打磨：首页不再用「以 Windows 为主」否定 Linux | 鹊桥-客户端 / 鹊桥-服务端 | **done（产品已验收）** | `docs/round-020.md` |
| R021 | 高频：复制安装说明带上官网下载地址；打磨：复制后人话反馈点明下载地址 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-021.md` |
| R022 | 高频：官网 Windows / Linux 安装包跟上含 R021 的客户端；打磨：安装说明「允许连网」跨平台中性措辞 | 鹊桥-客户端 / 鹊桥-服务端 | **done（产品已验收）** | `docs/round-022.md` |
| R023 | 高频：安装说明补 .NET 8 运行时短指引；打磨：官网 Linux 旁注对齐 + 补拍 R022 公网下载截图 | 鹊桥-客户端 / 鹊桥-服务端 | **done（产品已验收）** | `docs/round-023.md` |
| R024 | 高频：官网 Windows / Linux 安装包跟上含 R023 的客户端；打磨：替换后首页运行时旁注与导语不回退 | 鹊桥-客户端 / 鹊桥-服务端 | **done（产品已验收）** | `docs/round-024.md` |
| R025 | 高频：安装说明补「通常不会下载 / 不认识这个程序」时怎么选；打磨：官网 Windows 旁注对齐 | 鹊桥-客户端 / 鹊桥-服务端 | **done（产品已验收）** | `docs/round-025.md` |
| R026 | 高频：官网 Windows / Linux 安装包跟上含 R025 的客户端；打磨：替换后首页旁注与导语不回退 | 鹊桥-客户端 / 鹊桥-服务端 | **done（产品已验收）** | `docs/round-026.md` |
| R027 | 高频：去协助端复制 Skill 说明（仅安装 MCP）；打磨：复制后人话反馈 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-027.md` |
| R028 | 高频：接受协助端复制 Skill 说明（复用 R027 正文）；打磨：复制后人话反馈 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-028.md` |
| R029 | 高频：Windows 接受协助端菜单补复制 Skill 说明；打磨：复制后人话反馈 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-029.md` |
| R030 | 高频：官网 Windows / Linux 安装包跟上含 R027–R029 的客户端；打磨：替换后首页旁注与导语不回退 | 鹊桥-客户端 / 鹊桥-服务端 | **done（产品已验收）** | `docs/round-030.md` |
| R031 | 高频：接受协助端对方连上后主状态改为「对方已连上」；打磨：断开后回到等待句 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-031.md` |
| R032 | 高频：关闭窗口后留在托盘的说明只留一处；打磨：关窗后底栏一次性反馈 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-032.md` |
| R033 | 高频：Windows 接受协助端补「结束这次协助」；打磨：结束前确认 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-033.md` |
| R034 | 高频：官网安装包跟上含 R031–R033 的客户端；打磨：替换后首页旁注与导语不回退 | 鹊桥-客户端 / 鹊桥-服务端 | **done（产品已验收）** | `docs/round-034.md` |
| R035 | 高频：去协助端对方结束协助后主状态改为「对方已结束这次协助。」；打磨：未连上失败与自己断开仍用原文 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-035.md` |
| R036 | 高频：去协助端连上后「连接」改为不可点的「已连接」；打磨：结束后恢复实心「连接」 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-036.md` |
| R037 | 高频：去协助端已连上后不再显示「粘贴后点「连接」」；打磨：结束后这句提示再出现 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-037.md` |
| R038 | 高频：官网安装包跟上含 R035–R037 的客户端；打磨：首页旁注不改、补公网下载截图 | 鹊桥-客户端出包 / 鹊桥-服务端上架 | **done（产品已验收）** | `docs/round-038.md` |
| R039 | 高频：Avalonia 去协助端连上后五个常用操作出现在首屏；打磨：未连上时五键仍隐藏 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-039.md` |
| R040 | 高频：官网安装包跟上含 R039 的客户端；打磨：首页旁注不改、补公网下载截图 | 鹊桥-客户端出包 / 鹊桥-服务端上架 | **done（产品已验收）** | `docs/round-040.md` |
| R041 | 高频：S004「提议一项改动」等待/结果四态人话；打磨：连上后结果区去掉 PAIR_OK | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-041.md` |
| R042 | 高频：官网安装包跟上含 R041 的客户端；打磨：首页旁注不改、补公网下载截图 | 鹊桥-客户端出包 / 鹊桥-服务端上架 | **done（产品已验收）** | `docs/round-042.md` |
| R043 | 高频：Windows net48 主路径对齐（会话码+公网中继、去局域网主表单、品牌鹊桥、被控首启人话、**连接凭证一次粘贴/8位验证码**）；打磨：Avalonia/CLI 凭证同步或 S005，产能紧则 R044 | 鹊桥-客户端 | **PASS_WITH_NOTES**（编译/文档；真连 blocked） | `docs/round-043.md`、`docs/quality-connection-credential.md` |
| R044 | 高频：完整连接凭证**逐字文案**扫尾（亲友 UI 无指纹/TLS 技术词）；Avalonia/CLI 本轮未改（完整载荷未移植） | 鹊桥-体验（规格已定）/ 鹊桥-客户端（实现） | **PASS_WITH_NOTES**（编译/文档 `7cad0ab`；运行 blocked） | `docs/round-044.md`、`docs/quality-connection-credential.md` |
| R045 | 高频：亲友失败文案残留（码+密码→完整连接凭证）+ Helper 缺安全验证提示收窄（仅缺 fp/短凭证）+ 文档状态行清理；稳定性/UX，不扩功能 | 鹊桥-客户端 | **PASS_WITH_NOTES**（编译/文档 `39d6dc1`；运行 blocked） | `docs/round-045.md` |
| R046 | 高频：中继 SQLite 会话码审计表（生成/join 成败/结束）+ 鉴权只读管理列表；禁记禁展 pwd/hash/fp/完整凭证；不改 guest_*；与 site 接单分离 | 鹊桥-服务端（主） | **open（规格已拍板）** | `docs/round-046.md`、`docs/relay-audit.md` |
| R047 | 高频：Windows **net48 Framework 4.8 依赖小 Setup**（Inno/MSI；非几十兆自包含）；打磨：安装说明须写 **Framework 4.8**（改掉仍写「.NET 8 运行时」的 `WriteConfirmCopy`，须客户端授权）；官网上传可选另轮 | 鹊桥-客户端（主） | **compiled（安装向导/官网未验）** | `docs/round-047.md` |
| R048 | 高频：net48 **去协助 / Helper** 补「复制安装说明」+ MCP「复制 MCP 配置」JSON（对齐 net8/Avalonia）；打磨：缺完整凭证阻断、禁明文降级 | 鹊桥-客户端（主） | **PASS_WITH_NOTES**（编译/文档 `2d427d4`；运行 blocked） | `docs/round-048.md` |
| R049 | 高频：首页与「去协助/接受协助」**单主窗口**页内切换（不叠窗、保位置/大小；有会话返回/切角色须本机确认结束；安全确认窗不在合并范围）；打磨：中继地址仅高级设置可改、默认内置（与 PAKE/**R050** **独立**，可另授权） | 鹊桥-客户端（主） | **draft / 待用户确认交互**（体验草案已出；未确认前不开 UI 实现） | `docs/round-049.md`、`docs/quality-single-window-codes-draft.md`（**待确认**） |
| R050 | 高频：亲友主路径**短码**安全可连 + **严格唯一凭证**（code+8位密码；取消账号免密/`connect_device`/`ownerLink` 与设备密钥快捷连；自有设备也输入；服务端不知明文密码；成熟 PAKE） | 架构（PAKE 正式评估）→ 产品对照验收 → **授权后**实现 | **blocked**（决策已锁定；**评估收口已入库** 2026-11-06 见 `pake-unique-credential-eval.md` §11；**仍未授权实现**；勿宣称现网已安全满足） | `docs/round-050.md`、`docs/PRODUCT.md`、`docs/pake-unique-credential-eval.md` §11 |

> **当前**：并行——**R046** open；**R047** compiled（Net48-Setup 已出；安装向导/官网未验）；**R048** compiled（运行未验收）。**R049** draft / **待用户确认**（交互见 `quality-single-window-codes-draft.md`；确认前不开 UI）。**R050** **blocked**（**严格唯一凭证已拍板**；架构评估收口已入库 2026-11-06/`pake-unique-credential-eval.md` §11；**仍未授权实现**；**不得**宣称现网已安全满足；见 `round-050.md`）。R045/R044 运行仍 blocked；R042 已产品验收。
>
> **Windows 主维护锁定（2026-11-06）**：亲友主路径 = **net48 Framework 4.8 依赖小包**；**冻结 `src/` net8 WinForms 功能**（仅安全/协议关键回补）。**net8 上已迭代且仍属产品意图的能力须全量迁 net48（能力对等）**；已知 backlog：信任三档、「结束这次协助」、已确认改动回看、`WriteConfirmCopy` 文案/安装前提；**不迁账号免密**（已废止）。**不为对等回填 net8**；**Avalonia = Mac/Linux**，非 Windows 亲友主路径。

> R041 已产品验收（`aeafeeb`，2026-11-06）；R042 已产品验收（公网 Win 75,587,213 / Linux 8,005,849）。

## P0 — 可演示闭环（不后挪）

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 公网中继访客配对（code+密码）+ 透明转发 | 鹊桥-服务端 | **done** | 两机经中继可配对；协议与 `relay-protocol.md` 一致；`go test ./internal/relay` PASS |
| 同一 App 双模式 UI + 安装包 | 鹊桥-客户端 | **done（R008）** | 安装后可选接受/去协助；被控展示 code+密码；见 `round-008.md` |
| 业务 TLS+指纹；写操作本机确认 | 鹊桥-客户端 | **done（R004 文案）** | Assist 写命令弹确认；拒绝则不执行。验收文案见 PRODUCT.md：对方想在这台电脑执行操作 + 白话说明 + 允许/拒绝；超时不自动允许；拒绝后提示「已拒绝，未做任何改动」 |
| 客户端/MCP 对接 JSONL 中继（访客 code+密码） | 鹊桥-客户端 | **done** | RelayJsonlIntegrationTests + 本机 relay smoke；MCP --relay/--code/--pwd |
| MCP stdio 连会话并调只读/提议命令 | 鹊桥-客户端 | **done（R008）** | Agent 能连上并完成至少：列进程、读日志、提议命令走确认；见 `round-008.md` / McpStdioLoopIntegrationTests |
| 被控一键复制「会话码+密码」纯文本 | 鹊桥-客户端 | **done（R001）**；主形态升级为连接凭证并入 **R043** | 见 `round-001.md`、`quality-connection-credential.md` |
| 界面品牌统一「鹊桥」 | 鹊桥-客户端 | **done（R001）** | 见 `round-001.md` |

## P1 — 账号与免密

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 中继：注册/登录/token、设备上线、列表、免密连接 | 鹊桥-服务端 | **done** | signup/login/device_online/list_devices/connect_device + 测试覆盖 |
| 客户端：注册/登录、设备上线、列表、免密连接 | 鹊桥-客户端 | **done（最小闭环）** | UI 登录/列表/免密 + MCP --token/--device-id；集成测 Account_Login_List_Connect |
| 自有设备跳过配对确认窗；写操作仍确认（S003） | 鹊桥-客户端 | **done（R002）** | 同账号连已上线设备：无配对确认窗；写操作仍确认；访客仍要配对确认；中继配对成功 ≠ 写权限 |
| 设置项：默认/自定义中继地址 | 鹊桥-客户端 | **done** | 默认 `121.196.45.81:17891`；UI/设置/MCP/`--relay` 可覆盖 |
| 连不上时的可执行引导（S002） | 鹊桥-客户端 | **done（R003）** | 见 `round-003.md`：错密码 / 已换新码 / 到不了中继 / 对方未开始接受协助；与首次「请点允许」合成。亲友主路径不写端口、不写「放行入站」 |

## 平台 — macOS / Linux（2026-11-05 起，不抢 P0）

场景闭环见 `docs/mac-linux-scenes.md`。R010 Linux Avalonia 被控窗已产品验收；osx 已出包未上真机，不写成 Mac 已验收；R011 Linux Avalonia 双模式已产品验收；osx 已出包未上真机，不写成 Mac 已验收；R012 已产品验收 Avalonia 信任等级 + Windows P2-7；R013 已产品验收 Avalonia 已确认改动回看 + 刷新确认；R014 已产品验收 Avalonia 账号登录免密 + 审计人话（edddc11，2026-11-05）；R015 已产品验收 Avalonia 设备列表人话与免密连接可点击态（e11a8a7，2026-11-05）；R016 已产品验收 Avalonia 协助端连上后五键白话对齐（f083f51，2026-11-05）；R017 已产品验收 Avalonia 去协助端 MCP / 复制安装说明（5bcab34，2026-11-05）；R018 已产品验收接受协助端可选 MCP 说明（`b8c215c`，2026-11-05）；R019 已产品验收官网 Windows / Linux 安装包下载（`04347f3`，2026-11-05；macOS 仍「即将提供」）；R020 已产品验收官网 Linux 包跟上 R015–R018（出包 `bafe80c`，上架 `8ce2bd9`，2026-11-05）；R021 已产品验收去协助端「复制安装说明」带上官网下载地址（`632928a`，2026-11-05）；R022 已产品验收（出包 `71893d2`，公网 Win/Linux 包字节与 SHA 已核对，2026-11-06）；R023 已产品验收（文案 `a5eb2df`，旁注 `3a0d53b`，2026-11-06）；R024 已产品验收（出包 HEAD `bf078ac` / 交付 `8cf2810`，上架 `1ee1e9c`，2026-11-06）；R025 已产品验收（文案 `f423db1`，旁注 `40cae49`，2026-11-06）；R026 已产品验收（出包 HEAD `73b2fea` / 交付 `62ac5ce`，上架 `a97a2f5`，2026-11-06）；R027 已产品验收（`65f2e2d`，2026-11-06）；R028 已产品验收（`9c07065`，2026-11-06）；R029 已产品验收（`5909d99`，2026-11-06）；R030 已产品验收（出包 HEAD `c7dc792` / 交付 `15e57ae`，上架 `9108924`，2026-11-06）；R031 已产品验收（`e40e70a`，2026-11-06）；R032 已产品验收（`b78fc1e`，2026-11-06）；R033 已产品验收（`3b01e7f`，2026-11-06）；R034 已产品验收（出包 HEAD `7bf7924`，上架 `b5f089d`，2026-11-06）；R035 已产品验收（`b707804`，2026-11-06）；R036 已产品验收（`d24fb44`，2026-11-06）；R037 已产品验收（`0405491`，2026-11-06）；R038 已产品验收（出包 `0efc3c4`，上架 `68c5abc`，2026-11-06）；R039 已产品验收（`5c276ec`，2026-11-06）；R040 已产品验收（出包 `49a4292`，上架 `2e51cea`，2026-11-06）；R041 已产品验收（`aeafeeb`，2026-11-06）；R042 已产品验收（出包 `8d61398`，上架 `f90bd8f`，2026-11-06）；R043 编译/文档 PASS_WITH_NOTES（`0122127`；真连 blocked）；R044 编译/文档 PASS_WITH_NOTES（`7cad0ab`；运行 blocked；见 `docs/round-044.md`）；R045 编译/文档 PASS_WITH_NOTES（`39d6dc1`；运行 blocked；见 `docs/round-045.md`）；R046 打开（规格已拍板：审计表+管理台只读；见 `docs/round-046.md`、`docs/relay-audit.md`）；R047 打开（net48 安装包；见 `docs/round-047.md`）；R048 编译/文档 PASS_WITH_NOTES（`2d427d4`；运行 blocked；见 `docs/round-048.md`）。

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 技术栈与体积方案 | 鹊桥-架构 | **done（2026-11-06 主维护锁定）** | Windows **主维护=net48 + Framework 4.8 小包**；冻结 net8 WinForms；**net8 已迭代能力迁 net48 对等**（信任三档/结束协助/已确认改动/WriteConfirmCopy；不迁免密）；Avalonia=Mac/Linux；见 PRODUCT |
| Mac/Linux 被控端 + MCP（先） | 鹊桥-客户端 | **done（Linux：R009+R010 产品已验收；osx 已出包未上真机，不写成 Mac 已验收）** | 经默认中继配对；只读查询；写操作本机确认；命令行（CLI/MCP）与 Avalonia 界面都要，不是二选一。R009 CLI + R010 Avalonia 被控窗已产品验收（Linux）。osx 已出包未上真机。默认发依赖 .NET 8 运行时的小包（附安装说明）；自包含为备选。双模式 GUI 见下一平台项 |
| Mac/Linux 双模式 GUI（后） | 鹊桥-客户端 | **done（Linux：R011 产品已验收；osx 已出包未上真机，不写成 Mac 已验收）** | 与 Windows 对等：接受协助 / 去协助；见 `round-011.md` |

## P2 — 体验（backlog，按易用性排序）

1. **P2-1** 协助方一键复制「安装包链接/目录说明」文案 → **done（R004）**
2. **P2-2** 首次启动防火墙/失败文案 → 已并入 R003（`round-003.md` 时刻 A）
3. **P2-3** 被控复制载荷/二维码体验 → 部分并入 R003（分享区只留主按钮，其余进「更多」）
4. **P2-4 / S001** 被控本机回看本次会话已确认的写操作审计（客户支持收尾；未确认的提议不得显示为已执行；不做云端工单）→ **done（R007）**
5. **P2-5** Skill 薄封装（仅安装 MCP 指引，非控制面）→ 界面三步指引 **done（R005）**；去协助端 **done（R027）**；Avalonia 接受协助端 **done（R028）**；Windows 接受协助端菜单 **done（R029）**；公网包 **done（R030）**
6. **P2-6 / 质感** 刷新不抢点击、账号标签与选机、协助端白话操作、首页可点卡片 → **done（R006）** 见 `round-006.md` / `quality.md`
7. **P2-7** 完全信任主界面操作改为「结束完全信任」+ 二次确认正文补全（见 trust-levels.md 2026-11-05 修正）→ **done（R012）**

## 官网与接单（低配机，轻量）

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 技术栈与部署（端口 80、安全组、独立 Go 进程） | 鹊桥-架构 | **done** | `architecture.md` §14 与 `docs/site-deploy.md`：`remote-assist-site`、:80、site.db、口令走 /etc/remote-assist-site.env、MemoryMax=64M |
| 静态介绍页 + 接单表单 | 鹊桥-服务端 | **done** | `site/` 静态页；文案对齐 `docs/site-copy.md` |
| 接单 API + /admin + 上机 | 鹊桥-服务端 | **done** | 2026-11-05 公网浏览器验收通过（对照 site.md / site-copy.md）：介绍页与 /ask 文案对齐；两次例行探活提交成功并到 /thanks（称呼 例行探活，微信 qiaoqiao_probe_20261105，非真客户勿跟进）；/admin 可登录、改状态/备注，同一微信聚合为一条客户（历史条数 2）。状态仅 新留言/已联系/已结束。中继 17891 仍通。 |
| 安装包下载（Windows / Linux 真实文件；macOS 仍即将提供） | 鹊桥-服务端 | **done（R019–R034）** | 见 `round-019.md`～`round-034.md`：首页可下 Windows / Linux 包；R034 公网包已换成含 R031–R033 的客户端（Windows 75,584,964 / Linux 8,002,690）；Mac 不假装已可下；不装重软件、不占 17891 |
## Tauri 窗口待办（2026-11-06，复用 CLIENT-UI，不新开能力）

| 优先级 | 项 | 用户价值 | 验收 | 证据 |
|---|---|---|---|---|
| 本轮完成 | 空闲时只显示双栏首页；会话/设置/弹窗互斥；弹窗有标题正文，默认拒绝 | 亲友打开不会看成已经连上 | 桌面初始画面无会话条、无设置、无空弹窗 | `acceptance/desktop-idle.png`；浏览器互斥检查失败项为空 |
| 本轮完成 | 窄于 760 时上下排列并可滚到「我帮别人」 | 小窗口仍能点连接 | 640×360 两栏同 x、右栏在下；滚 `#home` 后连接按钮进入视口 | `acceptance/desktop-narrow.png`；浏览器 `zones.scrollTop` 后按钮 bottom=254 |
| 本轮完成 | 中继端口没开时停在失败文案，按钮为「重试开启」，不进会话 | 亲友知道没连上，可以再试 | 桌面点开启后出现指定文案和「重试开启」，没有「等待对方连接」 | `acceptance/desktop-open-fail.png`；断言 retry/message 为真，会话文案不存在 |
| 本轮完成 | 等待中点「停止接受」回到空闲，迟到的连接进不来 | 亲友能取消等待，不留下还能进入的会话 | 停止后按钮回到「开启接受协助」；没有等待文案和已连接；另一窗口用原凭证连接失败 | `acceptance/desktop-before-stop.png`、`desktop-while-waiting.png`、`desktop-after-stop.png`；迟到连接没有配对弹窗，协助端显示没能连上 |
| 本轮完成 | 有人来连时点「拒绝」，仍停在等待，对方进不了会话 | 亲友可以拒绝一次连接，不因此被连上 | 出现「有人想协助这台电脑」后点「拒绝」；仍有「等待对方连接」和「复制连接凭证」；没有已连接。协助端显示「对方没有同意这次协助。」 | 本机 `127.0.0.1:17991` 点击断言；`acceptance/desktop-before-reject.png`、`desktop-after-reject-helper.png` |
| 本轮完成 | 窄于 760 时滚轮把「连接」滚进视野 | 小窗口不用去拖滚动条也能点连接 | 客户区 640×360 时「连接」先在客户区下方，滚轮后进入客户区 | `acceptance/desktop-narrow-before-wheel.png`、`desktop-narrow-after-wheel.png`；按钮从客户区外移到区内 |
| 本轮完成 | 协助端点「取消」后双方都不进会话，被协助端仍在等待 | 连到一半可以停，不会因此被连上 | 弹窗出现后点「取消」；弹窗消失；仍有「等待对方连接」；没有已连接。协助端回到「连接」，没有「取消」 | 本机 `127.0.0.1:17991`；`acceptance/desktop-before-cancel.png`、`desktop-after-cancel-helper.png` |
| 本轮完成 | 配对弹窗不把后面的连接原文透出来 | 旁人看不到凭证 | 弹窗打开时界面树里没有凭证原文，画面是实底遮罩加弹窗 | `acceptance/desktop-cancel-dialog.png`；断言 secretDuringDialog 为假 |
| 本轮完成 | 已连接后点「结束本次协助」，双方离开，这次不能再进 | 亲友能结束，结束后旧凭证进不来 | 双方都出现已连接；确认结束后都回到首页；再连没有弹窗、没有已连接，协助端显示没能连上 | 本机 `127.0.0.1:17991`；`acceptance/desktop-during-session.png`、`desktop-after-end.png`、`desktop-after-end-helper.png` |
| 本轮完成 | 协助端点「取消」后显示「已取消，没有连上。」 | 点了取消能看出来没有连上 | 取消后出现这句，没有进入会话 | `acceptance/desktop-cancel-hint.png` |
| 本轮完成 | 提议改动被拒绝后不执行，并看到「已拒绝，未做任何改动」 | 不同意的改动不会发生 | 双方都出现这句；用来验证的标记文件没有生成 | 本机 `127.0.0.1:17991`；`acceptance/desktop-deny-dialog.png`、`desktop-after-deny.png`、`desktop-after-deny-helper.png` |
| 本轮完成 | 对方不在等待时不要只提示检查网络 | 亲友知道该让对方重新开启 | 结束后用原凭证再连，出现「对方现在没有在等待协助」，没有「请检查网络」 | `acceptance/desktop-not-waiting.png` |
| 本轮完成 | 点「允许」后改动才执行，协助端看得到结果 | 同意的改动才会发生，对方能核对 | 点允许后协助端结果含输出；标记文件生成；被协助端记录「已允许」 | 本机 `127.0.0.1:17991`；`acceptance/desktop-allow-host.png`、`desktop-allow-result.png` |
| 本轮完成 | 拒绝之后协助端不再留着刚才的命令 | 下一次不会误发上一条 | 拒绝后「要做什么」和「命令」都空 | `acceptance/desktop-after-clear.png` |
| 本轮完成 | 确认超时后不执行这项改动 | 没人点允许就不会改电脑 | 等到超时；双方都出现「确认超时，未做任何改动」；标记文件没有生成 | 本机 `127.0.0.1:17991`，真实等满确认时间；`acceptance/desktop-timeout-dialog.png`、`desktop-timeout-host.png`、`desktop-timeout-helper.png` |
| 本轮完成 | 允许后的结果先显示输出正文 | 亲友先看到做了什么，而不是退出码 | 结果第一行是输出，exit 在后面，没有 stdout 分隔线 | `acceptance/desktop-result-body.png` |
| 本轮完成 | 只查看不弹确认，结果直接回来 | 看看程序不必先问一遍 | 点「查看正在运行的程序」没有确认弹窗；协助端出现进程结果；被协助端记下这次查看 | 本机 `127.0.0.1:17991`；`acceptance/desktop-read-result.png`、`desktop-read-host.png` |
| 本轮完成 | 改动确认的按钮显示剩余秒数 | 知道还要等多久 | 「允许」变成「允许（60）」这种带秒数的样子 | `acceptance/desktop-write-countdown.png` |
| 本轮完成 | 查看文件夹填好路径后直接出结果，不弹确认 | 看看目录不必先问一遍 | 填路径点查看后没有确认弹窗；结果里有该文件夹的文件名；被协助端记下查看文件夹 | 本机 `127.0.0.1:17991`；`acceptance/desktop-dir-result.png` |
| 本轮完成 | 正在运行的程序改成能看懂的程序名 | 不用对着原始表格找名字 | 结果是程序名，重复的带个数；没有 CSV 引号和内存列 | `acceptance/desktop-proc-names.png` |
| 本轮完成 | 查看文件填好路径后直接出内容，不弹确认 | 看看文件不必先问一遍 | 填路径点查看后没有确认弹窗；结果是文件内容；被协助端记下查看文件 | 本机 `127.0.0.1:17991`；`acceptance/desktop-file-before.png`、`desktop-file-after.png` |
| 本轮完成 | 文件夹结果用「文件 / 文件夹」 | 不用看英文缩写 | 结果行是「文件」和「文件夹」，没有 FILE/DIR | `acceptance/desktop-dir-kinds.png` |
| 本轮完成 | 查看日志填好路径后直接出末尾内容，不弹确认 | 看日志不必先问一遍，也不把开头当结果 | 填路径点查看后没有确认弹窗；结果含文件末尾行、不含开头行；被协助端记下查看日志 | 本机 `127.0.0.1:17991`；`acceptance/desktop-log-before.png`、`desktop-log-after.png` |
| 本轮完成 | 空文件夹写「这个文件夹是空的。」 | 打开空目录不会看成没结果 | 查看空文件夹后结果是这句，没有确认弹窗 | `acceptance/desktop-empty-dir.png` |
| 本轮完成 | 宽松时普通改动不再弹确认，删除仍要确认 | 少打扰，但删文件、关机仍要问 | 普通 echo 没有确认弹窗，结果回来，记下「已执行」；`del` 仍弹出确认，拒绝后没有执行 | 本机 `127.0.0.1:17991`；`acceptance/desktop-relaxed-host.png`、`desktop-relaxed-before.png`、`desktop-relaxed-ok.png`、`desktop-relaxed-risk.png` |
| 本轮完成 | 查看日志滚到末尾，并写明只显示了末尾多少行 | 最新一行不用再往下翻 | 结果框露出 `queqiao-log-tail` 和「只显示了末尾 80 行。」 | `acceptance/desktop-log-tail.png` |
| 本轮完成 | 路径旁的「查看」保持横排 | 按钮不会被挤成两行字 | 按钮宽 62、高 38 | `acceptance/desktop-log-button.png` |
| 本轮完成 | 完全信任只在这次协助里生效，结束后改动重新要确认 | 这次可以少问，结束就回到要问 | 横幅在时改动直接执行；点「结束完全信任」后横幅消失，下一次改动弹出确认，拒绝后没有执行 | 本机 `127.0.0.1:17991`；`acceptance/desktop-full-before.png`、`desktop-full-ran.png`、`desktop-full-ended.png`、`desktop-full-ask.png` |
| 本轮完成 | 确认框直接写出命令 | 不用再点开才知道要跑什么 | 确认框里能看见命令原文，没有「查看详细」 | `acceptance/desktop-full-ask.png` |
| 本轮完成 | 结束这次协助后再连上，完全信任不会还在 | 下一次改动重新要问 | 结束后回到首页；新凭证再连上没有完全信任横幅；下一次改动弹出确认，拒绝后没有执行 | 本机 `127.0.0.1:17991`；`acceptance/desktop-again-full.png`、`desktop-again-home.png`、`desktop-again-rejoined.png`、`desktop-again-ask.png` |
| 本轮完成 | 改动执行完后清掉命令 | 不会再点一次又执行 | 结果仍在，要做什么和命令都回到占位 | `acceptance/desktop-cleared.png` |
| 本轮完成 | 结束后点「查看上次协助记录」能看到「已执行」 | 回家后还能对上刚才做过的事 | 记录展开后有「已执行：写一句」，也有被拒绝的那条 | 本机 `127.0.0.1:17991`；`acceptance/desktop-history-before.png`、`desktop-history-after.png` |
| 本轮完成 | 确认框里的长命令不从词中间折断 | 命令保持一整行，用横滑看后面 | 框里是一行命令和横向滚动条，没有从连字符处折开 | `acceptance/desktop-cmd-line.png` |
| 本轮完成 | 关掉再打开，查看上次协助记录仍能看到「已执行：」 | 记录留在这台电脑上 | 结束并关掉进程后再打开，展开记录仍是「已执行：写一句」 | 本机同一数据目录；`acceptance/desktop-reopen-live.png`、`desktop-reopen-after.png` |
| 本轮完成 | 长命令确认框打开时滚到末尾 | 不用再滑才能看见路径 | 框里露出 `queqiao-reopen-end.txt` | `acceptance/desktop-cmd-end.png` |
| 本轮完成 | 上次记录保留「已允许这次协助」 | 连上之后这条不要消失 | 进行中能看见；结束后展开记录仍是这句 | 本机 `127.0.0.1:17991`；`acceptance/desktop-kept-live.png`、`desktop-kept-open.png` |
| 本轮完成 | 记录展开后按钮改成「收起」，再点合上 | 看完能收回去 | 展开是「收起」且能看见记录；再点回到「查看上次协助记录」，记录不再显示 | `acceptance/desktop-kept-open.png`、`desktop-kept-shut.png` |
| 本轮完成 | 协助端结束后，上次记录仍有「已允许这次协助」 | 对方先离开，这条也不丢 | 协助端结束；被协助端看到对方已结束；展开记录有「已允许这次协助」 | 本机 `127.0.0.1:17991`；`acceptance/desktop-both-before.png`、`desktop-both-open.png` |
| 本轮完成 | 先拒绝再允许，两条都留在记录里 | 拒绝过的那次不会被后来的允许盖掉 | 进行中和结束后都能看见「已拒绝这次协助」和「已允许这次协助」 | `acceptance/desktop-both-live.png`、`desktop-both-open.png` |
| 本轮完成 | 关掉再打开，拒绝和允许两条都还在 | 记录写在这台电脑上 | 结束并关掉进程后再打开，两条都还在 | 本机同一数据目录；`acceptance/desktop-scroll-after.png` |
| 本轮完成 | 记录太多时能往上翻到更早的一条 | 最新的在眼前，早的还能找到 | 打开时先看到最近的查看；往上翻能看到「已拒绝这次协助」 | `acceptance/desktop-scroll-before.png`、`desktop-scroll-after.png` |
| 本轮完成 | 进行中的「对方做过的事」太多时能往上翻到最早那条 | 最新的在眼前，最早的拒绝还在 | 先看到最近的查看；往上翻能看到「已拒绝这次协助」 | 本机 `127.0.0.1:17991`；`acceptance/desktop-audit-bottom.png`、`desktop-audit-top.png` |
| 本轮完成 | 记录滚动时不切掉半行 | 每一行都是完整的 | 进行中和首页记录的可见行都是整句 | `acceptance/desktop-audit-bottom.png`、`desktop-row-bottom.png`、`desktop-row-top.png` |
| 本轮完成 | 正在看最早那条时，新来的一条不把列表拉回最底下 | 看旧记录不被打断 | 停在「已拒绝这次协助」后新来一条，最早那条仍在眼前 | 本机 `127.0.0.1:17991`；`acceptance/desktop-stay-before.png`、`desktop-stay-after.png` |
| 本轮完成 | 首页记录框留下五行整行，开启按钮仍在下面 | 和进行中一样能看五行，按钮不被挡住 | 展开后看见五行整句，「开启接受协助」完整且在列表下面 | `acceptance/desktop-five-rows.png` |
| 本轮完成 | 列表停在最底下时，新来的一条仍滚到最底下 | 跟着最新的看，不会跳回开头 | 已在最近几条时再来一条，仍看不到更早的「已允许这次协助」 | 本机 `127.0.0.1:17991`；`acceptance/desktop-follow-before.png`、`desktop-follow-after.png` |
| 本轮完成 | 收起记录后，三步说明重新出现 | 看完记录，首页说明还在 | 展开时没有「发给帮你的人」；收起后这句回来 | `acceptance/desktop-steps-hidden.png`、`desktop-steps-back.png` |
| 本轮完成 | 协助端结果太多时仍停在最新一行，不切半行 | 先看到最后做了什么 | 四十行结果停在 `queqiao-line-40` 和 `exit=0`，第一行不在眼前 | 本机 `127.0.0.1:17991`；`acceptance/desktop-result-end.png` |
| 本轮完成 | 窄于 760 时五行记录不把「开启接受协助」挤出窗口 | 窄窗口仍能点开启 | 窗口宽 680；五行记录下面，开启按钮完整落在窗口内 | `acceptance/desktop-narrow-history.png` |
| 本轮完成 | 窄窗口展开记录后，往下滚仍能点到「连接」 | 记录展开也不会把连接挤没 | 窗口宽 680；展开前连接在窗口外；滚下来后连接完整可见，点下去出现「连接凭证格式不对」 | 本机 `127.0.0.1:17991`；`acceptance/desktop-place-before.png`、`desktop-place-after.png`、`desktop-place-click.png` |
| 本轮完成 | 结果区正在看前面几行时，新输出不拉回最后一行 | 看旧输出不被打断 | 先停在 `queqiao-line-1`；新输出含 `queqiao-line-41` 后，眼前仍是第一行，最后一行不在眼前 | 本机 `127.0.0.1:17991`；`acceptance/desktop-hold-before.png`、`desktop-hold-after.png` |
| 本轮完成 | 结果区滚回最底下后，新输出仍停在最新一行 | 回到末尾就继续跟着看 | 先回到 `queqiao-line-40` 和 `exit=0`；再打出四十二行后眼前是 `queqiao-line-42` 和 `exit=0`，第一行不在 | 本机 `127.0.0.1:17991`；`acceptance/desktop-refollow-before.png`、`desktop-refollow-after.png` |
| 本轮完成 | 看前面几行时，结果框不要被窗口下沿切掉 | 整框都在窗口里，行还是前面的 | 切之前只露出前几行且第四行被切；点开提议后露出 `queqiao-line-1` 到 `queqiao-line-8` 整行，第四十行不在眼前 | 本机 `127.0.0.1:17991`；`acceptance/desktop-fit-before.png`、`desktop-fit-after.png` |
| 本轮完成 | 点「之前的结果」能看到上一次输出，当前结果框仍留在窗口里 | 翻旧结果不把这次的结果顶走 | 当前是 `queqiao-now`；展开后还能看到 `queqiao-line-40`，`queqiao-now` 仍完整落在窗口内 | 本机 `127.0.0.1:17991`；`acceptance/desktop-past-before.png`、`desktop-past-after.png` |
| 本轮完成 | 结果框露全时，上面的「要做什么」不要被窗口上沿切掉 | 表单和结果一起看全 | 展开后 `queqiao-task` 整段在窗口里，没有被上沿切掉 | `acceptance/desktop-past-after.png` |
| 本轮完成 | 上一次结果的末行 `exit=0` 完整留在窗口里 | 展开后能对上这次有没有跑完 | 之前的结果底部是完整的 `exit=0`，落在窗口内 | 本机 `127.0.0.1:17991`；`acceptance/desktop-exit-before.png`、`desktop-exit-after.png` |
| 本轮完成 | 常用操作那一排不要被窗口上沿切成半个按钮 | 五个按钮都能点 | 「查看正在运行的程序」高 34，整颗在窗口里 | `acceptance/desktop-exit-after.png` |
| 本轮完成 | 之前的结果同时露出 `queqiao-line-40` 和 `exit=0` | 末两行一起看全 | 展开后这两行都是整行，都在窗口里 | 本机 `127.0.0.1:17991`；`acceptance/desktop-twoline-before.png`、`desktop-twoline-after.png` |
| 本轮完成 | 末行不要贴着「结束本次协助」 | 结果和结束按钮分开 | `exit=0` 底部距结束按钮顶部 21 | `acceptance/desktop-twoline-after.png` |
| 本轮完成 | 之前的结果往上翻能看到 `queqiao-line-1`，松手后 `exit=0` 仍是整行 | 翻旧的不把末行切半 | 翻到第一行能看见 `queqiao-line-1`；滚回末尾后 `queqiao-line-40` 和 `exit=0` 仍是整行 | 本机 `127.0.0.1:17991`；`acceptance/desktop-flip-before.png`、`desktop-flip-after.png` |
| 本轮完成 | 内容已经放下时，整页不要再多出一条滚动条 | 只有结果框自己的滚动条 | 协助页 `scrollHeight` 不大于可视高度，整页滚动是 hidden | `acceptance/desktop-flip-after.png` |
| 本轮完成 | 之前的结果里长行不要被右侧小滚动条挡住 | 行尾停在滚动条左边 | 长行右沿距结果框右边 36，字不压在滚动条上 | 本机 `127.0.0.1:17991`；`acceptance/desktop-gutter-after.png` |
| 本轮完成 | 滚轮在结果框里翻，不要带动整页 | 只翻结果，页面不动 | 结果从 1140 滚到 740，整页仍停在 1 | `acceptance/desktop-wheel-before.png`、`desktop-wheel-after.png` |
| 本轮完成 | 当前结果框的长行不要被自己的滚动条挡住 | 行尾停在滚动条左边 | 长行右沿距结果框右边 36 | 本机 `127.0.0.1:17991`；`acceptance/desktop-result-gutter.png` |
| 本轮完成 | 结果框到顶或到底时，继续滚轮不要带动整页 | 头尾都只停在结果框里 | 到底时结果停在 1140、整页仍是 1；到顶后再向上滚，结果仍是 0，整页仍是 1 | `acceptance/desktop-edge-after.png` |
| 本轮完成 | 长行可以横着滑，能看到行尾 | 不用猜后面还有没有字 | 滑之前行尾在框外；滑到头能看见 `queqiao-wide-QUEEND` | 本机 `127.0.0.1:17991`；`acceptance/desktop-slide-before.png`、`desktop-slide-after.png` |
| 本轮完成 | 横滑时不要把整页左右带动 | 只动结果框 | 结果横滑了 600，整页和文档的横向位置仍是 0 | `acceptance/desktop-slide-after.png` |
| 本轮完成 | 之前的结果里的长行也能横着滑到行尾 | 旧结果的行尾也能看见 | 滑之前行尾在框外；滑到头能看见 `queqiao-wide-QUEEND`，整页没有左右挪动 | 本机 `127.0.0.1:17991`；`acceptance/desktop-past-slide-before.png`、`desktop-past-slide-after.png` |
| 本轮完成 | 结果框出现横滑条后，整页不要再多出竖向滚动条 | 只有结果框自己的横滑条 | 长行出现后协助页高度不超出窗口，滚动是 hidden | `acceptance/desktop-pagebar.png` |
| 本轮完成 | 之前的结果出现横滑条时，两行文字仍完整 | 横滑条不切字 | 两行都完整，长行的字到条顶有 10px | 本机 `127.0.0.1:17991`；`acceptance/desktop-hbar-past.png` |
| 本轮完成 | 当前结果框的横滑条不要盖住最下面一行 | 横滑条在字下面 | `exit=0` 的字到条顶有 6px，三行都完整 | `acceptance/desktop-hbar-result.png` |
| 本轮完成 | 点开之前的结果后停在最新两行，能看见 exit=0 | 不用再往下找末行 | 滚动停在最底 36，眼前是长行和 `exit=0`，没有第一行 | 本机 `127.0.0.1:17991`；`acceptance/desktop-end-before-past.png`、`desktop-end-after.png` |
| 本轮完成 | 长行右边不要贴着竖滑条 | 行尾和滚动条分开 | 长行右边到结果框右沿 39；滑到头后 `QUEEND` 仍在空白左边 | `acceptance/desktop-gap-after.png`、`desktop-end-tail.png` |
| 本轮完成 | 横滑长行时短的 exit=0 留在框里 | 滑到行尾仍能看见有没有跑完 | 横滑 715 后 `exit=0` 在 37–77，`QUEEND` 在 727–767；改之前 `exit=0` 在 -678 | 本机 `127.0.0.1:17991`；`acceptance/desktop-stick-before.png`、`desktop-stick-after.png`、`desktop-stick-result.png` |
| 本轮完成 | 点开之后再往上翻，仍能看到第一行，松手停在整行 | 翻旧的不切成半行 | 滚动放到 9 再松手，回到 0，第一行高 20，整行在框里 | `acceptance/desktop-line1-before.png`、`desktop-line1-after.png` |
| 本轮完成 | 之前的结果已经到第一行时，继续向上滚，整页不要跟着动 | 翻到头就停在结果里 | 整页先放在 200，再向上滚两下仍是 200，之前的结果仍是 0 | 本机 `127.0.0.1:17991`；`acceptance/desktop-chain-before.png`、`desktop-chain-after.png` |
| 本轮完成 | 再滚回最底下时，长行仍停在刚才的行尾，exit=0 仍留在左边 | 上下翻不丢掉横滑位置 | 滚回最底 36，横向仍是 715，`exit=0` 在 37–77，`QUEEND` 在 727–767 | `acceptance/desktop-chain-tail-before.png`、`desktop-chain-tail.png` |
| 本轮完成 | 当前结果框已经到顶或到底时，继续滚，整页不要跟着动 | 到头就停在结果框里 | 先停在 0 再向上滚，整页仍是 80；到底后再向下滚，整页仍是 80 | 本机 `127.0.0.1:17991`；`acceptance/desktop-redge-before.png`、`desktop-redge-after.png` |
| 本轮完成 | 当前结果滑到行尾后，上下翻再回来，行尾还在，exit=0 仍留在左边 | 翻页不丢掉横滑位置 | 滚回最底后横向是 715，`exit=0` 在 52–92，`QUEEND` 在 727–767 | `acceptance/desktop-rtail-before.png`、`desktop-rtail.png` |
| 本轮完成 | 当前结果只有几行、框自己滚不动时，滚轮不要带动整页 | 滚轮停在结果框里 | 竖向可滚距离是 0；向上、向下各滚一下，整页仍是 80，结果仍是 0 | 本机 `127.0.0.1:17991`；`acceptance/desktop-short-before.png`、`desktop-short-after.png` |
| 本轮完成 | 短输出里的长行滑到行尾后，exit=0 仍留在左边 | 行尾和有没有跑完一起看见 | 横向 718，`exit=0` 在 37–77，`QUEEND` 在 724–764 | `acceptance/desktop-short-tail.png` |
| 本轮完成 | 之前的结果只有几行、自己滚不动时，滚轮不要带动整页 | 滚轮停在旧结果里 | 2 行，竖向可滚距离是 0；向上、向下各滚一下，整页仍是 80 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastshort-before.png`、`desktop-pastshort-after.png` |
| 本轮完成 | 短的旧结果里的长行滑到行尾后，exit=0 仍留在左边 | 行尾和有没有跑完一起看见 | 横向 718，`exit=0` 在 37–77，`QUEEND` 在 724–764 | `acceptance/desktop-pastshort-tail.png` |
| 本轮完成 | 在之前的结果里用横向滚轮滑到行尾，整页不要跟着动 | 只动旧结果自己的横向位置 | 横向滚三下从 0 到 718；整页仍是纵向 80、横向 40，文档横向仍是 0 | 本机 `127.0.0.1:17991`；`acceptance/desktop-hwheel-before.png`、`desktop-hwheel-after.png` |
| 本轮完成 | 滑到之后 exit=0 仍留在左边，QUEEND 仍在框里 | 行尾和有没有跑完一起看见 | `QUEEND` 在 684–724，`exit=0` 也在框里 | `acceptance/desktop-hwheel-after.png` |
| 本轮完成 | 当前结果框用横向滚轮滑到行尾，整页不要跟着动 | 只动结果框自己的横向位置 | 从 0 滑到 718；整页仍是纵向 80、横向 40，文档横向仍是 0 | 本机 `127.0.0.1:17991`；`acceptance/desktop-rwheel-before.png`、`desktop-rwheel-after.png` |
| 本轮完成 | 再滑一下贴到可滑尽头，QUEEND 不要停在尽头前面 | 滚轮停下的位置就是尽头 | 改之前算出来的尽头是 730、实际只能到 718；改之后尽头和停下的位置都是 718，`QUEEND` 在 684–724 | `acceptance/desktop-rwheel-old.png`、`desktop-rwheel-after.png` |
| 本轮完成 | 当前结果已经在最底下时，横向滚轮滑到行尾，竖向仍停在最底下 | 横滑不把视线从末行带走 | 先停在 76，横向滚完仍是 76 | 本机 `127.0.0.1:17991`；`acceptance/desktop-bottom-old.png`、`desktop-bottom-after.png` |
| 本轮完成 | 滑完后位置和可滑尽头一致，exit=0 仍留在左边 | 行尾和有没有跑完一起看见 | 横向 730，和尽头一致；`QUEEND` 在 672–712，`exit=0` 在框里 | `acceptance/desktop-bottom-after.png` |
| 本轮完成 | 之前的结果已经在最底下时，横向滚轮滑到行尾，竖向仍停在最底下 | 横滑不把视线从末行带走 | 先停在 96，横向滚完仍是 96 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pbottom-old.png`、`desktop-pbottom-after.png` |
| 本轮完成 | 之前的结果滑完后位置和可滑尽头一致，exit=0 仍留在左边 | 行尾和有没有跑完一起看见 | 横向 730，和尽头一致；`QUEEND` 在 672–712，`exit=0` 在框里 | `acceptance/desktop-pbottom-after.png` |
| 本轮完成 | 之前的结果横滑到行尾后再往上翻到第一行，横向仍停在行尾 | 翻上去不丢掉行尾 | 松手后竖向是 0，横向 715，和尽头一致；`exit=0` 从框左 24 起落在 37 | 本机 `127.0.0.1:17991`；`acceptance/desktop-up-old.png`、`desktop-up-after.png` |
| 本轮完成 | 松手后第一行是整行 | 翻到头不被切成半行 | 滚动放在 7 后松手回到 0，`queqiao-line-1` 是整行 | `acceptance/desktop-up-before.png`、`desktop-up-after.png` |
| 本轮完成 | 当前结果横滑到行尾后再往上翻到第一行，横向仍停在行尾 | 翻上去不丢掉行尾 | 松手后竖向是 0，横向 715，和尽头一致；`exit=0` 从框左 24 起落在 37 | 本机 `127.0.0.1:17991`；`acceptance/desktop-rup-old.png`、`desktop-rup-after.png` |
| 本轮完成 | 当前结果松手后第一行是整行 | 翻到头不被切成半行 | 滚动放在 7 后松手回到 0，`queqiao-line-1` 是整行 | `acceptance/desktop-rup-before.png`、`desktop-rup-after.png` |
| 本轮完成 | 当前结果翻到第一行后再滚回最底下，横向仍停在行尾 | 滚回去不丢掉行尾 | 松手后竖向回到 76，横向 715，和尽头一致 | 本机 `127.0.0.1:17991`；`acceptance/desktop-back-old.png`、`desktop-back-after.png` |
| 本轮完成 | 滚回后 exit=0 是整行，QUEEND 在框里 | 末行和行尾一起看全 | `exit=0` 是整行，`QUEEND` 在 727–767 | `acceptance/desktop-back-before.png`、`desktop-back-after.png` |
| 本轮完成 | 之前的结果翻到第一行后再滚回最底下，横向仍停在行尾 | 滚回去不丢掉行尾 | 松手后竖向回到 96，横向 715，和尽头一致 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pback-before.png`、`desktop-pback-after.png` |
| 本轮完成 | 之前的结果滚回后 exit=0 是整行，QUEEND 在框里 | 末行和行尾一起看全 | `exit=0` 是整行，`QUEEND` 在 727–767 | `acceptance/desktop-pback-after.png` |
| 本轮完成 | 翻回最底下后协助页不要多出竖向滚动条 | 正常高度只有结果框自己的滚动条 | 多出来的高度是 4，滚动是 hidden，页顶是 0 | 本机 `127.0.0.1:17991`；`acceptance/desktop-fitback-after.png` |
| 本轮完成 | 「要做什么」和「结束本次协助」仍完整留在窗口里 | 翻回去不把这两处顶出窗口 | 「要做什么」高 38，在 131–169；「结束本次协助」高 38，在 430–468，窗口高 480 | `acceptance/desktop-fitback-after.png` |
| 本轮完成 | 窗口缩到最小高度后，「要做什么」和「结束本次协助」仍完整留在窗口里 | 缩到最矮也不把这两处顶出窗口 | 客户区 840×360。「要做什么」高 30，在 101–131；「结束本次协助」高 38，在 320–358 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minh-before.png`、`desktop-minh-after.png` |
| 本轮完成 | 缩到最小高度后，协助页仍不要多出一条竖向滚动条 | 最矮时整页不再被撑高 | 多出来的高度是 0，滚动是 hidden；窗口右侧没有整页竖条 | `acceptance/desktop-minh-after.png`。改之前的 `desktop-minh-old.png` 缩下去后整页仍被撑高 |
| 本轮完成 | 缩到最小高度后，当前结果里的 exit=0 仍是整行 | 最矮时末行不被切成半行 | 客户区 840×360。框高 22，滚动 20。`exit=0` 行高 20，在 222–242，看得见的只有这一行 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minline-before.png`、`desktop-minline-after.png`。改之前的 `desktop-minline-old.png` 末行被切掉 |
| 本轮完成 | 之前的结果里看得见的那一行也是整行，横滑条坐在这行下面 | 最矮时横滑条不压住这一行 | 框高 34，横滑条占 12。`exit=0` 行高 20，在 261–281。墨水到画面 305，横滑条从 312 开始 | `acceptance/desktop-minline-after.png` |
| 本轮完成 | 缩到最小高度后，当前结果再往上翻一行，queqiao-now 仍是整行 | 最矮时往上翻不停在半行 | 从滚动 20 翻到 0。`queqiao-now` 行高 20，在 222–242，看得见的只有这一行 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minup-before.png`、`desktop-minup-after.png` |
| 本轮完成 | 之前的结果往上翻到长行时，这一行仍是整行，横滑条仍坐在下面 | 最矮时翻到长行不被横滑条切掉 | 从滚动 120 翻到 100。长行高 20，在 261–281。墨水到画面 305，横滑条从 312 开始 | `acceptance/desktop-minup-after.png` |
| 本轮完成 | 缩到最小高度后，把长行横滑到行尾，这一行仍是整行，横滑条仍在下面 | 最矮时横滑不把这一行切成半行 | 长行停在滚动 100。滑完横向 714，和尽头一致，竖向仍是 100。横滑条占 12。墨水到画面 305，横滑条从 312 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minslide-before.png`、`desktop-minslide-after.png` |
| 本轮完成 | 这样滑完 QUEEND 仍在框里，整页不要跟着动 | 行尾看得见，整页不被带走 | `QUEEND` 在 680–720。整页先放在纵向 40、横向 40，滑完没变，文档横向仍是 0 | `acceptance/desktop-minslide-after.png` |
| 本轮完成 | 缩到最小高度、长行已经在行尾时，再滚回 exit=0，这一行仍是整行，横滑条仍在下面 | 最矮时滚回末行不被横滑条切掉 | 竖向回到 120。`exit=0` 行高 20，在 261–281。横滑条占 12。墨水到画面 305，横滑条从 312 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minback-before.png`、`desktop-minback-after.png` |
| 本轮完成 | exit=0 仍留在左边，横向仍停在行尾 | 短行不跟着行尾跑走 | 字从 29 开始，框左是 16。横向 699，和可滑尽头一致 | `acceptance/desktop-minback-after.png` |
| 本轮完成 | 缩到最小高度、已经滚回 exit=0 且横向还在行尾时，再翻回长行，这一行仍是整行，横滑条仍在下面 | 最矮时翻回长行不被横滑条切掉 | 从竖向 120 翻回 100。长行是整行，横滑条占 12。墨水到画面 305，横滑条从 312 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minret-before.png`、`desktop-minret-after.png` |
| 本轮完成 | 翻回后 QUEEND 仍在框里 | 行尾没有在翻页时丢掉 | 横向仍是 699，和尽头一致。`QUEEND` 在 735–775 | `acceptance/desktop-minret-after.png` |
| 本轮完成 | 缩到最小高度后，当前结果里的长行横滑到行尾，这一行仍是整行，横滑条仍在下面 | 最矮时当前结果横滑不把长行切成半行 | 长行先在最底 80。滑完横向 714，和尽头一致，竖向仍是 80。横滑条占 12。墨水到画面 286，横滑条从 313 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minnow-before.png`、`desktop-minnow-after.png` |
| 本轮完成 | 这样滑完 QUEEND 仍在框里，整页不要跟着动 | 行尾看得见，整页不被带走 | `QUEEND` 在 680–720。整页先放在纵向 40、横向 40，滑完没变，文档横向仍是 0 | `acceptance/desktop-minnow-after.png` |
| 本轮完成 | 缩到最小高度、当前结果长行已经在行尾时，再往上翻一行，看得见的行仍是整行，横滑条仍在下面 | 最矮时往上翻不停在半行 | 从竖向 80 翻到 60。三行都是整行，横滑条占 12。横向仍是 699。墨水到画面 306，横滑条从 313 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minroll-before.png` |
| 本轮完成 | 再滚回最底下时，QUEEND 仍在框里，横向仍停在行尾 | 翻回去不丢掉行尾 | 竖向回到 80。横向 699，和尽头一致。`QUEEND` 在 735–775 | `acceptance/desktop-minroll-after.png` |
| 本轮完成 | 缩到最小高度、滚回最底下后，exit=0 仍留在左边，这一行仍是整行 | 最矮时末行不跟着行尾跑走 | 竖向 80。`exit=0` 行高 20，在 262–282。字从 29 到 69，框左是 16 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minexit-before.png`、`desktop-minexit-after.png` |
| 本轮完成 | 横滑条仍坐在这行下面 | 横滑条不压住 exit=0 | 横滑条占 12。墨水到画面 306，横滑条从 313 开始 | `acceptance/desktop-minexit-after.png` |
| 本轮完成 | 从最小高度拉回正常高度后，当前结果里看得见的行仍是整行，横滑条仍在下面 | 拉高后不露出半行 | 窗口高 480。看得见 4 行，都是整行。横滑条占 12。墨水到画面 408，横滑条从 415 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-mingrow-before.png`、`desktop-mingrow-after.png`。改之前的 `desktop-mingrow-old.png` 露着半行 |
| 本轮完成 | exit=0 仍留在左边，横向仍停在行尾，协助页不多出竖向滚动条 | 拉高后行尾跟着新的尽头，整页不另加竖条 | 字从 37 到 77，框左是 24。横向 715，和尽头一致。多出来的高度是 2，滚动是 hidden，页顶是 0 | `acceptance/desktop-mingrow-after.png` |
| 本轮完成 | 从最小高度拉回正常高度后，长行的 QUEEND 仍在框里 | 拉高后行尾没有滑出框外 | 窗口高 480。横向 715，和尽头一致。`QUEEND` 在 727–767 | 本机 `127.0.0.1:17991`；`acceptance/desktop-minqend-before.png`、`desktop-minqend-after.png` |
| 本轮完成 | 横滑条不压住这一行 | 长行整行留在横滑条上面 | 长行是整行，横滑条占 12。右侧墨水到画面 388，横滑条从 415 开始 | `acceptance/desktop-minqend-after.png` |
| 本轮完成 | 拉回正常高度后，再往上翻一行，看得见的行仍是整行，横滑条仍在下面 | 拉高后再翻不停在半行 | 竖向从 60 翻到 40。四行都是整行，横滑条占 12。横向仍是 715。墨水到画面 408，横滑条从 415 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growup-before.png` |
| 本轮完成 | 再滚回最底下时，QUEEND 仍在框里，横向仍停在行尾 | 翻回去不丢掉行尾 | 竖向回到 60。横向 715，和尽头一致。`QUEEND` 在 727–767 | `acceptance/desktop-growup-after.png` |
| 本轮完成 | 拉回正常高度后往上翻一行时，滚轮不要带动整页 | 翻结果框不把整页带走 | 整页先放在纵向 40、横向 40，下面还能滚 359、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。结果框从 60 到 40 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpage-before.png`、`desktop-growpage-after.png` |
| 本轮完成 | 这时长行的 QUEEND 仍在框里 | 往上翻不丢掉行尾 | `QUEEND` 在 687–727。横向仍是 715。长行仍是整行 | `acceptance/desktop-growpage-after.png` |
| 本轮完成 | 拉回正常高度、已经往上翻一行后，再滚回最底下时，滚轮也不要带动整页 | 滚回结果框不把整页带走 | 整页先放在纵向 40、横向 40，下面还能滚 359、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。结果框回到 60 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growdown-before.png`、`desktop-growdown-after.png` |
| 本轮完成 | QUEEND 仍在框里，横向仍停在行尾 | 滚回去不丢掉行尾 | 滚的时候 `QUEEND` 在 687–727，横向仍是 715。撤掉临时撑高后横向 715，和尽头一致，`QUEEND` 在 727–767 | `acceptance/desktop-growdown-after.png` |
| 本轮完成 | 滚回最底下后，exit=0 仍留在左边，这一行仍是整行 | 滚回去末行不跟着行尾跑走 | 竖向 60。`exit=0` 行高 20，在 364–384。字从 37 到 77，框左是 24 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growexit-before.png`、`desktop-growexit-after.png` |
| 本轮完成 | 横滑条仍坐在这行下面 | 横滑条不压住 exit=0 | 横滑条占 12。墨水到画面 408，横滑条从 415 开始 | `acceptance/desktop-growexit-after.png` |
| 本轮完成 | 滚回最底下后再点开之前的结果，当前结果里的 exit=0 仍是整行，还留在左边 | 点开不把当前末行带走 | 竖向 100。`exit=0` 行高 20，在 316–336。字从 37 到 77，框左是 24 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpast-before.png`、`desktop-growpast-after.png` |
| 本轮完成 | 之前的结果停在最新一行，这一行是整行，协助页不多出竖向滚动条 | 点开就看见最新一行，整页不另加竖条 | 可滚距离是 0。最新 `exit=0` 在 395–415，是整行。多出来的高度是 0，滚动是 hidden | `acceptance/desktop-growpast-after.png` |
| 本轮完成 | 点开之前的结果后，当前结果长行的 QUEEND 仍在框里 | 点开不把行尾滑出框外 | `QUEEND` 在 727–767，顶 299、底 313 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growqend-before.png`、`desktop-growqend-after.png` |
| 本轮完成 | 横滑条不压住这一行，协助页仍不多出竖向滚动条 | 长行留在横滑条上面，整页不另加竖条 | 横滑条占 12。右侧墨水到画面 340，横滑条从 367 开始。多出来的高度是 0，滚动是 hidden | `acceptance/desktop-growqend-after.png` |
| 本轮完成 | 点开之前的结果后，当前结果再往上翻一行，看得见的行仍是整行，横滑条仍在下面 | 点开后再翻不停在半行 | 竖向从 100 翻到 80。`queqiao-line-5` 在 256–276，长行在 276–296，都是整行。横滑条占 12。墨水到画面 360，横滑条从 367 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpup-before.png`、`desktop-growpup-after.png` |
| 本轮完成 | QUEEND 仍在框里，整页不要跟着动 | 往上翻不丢掉行尾，也不把整页带走 | 整页先放在纵向 40、横向 40，翻完仍是 40 和 40。`QUEEND` 在 687–727。横向仍是 715 | `acceptance/desktop-growpup-after.png` |
| 本轮完成 | 点开之前的结果、当前结果已经往上翻一行后，再滚回最底下，滚轮不要带动整页 | 滚回结果框不把整页带走 | 整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。结果框从 80 回到 100 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpdown-before.png`、`desktop-growpdown-after.png` |
| 本轮完成 | QUEEND 仍在框里，看得见的行仍是整行，横滑条仍在下面 | 滚回去不丢掉行尾，也不露出半行 | 滚的时候 `QUEEND` 在 687–727，横向仍是 715。长行在 256–276，`exit=0` 在 276–296，都是整行。横滑条占 12。撤掉临时撑高后，`exit=0` 墨水到画面 360，横滑条从 367 开始 | `acceptance/desktop-growpdown-after.png` |
| 本轮完成 | 滚回最底下后，当前结果里的 exit=0 仍留在左边，这一行仍是整行 | 滚回去末行不跟着行尾跑走 | 竖向 100，和尽头一致。横向 715，和尽头一致。`exit=0` 行高 20，在 316–336。字从 37 到 77，框左是 24 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpexit-before.png`、`desktop-growpexit-after.png` |
| 本轮完成 | 横滑条仍坐在这行下面 | 横滑条不压住 exit=0 | 横滑条占 12。墨水到画面 360，横滑条从 367 开始。多出来的高度是 0，滚动是 hidden，页顶是 0 | `acceptance/desktop-growpexit-after.png` |
| 本轮完成 | 滚回最底下后，在之前的结果里滚轮，不要带动整页，也不带动当前结果 | 之前的结果自己滚不动时，滚轮也不把整页和当前结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向仍是 100，横向仍是 715。之前的结果竖向仍是 0 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpwheel-before.png`、`desktop-growpwheel-after.png` |
| 本轮完成 | 之前的结果里看得见的行仍是整行 | 滚这一下不切掉之前的结果 | `queqiao-old-line` 在 375–395，`exit=0` 在 395–415，行高都是 20。撤掉临时撑高后多出来的高度是 0，滚动是 hidden | `acceptance/desktop-growpwheel-after.png` |
| 本轮完成 | 在之前的结果里滚过之后，当前结果的 exit=0 仍留在左边，这一行仍是整行 | 滚之前的结果不把当前末行带走 | 竖向 100，和尽头一致。横向 715，和尽头一致。`exit=0` 仍是整行、留在框左，并且是最下一行。横滑条占 12 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growptail-before.png`、`desktop-growptail-after.png` |
| 本轮完成 | 横滑条仍坐在这行下面，QUEEND 仍在框里 | 横滑条不压住 exit=0，行尾没有滑出框外 | `QUEEND` 在 727–767，顶 299、底 313。墨水到画面 360，横滑条从 367 开始。多出来的高度是 0，滚动是 hidden | `acceptance/desktop-growptail-after.png` |
| 本轮完成 | 滚过之后，之前的结果里的 exit=0 仍留在左边，这一行仍是整行 | 滚这一下不把上一次的末行带走 | 字从 37 到 77，框左是 24。行高 20，在 395–415，是看得见的最下一行 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpold-before.png`、`desktop-growpold-after.png` |
| 本轮完成 | 这一行下面没有横滑条压住它 | 短的之前结果不另加横滑条 | 横滑条占 2，只是边框。墨水到画面 439，下面没有横滑条 | `acceptance/desktop-growpold-after.png` |
| 本轮完成 | 滚过之后，之前的结果里的 queqiao-old-line 仍留在左边，这一行仍是整行 | 滚这一下不把上一次的第一行带走 | 字从 37 到 143，框左是 24。行高 20，在 375–395 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpline-before.png`、`desktop-growpline-after.png` |
| 本轮完成 | 这一行下面没有横滑条压住它 | 短的第一行下面不另加横滑条 | 横滑条占 2，只是边框。墨水到画面 419，横到 148，下面没有横滑条 | `acceptance/desktop-growpline-after.png` |
| 本轮完成 | 在之前的结果里再向下滚一下，不要带动整页，也不带动当前结果 | 向下滚也不把整页和当前结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向仍是 100，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-growpdrop-before.png`、`desktop-growpdrop-after.png` |
| 本轮完成 | 之前的结果里看得见的两行仍留在左边，都是整行，下面没有横滑条 | 向下滚不把短行带走，也不另加横滑条 | `queqiao-old-line` 字从 37 到 143，`exit=0` 字从 37 到 77，框左是 24。两行高 20，在 375–395 和 395–415。横滑条占 2 | `acceptance/desktop-growpdrop-after.png` |
| 本轮完成 | 上一次写成一条长行后再点开之前的结果，长行仍是整行，横滑条坐在下面 | 点开后长行不被横滑条切掉 | 长行高 20，在 363–383，是整行。横滑条占 12。长行墨水到画面 404，横滑条从 434 开始 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastwide-before.png`、`desktop-pastwide-after.png` |
| 本轮完成 | exit=0 仍留在左边，协助页不多出竖向滚动条 | 短的末行不跟着长行跑走，整页不另加竖条 | 字从 37 到 77，框左是 24。行高 20，在 383–403。多出来的高度是 0，滚动是 hidden，页顶是 0 | `acceptance/desktop-pastwide-after.png` |
| 本轮完成 | 点开这条长行之后，把它横滑到行尾，QUEEND 仍在框里，整页不要跟着动 | 横滑到行尾不把整页带走，行尾留在框里 | 整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。滑完仍是 40 和 40，文档横向仍是 0。撤掉临时撑高后横向 703，和尽头一致。`QUEEND` 在 739–779 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastend-before.png`、`desktop-pastend-after.png` |
| 本轮完成 | exit=0 仍留在左边，横滑条仍在下面 | 短的末行不跟着行尾跑走，横滑条不压住它 | 字从 37 到 77，框左是 24。行高 20，在 383–403。横滑条占 12。墨水到画面 427，横滑条从 434 开始 | `acceptance/desktop-pastend-after.png` |
| 本轮完成 | 横滑到行尾后，在之前的结果里再往上翻一下，不要带动整页，横向仍停在行尾 | 这一框自己滚不动时，向上翻也不把整页带走，行尾还在 | 整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。之前的结果可滚距离是 0，竖向仍是 0。横向仍是 703，和尽头一致 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastup-before.png`、`desktop-pastup-after.png` |
| 本轮完成 | QUEEND 仍在框里，exit=0 仍留在左边，横滑条仍在下面 | 向上翻不丢掉行尾，也不把短行带走 | `QUEEND` 在 739–779，顶 366、底 380。`exit=0` 字从 37 到 77，框左是 24，行高 20，在 383–403。横滑条占 12。墨水到画面 427，横滑条从 434 开始 | `acceptance/desktop-pastup-after.png` |
| 本轮完成 | 上一次多写几行，使之前的结果自己能往上翻。横滑到行尾后再往上翻一行，不要带动整页，横向仍停在行尾 | 真的翻走一行时，整页不动，行尾还在 | 可滚距离是 120。整页先放在纵向 40、横向 40，翻完仍是 40 和 40，文档横向仍是 0。竖向从 120 到 100。横向仍是 715，和尽头一致 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastrows-before.png`、`desktop-pastrows-after.png` |
| 本轮完成 | 看得见的行仍是整行，QUEEND 仍在框里，横滑条仍在下面 | 翻走一行不露出半行，行尾不滑出框外 | `queqiao-old-6` 在 363–383，长行在 383–403，都是整行。`QUEEND` 在 727–767，顶 386、底 400。横滑条占 12。长行墨水到画面 427，横滑条从 434 开始 | `acceptance/desktop-pastrows-after.png` |
| 本轮完成 | 再滚回最底下，不要带动整页，横向仍停在行尾 | 滚回这一框不把整页带走，行尾还在 | 整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。竖向从 100 回到 120，和可滚尽头一致。横向仍是 715，和尽头一致 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastback-before.png`、`desktop-pastback-after.png` |
| 本轮完成 | exit=0 仍留在左边且是整行，QUEEND 仍在框里，横滑条仍在下面 | 滚回去末行不跟着行尾跑走，横滑条不压住它 | 字从 37 到 77，框左是 24。行高 20，在 383–403。`QUEEND` 在 727–767，顶 366、底 380。横滑条占 12。墨水到画面 427，横滑条从 434 开始 | `acceptance/desktop-pastback-after.png` |
| 本轮完成 | 往上翻一行后，queqiao-old-6 仍留在左边，这一行仍是整行 | 短行不跟着行尾跑走 | 字从 37 到 129，框左是 24。行高 20，在 363–383 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastold6-before.png`、`desktop-pastold6-after.png` |
| 本轮完成 | 横滑条仍在下面，横向仍停在行尾 | 横滑条不压住这一行，行尾还在 | 横滑条占 12。墨水到画面 407，横在 47–128，横滑条从 434 开始。横向 715，和尽头一致 | `acceptance/desktop-pastold6-after.png` |
| 本轮完成 | 再往上翻一行，queqiao-old-5 仍留在左边，这一行仍是整行，不要带动整页 | 再翻一行不把短行带走，也不把整页带走 | 整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。竖向从 100 到 80。字从 37 到 129，框左是 24。行高 20，在 363–383 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastold5-before.png`、`desktop-pastold5-after.png` |
| 本轮完成 | 横向仍停在行尾，横滑条仍在下面 | 长行翻出这一屏后行尾还在，横滑条不压住字 | 横向 715，和尽头一致。横滑条占 12。`queqiao-old-5` 墨水到画面 407，`queqiao-old-6` 墨水到 427，横滑条从 434 开始 | `acceptance/desktop-pastold5-after.png` |
| 本轮完成 | 再滚回一行，长行的 QUEEND 重新回到框里，不要带动整页，横向仍停在行尾 | 滚回一行把行尾带回框里，整页不动 | 整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。竖向从 80 回到 100。横向仍是 715，和尽头一致。`QUEEND` 在 727–767 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastret-before.png`、`desktop-pastret-after.png` |
| 本轮完成 | queqiao-old-6 仍留在左边且是整行，横滑条仍在下面 | 滚回去短行不跟着行尾跑走 | 字从 37 到 129，框左是 24。行高 20，在 363–383。横滑条占 12。墨水到画面 407，横滑条从 434 开始 | `acceptance/desktop-pastret-after.png` |
| 本轮完成 | 这时在当前结果里滚轮，不要带动整页，也不带动之前的结果 | 滚当前结果不把整页和之前的结果带走 | 滚轮打在当前结果上。整页先放在纵向 40、横向 40，下面还能滚 357、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向仍是 100，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-nowheel-before.png`、`desktop-nowheel-after.png` |
| 本轮完成 | 之前的结果仍停在这一行，QUEEND 仍在框里，queqiao-old-6 仍留在左边 | 滚当前结果不改之前的结果 | 横向 715，和尽头一致。`QUEEND` 在 727–767，顶 386、底 400。`queqiao-old-6` 字从 37 到 129，框左是 24，行高 20，在 363–383 | `acceptance/desktop-nowheel-after.png` |
| 本轮完成 | 当前结果也多写几行，使它自己能往上翻。在当前结果里往上翻一行，不要带动整页，也不带动之前的结果 | 当前结果真的翻走一行时，整页和之前的结果不动 | 当前结果可滚距离是 140，竖向从 140 到 120。整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。翻完仍是 40 和 40。之前的结果竖向仍是 100，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-nowup-before.png`、`desktop-nowup-after.png`。改了收行：点开后协助页多出来的高度回到 0 |
| 本轮完成 | 之前的结果仍停在这一行，QUEEND 仍在框里，横向仍停在行尾 | 翻当前结果不丢掉之前的行尾 | 横向 715，和尽头一致。`QUEEND` 在 727–767，顶 370、底 384。`queqiao-now-7` 在 288–308，`queqiao-now-8` 在 308–328，都是整行 | `acceptance/desktop-nowup-after.png` |
| 本轮完成 | 在当前结果里再滚回一行，不要带动整页，也不带动之前的结果 | 滚回当前结果不把整页和之前的结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 120 回到 140，和可滚尽头一致。之前的结果竖向仍是 100，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-nowback-before.png`、`desktop-nowback-after.png` |
| 本轮完成 | 当前结果回到最底下，看得见的行仍是整行，QUEEND 仍在框里，横向仍停在行尾 | 滚回去不露出半行，也不丢掉之前的行尾 | `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 370、底 384 | `acceptance/desktop-nowback-after.png` |
| 本轮完成 | 当前结果回到最底下后，在之前的结果里再往上翻一行，不要带动整页，也不带动当前结果 | 翻之前的结果不把整页和当前结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 100 到 80。当前结果竖向仍是 140，和可滚尽头一致 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pasthold-before.png`、`desktop-pasthold-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行，之前的结果横向仍停在行尾 | 翻走一行后行尾位置还在；长行离开这一屏 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。之前的结果横向 715，和尽头一致。这一屏是 `queqiao-old-5` 在 347–367、`queqiao-old-6` 在 367–387，都是整行。横滑条占 12，从画面 418 开始 | `acceptance/desktop-pasthold-after.png` |
| 本轮完成 | 再滚回一行，长行的 QUEEND 重新回到框里，不要带动整页，也不带动当前结果 | 滚回一行把行尾带回框里，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 80 回到 100。当前结果竖向仍是 140。`QUEEND` 在 727–767 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastqend-before.png`、`desktop-pastqend-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行，横向仍停在行尾 | 滚回去不露出半行，行尾还在 | `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。横向 715，和尽头一致。长行墨水到画面 411，横滑条从 418 开始 | `acceptance/desktop-pastqend-after.png` |
| 本轮完成 | 再滚回最底下，exit=0 重新出现并留在左边，不要带动整页，也不带动当前结果 | 滚到真正最底时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 100 回到 120，和可滚尽头一致。当前结果竖向仍是 140。`exit=0` 字从 37 到 77，框左是 24 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastbottom-before.png`、`desktop-pastbottom-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行，QUEEND 仍在框里，横向仍停在行尾 | 滚回最底不露出半行，行尾还在 | `queqiao-now-8` 在 288–308，当前结果的 `exit=0` 在 308–328，都是整行。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。之前的结果 `exit=0` 墨水到画面 411，横滑条从 418 开始 | `acceptance/desktop-pastbottom-after.png` |
| 本轮完成 | 滚回最底下后，在当前结果里再往上翻一行，不要带动整页，也不带动之前的结果 | 翻当前结果不把整页和之前的结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 140 到 120。之前的结果竖向仍是 120，和可滚尽头一致，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-nowstay-before.png`、`desktop-nowstay-after.png` |
| 本轮完成 | 之前的结果仍停在最底下，exit=0 仍留在左边且是整行，QUEEND 仍在框里，横向仍停在行尾 | 翻当前结果不丢掉之前的末行和行尾 | 之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387，是整行。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。当前结果 `queqiao-now-7` 在 288–308，`queqiao-now-8` 在 308–328，都是整行 | `acceptance/desktop-nowstay-after.png` |
| 本轮完成 | 在当前结果里再滚回一行，不要带动整页，也不带动之前的结果 | 滚回当前结果不把整页和之前的结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 120 回到 140，和可滚尽头一致。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-resback-before.png`、`desktop-resback-after.png` |
| 本轮完成 | 当前结果回到最底下，看得见的行仍是整行；之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾 | 滚回去不露出半行，也不丢掉之前的末行 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。之前的结果竖向仍是 120。`exit=0` 字从 37 到 77，框左是 24。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364 | `acceptance/desktop-resback-after.png` |
| 本轮完成 | 两边都回到最底下后，在之前的结果里再往上翻一行，不要带动整页，也不带动当前结果 | 翻之前的结果不把整页和当前结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 120 到 100。当前结果竖向仍是 140，和可滚尽头一致 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastsix-before.png`、`desktop-pastsix-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；这一屏变成 queqiao-old-6 和长行，QUEEND 仍在框里，横向仍停在行尾 | 翻走一行后当前结果和行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。`queqiao-old-6` 字从 37 到 129，框左是 24，行在 347–367。长行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 370、底 384 | `acceptance/desktop-pastsix-after.png` |
| 本轮完成 | 再往上翻一行，这一屏变成 queqiao-old-5 和 queqiao-old-6，长行离开这一屏，不要带动整页，也不带动当前结果 | 再翻一行不把整页和当前结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 100 到 80。当前结果竖向仍是 140。长行不在这一屏，`QUEEND` 的字在 390–404 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastfive-before.png`、`desktop-pastfive-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；之前的结果横向仍停在行尾 | 长行离开后行尾位置还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。横向 715，和尽头一致。`queqiao-old-5` 和 `queqiao-old-6` 都在 347–387，字从 37 到 129，框左是 24 | `acceptance/desktop-pastfive-after.png` |
| 本轮完成 | 再滚回一行，长行的 QUEEND 重新回到框里，不要带动整页，也不带动当前结果 | 滚回一行把行尾带回框里，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 80 回到 100。当前结果竖向仍是 140。`QUEEND` 在 727–767，顶 370、底 384 | 本机 `127.0.0.1:17991`；`acceptance/desktop-sixback-before.png`、`desktop-sixback-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；这一屏变成 queqiao-old-6 和长行，横向仍停在行尾 | 滚回去不露出半行，行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。`queqiao-old-6` 字从 37 到 129，框左是 24，行在 347–367。长行在 367–387。横向 715，和尽头一致 | `acceptance/desktop-sixback-after.png` |
| 本轮完成 | 再滚回最底下，之前的结果里的 exit=0 重新出现并留在左边，不要带动整页，也不带动当前结果 | 滚到真正最底时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 100 回到 120，和可滚尽头一致。当前结果竖向仍是 140。`exit=0` 字从 37 到 77，框左是 24 | 本机 `127.0.0.1:17991`；`acceptance/desktop-exitback-before.png`、`desktop-exitback-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行，QUEEND 仍在框里，横向仍停在行尾 | 滚回最底不露出半行，行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。之前的结果 `exit=0` 墨水到画面 411，横滑条从 418 开始 | `acceptance/desktop-exitback-after.png` |
| 本轮完成 | 已经在最底下时，在之前的结果里再向下滚一下，不要带动整页，也不带动当前结果 | 到底后再向下滚，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向仍是 120，和可滚尽头一致。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastdrop-before.png`、`desktop-pastdrop-after.png` |
| 本轮完成 | 之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾；当前结果仍停在最底下，看得见的行仍是整行 | 再向下滚不改末行和行尾 | 之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-pastdrop-after.png` |
| 本轮完成 | 已经在最底下时，在当前结果里再向下滚一下，不要带动整页，也不带动之前的结果 | 当前结果到底后再向下滚，整页和之前的结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向仍是 140，和可滚尽头一致。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-resdrop-before.png`、`desktop-resdrop-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾 | 再向下滚不改两边的末行和行尾 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。之前的结果 `exit=0` 字从 37 到 77，框左是 24。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364 | `acceptance/desktop-resdrop-after.png` |
| 本轮完成 | 已经在行尾时，在之前的结果里再向右横滑一下，不要带动整页，也不带动当前结果 | 行尾再向右滑，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滑完仍是 40 和 40，文档横向仍是 0。框被竖条收窄时横向从 715 收到当时的尽头 730。当前结果竖向仍是 140。之前的结果竖向仍是 120 | 本机 `127.0.0.1:17991`；`acceptance/desktop-tailmore-before.png`、`desktop-tailmore-after.png` |
| 本轮完成 | 横向仍停在行尾，QUEEND 仍在框里；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 撤掉临时撑高后仍贴着行尾 | 横向回到 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-tailmore-after.png` |
| 本轮完成 | 再向左横滑一下，不要带动整页，也不带动当前结果 | 离开行尾时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滑完仍是 40 和 40，文档横向仍是 0。横向从 715 到 515，离尽头还差 200。当前结果竖向仍是 140。之前的结果竖向仍是 120 | 本机 `127.0.0.1:17991`；`acceptance/desktop-tailleft-before.png`、`desktop-tailleft-after.png` |
| 本轮完成 | 横向离开行尾，长行的 QUEEND 离开框的右边；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 离开行尾后末行仍留在左边 | `QUEEND` 在 927–967，顶 350、底 364，已经出了框的右边。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-tailleft-after.png` |
| 本轮完成 | 再向右滑回行尾，QUEEND 重新回到框里，不要带动整页，也不带动当前结果 | 滑回行尾时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滑完仍是 40 和 40，文档横向仍是 0。横向从 515 收到当时的尽头 730。当前结果竖向仍是 140。之前的结果竖向仍是 120。`QUEEND` 在 672–712 | 本机 `127.0.0.1:17991`；`acceptance/desktop-tailback-before.png`、`desktop-tailback-after.png` |
| 本轮完成 | 横向回到行尾；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 撤掉临时撑高后贴着行尾 | 横向回到 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-tailback-after.png` |
| 本轮完成 | 滑回行尾后，在之前的结果里再往上翻一行，不要带动整页，也不带动当前结果 | 翻走一行时整页和当前结果不动，行尾还在 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 120 到 100。横向仍是 715。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-tailup-before.png`、`desktop-tailup-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；这一屏变成 queqiao-old-6 和长行，QUEEND 仍在框里，横向仍停在行尾 | 翻走一行后行尾和当前结果还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。`queqiao-old-6` 字从 37 到 129，框左是 24，行在 347–367。长行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 370、底 384 | `acceptance/desktop-tailup-after.png` |
| 本轮完成 | 再滚回最底下，之前的结果里的 exit=0 重新出现并留在左边，不要带动整页，也不带动当前结果 | 滚到真正最底时整页和当前结果不动，行尾还在 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 100 回到 120，和可滚尽头一致。横向仍是 715。当前结果竖向仍是 140。`exit=0` 字在框的左边 | 本机 `127.0.0.1:17991`；`acceptance/desktop-taildown-before.png`、`desktop-taildown-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行，QUEEND 仍在框里，横向仍停在行尾 | 滚回最底不露出半行，行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387 | `acceptance/desktop-taildown-after.png` |
| 本轮完成 | 滚回最底下后，在当前结果里再往上翻一行，不要带动整页，也不带动之前的结果 | 翻当前结果不把整页和之前的结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。翻完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 140 到 120。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-resup-before.png`、`desktop-resup-after.png` |
| 本轮完成 | 之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾；当前结果看得见的行仍是整行 | 翻当前结果不丢掉之前的末行和行尾 | 之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。当前结果 `queqiao-now-7` 在 288–308，`queqiao-now-8` 在 308–328，都是整行 | `acceptance/desktop-resup-after.png` |
| 本轮完成 | 在当前结果里再滚回一行，不要带动整页，也不带动之前的结果 | 滚回一行不把整页和之前的结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 120 回到 140，和可滚尽头一致。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-resdown-before.png`、`desktop-resdown-after.png` |
| 本轮完成 | 当前结果回到最底下，看得见的行仍是整行；之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾 | 滚回最底不露出半行，之前的末行和行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364 | `acceptance/desktop-resdown-after.png` |
| 本轮完成 | 已经在最底下时，在当前结果里再向下滚一下，不要带动整页，也不带动之前的结果 | 当前结果到底后再向下滚，整页和之前的结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向仍是 140，和可滚尽头一致。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-resmore-before.png`、`desktop-resmore-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾 | 再向下滚不改两边的末行和行尾 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。前后画面逐像素相同 | `acceptance/desktop-resmore-after.png` |
| 本轮完成 | 已经在最底下时，在之前的结果里再向下滚一下，不要带动整页，也不带动当前结果 | 之前的结果到底后再向下滚，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向仍是 120，横向仍是 715。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastmore-before.png`、`desktop-pastmore-after.png` |
| 本轮完成 | 之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾；当前结果仍停在最底下，看得见的行仍是整行 | 再向下滚不改两边的末行和行尾 | 之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。前后画面逐像素相同 | `acceptance/desktop-pastmore-after.png` |
| 本轮完成 | 已经在行尾时，在之前的结果里再向右横滑一下，不要带动整页，也不带动当前结果 | 行尾再向右滑，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滑完仍是 40 和 40，文档横向仍是 0。框被竖条收窄时横向从 715 收到当时的尽头 730。当前结果竖向仍是 140。之前的结果竖向仍是 120 | 本机 `127.0.0.1:17991`；`acceptance/desktop-tailagain-before.png`、`desktop-tailagain-after.png` |
| 本轮完成 | 横向仍停在行尾，QUEEND 仍在框里；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 撤掉临时撑高后仍贴着行尾 | 横向回到 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。前后画面逐像素相同 | `acceptance/desktop-tailagain-after.png` |
| 本轮完成 | 再向左横滑一下，不要带动整页，也不带动当前结果 | 离开行尾时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滑完仍是 40 和 40，文档横向仍是 0。横向从 715 到 515，离当时的尽头还差 215。当前结果竖向仍是 140。之前的结果竖向仍是 120 | 本机 `127.0.0.1:17991`；`acceptance/desktop-leftagain-before.png`、`desktop-leftagain-after.png` |
| 本轮完成 | 横向离开行尾，长行的 QUEEND 离开框的右边；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 离开行尾后末行仍留在左边 | 撤掉临时撑高后横向停在 515，离尽头还差 200。`QUEEND` 在 927–967，顶 350、底 364，已经出了框的右边。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-leftagain-after.png` |
| 本轮完成 | 再向右滑回行尾，QUEEND 重新回到框里，不要带动整页，也不带动当前结果 | 滑回行尾时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滑完仍是 40 和 40，文档横向仍是 0。横向从 515 收到当时的尽头 730。当前结果竖向仍是 140。之前的结果竖向仍是 120。`QUEEND` 在 672–712 | 本机 `127.0.0.1:17991`；`acceptance/desktop-rightback-before.png`、`desktop-rightback-after.png` |
| 本轮完成 | 横向回到行尾；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 撤掉临时撑高后贴着行尾 | 横向回到 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-rightback-after.png` |
| 本轮完成 | 滑回行尾后，在之前的结果里再往上翻一行，不要带动整页，也不带动当前结果 | 翻走一行时整页和当前结果不动，行尾还在 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。翻完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 120 到 100。横向仍是 715。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-upagain-before.png`、`desktop-upagain-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；这一屏变成 queqiao-old-6 和长行，QUEEND 仍在框里，横向仍停在行尾 | 翻走一行后行尾和当前结果还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。`queqiao-old-6` 字从 37 到 129，框左是 24，行在 347–367。长行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 370、底 384 | `acceptance/desktop-upagain-after.png` |
| 本轮完成 | 再滚回最底下，之前的结果里的 exit=0 重新出现并留在左边，不要带动整页，也不带动当前结果 | 滚到真正最底时整页和当前结果不动，行尾还在 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 100 回到 120，和可滚尽头一致。横向仍是 715。当前结果竖向仍是 140。`exit=0` 字在框的左边 | 本机 `127.0.0.1:17991`；`acceptance/desktop-downagain-before.png`、`desktop-downagain-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行，QUEEND 仍在框里，横向仍停在行尾 | 滚回最底不露出半行，行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387 | `acceptance/desktop-downagain-after.png` |
| 本轮完成 | 滚回最底下后，在当前结果里再往上翻一行，不要带动整页，也不带动之前的结果 | 翻当前结果不把整页和之前的结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 255，在之前的结果上沿 306 的上面。翻完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 140 到 120。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-nowupagain-before.png`、`desktop-nowupagain-after.png` |
| 本轮完成 | 之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾；当前结果看得见的行仍是整行 | 翻当前结果不丢掉之前的末行和行尾 | 之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。当前结果 `queqiao-now-7` 在 288–308，`queqiao-now-8` 在 308–328，都是整行 | `acceptance/desktop-nowupagain-after.png` |
| 本轮完成 | 在当前结果里再滚回一行，不要带动整页，也不带动之前的结果 | 滚回一行不把整页和之前的结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 255，在之前的结果上沿 306 的上面。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 120 回到 140，和可滚尽头一致。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-resdownagain-before.png`、`desktop-resdownagain-after.png` |
| 本轮完成 | 当前结果回到最底下，看得见的行仍是整行；之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾 | 滚回最底不露出半行，之前的末行和行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364 | `acceptance/desktop-resdownagain-after.png` |
| 本轮完成 | 已经在最底下时，在当前结果里再向下滚一下，不要带动整页，也不带动之前的结果 | 当前结果到底后再向下滚，整页和之前的结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 255，在之前的结果上沿 306 的上面。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向仍是 140，和可滚尽头一致。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-resmoreagain-before.png`、`desktop-resmoreagain-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾 | 再向下滚不改两边的末行和行尾 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。前后画面逐像素相同 | `acceptance/desktop-resmoreagain-after.png` |
| 本轮完成 | 已经在最底下时，在之前的结果里再向下滚一下，不要带动整页，也不带动当前结果 | 之前的结果到底后再向下滚，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向仍是 120，横向仍是 715。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastmoreagain-before.png`、`desktop-pastmoreagain-after.png` |
| 本轮完成 | 之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾；当前结果仍停在最底下，看得见的行仍是整行 | 再向下滚不改两边的末行和行尾 | 之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。前后画面逐像素相同 | `acceptance/desktop-pastmoreagain-after.png` |
| 本轮完成 | 已经在行尾时，在之前的结果里再向右横滑一下，不要带动整页，也不带动当前结果 | 行尾再向右滑，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滑完仍是 40 和 40，文档横向仍是 0。框被竖条收窄时横向从 715 收到当时的尽头 730。当前结果竖向仍是 140。之前的结果竖向仍是 120 | 本机 `127.0.0.1:17991`；`acceptance/desktop-rightmoreagain-before.png`、`desktop-rightmoreagain-after.png` |
| 本轮完成 | 横向仍停在行尾，QUEEND 仍在框里；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 撤掉临时撑高后仍贴着行尾 | 横向回到 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。前后画面逐像素相同 | `acceptance/desktop-rightmoreagain-after.png` |
| 本轮完成 | 再向左横滑一下，不要带动整页，也不带动当前结果 | 离开行尾时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滑完仍是 40 和 40，文档横向仍是 0。横向从 715 到 515，离当时的尽头还差 215。当前结果竖向仍是 140。之前的结果竖向仍是 120 | 本机 `127.0.0.1:17991`；`acceptance/desktop-leftmoreagain-before.png`、`desktop-leftmoreagain-after.png` |
| 本轮完成 | 横向离开行尾，长行的 QUEEND 离开框的右边；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 离开行尾后末行仍留在左边 | 撤掉临时撑高后横向停在 515，离尽头还差 200。`QUEEND` 在 927–967，顶 350、底 364，已经出了框的右边。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-leftmoreagain-after.png` |
| 本轮完成 | 再向右滑回行尾，QUEEND 重新回到框里，不要带动整页，也不带动当前结果 | 滑回行尾时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滑完仍是 40 和 40，文档横向仍是 0。横向从 515 收到当时的尽头 730。当前结果竖向仍是 140。之前的结果竖向仍是 120。`QUEEND` 在 672–712 | 本机 `127.0.0.1:17991`；`acceptance/desktop-rightbackagain-before.png`、`desktop-rightbackagain-after.png` |
| 本轮完成 | 横向回到行尾；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 撤掉临时撑高后贴着行尾 | 横向回到 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。`exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-rightbackagain-after.png` |
| 本轮完成 | 滑回行尾后，在之前的结果里再往上翻一行，不要带动整页，也不带动当前结果 | 翻走一行时整页和当前结果不动，行尾还在 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。翻完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 120 到 100。横向仍是 715。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastupagain-before.png`、`desktop-pastupagain-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；这一屏变成 queqiao-old-6 和长行，QUEEND 仍在框里，横向仍停在行尾 | 翻走一行后行尾和当前结果还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。`queqiao-old-6` 字从 37 到 129，框左是 24，行在 347–367。长行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 370、底 384 | `acceptance/desktop-pastupagain-after.png` |
| 本轮完成 | 再滚回最底下，之前的结果里的 exit=0 重新出现并留在左边，不要带动整页，也不带动当前结果 | 滚到真正最底时整页和当前结果不动，行尾还在 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，低于当前结果下沿 289。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向从 100 回到 120，和可滚尽头一致。横向仍是 715。当前结果竖向仍是 140。`exit=0` 字在框的左边 | 本机 `127.0.0.1:17991`；`acceptance/desktop-pastdownagain-before.png`、`desktop-pastdownagain-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行，QUEEND 仍在框里，横向仍停在行尾 | 滚回最底不露出半行，行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387 | `acceptance/desktop-pastdownagain-after.png` |
| 本轮完成 | 滚回最底下后，在当前结果里再往上翻一行，不要带动整页，也不带动之前的结果 | 翻当前结果不把整页和之前的结果带走 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 255，在之前的结果上沿 306 的上面。翻完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 140 到 120。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-resupagain-before.png`、`desktop-resupagain-after.png` |
| 本轮完成 | 之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾；当前结果看得见的行仍是整行 | 翻当前结果不丢掉之前的末行和行尾 | 之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。当前结果 `queqiao-now-7` 在 288–308，`queqiao-now-8` 在 308–328，都是整行 | `acceptance/desktop-resupagain-after.png` |
| 本轮完成 | 在当前结果里再滚回一行，不要带动整页，也不带动之前的结果 | 滚回一行时整页和之前的结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 255，在之前的结果上沿 306 的上面。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向从 120 回到 140，和可滚尽头一致。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-nowdownagain-before.png`、`desktop-nowdownagain-after.png` |
| 本轮完成 | 当前结果回到最底下，看得见的行仍是整行；之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾 | 滚回最底不露出半行，行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。`queqiao-now-7` 已经离开这一屏。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364 | `acceptance/desktop-nowdownagain-after.png` |
| 本轮完成 | 已经在最底下时，在当前结果里再向下滚一下，不要带动整页，也不带动之前的结果 | 到底后再向下滚，整页和之前的结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 255，在之前的结果上沿 306 的上面。滚完仍是 40 和 40，文档横向仍是 0。当前结果竖向仍是 140，和可滚尽头一致。之前的结果竖向仍是 120，横向仍是 715 | 本机 `127.0.0.1:17991`；`acceptance/desktop-nowmoreagain-before.png`、`desktop-nowmoreagain-after.png` |
| 本轮完成 | 当前结果仍停在最底下，看得见的行仍是整行；之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾 | 多滚一下不露出半行，行尾还在 | 当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364 | `acceptance/desktop-nowmoreagain-after.png` |
| 本轮完成 | 已经在最底下时，在之前的结果里再向下滚一下，不要带动整页，也不带动当前结果 | 到底后再向下滚，整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，在当前结果下沿 289 的下面。滚完仍是 40 和 40，文档横向仍是 0。之前的结果竖向仍是 120，和可滚尽头一致。横向仍是 715。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-oldmoreagain-before.png`、`desktop-oldmoreagain-after.png` |
| 本轮完成 | 之前的结果仍停在最底下，exit=0 仍留在左边，QUEEND 仍在框里，横向仍停在行尾；当前结果仍停在最底下，看得见的行仍是整行 | 多滚一下不露出半行，行尾还在 | 之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。横向 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-oldmoreagain-after.png` |
| 本轮完成 | 已经在行尾时，在之前的结果里再向右横滑一下，不要带动整页，也不带动当前结果 | 行尾再向右滑，整页和当前结果不动，钉在新的行尾 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，在当前结果下沿 289 的下面。框被收窄后可滑尽头是 730，当时横向是 715。再向右滑一下后仍是 40 和 40，文档横向仍是 0。横向变成 730，和这时的尽头一致。之前的结果竖向仍是 120。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-rightmoreagain-before.png`、`desktop-rightmoreagain-after.png` |
| 本轮完成 | 横向仍停在行尾，QUEEND 仍在框里；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 多滑一下不离开行尾 | 收窄时 `QUEEND` 在 672–712，在框里。撤掉撑高后横向回到 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-rightmoreagain-after.png` |
| 本轮完成 | 再向左横滑一下，不要带动整页，也不带动当前结果 | 离开行尾时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，在当前结果下沿 289 的下面。框被收窄后可滑尽头是 730，当时横向是 715。向左滑一下后仍是 40 和 40，文档横向仍是 0。横向变成 515，离这时的尽头还有 215。之前的结果竖向仍是 120。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-oldleftagain-before.png`、`desktop-oldleftagain-after.png` |
| 本轮完成 | 横向离开行尾，长行的 QUEEND 离开框的右边；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 行尾离开后短行仍留在左边 | 收窄时 `QUEEND` 在 887–927，已经离开框的右边。撤掉撑高后横向是 515，可滑尽头 715，还差 200。`QUEEND` 在 927–967，顶 350、底 364。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-oldleftagain-after.png` |
| 本轮完成 | 再向右滑回行尾，不要带动整页，也不带动当前结果 | 滑回行尾时整页和当前结果不动 | 整页先放在纵向 40、横向 40，下面还能滚 337、左右还能滑 1023。落点纵向 314，在当前结果下沿 289 的下面。框被收窄后可滑尽头是 730，当时横向是 515，还差 215。再向右滑一下后仍是 40 和 40，文档横向仍是 0。横向变成 730，和这时的尽头一致。之前的结果竖向仍是 120。当前结果竖向仍是 140 | 本机 `127.0.0.1:17991`；`acceptance/desktop-oldrightback-before.png`、`desktop-oldrightback-after.png` |
| 本轮完成 | QUEEND 重新回到框里，横向回到行尾；之前的结果仍停在最底下，exit=0 仍留在左边；当前结果仍停在最底下，看得见的行仍是整行 | 行尾回来后短行仍留在左边 | 收窄时 `QUEEND` 在 672–712，重新回到框里。撤掉撑高后横向回到 715，和尽头一致。`QUEEND` 在 727–767，顶 350、底 364。之前的结果 `exit=0` 字从 37 到 77，框左是 24，行在 367–387。当前结果 `queqiao-now-8` 在 288–308，`exit=0` 在 308–328，都是整行 | `acceptance/desktop-oldrightback-after.png` |
| 未完成 | 复制连接凭证写入剪贴板 | 亲友能把整段发给对方 | 成功提示「已复制，请完整发给对方。」且读回内容一致；失败不报成功，原文框获焦 | 失败路径已验。成功写入未出现。本机 `OpenClipboard` 错误 5，本轮未当作已修好 |

深色主题：样式没有 `prefers-color-scheme`，本轮不验、不加。系统缩放：当时窗口 DPI=96（100%），未改系统缩放。

## 明确暂缓

完整远程桌面、计费/组织、改 AI-SSH-Pro。






