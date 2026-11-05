# RemoteAssist 安装包说明（统一 App）

## 产物

- 脚本：`installer/RemoteAssist.iss`（Inno Setup 6）
- 推荐输出：`artifacts/installer/RemoteAssist-Setup-0.1.0.exe`
- 内容：同一个 `RemoteAssist.exe`（被控 + 控制双模式）

## 一键构建

```powershell
cd E:\IdeaProjects\remote-assist
.\scripts\build-installer.ps1
```

1. `publish-app.ps1` → `artifacts\app-win-x64-sc\`
2. 查找本机 `ISCC.exe`
3. 编译 Setup.exe

无 Inno 时：

```powershell
.\scripts\install-app-local.ps1
```

## 安装行为

| 项 | 行为 |
|---|---|
| 安装目录 | `%LOCALAPPDATA%\Programs\RemoteAssist` |
| 主程序 | `RemoteAssist.exe` |
| 开始菜单 | 「鹊桥远程协助」+ 卸载 |
| 桌面快捷方式 | 可选 |
| 开机自启 | 可选 |

装好后打开首页，选「接受协助」或「去协助别人」。