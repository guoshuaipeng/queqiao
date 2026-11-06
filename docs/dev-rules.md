# 开发约定

## 协作（多 bot）
- 规范化开发迭代步骤见 `docs/dev-iteration.md`（选题/授权/验收全流程；本文只留短规则）。

- 决策与分工一律写进本仓库 `docs/`，**禁止只依赖 bot 记忆**。
- 改动前先在文档标明本轮负责模块，避免互相覆盖。
- **不改 AI-SSH-Pro**；本仓库独立演进。
- 本机开发；推送前看清未提交文件。

## 当前分工（2026-11-05 更新）

| 角色 | 负责 | 不负责 |
|------|------|--------|
| **鹊桥-客户端**（原「远端协助」） | .NET 客户端：`src/`、统一 App / UI、Helper / AgentHost、MCP、`installer/`；按 `docs/relay-protocol.md`（JSONL）对接中继；默认中继地址见 `docs/relay-deploy.md` | 不改 `relay/`，不上生产中继 |
| **鹊桥-服务端**（原「远端协助-并行」） | Go 中继：`relay/`、JSONL 控制实现、SQLite、按 `docs/relay-deploy.md` 部署生产中继 | 不改 `src/`、`installer/`、MCP/UI |
| **鹊桥-架构** | 对齐边界，产出写入 `docs/`（如 `architecture.md`、`relay-protocol.md`、`relay-deploy.md`） | 不写业务功能代码、不上机 |
| **鹊桥-产品** | 定位、优先级、路线图（`PRODUCT.md`、`roadmap.md`） | 不写业务代码 |

边界：服务端 **不改** `src/`；客户端 **不改** `relay/`。中继协议变更必须先更新 `relay-protocol.md`，再改 Go；客户端跟随该文档。

## 技术红线

- **默认连接路径 = 公网中继**（访客 code+密码，或账号设备列表免密）；LAN 仅可选/调试。
- Assist 写操作必须被控本机确认，不可由 MCP/中继绕过。
- 密钥、会话码、审计日志、`relay/data/*.db` 不进 git。
- 控制协议为 **JSONL**（不是空格分隔文本行）。