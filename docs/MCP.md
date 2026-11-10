# MCP 接入说明（stdio）

`RemoteAssist.Mcp` 是通用 **stdio MCP Server**（JSON-RPC 2.0 + `Content-Length` 帧）。  
协助方机器上由 Cursor 等拉起；**未连接也可启动**，再用工具 `connect` 传入短码/完整凭证连到被控端。**写操作（如 `run_command`）仍须被控本机确认，MCP 无法绕过。**

中继控制协议见 `docs/relay-protocol.md`。业务工具名在线上为 PascalCase；MCP 对外仍用 snake_case。

## 构建

```powershell
.\scripts\publish-mcp.ps1
# 产物：artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe
```

跨平台发版（macOS / Linux 含随包 MCP）：GitHub Actions 工作流 `Release packages (MCP)`（`.github/workflows/release-packages.yml`）。打包后上传官网与 `latest.json` 见 **[docs/release.md](release.md)**。

源码：`apps/queqiao-mcp`（依赖 `crates/queqiao-protocol`）。桌面安装包可通过 `src-tauri/resources/mcp/` 随包携带同名二进制。

## 推荐连法

### 1. 常驻 MCP + 工具传码（推荐）

`mcp.json` 只配命令（可选默认中继），Cursor 启动时即可连上 MCP。对 Agent 说短码后调 `connect`：

```json
{
  "mcpServers": {
    "remote-assist": {
      "command": "E:/IdeaProjects/remote-assist/artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe",
      "args": [],
      "env": {
        "REMOTE_ASSIST_RELAY": "121.196.45.81:17891"
      }
    }
  }
}
```

工具：`connect(payload, relay?)` → 业务工具 → 可选 `disconnect`。`connection_status` 查看是否已配对。

### 2. 环境变量启动时尝试配对（可选）

有 `REMOTE_ASSIST_PAYLOAD` 时启动会**尝试**配对；失败只打 stderr，**不退出**，仍可再 `connect`：

```text
# 完整载荷（自带中继与 fp=）
REMOTE_ASSIST_PAYLOAD=remoteassist:pair?v=2&via=relay&relay=127.0.0.1:17891&code=...&pwd=...&tls=1&fp=...

# 或短码（须另给中继，与对方客户端设置里的服务器一致）
REMOTE_ASSIST_PAYLOAD=8CEJTF-79570185
REMOTE_ASSIST_RELAY=121.196.45.81:17891
```

或命令行：

```powershell
.\artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe
.\artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe --payload "remoteassist:pair?..."
.\artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe --payload "CODE-########" --relay "HOST:17891"
```

完整载荷须含业务证书指纹 `fp=`。短码走与客户端相同的 SPAKE2 握手，密码不发给中继。

## Cursor（mcp.json）

客户端首页 / 设置「复制 MCP 配置」默认只留 `command` + `REMOTE_ASSIST_RELAY`（未连接也可装）。若当时已有会话凭证，会一并写入 `REMOTE_ASSIST_PAYLOAD` 方便启动即配对；短码仍可再交给 Agent 用 `connect` 传入。

MCP `initialize` 会返回 `instructions`（能力边界与推荐工作方式）；`tools/list` 含较完整的中文工具说明，供 Cursor 自动选工具。

## 工具

| MCP 名 | 线上 tool | 说明 |
|--------|-----------|------|
| `connect` | （本地） | 传短码/完整载荷配对；可选 `relay` |
| `disconnect` | （本地） | 断开当前会话 |
| `connection_status` | （本地） | `connected` / `disconnected` |
| `list_processes` | `ListProcesses` | 列出进程 |
| `list_services` | `ListServices` | 列出服务 |
| `list_dir` | `ListDir` | 列目录（参数 `path`） |
| `read_file` | `ReadFile` | 读文本（参数 `path`） |
| `read_log_tail` | `ReadLogTail` | 日志尾（`path`，可选 `lines`） |
| `run_command` | `RunCommand` | 提议执行命令；对方本机确认 |

业务工具在未 `connect` 时返回明确错误，不会假装成功。

## 安全

- stdout 仅用于 MCP 协议；诊断日志写 stderr。
- 中继与 MCP 都不能代执行或跳过确认。
- 本轮不含：安装包内嵌 MCP、账号免密 MCP、公网发版。
