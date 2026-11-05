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
- `src/RemoteAssist.Agent` — 被协助端（生成会话码、控制台确认、审计日志）
- `src/RemoteAssist.Helper` — 协助端 CLI
- `src/RemoteAssist.Mcp` — stdio MCP Server
- `src/RemoteAssist.Relay.Stub` — 公网中继 TODO
- `tests/RemoteAssist.Protocol.Tests` — 基础单元测试

## 环境要求

- Windows（Agent 面向修 Windows）
- .NET SDK 8+

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

记下输出的 **会话码** 与 **本机 IP**。配对请求与写操作确认都会出现在此窗口：输入 `Y` 同意 / `N` 拒绝。

### 终端 B — Helper（协助端）

```powershell
cd E:\IdeaProjects\remote-assist
dotnet run --project src/RemoteAssist.Helper -- pair 127.0.0.1 <会话码>
```

Agent 窗口会提示配对，输入 `Y`。然后在 Helper：

```text
helper> procs
helper> ls C:\
helper> run echo hello-from-remote-assist
```

`run` 在 Assist 模式下会阻塞，直到 Agent 窗口确认 `Y`；拒绝则返回失败且不可绕过。

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

### 一键联调（可选）

Agent 加 `--yes` 会自动同意配对与写操作（**仅本地冒烟**，正式协助不要开）：

```powershell
dotnet run --project src/RemoteAssist.Agent -- --port 17890 --yes
dotnet run --project src/RemoteAssist.Helper -- oneshot 127.0.0.1 <会话码>
```

## 已知限制（v1）

- 无 TLS / 无端到端加密（仅信任局域网）
- 确认 UI 为控制台，非系统托盘弹窗
- 无公网中继 / 无扫码 UI（会话码为文本）
- `run_command` 经 `cmd.exe /c`，超时约 15s
- 部分敏感路径硬编码拦截，未做完整沙箱

## 仓库

计划发布到 Gitee 私有仓；当前可本地 `git init`，**默认不 push**。
