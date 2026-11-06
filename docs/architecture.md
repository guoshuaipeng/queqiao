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
2. **业务面**：管道内业务帧（4 字节 LE 长度 + UTF-8 JSON）+ **TLS（自签 + 证书指纹钉扎）**。`--insecure` 仅显式调试。中继只转发，不终结业务 TLS。指纹为被控端临时业务证书，经完整载荷分享；规则见 **`tls-trust.md`** 与本文 §15。
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
| 中继 SQLite | 账号、token、设备元数据、访客会话等待态；访客会话码审计事件（表 `guest_audit`，见 `relay-session-audit.md`） | 业务命令内容、业务 TLS 明文、密码明文、完整 pair 载荷、`fp`、v1 不存连接 IP |
| 被控本机 | 审计日志、本地设置、证书指纹信任 | 不向中继回传写操作明文「代执行」 |
| 官网 site SQLite | 接单客户/留言 | 会话码审计（不与中继共库） |

敏感路径可读拒绝；密码字段不得写入日志（协议约定）。

## 7. 安全底线（不可破）

- 中继**不得**代执行写操作或绕过本机确认。
- 业务 TLS 端到端；本阶段不做中继侧 TLS 终结业务流量、不做正式 CA 互认。
- **E2E 业务 TLS + Agent 临时证书指纹钉扎**（经完整安全载荷 `fp=` 分享）；短凭证只授权会话、**不能单独**建安全业务通道；**禁止 TOFU**；**禁止**因缺指纹自动降明文 / skipTls。细则见 `tls-trust.md` / §15。
- SSH / 密码 / 私钥不进仓库、不进聊天转发；凭据走保险箱或用户本机操作。
- 生产防火墙 / 安全组：**仅放行 TCP 17891**（入站）。官网另放行 TCP 80，见 `site-deploy.md`。

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
| `tls-trust.md` | 业务 TLS 指纹与短凭证信任模型 |
| `quality-connection-credential.md` | 连接凭证（码-8位验证码）人话形态 |
| `relay-session-audit.md` | 中继会话码审计（R046；旁路只读 HTTP） |
| `site.md` | 官网与接单产品范围 |
| `site-deploy.md` | 官网独立进程部署 |

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
- **Linux 双形态**（2026-11-05 补充）：同时支持 **命令行** 与 **图形界面**，不是二选一。
  - 命令行：无界面被控端 / Helper CLI，以及 MCP stdio（给协助方 Agent 用）；写操作仍须本机确认（可用终端提示或拉起确认窗）。
  - 界面：Avalonia 被控状态与确认，以及后续对等的双模式 GUI。
  - 两者共用同一套 `net8.0` 协议与中继对接；可只装 CLI，也可同时提供带界面的包。
  - macOS 同样保留 CLI（含 MCP）+ Avalonia 界面，节奏与 Linux 一致：先 CLI/MCP + 确认，再补对等 GUI。
- **Windows**：现阶段保持现有 WinForms（`net8.0-windows`），不在 P0 把 Windows 迁到 Avalonia。
- 共享层继续是 `net8.0` 的 Protocol / Helper / MCP；平台 UI 只包确认与展示。
- 确认窗：Mac/Linux 用 Avalonia 实现与 Windows 相同的 Assist 规则（写操作本机确认，不可由 MCP/中继绕过）。不要以 osascript / zenity 脚本作为正式方案。

### 13.2 分发形态

| 平台 | 第一期 | 随后可加 |
|------|--------|----------|
| Windows | 现有 Inno Setup 安装包；小体积路径继续用 `src-net48` 依赖系统自带的 .NET Framework 4.8 | 自包含 .NET 8 安装包（给没装 4.8 桌面组件、或要免装运行时的场景） |
| macOS | `.app` 放入 `.dmg` | 以后再考虑公证 / 签名 |
| Linux | 便携 `.tar.gz` | 可选 `.deb` |

`src-net48` / .NET Framework 4.8 **不能**用在 Mac/Linux（那是 Windows 才有的系统组件）。Mac/Linux 要对齐「几兆」体量，只能发 **依赖已安装的 .NET 8（含桌面/Avalonia 所需运行时）** 的包，运行时用系统包管理器或官方运行时安装器装一次。

### 13.3 体积（发布验收）

实测基线（本机 `artifacts/`，2026-11-05）：

| 产物 | 大小 | 含义 |
|------|------|------|
| `app-net48-fd/`（依赖 .NET Framework 4.8） | **约 1.4 MB** | Windows「几兆」对比版：不打进运行时，用系统自带 4.8 |
| `RemoteAssist-Setup-0.1.0.exe`（.NET 8 自包含安装包） | 约 72 MB | 把运行时打进安装包，对方不用先装 .NET 8 |
| `RemoteAssist-Agent-Setup-0.1.0.exe` | 约 63.5 MB | 同上，Agent 安装包 |
| `app-win-x64-sc/RemoteAssist.exe` | 约 77 MB | .NET 8 自包含单文件 |

用户补充（2026-11-05）：**macOS / Linux 尽量保持与 Windows net48 对比版同一量级（约 1.4 MB 这一档），而不是默认 72 MB 自包含。**

要做到「尽量这么小」：

1. Mac/Linux **默认发依赖运行时的小包**（framework-dependent）：应用本身目标尽量接近 `app-net48-fd` 的约 1.4 MB；Avalonia 与原生依赖可能导致略大，发布时写实测数字，应力争个位数 MB，而不是几十 MB。
2. 运行时不打进应用包。对方机器需已有 **.NET 8 桌面方向运行时**（macOS 可用官方安装包或 Homebrew；Linux 用发行版/微软包）。首次使用要有简短安装说明，避免只弹英文「Download runtime」却讲不清。
3. **自包含包保留为备选**（给不能/不愿装运行时的人），体积以不差于当前 Windows 自包含安装包（约 72 MB）为上限；用 `PublishTrimmed` + 单文件、后续可试 Native AOT 再压。
4. 不把 .NET Framework 4.8 当作跨平台缩体积方案。

