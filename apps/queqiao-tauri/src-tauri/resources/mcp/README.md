# MCP 随包资源

CI / 本地发版前，把对应平台的 `RemoteAssist.Mcp`（Windows 为 `RemoteAssist.Mcp.exe`）放到本目录，再执行 Tauri 打包。

```powershell
# Windows 示例
.\scripts\publish-mcp.ps1
Copy-Item artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe apps\queqiao-tauri\src-tauri\resources\mcp\
```

空目录不能打包时，可保留本 README；真正发版必须有可执行文件。
