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
| R014 | 高频：Avalonia 账号登录免密连自有设备；打磨：最近审计人话 | 鹊桥-客户端 | **wip** | `docs/round-014.md` |

## P0 — 可演示闭环（不后挪）

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 公网中继访客配对（code+密码）+ 透明转发 | 鹊桥-服务端 | **done** | 两机经中继可配对；协议与 `relay-protocol.md` 一致；`go test ./internal/relay` PASS |
| 同一 App 双模式 UI + 安装包 | 鹊桥-客户端 | **done（R008）** | 安装后可选接受/去协助；被控展示 code+密码；见 `round-008.md` |
| 业务 TLS+指纹；写操作本机确认 | 鹊桥-客户端 | **done（R004 文案）** | Assist 写命令弹确认；拒绝则不执行。验收文案见 PRODUCT.md：对方想在这台电脑执行操作 + 白话说明 + 允许/拒绝；超时不自动允许；拒绝后提示「已拒绝，未做任何改动」 |
| 客户端/MCP 对接 JSONL 中继（访客 code+密码） | 鹊桥-客户端 | **done** | RelayJsonlIntegrationTests + 本机 relay smoke；MCP --relay/--code/--pwd |
| MCP stdio 连会话并调只读/提议命令 | 鹊桥-客户端 | **done（R008）** | Agent 能连上并完成至少：列进程、读日志、提议命令走确认；见 `round-008.md` / McpStdioLoopIntegrationTests |
| 被控一键复制「会话码+密码」纯文本 | 鹊桥-客户端 | **done（R001）** | 见 `round-001.md` |
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

场景闭环见 `docs/mac-linux-scenes.md`。R010 Linux Avalonia 被控窗已产品验收；osx 已出包未上真机，不写成 Mac 已验收；R011 Linux Avalonia 双模式已产品验收；osx 已出包未上真机，不写成 Mac 已验收；R012 已产品验收 Avalonia 信任等级 + Windows P2-7；R013 已产品验收 Avalonia 已确认改动回看 + 刷新确认；下一步 R014 Avalonia 账号登录免密 + 审计人话。

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 技术栈与体积方案 | 鹊桥-架构 | **done** | `architecture.md` §13：Mac/Linux=Avalonia；Windows 暂留 WinForms；默认小包依赖运行时（目标约 1.4 MB 档）；自包含备选上限约 72 MB；macOS `.dmg`，Linux `.tar.gz` |
| Mac/Linux 被控端 + MCP（先） | 鹊桥-客户端 | **done（Linux：R009+R010 产品已验收；osx 已出包未上真机，不写成 Mac 已验收）** | 经默认中继配对；只读查询；写操作本机确认；命令行（CLI/MCP）与 Avalonia 界面都要，不是二选一。R009 CLI + R010 Avalonia 被控窗已产品验收（Linux）。osx 已出包未上真机。默认发依赖 .NET 8 运行时的小包（附安装说明）；自包含为备选。双模式 GUI 见下一平台项 |
| Mac/Linux 双模式 GUI（后） | 鹊桥-客户端 | **done（Linux：R011 产品已验收；osx 已出包未上真机，不写成 Mac 已验收）** | 与 Windows 对等：接受协助 / 去协助；见 `round-011.md` |

## P2 — 体验（backlog，按易用性排序）

1. **P2-1** 协助方一键复制「安装包链接/目录说明」文案 → **done（R004）**
2. **P2-2** 首次启动防火墙/失败文案 → 已并入 R003（`round-003.md` 时刻 A）
3. **P2-3** 被控复制载荷/二维码体验 → 部分并入 R003（分享区只留主按钮，其余进「更多」）
4. **P2-4 / S001** 被控本机回看本次会话已确认的写操作审计（客户支持收尾；未确认的提议不得显示为已执行；不做云端工单）→ **done（R007）**
5. **P2-5** Skill 薄封装（仅安装 MCP 指引，非控制面）→ 界面三步指引 **done（R005）**；Skill 封装仍 backlog
6. **P2-6 / 质感** 刷新不抢点击、账号标签与选机、协助端白话操作、首页可点卡片 → **done（R006）** 见 `round-006.md` / `quality.md`
7. **P2-7** 完全信任主界面操作改为「结束完全信任」+ 二次确认正文补全（见 trust-levels.md 2026-11-05 修正）→ **done（R012）**

## 官网与接单（低配机，轻量）

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 技术栈与部署（端口 80、安全组、独立 Go 进程） | 鹊桥-架构 | **done** | `architecture.md` §14 与 `docs/site-deploy.md`：`remote-assist-site`、:80、site.db、口令走 /etc/remote-assist-site.env、MemoryMax=64M |
| 静态介绍页 + 接单表单 | 鹊桥-服务端 | **done** | `site/` 静态页；文案对齐 `docs/site-copy.md` |
| 接单 API + /admin + 上机 | 鹊桥-服务端 | **done** | 2026-11-05 公网浏览器验收通过（对照 site.md / site-copy.md）：介绍页与 /ask 文案对齐；两次例行探活提交成功并到 /thanks（称呼 例行探活，微信 qiaoqiao_probe_20261105，非真客户勿跟进）；/admin 可登录、改状态/备注，同一微信聚合为一条客户（历史条数 2）。状态仅 新留言/已联系/已结束。中继 17891 仍通。 |
## 明确暂缓

完整远程桌面、计费/组织、改 AI-SSH-Pro。



