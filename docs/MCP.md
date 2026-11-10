# MCP 接入说明（stdio）

`RemoteAssist.Mcp` 是通用 **stdio MCP Server**（JSON-RPC 2.0 + `Content-Length` 帧）。  
协助方机器上由 Cursor 等拉起；用完整连接凭证连到被控端。**写操作（如 `run_command`）仍须被控本机确认，MCP 无法绕过。**

中继控制协议见 `docs/relay-protocol.md`。业务工具名在线上为 PascalCase；MCP 对外仍用 snake_case。

## 构建

```powershell
.\scripts\publish-mcp.ps1
# 产物：artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe
```

跨平台发版（macOS / Linux 含随包 MCP）：GitHub Actions 工作流 `Release packages (MCP)`（`.github/workflows/release-packages.yml`）。

源码：`apps/queqiao-mcp`（依赖 `crates/queqiao-protocol`）。桌面安装包可通过 `src-tauri/resources/mcp/` 随包携带同名二进制。

## 推荐连法（完整载荷）

环境变量（与客户端「复制 MCP 配置」一致）：

```text
REMOTE_ASSIST_PAYLOAD=remoteassist:pair?v=2&via=relay&relay=127.0.0.1:17891&code=...&pwd=...&tls=1&fp=...
```

或命令行：

```powershell
.\artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe --payload "remoteassist:pair?..."
```

须含业务证书指纹 `fp=`。短码单独启动会拒绝（与当前安全主路径一致）。

## Cursor（mcp.json）

```json
{
  "mcpServers": {
    "remote-assist": {
      "command": "E:/IdeaProjects/remote-assist/artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe",
      "args": [],
      "env": {
        "REMOTE_ASSIST_PAYLOAD": "remoteassist:pair?v=2&via=relay&relay=HOST:17891&code=CODE&pwd=PWD&tls=1&fp=FP"
      }
    }
  }
}
```

客户端设置里点「复制 MCP 配置」可生成同结构片段（找到本机 exe 后路径为绝对路径）。

## 工具

| MCP 名 | 线上 tool | 说明 |
|--------|-----------|------|
| `list_processes` | `ListProcesses` | 列出进程 |
| `list_services` | `ListServices` | 列出服务 |
| `list_dir` | `ListDir` | 列目录（参数 `path`） |
| `read_file` | `ReadFile` | 读文本（参数 `path`） |
| `read_log_tail` | `ReadLogTail` | 日志尾（`path`，可选 `lines`） |
| `run_command` | `RunCommand` | 提议执行命令；对方本机确认 |

## 安全

- stdout 仅用于 MCP 协议；诊断日志写 stderr。
- 中继与 MCP 都不能代执行或跳过确认。
- 本轮不含：安装包内嵌 MCP、账号免密 MCP、公网发版。
