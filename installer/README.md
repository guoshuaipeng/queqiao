# 鹊桥安装包（Windows）

## 亲友主路径（R047 / net48）

| 项 | 值 |
|---|---|
| 脚本 | `scripts/build-installer-net48.ps1` |
| Inno | `installer/RemoteAssist-Net48.iss` |
| 输入 | `artifacts/app-net48-fd/`（含 `RemoteAssist.Net48.exe` + `RemoteAssist.Mcp.exe`） |
| 输出 | `artifacts/installer/RemoteAssist-Net48-Setup-0.1.0.exe` |
| 运行时 | **需要 .NET Framework 4.8**（非自包含；见 `installer/net48-runtime-note.txt`） |
| 安装目录 | `%LOCALAPPDATA%\Programs\RemoteAssist` |

```powershell
cd E:\IdeaProjects\remote-assist
.\scripts\build-installer-net48.ps1
```

官网下载文件名建议：`RemoteAssist-Net48-Setup-0.1.0.exe`；旁注必须写「需要 .NET Framework 4.8」。上传/换链由产品另排，本仓库只出制品。

## 兼容备用（net8 自包含，勿作亲友主推）

| 项 | 值 |
|---|---|
| 脚本 | `scripts/build-installer.ps1` |
| Inno | `installer/RemoteAssist.iss` |
| 输入 | `artifacts/app-win-x64-sc/` |
| 输出 | `artifacts/installer/RemoteAssist-Setup-0.1.0.exe` |

无 Inno 时可用：`.\scripts\install-app-local.ps1`（仅本机解压，非正式 Setup）。
