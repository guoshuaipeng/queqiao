# 鹊桥远程协助 · 智能体 — 架构总览

> 现状固化（2026-11-05 Asia/Shanghai）。产品决策以 `PRODUCT.md` 为准；协议细节见 `relay-protocol.md`；生产部署见 `relay-deploy.md`。  
> 本文只描述**已拍板的系统边界与组件关系**，不替代上述文档。改架构先改 docs，再通知开发 / 产品 bot。

## 1. 一句话

**命令 + MCP 控制面**的远程协助：被控端轻量常驻，协助方（人或 Agent）经公网中继配对后查进程/日志/文件、提议命令；**写操作必须在被控本机确认**。不是远程桌面，不改 AI-SSH-Pro。

## 2. 组件与仓库布局

| 组件 | 技术 | 仓库路径 / 产物 | 职责 |
|------|------|-----------------|------|
| 统一 App（双模式） | .NET 8 | `src/`、安装包 `installer/` | 接受协助 / 去协助；会话码与密码、账号设备列表、确认窗、审计入口 |
| Helper / AgentHost | .NET 8 | `src/`（及兼容入口） | 被控侧执行只读查询与经确认的写操作；业务 TLS |
| MCP（stdio） | .NET 8 | 安装包可选启用 | 供协助方 Agent 使用；与 UI 共用连接与确认流程 |
| 公网中继 | Go + SQLite | `relay/` | JSONL 控制配对 → 透明双向 pipe；账号/设备轻量存储 |
| 局域网直连 | .NET | 历史端口 `17890`，`via=lan` | 高级/调试，非默认路径 |

约定：架构与产品决策写进本仓库 `docs/`，**不靠 bot 记忆**。架构 bot 不写业务代码、不上机部署。

## 3. 连接拓扑

```
协助方 (App UI / MCP)                    被控方 (App / Helper)
        |                                         |
        |  TCP :17891（JSONL 控制 → opaque pipe）  |
        +------------------> 公网中继 <-------------+
                              (Go)
                    业务 TLS 端到端骑在 pipe 内
                    中继不解析业务帧
```

| 模式 | 默认？ | 配对方式 | 备注 |
|------|--------|----------|------|
| 公网中继 · 访客 | 是（无账号时） | `guest_host` / `guest_join`（code + 密码） | 刷新会话码须同时换密码 |
| 公网中继 · 账号 | 是（有账号时） | `login`/`auth_token` → `device_online` / `list_devices` / `connect_device` | 同账号免密连在线设备 |
| 局域网直连 | 否 | 历史 LAN 路径 | 仅高级/调试 |

生产中继（已验收）：`121.196.45.81:17891`，systemd `remote-assist-relay`，SQLite `/var/lib/remote-assist-relay/relay.db`。客户端 / MCP 默认同此地址，可覆盖。详见 `relay-deploy.md`。

## 4. 协议分层

1. **中继控制面**：单 TCP；配对前 **JSON Lines**（`signup` / `login` / `auth_token` / `device_*` / `guest_*`）；配对成功两端收到 `paired` 后切 **原始双向字节管道**。契约：`relay-protocol.md`。
2. **业务面**：管道内业务帧（4 字节 LE 长度 + UTF-8 JSON）+ **TLS（自签 + 证书指纹钉扎）**。`--insecure` 仅调试。中继只转发，不终结业务 TLS。
3. **MCP**：stdio 工具映射到同一客户端协议；`run_command` 等写路径走本机确认。连接参数支持中继载荷（含 `via=relay&pwd=`）及 token / device-id。详见 `MCP.md`。

## 5. 鉴权与权限

### 5.1 接入身份

- **访客**：中继侧 code + 密码（密码 ≥ 4）；无账号体系。
- **账号**：中继轻量 `signup`/`login`，`token` 约 7 天；设备以稳定 `device_id` 上线。MVP **不做**完整用户中心 / 计费 / 组织工单。

### 5.2 本机权限三级

| 级别 | 含义 |
|------|------|
| Guest | 访客只读 |
| Assist（默认） | 写操作本机确认，不可由中继或协助方绕过 |
| Admin | 可跳过部分确认（仍审计）；仅被控方主动授予 |

账号免密连**自己的机器**：可跳过「配对确认窗」（`ownerLink` 自动批准 Assist 会话）；**写操作确认仍不可绕过**（除非对方主动授 Admin）。中继配对成功 ≠ 授权写操作。

## 6. 数据与审计

| 位置 | 存什么 | 不存什么 |
|------|--------|----------|
| 中继 SQLite | 账号、token、设备元数据、访客会话等待态 | 业务命令内容、业务 TLS 明文、密码明文日志 |
| 被控本机 | 审计日志、本地设置、证书指纹信任 | 不向中继回传写操作明文「代执行」 |

敏感路径可读拒绝；密码字段不得写入日志（协议约定）。

## 7. 安全底线（不可破）

- 中继**不得**代执行写操作或绕过本机确认。
- 业务 TLS 端到端；本阶段不做中继侧 TLS 终结业务流量、不做正式 CA 互认。
- SSH / 密码 / 私钥不进仓库、不进聊天转发；凭据走保险箱或用户本机操作。
- 生产防火墙 / 安全组：**仅放行 TCP 17891**（入站）。

## 8. 明确不做（当前阶段）

- 完整远程桌面 / 键鼠注入
- 多实例中继、域名与正式 CA、负载均衡
- 完整云端用户中心 / 计费 / 组织工单
- 用纯 Skill 替代 MCP/协议作为控制面
- 修改 AI-SSH-Pro 本体

## 9. 文档地图

