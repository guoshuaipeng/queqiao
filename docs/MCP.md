# MCP 接入说明（stdio）

`RemoteAssist.Mcp` 是通用 **stdio MCP Server**（JSON-RPC 2.0 + `Content-Length` 帧）。  
启动时按参数/环境变量配对到被控端；**写操作（如 `run_command`）仍须被控本机确认，MCP 无法绕过**。

中继控制协议见 `docs/relay-protocol.md`（JSON Lines）。

## 两种推荐连法

### 1）临时远程 ID + 密码（访客 · 经中继）

```powershell
dotnet run --project src/RemoteAssist.Mcp -- `
  --relay 1.2.3.4:17891 --code AB12CD --pwd secret1 --fp <SHA256>
```

或整段载荷：

```powershell
dotnet run --project src/RemoteAssist.Mcp -- --payload "remoteassist:pair?v=2&via=relay&relay=1.2.3.4:17891&code=AB12CD&pwd=secret1&tls=1&fp=<SHA256>"
```

环境变量：`REMOTE_ASSIST_RELAY` / `REMOTE_ASSIST_CODE` / `REMOTE_ASSIST_PWD` / `REMOTE_ASSIST_FP`。

### 2）登录后连自己的机器（账号 · 免密）

```powershell
dotnet run --project src/RemoteAssist.Mcp -- `
  --relay 1.2.3.4:17891 --token <hex> --device-id <uuid> --fp <SHA256>

# 或启动时登录
dotnet run --project src/RemoteAssist.Mcp -- `
  --relay 1.2.3.4:17891 --user alice --account-password secret1 --device-id <uuid> --fp <SHA256>
```

环境变量：`REMOTE_ASSIST_TOKEN` 或 `REMOTE_ASSIST_USER` + `REMOTE_ASSIST_ACCOUNT_PASSWORD`，以及 `REMOTE_ASSIST_DEVICE_ID` / `REMOTE_ASSIST_RELAY` / `REMOTE_ASSIST_FP`。

### 局域网（高级/调试）

```powershell
dotnet run --project src/RemoteAssist.Mcp -- --payload "remoteassist:pair?v=1&via=lan&host=127.0.0.1&port=17890&code=ABC123&tls=1&fp=<SHA256>"
```

明文调试：`--insecure` / `REMOTE_ASSIST_INSECURE=1`。完整参数见 `--help`。

## 分发 MCP（publish）

```powershell
.\scripts\publish-mcp.ps1
# 产物：artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe
.\scripts\publish-mcp.ps1 -FrameworkDependent
```

## Cursor（mcp.json）示例

访客中继：

```json
{
  "mcpServers": {
    "remote-assist": {
      "command": "E:/IdeaProjects/remote-assist/artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe",
      "args": ["--relay", "1.2.3.4:17891", "--code", "AB12CD", "--pwd", "secret1", "--fp", "<SHA256>"]
    }
  }
}
```

账号免密：

```json
{
  "mcpServers": {
    "remote-assist": {
      "command": "E:/IdeaProjects/remote-assist/artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe",
      "env": {
        "REMOTE_ASSIST_RELAY": "1.2.3.4:17891",
        "REMOTE_ASSIST_TOKEN": "<hex>",
        "REMOTE_ASSIST_DEVICE_ID": "<uuid>",
        "REMOTE_ASSIST_FP": "<SHA256>"
      }
    }
  }
}
```

## 安全

- `run_command` 在 Assist 权限下 **必须** Agent 本机确认，MCP / 中继都无法绕过。
- stdout 仅用于 MCP 协议；日志在 stderr。
