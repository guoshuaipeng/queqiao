# MCP 接入说明（stdio）

`RemoteAssist.Mcp` 是通用 **stdio MCP Server**（JSON-RPC 2.0 + `Content-Length` 帧）。  
启动时会用会话码/载荷 **自动配对** Agent；配对失败则进程退出（stderr 有清晰错误），不会假装已连接。

## 连接参数

同名字段：**命令行优先于环境变量**。

| 方式 | 环境变量 | 启动参数 |
|---|---|---|
| 整段载荷（推荐） | `REMOTE_ASSIST_PAYLOAD` | `--payload` / `-P` 或首个 positional |
| host + code | `REMOTE_ASSIST_HOST` + `REMOTE_ASSIST_CODE` | `--host` + `--code` |
| 端口 | `REMOTE_ASSIST_PORT`（默认 `17890`） | `--port` / `-p` |
| 权限 | `REMOTE_ASSIST_PERM`（默认 `Assist`） | `--perm` |
| 证书指纹 | `REMOTE_ASSIST_FP` | `--fp`（载荷已含 `fp=` 时可省略） |
| 明文调试 | `REMOTE_ASSIST_INSECURE=1` | `--insecure`（Agent 也需 `--insecure`） |

载荷格式（与 Agent 会话窗「复制载荷」一致；默认 TLS）：

```text
remoteassist:pair?v=1&host=192.168.1.10&port=17890&code=ABC123&tls=1&fp=<SHA256>
```

`--port` / `--host` / `--code` 在已给载荷时也可覆盖（本机端口转发调试用）。

## 命令行示例

```powershell
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH
cd E:\IdeaProjects\remote-assist

# 先启动 Agent（另一终端），复制会话载荷后：
dotnet run --project src/RemoteAssist.Mcp -- --payload "remoteassist:pair?v=1&host=127.0.0.1&port=17890&code=ABC123&tls=1&fp=<SHA256>"

# 或环境变量：
$env:REMOTE_ASSIST_PAYLOAD = "remoteassist:pair?v=1&host=127.0.0.1&port=17890&code=ABC123&tls=1&fp=<SHA256>"
dotnet run --project src/RemoteAssist.Mcp

# 或传统 host+code：
$env:REMOTE_ASSIST_HOST = "127.0.0.1"
$env:REMOTE_ASSIST_CODE = "ABC123"
dotnet run --project src/RemoteAssist.Mcp
```

失败时 stderr 示例：

- 缺参数：提示需要 `REMOTE_ASSIST_PAYLOAD` 或 `HOST+CODE`
- 载荷坏：`无法解析 ...`
- 配对失败：目标 host:port/code +「确认 Agent 已启动且本机已确认配对」

退出码：`2` 参数错误，`3` 配对失败。

## 分发 MCP（publish）

自包含单文件（推荐，Cursor 直接指 exe）：

```powershell
.\scripts\publish-mcp.ps1
# 产物：artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe
```

框架依赖备选（本机需 [.NET 8 Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0)）：

```powershell
.\scripts\publish-mcp.ps1 -FrameworkDependent
# 产物：artifacts\mcp-win-x64-fd\
```

可选：`.\scripts\publish-helper.ps1` 同步打 Helper CLI。

## Cursor（mcp.json）示例

**推荐**：先 `publish-mcp.ps1`，再在 Agent 托盘右键 **「复制 MCP 配置片段」**（含当前会话载荷；env + args 两种写法）。粘贴进 Cursor MCP 配置即可。

手动配置时，把下面放进 Cursor 的 MCP 配置。  
**注意**：每次换会话码都要改 `REMOTE_ASSIST_PAYLOAD`（或改用 host+code 并更新 CODE）。

### 指向 publish 出的 exe（推荐）

```json
{
  "mcpServers": {
    "remote-assist": {
      "command": "E:/IdeaProjects/remote-assist/artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe",
      "args": [],
      "env": {
        "REMOTE_ASSIST_PAYLOAD": "remoteassist:pair?v=1&host=127.0.0.1&port=17890&code=REPLACE&tls=1&fp=<SHA256>"
      }
    }
  }
}
```

args 写法：

```json
{
  "mcpServers": {
    "remote-assist": {
      "command": "E:/IdeaProjects/remote-assist/artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe",
      "args": ["--payload", "remoteassist:pair?v=1&host=127.0.0.1&port=17890&code=REPLACE&tls=1&fp=<SHA256>"]
    }
  }
}
```

### 开发期：dotnet run

```json
{
  "mcpServers": {
    "remote-assist": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "E:/IdeaProjects/remote-assist/src/RemoteAssist.Mcp",
        "--no-build"
      ],
      "env": {
        "REMOTE_ASSIST_PAYLOAD": "remoteassist:pair?v=1&host=127.0.0.1&port=17890&code=REPLACE&tls=1&fp=<SHA256>"
      }
    }
  }
}
```

## 通用宿主

任意支持 **stdio MCP** 的宿主：

1. 启动命令指向 `dotnet run --project ...RemoteAssist.Mcp` 或 publish 出的 exe  
2. 通过 `env` 或 `args` 传入载荷 / host+code  
3. stdout 仅用于 MCP 协议；日志在 stderr  

工具：`list_processes` / `read_log_tail` / `list_dir` / `read_file` / `run_command`  
`run_command` 在 Assist 权限下 **必须** Agent 本机确认，MCP 无法绕过。

