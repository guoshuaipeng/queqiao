# RemoteAssist Agent 安装包说明

## 产物

- 脚本：`installer/RemoteAssist.iss`（Inno Setup 6）
- 推荐输出：`artifacts/installer/RemoteAssist-Agent-Setup-0.1.0.exe`

## 一键构建（推荐）

```powershell
cd E:\IdeaProjects\remote-assist
.\scripts\build-installer.ps1
```

脚本会：

1. `publish-agent.ps1` 生成自包含 `artifacts\agent-win-x64-sc\`
2. 查找本机 `ISCC.exe`（Inno Setup 6）
3. 编译得到 Setup.exe

## 手动

1. 安装 [Inno Setup 6](https://jrsoftware.org/isdl.php)（或 GitHub Releases：`innosetup-6.x.x.exe`）
2. Publish：

```powershell
.\scripts\publish-agent.ps1
```

3. 编译：

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\RemoteAssist.iss
```

## 安装行为（默认）

| 项 | 行为 |
|---|---|
| 安装目录 | `%LOCALAPPDATA%\Programs\RemoteAssist`（当前用户，默认无需管理员） |
| 开始菜单 | 「远端协助」+ 卸载 |
| 桌面快捷方式 | 可选 |
| 开机自启 | 可选（HKCU Run） |
| 卸载 | 控制面板 / 设置 → 应用；可清审计日志 |

装好后双击打开主界面即可生成会话码；关闭窗口会留在托盘。