| 文档 | 内容 |
|------|------|
| `PRODUCT.md` | 品牌、定位、连接与权限产品决策 |
| `architecture.md`（本文） | 组件、拓扑、分层、边界总览 |
| `relay-protocol.md` | 中继 JSONL 控制契约 |
| `relay-deploy.md` | 生产单机部署与验收 |
| `relay.md` | 本机跑中继 / 开发入口 |
| `MCP.md` | MCP 启用与参数 |
| `roadmap.md` | P0/P1/P2 优先级与状态 |
| `dev-rules.md` | 开发协作约定 |

## 10. 变更流程

1. 在 `docs/` 拍板并更新本文及相关专题文档。
2. 知会「鹊桥-产品」（优先级）、「鹊桥-客户端」（.NET）、「鹊桥-服务端」（Go 中继 / 部署）。
3. 开发侧改代码与上机；架构侧不上机写业务。

## 11. 协作角色（开发分工）

| 角色 | 负责 |
|------|------|
| 鹊桥-架构 | 对齐边界，写入 `docs/`；不写业务代码、不上机 |
| 鹊桥-产品 | 定位、优先级、路线图 |
| 鹊桥-客户端（原「远端协助」） | `src/`、统一 App / Helper / MCP / `installer/`；对接 `relay-protocol.md` |
| 鹊桥-服务端（原「远端协助-并行」） | `relay/` Go 中继、协议实现、生产部署（`relay-deploy.md`） |

协议变更：先改 `relay-protocol.md`，再由服务端改 Go，客户端跟随文档。细节见 `dev-rules.md`。

## 12. 客户端窗口位置（2026-11-05 拍板）

统一 App（`RemoteAssist.exe`）**记住上次窗口所在的显示器、位置和大小**，下次启动还原到那里。

- 目的：本机多屏开发/使用时，不要每次都弹到主屏打断当前工作。
- 范围：主窗口（接受协助 / 去协助首页及主界面）。确认窗跟随主窗口所在屏，避免跑到另一块屏。
- 存储：本机用户设置（与现有本地设置一起即可）；不写中继、不进仓库。
- 退化：所记录显示器已断开，或还原后窗口完全不可见 → 回退到当前主屏可见区域（不要生成看不见的窗口）。
- 实现：`鹊桥-客户端`（`src/`）；不改 `relay/`。
- 实现落点：HelperUiSettings.WindowPlacement（%LocalAppData%/RemoteAssist/helper-ui.json）+ src/UiShared/UiWindowPlacement.cs（App/Agent/Helper.UI Link）+ WindowPlacementMath（可单测）。确认窗 PlaceDialog 跟随 UiWindowPlacement.MainWindow 所在工作区。

## 13. 跨平台客户端（macOS / Linux，2026-11-05 拍板）

产品范围见 `PRODUCT.md`「支持平台」：正式要做，**不阻塞**当前 Windows P0 演示。分两步：先 Mac/Linux **被控端 + MCP**（写操作同样本机确认），再对等双模式 GUI。不拆三套原生重写。Windows 上正在进行的工作（如 R001、窗口位置记忆）不停。

### 13.1 GUI 栈

- **macOS / Linux**：用 **Avalonia**（.NET）做被控端状态窗与确认窗，后续双模式 GUI 也走同一套。
- **Windows**：现阶段保持现有 WinForms（`net8.0-windows`），不在 P0 把 Windows 迁到 Avalonia。
- 共享层继续是 `net8.0` 的 Protocol / Helper / MCP；平台 UI 只包确认与展示。
- 确认窗：Mac/Linux 用 Avalonia 实现与 Windows 相同的 Assist 规则（写操作本机确认，不可由 MCP/中继绕过）。不要以 osascript / zenity 脚本作为正式方案。

### 13.2 分发形态

默认按「对方没装 .NET 运行时」发布（自包含或等价）。

| 平台 | 第一期 | 随后可加 |
|------|--------|----------|
| Windows | 现有 Inno Setup 安装包（`RemoteAssist-Setup-*.exe`） | — |
| macOS | `.app` 放入 `.dmg` | 以后再考虑公证 / 签名 |
| Linux | 便携 `.tar.gz` | 可选 `.deb` |

`src-net48` 只作 Windows 体积对比，不上 Mac/Linux。

### 13.3 体积（发布验收）

实测基线（本机 `artifacts/`，2026-11-05）：

| 产物 | 大小 |
|------|------|
| `RemoteAssist-Setup-0.1.0.exe`（Windows 自包含安装包） | **约 72 MB** |
| `RemoteAssist-Agent-Setup-0.1.0.exe` | 约 63.5 MB |
| `app-win-x64-sc/RemoteAssist.exe`（自包含单文件） | 约 77 MB |

结论：**不会「很小」（不是几 MB）**。自包含必须带上 .NET 运行时，Avalonia 也是同类桌面栈，各平台分发包的数量级仍是数十 MB。

验收目标：每个平台的对外分发包体积 **不差于** 上述 Windows 自包含安装包（约 72 MB）。允许略大，但发布说明里要写实测数字。

压体积手段（按优先级）：

1. 自包含 + `PublishTrimmed` + 单文件（`PublishSingleFile`），按 RID 出包（至少 `osx-arm64` / `osx-x64` / `linux-x64`，按需 `linux-arm64`）。
2. 去掉调试符号与不需要的附属 exe。
3. **Native AOT** 作为后续试验，不作为第一期必达（UI + 反射/TLS 风险较高）。

实现节奏：Windows 演示与 R001 优先；Mac/Linux 按产品「平台」轨道排期，由「鹊桥-客户端」做，不改 `relay/`。
