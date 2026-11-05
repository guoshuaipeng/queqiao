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

- `src/RemoteAssist.Protocol` — 共享消息 / 权限 / 会话契约 / TCP 帧 / 客户端 / 二维码载荷 / 局域网地址筛选
- `src/RemoteAssist.Agent` — 被协助端（会话码、**托盘常驻**、确认窗口、审计日志）
- `src/RemoteAssist.Helper` — 协助端 CLI（支持整段 QR 载荷 / `--payload` / `--clipboard`）
- `src/RemoteAssist.Mcp` — stdio MCP Server（支持整段载荷 / host+code；见 docs/MCP.md）
- `src/RemoteAssist.Relay.Stub` — 公网中继 TODO
- `tests/RemoteAssist.Protocol.Tests` — 基础单元测试

## 环境要求

- Windows（Agent 面向修 Windows；托盘 / 确认窗口依赖 WinForms）
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

默认行为：

- **系统托盘常驻**（蓝点图标）。右键：显示会话信息、**复制 MCP 配置片段**、刷新会话码、**已配对客户端（断开）**、退出。
- 启动时弹出**会话信息窗**：大号会话码 + 二维码；**主机 IP 下拉**（过滤 127 / APIPA 169.254 / 198.18–19 虚拟网段，默认挑最合理局域网地址）；「复制载荷」。关闭窗口 = 隐藏到托盘。
- **配对 / 写操作**：本机确认窗口（串行排队；超时拒绝）。

二维码载荷格式：

```text
remoteassist:pair?v=1&host=<所选IP>&port=17890&code=<SESSION>
```

常用参数：

| 参数 | 含义 |
|---|---|
| `--port 17890` | 监听端口 |
| `--confirm-timeout 60` | 配对/写操作确认超时（秒，5–600） |
| `--headless` | 无 UI：不启托盘/会话窗；配对与写操作回退控制台 |
| `--yes` | 自动同意配对与写操作（**仅本地冒烟**；亦不启托盘） |

### 终端 B — Helper（协助端）

手输码：

```powershell
dotnet run --project src/RemoteAssist.Helper -- pair 192.168.1.10 <会话码>
```

整段 QR 载荷（从会话窗复制）：

```powershell
dotnet run --project src/RemoteAssist.Helper -- pair "remoteassist:pair?v=1&host=192.168.1.10&port=17890&code=ABC123"
# 或
dotnet run --project src/RemoteAssist.Helper -- pair --payload "remoteassist:pair?v=1&host=..."
# 或先复制载荷到剪贴板
dotnet run --project src/RemoteAssist.Helper -- pair --clipboard
```

Agent 本机窗口确认配对后，在 Helper：

```text
helper> procs
helper> ls C:\
helper> run echo hello-from-remote-assist
```

`run` 在 Assist 模式下会阻塞，直到 Agent 本机窗口确认（或 `--headless` 下控制台确认）；拒绝/超时则返回失败且不可绕过。

托盘 **已配对客户端** 可一键断开（撤销令牌并踢掉 TCP），审计记 `session_kick`。

### MCP（stdio）

详细说明见 [docs/MCP.md](docs/MCP.md)（含 Cursor `mcp.json` 示例）。

先启动 Agent，再由宿主拉起。**推荐整段载荷**（与会话窗「复制载荷」一致）：

```powershell
$env:REMOTE_ASSIST_PAYLOAD = "remoteassist:pair?v=1&host=127.0.0.1&port=17890&code=<会话码>"
dotnet run --project src/RemoteAssist.Mcp --no-build

# 或启动参数：
dotnet run --project src/RemoteAssist.Mcp -- --payload "remoteassist:pair?v=1&host=127.0.0.1&port=17890&code=ABC123"

# 仍支持 host+code：
$env:REMOTE_ASSIST_HOST = "127.0.0.1"
$env:REMOTE_ASSIST_PORT = "17890"
$env:REMOTE_ASSIST_CODE = "<会话码>"
$env:REMOTE_ASSIST_PERM = "Assist"
dotnet run --project src/RemoteAssist.Mcp --no-build
```

工具：`list_processes` / `read_log_tail` / `list_dir` / `read_file` / `run_command`（需确认，MCP 不可绕过）。  
缺参 / 坏载荷 / 配对失败会写 stderr 并分别以退出码 `2` / `3` 退出。

## 审计日志

默认写入：`%LOCALAPPDATA%\RemoteAssist\audit.log`

**配对**确认/拒绝：`pair_ok` / `pair_reject` / `pair_timeout`。

**写操作**确认/拒绝：含 `via` / `result` / `at` / `source`。

**刷新会话码**：`session_refresh`，并踢掉全部已配对会话。

**切换 QR 主机 IP**：`host_selected`。

**断开客户端**：`session_kick`（托盘一键断开 / 断开全部）。

### 一键联调（可选）

```powershell
dotnet run --project src/RemoteAssist.Agent -- --port 17890 --yes
dotnet run --project src/RemoteAssist.Helper -- oneshot --payload "<从 Agent 横幅复制的载荷>"
# 仍可用：
dotnet run --project src/RemoteAssist.Helper -- oneshot 127.0.0.1 <会话码> --port 17890
```

无桌面会话时可用：

```powershell
dotnet run --project src/RemoteAssist.Agent -- --headless --confirm-timeout 30
```


## 分发 Agent（给亲友拷贝）

自包含单文件（推荐，目标机通常无需再装 .NET）：

```powershell
.\scripts\publish-agent.ps1
# 产物目录：artifacts\agent-win-x64-sc\
# 主程序：RemoteAssist.Agent.exe
```

等价命令：

```powershell
dotnet publish src/RemoteAssist.Agent -c Release -r win-x64 --self-contained true `
  -o artifacts/agent-win-x64-sc `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

若自包含体积过大，可用框架依赖版（目标机需 [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0)）：

```powershell
.\scripts\publish-agent.ps1 -FrameworkDependent
# 产物：artifacts\agent-win-x64-fd\
```
## 分发 MCP（给 Cursor / 其它 stdio 宿主）

```powershell
.\scripts\publish-mcp.ps1
# 产物：artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe
.\scripts\publish-mcp.ps1 -FrameworkDependent
# 产物：artifacts\mcp-win-x64-fd\
```

Agent 托盘 **「复制 MCP 配置片段」** 会生成可粘贴的 Cursor `mcp.json`（含当前会话载荷；env 写法 + args 写法）。  
详见 [docs/MCP.md](docs/MCP.md)。

可选 Helper：

```powershell
.\scripts\publish-helper.ps1
# 产物：artifacts\helper-win-x64-sc\
```

## 已知限制（v1）

- 无 TLS / 无端到端加密（仅信任局域网）
- 无公网中继（仅 stub）
- `run_command` 经 `cmd.exe /c`，超时约 15s
- 部分敏感路径硬编码拦截，未做完整沙箱
- `--clipboard` 依赖 Windows PowerShell `Get-Clipboard`

## 仓库

计划发布到 Gitee 私有仓；当前本地 git，**默认不 push**。

