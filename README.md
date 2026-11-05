# RemoteAssist / 远端协助

独立产品：可控远程协助 + AI 排障（可 MCP 嵌入其他智能体）。**不修改 AI-SSH-Pro。**

> **给亲友怎么用（一句话）**  
> 安装「远端协助」→ 打开后选 **接受协助** 或 **去协助别人** → 被控端把会话码发给协助方 → 危险操作需被控本机点允许。

## 已锁定决策（v1）

| 项 | 选择 |
|---|---|
| 语言 | .NET 8 |
| 形态 | **同一安装包 / 同一 App**（被控 + 控制双模式，类似向日葵） |
| 鉴权 | 无账号；会话码配对 + 短时令牌 |
| 权限 | Guest 只读 / Assist 确认后执行 / Admin |
| 连接 | **仅局域网**（公网中继后期，见 `src/RemoteAssist.Relay.Stub`） |
| MCP | 通用 stdio |
| 远控桌面 | 不做 |
| 安装 | Inno Setup 安装包 / 本机软安装脚本 |

详见 [docs/PRODUCT.md](docs/PRODUCT.md)。

## 解决方案结构

- `src/RemoteAssist.App` — **产品主入口**：首页双模式（接受协助 / 去协助别人）
- `src/RemoteAssist.Protocol` — 共享消息 / 权限 / 会话契约 / TCP·TLS 帧 / 客户端 / 二维码载荷
- `src/RemoteAssist.Agent` — 被控能力库 + 兼容独立入口（`--headless` / 旧 publish）
- `src/RemoteAssist.Helper` — 协助端 CLI
- `src/RemoteAssist.Helper.UI` — 控制端界面（由统一 App 复用；也可单独跑）
- `src/RemoteAssist.Mcp` — stdio MCP Server（见 docs/MCP.md）
- `src/RemoteAssist.Relay.Stub` — 公网中继占位（可用纯 IP）
- `installer/` — Inno Setup 脚本与说明
- `tests/RemoteAssist.Protocol.Tests` — 单元测试

## 环境要求

- Windows 10/11 x64
- 开发机：.NET SDK 8+（Windows Desktop 工作负载）
- 亲友机：使用**自包含安装包**时通常**无需**再装 .NET

```powershell
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH
```

## 给亲友安装（推荐 · 一个安装包）

### 方式 A：Setup 安装包

```powershell
cd E:\IdeaProjects\remote-assist
.\scripts\build-installer.ps1
# 产物：artifacts\installer\RemoteAssist-Setup-0.1.0.exe
```

需要 [Inno Setup 6](https://jrsoftware.org/isdl.php)。详见 [installer/README.md](installer/README.md)。

亲友侧：双击 Setup → 开始菜单打开 **「远端协助」** → 首页选角色。

### 方式 B：本机软安装（无 Inno）

```powershell
.\scripts\install-app-local.ps1
# 可选桌面图标：
.\scripts\install-app-local.ps1 -DesktopShortcut
```

### 方式 C：绿色拷贝

```powershell
.\scripts\publish-app.ps1
# 拷贝 artifacts\app-win-x64-sc\ ，运行 RemoteAssist.exe
```

## 怎么用

1. **被控（接受协助）**：首页点「接受协助」→ 会话码 / 二维码 / 托盘；可「返回首页」切换角色。
2. **控制（去协助别人）**：首页点「去协助别人」→ 粘贴载荷连接 → 列进程/目录/日志/执行（写操作对方确认）。
3. **直达参数**：`RemoteAssist.exe --accept` / `--control`；被控 CLI：`--headless` / `--yes`。

开发调试：

```powershell
dotnet run --project src/RemoteAssist.App
dotnet run --project src/RemoteAssist.App -- --accept
dotnet run --project src/RemoteAssist.Agent -- --headless   # 兼容独立被控
dotnet run --project src/RemoteAssist.Helper.UI            # 兼容独立控制端
```

| 参数 | 含义 |
|---|---|
| `--accept` / `--agent` | 启动后直接进入被控 |
| `--control` / `--helper` | 启动后直接进入控制 |
| `--port 17890` | 被控监听端口 |
| `--confirm-timeout 60` | 确认超时（秒） |
| `--headless` | 无 UI 被控（控制台确认） |
| `--yes` | 自动同意（仅冒烟） |
| `--insecure` | 明文 TCP（仅调试） |

二维码载荷（默认 TLS）：

```text
remoteassist:pair?v=1&host=<所选IP>&port=17890&code=<SESSION>&tls=1&fp=<SHA256>
```

## 协助端 CLI / MCP（高级）

```powershell
dotnet run --project src/RemoteAssist.Helper -- pair --payload "<载荷>"
.\scripts\publish-mcp.ps1
```

MCP 详见 [docs/MCP.md](docs/MCP.md)。被控界面 / 托盘可「复制 MCP 配置」。

## 构建与测试

```powershell
cd E:\IdeaProjects\remote-assist
dotnet build
dotnet test
.\scripts\publish-app.ps1
.\scripts\build-installer.ps1
```

## 审计日志

`%LOCALAPPDATA%\RemoteAssist\audit.log`

## 已知限制（v1）

- TLS 自签 + 指纹钉扎
- 无公网中继（仅 stub；设置可填 IP:端口占位）
- `run_command` 经 `cmd.exe /c`，超时约 15s
- 自包含安装包体积仍偏大（后续可压体积 / 换运行时策略）

## 仓库

计划发布到 Gitee 私有仓；当前本地 git，**默认不 push**。