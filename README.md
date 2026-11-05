# RemoteAssist / 远端协助

独立产品：可控远程协助 + AI 排障（可 MCP 嵌入其他智能体）。**不修改 AI-SSH-Pro。**

## 已锁定决策（v1）

| 项 | 选择 |
|---|---|
| 语言 | .NET 8 |
| 鉴权 | 无账号；会话码配对 + 短时令牌 |
| 权限 | Guest 只读 / Assist 确认后执行 / Admin |
| 连接 | **仅局域网**（公网中继后期，见 `src/RemoteAssist.Relay.Stub`） |
| MCP | 通用 stdio |
| 远控桌面 | 不做 |

详见 [docs/PRODUCT.md](docs/PRODUCT.md)。

## 解决方案结构

- `src/RemoteAssist.Protocol` — 共享消息 / 权限 / 会话契约 / TCP 帧 / 客户端
- `src/RemoteAssist.Agent` — 被协助端（会话码、**本机确认窗口**、审计日志）
- `src/RemoteAssist.Helper` — 协助端 CLI
- `src/RemoteAssist.Mcp` — stdio MCP Server
- `src/RemoteAssist.Relay.Stub` — 公网中继 TODO
- `tests/RemoteAssist.Protocol.Tests` — 基础单元测试

## 环境要求

- Windows（Agent 面向修 Windows；确认窗口依赖 WinForms）
- .NET SDK 8+（需 Windows Desktop 工作负载 / `Microsoft.WindowsDesktop.App`）

若 `dotnet` 不在 PATH，可先：

```powershell
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH
```

## 构建

```powershell
cd E:\IdeaProjects\remote-assist
dotnet build
dotnet test
```

## 本机跑通（happy path）

开两个终端。

### 终端 A — Agent（被协助的 Windows）

```powershell
cd E:\IdeaProjects\remote-assist
dotnet run --project src/RemoteAssist.Agent
```

记下输出的 **会话码** 与 **本机 IP**。

- **配对**：仍在 Agent 控制台确认（`Y` / `G` 仅访客 / `N` 拒绝）。
- **写操作（Assist）**：默认弹出**本机确认窗口**，显示请求来源、拟执行命令；点「允许」或「拒绝」。默认 **60 秒无操作自动拒绝**。

常用参数：

| 参数 | 含义 |
|---|---|
| `--port 17890` | 监听端口 |
| `--confirm-timeout 60` | 写操作确认超时（秒，5–600） |
| `--headless` | 无 UI：写操作回退控制台 Y/N（超时自动拒绝） |
| `--yes` | 自动同意配对与写操作（**仅本地冒烟**） |

### 终端 B — Helper（协助端）

```powershell
cd E:\IdeaProjects\remote-assist
dotnet run --project src/RemoteAssist.Helper -- pair 127.0.0.1 <会话码>
```

Agent 控制台确认配对后，在 Helper：

```text
helper> procs
helper> ls C:\
helper> run echo hello-from-remote-assist
```

`run` 在 Assist 模式下会阻塞，直到 Agent 本机窗口确认（或 `--headless` 下控制台确认）；拒绝/超时则返回失败且不可绕过。

### MCP（stdio）

先启动 Agent，再由宿主拉起：

```powershell
$env:REMOTE_ASSIST_HOST = "127.0.0.1"
$env:REMOTE_ASSIST_PORT = "17890"
$env:REMOTE_ASSIST_CODE = "<会话码>"
$env:REMOTE_ASSIST_PERM = "Assist"
dotnet run --project src/RemoteAssist.Mcp --no-build
```

工具：`list_processes` / `read_log_tail` / `list_dir` / `read_file` / `run_command`（需确认）。

## 审计日志

默认写入：`%LOCALAPPDATA%\RemoteAssist\audit.log`

写操作确认/拒绝会记录：

- `via=ui|console|autoyes` — 确认方式
- `result=approved|denied|timedout` — 结果
- `at=` — UTC 时间（ISO-8601）
- `source=` — 协助端客户端名

### 一键联调（可选）

Agent 加 `--yes` 会自动同意配对与写操作（**仅本地冒烟**，正式协助不要开）：

```powershell
dotnet run --project src/RemoteAssist.Agent -- --port 17890 --yes
dotnet run --project src/RemoteAssist.Helper -- oneshot 127.0.0.1 <会话码>
```

无桌面会话时可用：

```powershell
dotnet run --project src/RemoteAssist.Agent -- --headless --confirm-timeout 30
```

## 已知限制（v1）

- 无 TLS / 无端到端加密（仅信任局域网）
- 确认 UI 为简易 WinForms 对话框（非系统托盘常驻）
- 配对确认仍为控制台（写操作才弹窗）
- 无公网中继 / 无扫码 UI（会话码为文本）
- `run_command` 经 `cmd.exe /c`，超时约 15s
- 部分敏感路径硬编码拦截，未做完整沙箱

## 仓库

计划发布到 Gitee 私有仓；当前本地 git，**默认不 push**。