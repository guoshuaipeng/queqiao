# RemoteAssist / 远端协助

独立产品：可控远程协助 + AI 排障（可 MCP 嵌入其他智能体）。**不修改 AI-SSH-Pro。**

> **给亲友怎么用（一句话）**  
> 安装「远端协助」→ 打开主界面 → 把**会话码**或二维码发给协助方 → 对方连上后，你在本机点**允许**才执行危险操作。

## 已锁定决策（v1）

| 项 | 选择 |
|---|---|
| 语言 | .NET 8 |
| 鉴权 | 无账号；会话码配对 + 短时令牌 |
| 权限 | Guest 只读 / Assist 确认后执行 / Admin |
| 连接 | **仅局域网**（公网中继后期，见 `src/RemoteAssist.Relay.Stub`） |
| MCP | 通用 stdio |
| 远控桌面 | 不做 |
| 安装 | Inno Setup 安装包 / 本机软安装脚本 |

详见 [docs/PRODUCT.md](docs/PRODUCT.md)。

## 解决方案结构

- `src/RemoteAssist.Protocol` — 共享消息 / 权限 / 会话契约 / TCP·TLS 帧 / 客户端 / 二维码载荷（含 fp） / 局域网地址筛选 / 协助端连接草稿解析
- `src/RemoteAssist.Agent` — **被协助端产品**：主界面 + 托盘、确认窗口、审计、会话码
- `src/RemoteAssist.Helper` — 协助端 CLI（支持整段 QR 载荷 / `--payload` / `--clipboard`）
- `src/RemoteAssist.Helper.UI` — **协助端 GUI**：配对向导、常用操作、中继设置占位
- `src/RemoteAssist.Mcp` — stdio MCP Server（见 docs/MCP.md）
- `src/RemoteAssist.Relay.Stub` — 公网中继占位（端点解析 + UpcomingHint；可用纯 IP）
- `installer/` — Inno Setup 脚本与说明
- `tests/RemoteAssist.Protocol.Tests` — 单元测试

## 环境要求

- Windows 10/11 x64（Agent 面向修 Windows；主界面 / 托盘 / 确认窗依赖 WinForms）
- 开发机：.NET SDK 8+（Windows Desktop 工作负载）
- 亲友机：使用**自包含安装包**时通常**无需**再装 .NET

若本机 `dotnet` 不在 PATH：

```powershell
$env:PATH = "C:\Users\user\AppData\Local\Microsoft\dotnet;" + $env:PATH
```

## 给亲友安装（推荐）

### 方式 A：Setup 安装包（标准产品）

开发机生成：

```powershell
cd E:\IdeaProjects\remote-assist
.\scripts\build-installer.ps1
# 产物：artifacts\installer\RemoteAssist-Agent-Setup-0.1.0.exe
```

需要本机已装 [Inno Setup 6](https://jrsoftware.org/isdl.php)。未安装时脚本会提示；也可先看 [installer/README.md](installer/README.md)。

亲友侧：

1. 双击 `RemoteAssist-Agent-Setup-*.exe`
2. 默认装到 `%LOCALAPPDATA%\Programs\RemoteAssist`（当前用户，一般不用管理员）
3. 开始菜单打开 **「远端协助」**
4. 主界面会出现**大号会话码 + 二维码**；把码发给你
5. 关闭窗口 ≠ 退出：程序留在托盘（蓝点图标）

### 方式 B：本机软安装（无 Inno 时）

```powershell
.\scripts\install-agent-local.ps1
# 可选桌面图标：
.\scripts\install-agent-local.ps1 -DesktopShortcut
```

同样装到 `%LOCALAPPDATA%\Programs\RemoteAssist`，并写开始菜单 /「应用和功能」卸载项。

### 方式 C：绿色拷贝

```powershell
.\scripts\publish-agent.ps1
# 把 artifacts\agent-win-x64-sc\ 整个目录拷给对方，运行 RemoteAssist.Agent.exe
```

## 开协助（被协助端主界面）

默认双击 / 开始菜单启动后：

- **主界面**：会话码、二维码、本机 IP、短 TLS 指纹、复制载荷 / MCP 配置、已配对列表、最近审计
- **托盘**：双击打开主界面；右键可刷新会话码、断开客户端、退出
- **确认窗**：配对与写操作必须本机点允许（超时拒绝）

开发调试仍可用：

```powershell
dotnet run --project src/RemoteAssist.Agent
# CLI / 冒烟：
dotnet run --project src/RemoteAssist.Agent -- --headless
dotnet run --project src/RemoteAssist.Agent -- --yes
```

| 参数 | 含义 |
|---|---|
| `--port 17890` | 监听端口 |
| `--confirm-timeout 60` | 配对/写操作确认超时（秒） |
| `--headless` | 无 UI：不启主界面/托盘；确认回退控制台 |
| `--yes` | 自动同意（仅本地冒烟） |
| `--insecure` | 明文 TCP（仅调试） |

二维码载荷（默认 TLS）：

```text
remoteassist:pair?v=1&host=<所选IP>&port=17890&code=<SESSION>&tls=1&fp=<SHA256>
```

## 协助端（Helper GUI / CLI / MCP）

### 方式 A：协助端 GUI（推荐）

```powershell
dotnet run --project src/RemoteAssist.Helper.UI
# 或单文件发布后双击：
.\scripts\publish-helper-ui.ps1
# 产物：artifacts\helper-ui-win-x64-sc\RemoteAssist.Helper.UI.exe
```

界面流程：

1. **从剪贴板** / 粘贴对方主界面复制的 `remoteassist:pair?...` 载荷（或手填主机 + 会话码 + TLS 指纹）
2. 确认预览行里的 host / port / TLS 指纹后点 **连接**
3. 连接后用「列进程 / 列目录 / 读日志尾 / 读文件 / 执行命令」；执行类操作仍需对方本机确认
4. 设置里的 **中继服务器（IP:端口）** 为占位，标注「即将支持」，当前仍走局域网直连

### 方式 B：CLI（保留）

```powershell
dotnet run --project src/RemoteAssist.Helper -- pair --payload "<从主界面复制>"
# 或
dotnet run --project src/RemoteAssist.Helper -- pair --clipboard
```

配对确认后：

```text
helper> procs
helper> ls C:\
helper> run echo hello-from-remote-assist
```

MCP 详见 [docs/MCP.md](docs/MCP.md)。Agent 主界面 / 托盘 **「复制 MCP 配置」** 可生成 Cursor `mcp.json` 片段。

## 构建与测试

```powershell
cd E:\IdeaProjects\remote-assist
dotnet build
dotnet test
.\scripts\build-installer.ps1
```

## 审计日志

`%LOCALAPPDATA%\RemoteAssist\audit.log`  
主界面底部显示最近若干条；可点「打开日志文件」。

## 已知限制（v1）

- TLS 为自签证书 + 指纹钉扎；非完整 PKI
- 无公网中继（仅 stub）
- 公网中继未接通（设置里可填 IP:端口占位；可用纯 IP，不强制域名）
- `run_command` 经 `cmd.exe /c`，超时约 15s
- 部分敏感路径硬编码拦截

## 仓库

计划发布到 Gitee 私有仓；当前本地 git，**默认不 push**。
