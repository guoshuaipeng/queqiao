# 鹊桥远程协助 · 智能体 — 架构总览

> 产品决策以 `PRODUCT.md` 为准；协议细节见 `relay-protocol.md`；生产部署见 `relay-deploy.md`。
> 本文只描述**系统边界与组件关系**。改架构先改 docs，再动代码。

## 1. 一句话

**命令控制面**的远程协助：被控端轻量常驻，协助方（人或 Agent）经公网中继配对后查进程/日志/文件、提议命令；**写操作必须在被控本机确认**。不是远程桌面，不改 AI-SSH-Pro。

## 2. 组件与仓库布局

| 组件 | 技术 | 仓库路径 | 职责 |
|------|------|----------|------|
| 桌面客户端 | Tauri 2 | `apps/queqiao-tauri` | 接受协助 / 去协助；会话码与密码、确认窗、账号与设备 |
| 公网中继 | Go + SQLite | `relay/` | JSONL 控制配对 → 透明双向 pipe；账号/设备轻量存储 |
| 官网 | Go + SQLite | `site/` | 介绍页、接单、`/admin` |
| 局域网直连 | 客户端高级路径 | 历史端口 `17890`，`via=lan` | 非默认 |

## 3. 连接拓扑

```
协助方 (App UI)                          被控方 (App)
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
| 公网中继 · 账号 | 是（有账号时） | `login`/`auth_token` → `device_online` / `list_devices` / `connect_device` | 同账号连本机已允许的设备免输会话密码 |
| 局域网直连 | 否 | 历史 LAN 路径 | 仅高级/调试 |

生产中继：`YOUR_RELAY_HOST:17891`，systemd `remote-assist-relay`，SQLite `/var/lib/remote-assist-relay/relay.db`。开源客户端默认 `127.0.0.1:17891`，正式发版或设置可覆盖。详见 `relay-deploy.md`、本机 `private/deploy.md`。

## 4. 协议分层

1. **中继控制面**：单 TCP；配对前 **JSON Lines**（`signup` / `login` / `auth_token` / `device_*` / `guest_*`）；配对成功两端收到 `paired` 后切 **原始双向字节管道**。契约：`relay-protocol.md`。
2. **业务面**：管道内业务帧（4 字节 LE 长度 + UTF-8 JSON）+ **TLS（自签 + 证书指纹钉扎）**。`--insecure` 仅显式调试。中继只转发，不终结业务 TLS。指纹为被控端临时业务证书，经完整载荷分享；规则见 **`tls-trust.md`** 与本文 §15。
3. **MCP**：协助方机器上的 stdio 服务（`apps/queqiao-mcp` → `RemoteAssist.Mcp.exe`）。读完整连接凭证，经中继 + 业务 TLS 后发 `pair`/`tool`；工具映射到同一业务协议，`run_command` 等写路径仍走被控本机确认。详见 `docs/MCP.md`。

## 5. 鉴权与权限

### 5.1 接入身份

- **访客**：中继侧 code + 密码；无账号体系。
- **账号**：中继轻量 `signup`/`login`，`token` 约 7 天；设备以稳定 `device_id` 上线。**不做**完整用户中心 / 计费 / 组织工单。

### 5.2 本机权限三级

| 级别 | 含义 |
|------|------|
| Guest | 访客只读 |
| Assist（默认） | 写操作本机确认，不可由中继或协助方绕过 |
| Admin | 可跳过部分确认（仍审计）；仅被控方主动授予 |

账号免密连**本机已允许**的设备：可跳过「配对确认窗」；**写操作确认仍不可绕过**（除非对方主动授 Admin）。中继配对成功 ≠ 授权写操作。信任三档（每次询问 / 宽松 / 完全信任）见 `trust-levels.md`。

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
- 用纯 Skill 替代协议作为控制面
- 修改 AI-SSH-Pro 本体
- 恢复 .NET 客户端

## 9. 文档地图

| 文档 | 内容 |
|------|------|
| `PRODUCT.md` | 品牌、定位、连接与权限产品决策 |
| `PRODUCT-VERSIONS.md` | 需求队列（迭代唯一入口） |
| `architecture.md`（本文） | 组件、拓扑、分层、边界总览 |
| `CLIENT-UI.md` | 客户端界面与交互规格（效果图 `client-ui-mockup.html`） |
| `relay-protocol.md` | 中继 JSONL 控制契约 |
| `relay.md` / `relay-deploy.md` | 本机开发入口 / 生产部署 |
| `relay-audit.md` / `relay-session-audit.md` | 会话码审计产品规格 / 架构定稿 |
| `tls-trust.md` / `trust-levels.md` | 业务 TLS 指纹信任模型 / 本机信任三档 |
| `roadmap.md` | 优先级与状态 |
| `dev-rules.md` | 开发约定 |
| `site.md` / `site-copy.md` / `site-deploy.md` | 官网产品范围 / 上屏文案 / 部署 |

## 10. 变更约定

- 协议变更：先改 `relay-protocol.md`，再改 Go 中继，客户端跟随文档。
- 模块边界：客户端 `apps/queqiao-tauri`；中继 `relay/`；官网 `site/`。客户端不改中继协议字段。
- 决策写进 `docs/`，不靠聊天记忆。

## 11. 客户端窗口

主窗口记住上次所在的显示器、位置和大小，下次启动还原。确认窗跟主窗口同一块屏。显示器断开或还原后完全不可见时，回到当前主屏可见区域。设置只存在本机，不写中继。实现在 Tauri 客户端，不改 `relay/`。

## 12. 平台

当前客户端是 Windows 上的 Tauri。写操作本机确认不因平台而取消。

## 13. 官网与接单（同机独立进程）

产品范围见 `site.md`。部署细节见 `site-deploy.md`。**官网改版已暂停（2026-11-06），交由其他人制作。**

- **独立 Go 小二进制** + systemd `remote-assist-site`，与 `remote-assist-relay` 分进程、分用户（`rasite`）、分 SQLite 文件。官网故障不得拖死中继。
- 监听 **`SITE_ADDR=:80`**（MVP：`http://YOUR_SITE_HOST`）。不占用 `17891`。本阶段不上 HTTPS、不放行 443。
- 数据：`SITE_DATA=/var/lib/remote-assist-site/site.db`。
- 运营口令：`SITE_ADMIN_PASSWORD`，仅写在服务器 `/etc/remote-assist-site.env`，不进仓库。
- 资源：`MemoryMax=64M`、`CPUQuota=30%`；不装 Node/Java/PHP/MySQL/Docker 等重栈。
- 安全组：在既有 TCP 17891 之外 **额外放行入站 TCP 80**。
- 上屏文案：`docs/site-copy.md`；仓库静态页：`site/`。
- 后台订单状态只用三档：**新留言** / **已联系** / **已结束**（不要「进行中 / 完成 / 关闭」等其它枚举）。

## 14. 业务 TLS 指纹与连接分享

全文见 **`docs/tls-trust.md`**。摘要（已废止「默认中继内置官方业务指纹」）：

- **信任**：pipe 内 E2E 业务 TLS；被控端每进程临时自签业务证书；协助端钉扎**该次 Agent `fp`**。
- **默认分享必须带 `fp`**（完整 `remoteassist:pair…` 载荷）。短凭证 `{会话码}-{8位验证码}` 只授权中继会话，**不能单独**建立安全业务通道。
- **缺指纹 → 安全阻断**；禁止 TOFU；禁止自动 skipTls / Insecure 降明文。`--insecure` 仅显式调试。
- **与现网**：中继 `:17891` 普通 TCP、不终结业务 TLS；不改 `guest_*` 字段。
- 短码可连（R050）仍 **blocked**、未授权实现；产品意图见 `PRODUCT.md`，状态见 `roadmap.md`。