Windows 主线演示仍按现有安排；小体积 4.8 对比版仅 Windows。实现由「鹊桥-客户端」在平台轨道执行，不改 `relay/`，也不停 R001 / 窗口位置记忆。

## 14. 官网与接单（同机独立进程，2026-11-05 拍板）

产品范围见 `site.md`。部署细节见 `site-deploy.md`。

- **独立 Go 小二进制** + systemd `remote-assist-site`，与 `remote-assist-relay` 分进程、分用户（`rasite`）、分 SQLite 文件。官网故障不得拖死中继。
- 监听 **`SITE_ADDR=:80`**（MVP：`http://121.196.45.81`）。不占用 `17891`。本阶段不上 HTTPS、不放行 443。
- 数据：`SITE_DATA=/var/lib/remote-assist-site/site.db`。
- 运营口令：`SITE_ADMIN_PASSWORD`，仅写在服务器 `/etc/remote-assist-site.env`，不进仓库。
- 资源：`MemoryMax=64M`、`CPUQuota=30%`；不装 Node/Java/PHP/MySQL/Docker 等重栈。
- 安全组：在既有 TCP 17891 之外 **额外放行入站 TCP 80**。
- 上屏文案：`docs/site-copy.md`；仓库静态页：`site/`。
- 后台订单状态只用三档：**新留言** / **已联系** / **已结束**（不要「进行中 / 完成 / 关闭」等其它枚举）。
- 实现：「鹊桥-服务端」；不改客户端、不改中继协议。


## 15. 业务 TLS 指纹与连接分享（2026-11-06 纠错）

全文见 **`docs/tls-trust.md`**。摘要（已废止「默认中继内置官方业务指纹」）：

- **信任**：pipe 内 E2E 业务 TLS；被控端 `CreateEphemeralServerCert`（每进程临时证书）；协助端钉扎**该次 Agent `fp`**。
- **默认分享必须带 `fp`**（完整 `remoteassist:pair…` 载荷 / 二维码等）。短凭证 `{会话码}-{8位验证码}` 只授权中继会话，**不能单独**建立安全业务通道。
- **缺指纹 → 安全阻断**；禁止 TOFU；禁止自动 skipTls / Insecure 降明文。`--insecure` 仅显式调试。
- **与现网**：中继 `:17891` 普通 TCP、不终结业务 TLS；不改 `guest_*` 字段。
- 实现：「鹊桥-客户端」；不改 `relay/` 协议字段。

## 短凭证与唯一凭证（文档指针）

职责记忆：见 [agents/架构.md](agents/架构.md)。

主路径目标：只复制会话码+密码；业务仍钉 Agent fp。过渡双路径 S0/S1 见下表；S1 身份引入依赖官方中继诚实。PAKE 唯一凭证须端点终止认证且收口服务器授权旁路后才可宣称。**账号免密 / 设备密钥快捷连产品意图已废止**（2026-11-06）；遗留代码待 R050 实现轮收口。身份认证与三档写确认正交。

| 文档 | 内容 |
|------|------|
| [short-credential-trust.md](short-credential-trust.md) | 短凭证 S0/S1 信任模型（密封指纹过渡）与不可降级底线 |
| [pake-unique-credential-eval.md](pake-unique-credential-eval.md) | PAKE 唯一凭证正式评估与 **§11 正式收口**（**≠ 实现授权**） |
| [backlog-linux-credential-align.md](backlog-linux-credential-align.md) | Linux/Avalonia 凭证 UX 对齐：**待查重**（历史 r011 ≠ 现行缺口） |

> 2026-11-06：用户拍板严格唯一凭证（仅 code+密码；取消免密/设备密钥快捷连）。S1 仅过渡；节奏见评估 §11.8（交产品拍板）。R050 评估收口已入库，**仍未授权实现**。

## 16. 实现与部署状态三态（2026-11-06）

> 用于 round / 测试报告 / 验收表述，避免把「本机编过」写成「公网已好」。**不替代** `PRODUCT-VERSIONS.md` 产品登记（该文件由产品维护）。

| 状态 | 含义 | 典型证据 |
|------|------|----------|
| `implemented-local` | 本机或 CI **已实现并自测**；代码在约定分支/提交，测试矩阵按条目执行 | 编译产物、本机/CI 日志、自测记录；**尚无**生产/公网授权或未部署 |
| `pending-prod-auth` | 实现已齐（或足以上线），**等待**生产/公网上线授权 | 产品/用户未授上机或未授推生产；报告须写「待上线授权」而非「功能失败」 |
| `prod-verified` | 已按该条目验收点在**公网/生产**验证通过 | 公网哈希/接口/浏览器或约定探活证据；与本机自测分列 |

**报告纪律：**

- 须区分 **「未部署 / 未授权上生产」** 与 **「已部署但功能坏」**；前者不是 FAIL 功能，后者才是。
- **构建 / 编译通过 ≠ UI / 运行通过**；**Linux 客户端路径 ≠ Net48 Windows 亲友主路径**（须分标 PASS）。
- 安全类（如 R050）：即便 `implemented-local`，在恶意中继等验收未跑通前**不得**标安全 PASS，更不得跳到 `prod-verified` 宣称现网已满足严格唯一凭证。

交叉引用：迭代验收见 `docs/dev-iteration.md` §7。

