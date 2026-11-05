# 鹊桥 — 路线图（产品优先级）

> 以 `docs/PRODUCT.md` 为准。改优先级先改本文档再 @ 实现 bot。  
> 状态：`done` / `wip` / `todo`（中继侧进度经本机 `go test ./internal/relay` 核对，2026-11-05）。  
> **持续迭代**：每轮见 `docs/round-NNN.md`；复盘见 `docs/retro.md`。产品 bot 驱动选题与验收，实现 bot 写代码。

## 当前迭代

| 轮次 | 项 | 负责 | 状态 | 文档 |
|------|----|------|------|------|
| R001 | 被控「一键复制码+密码」+ 品牌文案「鹊桥」 | 远端协助 | **done** | `docs/round-001.md` |

## P0 — 可演示闭环

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 公网中继访客配对（code+密码）+ 透明转发 | 远端协助-并行 | **done** | 两机经中继可配对；协议与 `relay-protocol.md` 一致；`go test ./internal/relay` PASS |
| 同一 App 双模式 UI + 安装包 | 远端协助 | wip | 安装后可选接受/去协助；被控展示 code+密码 |
| 业务 TLS+指纹；写操作本机确认 | 远端协助 | wip | Assist 写命令弹确认；拒绝则不执行 |
| 客户端/MCP 对接 JSONL 中继（访客 code+密码） | 远端协助 | **done** | RelayJsonlIntegrationTests + 本机 relay smoke；MCP --relay/--code/--pwd |
| MCP stdio 连会话并调只读/提议命令 | 远端协助 | wip | Agent 能连上并完成至少：列进程、读日志、提议命令走确认 |
| 被控一键复制「会话码+密码」纯文本 | 远端协助 | **done（R001）** | 见 `round-001.md`；截图 r001-after-* |
| 界面品牌统一「鹊桥」 | 远端协助 | **done（R001）** | 见 `round-001.md`；安装包 AppName「鹊桥远程协助」 |

## P1 — 账号与免密

| 项 | 负责 | 状态 | 验收 |
|----|------|------|------|
| 中继：注册/登录/token、设备上线、列表、免密连接 | 远端协助-并行 | **done** | signup/login/device_online/list_devices/connect_device + 测试覆盖 |
| 客户端：注册/登录、设备上线、列表、免密连接 | 远端协助 | **done（最小闭环）** | UI 登录/列表/免密 + MCP --token/--device-id；集成测 Account_Login_List_Connect |
| 自有设备跳过配对确认窗；写操作仍确认 | 两端 | **待验收**（AgentHost 已有 ownerLink 自动授予） | 见 PRODUCT.md；R002 专项验 |
| 设置项：默认/自定义中继地址 | 远端协助 | **done** | 默认 `121.196.45.81:17891`；UI/设置/MCP/`--relay` 可覆盖 |

## P2 — 体验（backlog，按易用性排序）

1. **P2-1** 协助方一键复制「安装包链接/目录说明」文案
2. **P2-2** 首次启动防火墙放行引导（截图已见拦窗）
3. **P2-3** 被控「复制载荷 / 二维码」体验打磨（载荷已有按钮，文案可更亲友向）
4. **P2-4** 审计日志查看增强
5. **P2-5** Skill 薄封装（仅安装 MCP 指引，非控制面）

## 明确暂缓

完整远程桌面、计费/组织、改 AI-SSH-Pro。