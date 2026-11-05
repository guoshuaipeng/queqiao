# 远端协助 — 产品决策

> 独立软件 + 可 MCP 嵌入其他智能体。不做进 AI-SSH-Pro。

## 定位

帮别人修 Windows：对方装轻量 Agent，协助方（人或 AI/MCP）连上后查进程/日志/文件；**危险操作必须在被协助本机确认**后才执行。不做完整远程桌面。

## 目标用户

亲友 + 客户支持 + 同事运维兼顾。v1 不引入组织账号/工单，统一用会话码降低门槛。

## 鉴权与登录（已锁定）

- **v1 无个人账号**：不注册、不邮箱密码、不 OAuth。
- **一次性会话码 + 设备配对**：Agent 启动生成短码；协助方输入码或粘贴 QR 载荷建立会话。
- **短时令牌**：配对成功后下发 token（默认约 2 小时），可随 Agent 重启、「刷新会话码」或托盘「断开」失效。
- 公网中继阶段仍只验会话令牌，不做用户中心（后期若要客户支持再加组织账号）。

## 权限三级

1. **Guest（访客只读）**：列进程、读日志、列目录、读文件（敏感路径可拒绝）。
2. **Assist（默认）**：只读 + 提议命令；**对方本机确认**后才执行。不可由 MCP/协助端绕过。
3. **Admin**：可跳过部分确认（仍审计）；仅对方在配对时主动授予。

配对时 Agent 操作者可选择：按请求权限 / 降为 Guest / 拒绝。

## 连接

- **v1：仅局域网** TCP 直连（默认端口 `17890`）。
- **传输：默认 TLS 1.2/1.3**（Agent 进程内临时自签证书；QR 载荷带 `fp=` SHA-256 指纹，客户端钉扎校验）。`--insecure` 明文仅调试。
- **后期：公网中继隧道** — 代码占位见 `src/RemoteAssist.Relay.Stub`。可选自建 relay 或临时用 FRP 类方案。

## 产品形态、主界面与安装

- **同一 App 双模式**（类似向日葵）：启动首页选「接受协助（被控）」或「去协助别人（控制）」；模式页可互相返回。
- 被控：主界面 + 托盘（会话码 / 二维码 / TLS 指纹 / 配对 / 确认 / 审计）；关闭主窗 = 回托盘。
- 控制：粘贴载荷配对向导 + 常用排障操作 + 中继设置占位。
- **一个安装包**：`installer/RemoteAssist.iss` + `scripts/build-installer.ps1` → `RemoteAssist-Setup-*.exe`（内含 `RemoteAssist.exe`）。
- 无 Inno：`scripts/install-app-local.ps1`（旧名 `install-agent-local.ps1` 仍转发）。
- 默认安装目录：`%LOCALAPPDATA%\Programs\RemoteAssist`。
- 兼容入口：独立 `RemoteAssist.Agent` / `RemoteAssist.Helper.UI` / Helper CLI / MCP 仍可单独 publish。
- `--headless` / `--yes`：统一 App 或 Agent 均可走无 UI 被控。

## MCP

- 优先 **通用 stdio**（JSON-RPC + Content-Length）。
- 工具与 Helper 同一客户端协议；`run_command` 走 Agent 确认流程。

## 确认交互

- **配对**与 **Assist 写操作**默认均弹出本机 WinForms 确认窗口。
  - 配对窗：来源 IP / 客户端名 / 请求权限；允许、仅访客、拒绝；超时拒绝。
  - 写操作窗：来源 / 命令；允许、拒绝；默认 60s 超时拒绝。
- **串行排队**：配对与写操作共用同一确认队列，一次只处理一个 UI 确认，并发请求排队不丢。
- `--headless` 回退控制台；`--yes` 仅联调自动通过。

## 审计

每次配对、工具成功/失败、用户确认/拒绝、会话码刷新、主机 IP 选择、断开客户端写入本地审计日志。确认记录含 `via`（ui/console/autoyes）、`result`、`at`。配对另含 `requested` / `granted`。断开记 `session_kick`。

## 明确不做（v1）

- 完整远程桌面 / 键鼠注入
- 账号体系与云端用户中心
- 公网中继接通（仅 stub + UI 占位字段）
- 修改 AI-SSH-Pro 本体
- 正式 CA / 客户端证书互认（v1 用自签 + 指纹）

## 技术选型

- Windows Agent / Helper / MCP：**.NET 8**
- 二维码：QRCoder
- 传输：长度前缀 JSON over TCP；默认 SslStream TLS + 指纹钉扎（局域网）

