# 第 8 轮 — 补齐剩余 P0：MCP 提议命令闭环 + 双模式安装包

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（只改 src/ / MCP / 安装包，不改中继、不换 UI 框架）
> 状态：**done**（交付见文末）
> 依据：docs/PRODUCT.md、docs/roadmap.md 中仍为 wip 的两项 P0

Windows 主路径（配对、确认、信任等级、质感）已经可演示。本轮把路线图里还标着 wip 的两件 P0 收口，不新开功能面，不做 Mac/Linux（不抢 P0）。

## 本轮目标

1. **高频**：MCP stdio 连上当前会话后，智能体能完成只读查询，并能提议写操作；写操作必须按被协助端当前信任等级确认，智能体不能绕过。
2. **打磨**：同一安装包双模式扫尾。安装打开后仍是首页「接受协助 / 去协助别人」；不要另做第二套安装程序。

## A. MCP 提议命令闭环

协助端已有「如何安装 MCP」和「复制 MCP 配置」（R005）。本轮要让这条路真正走通，而不是只有说明。

验收：

- 用界面复制出的 MCP 配置，智能体可以连上当前会话（经默认公网中继）。
- 至少能完成：查看正在运行的程序、查看日志、提议一项改动。
- 提议写操作时，被协助端按当前信任等级处理：每次询问必须弹确认；拒绝则未做任何改动；智能体不能绕过。完全信任时本会话不再弹窗，但仍记入「本次协助已确认的改动」。
- 仅接受协助时不自动对外监听 MCP；被协助端默认首屏仍不出现 MCP。
- 不要另发明安装文案；R005 的三步说明保持原样。

截图或可复核记录放到 `docs/screenshots/r008-*`（被协助端确认窗 + 协助端/说明不足以代替「提议后本机确认」）。

## B. 双模式安装包扫尾

验收：

- 同一个安装包安装后，打开是首页两张卡片：接受协助 / 去协助别人，可选其中一项。
- 不要拆成两个安装包，不要改中继，不要换 UI 框架。
- 若安装流程或快捷方式已经满足上面两点，在 round-008 交付说明里写明「已满足、本轮无代码改动」并附首页截图即可，不要为了改而改。

## 本轮不做

- 信任等级（R007 已验收）
- 重做 R006 质感、配对确认窗
- Mac/Linux 客户端（等 P0 收口）
- 改中继、改 AI-SSH-Pro、完整远程桌面
- 官网下载按钮改成真实安装包地址（仍「即将提供」，除非本轮确实产出了可公开的安装包链接）

## 交付说明（2026-11-05 Asia/Shanghai）

### 状态

**done**（本轮收口两项 P0；未开 R009）

### 代码

- **A. MCP 提议命令闭环**：新增 `McpStdioLoopIntegrationTests`——真实拉起 `RemoteAssist.Mcp.exe`，经本机中继 Content-Length 完成 `list_processes` / `read_log_tail` / `run_command`；另测 `source=mcp` 在「每次询问」下写操作超时拒绝、审计 `tool_denied_user`、无 `tool_ok` RunCommand（智能体不能绕过）。协助端仍用 R005「如何安装 MCP」/「复制 MCP 配置」原文，未另发明文案。
- **B. 双模式安装包**：**已满足、本轮无代码改动**。同一 `installer/RemoteAssist.iss`（AppName=鹊桥远程协助）安装后打开仍是 `ModeSelectForm` 首页两张卡片「接受协助 / 去协助别人」。见 `r008-home-dual-mode.png`。
- 被协助端默认首屏不出现「如何安装 MCP」；仅接受协助时不启动 MCP stdio 监听（AgentHost / AgentDesktopController 无 MCP server）。
- UiShot：`tools/UiShot` 本轮改为出 r008 截图；协助端增加 `PrepareMcpCopiedForShot` / `PrepareConnectedToolsForShot` 等截图辅助（不改亲友主路径交互）。

### 测试

- 全量 `RemoteAssist.Protocol.Tests`：**167 通过**（含 McpStdioLoop 2 条）

### 截图

| 文件 | 屏幕上可见原文（要点） |
|------|------------------------|
| `docs/screenshots/r008-home-dual-mode.png` | 「接受协助」「去协助别人」；副文「一个应用，两种角色 — 先选你要做什么」 |
| `docs/screenshots/r008-helper-mcp-guide.png` | 「如何安装 MCP」三步 +「复制 MCP 配置」；步骤 3 含「智能体不能绕过」 |
| `docs/screenshots/r008-helper-mcp-copied.png` | 「已复制 MCP 配置。请粘贴到 Cursor 的 MCP 设置并保存。」 |
| `docs/screenshots/r008-mcp-tools-session.png` | 「查看正在运行的程序」「查看日志」「提议一项改动」；输出区三行 ok=True（真实 `source=mcp`） |
| `docs/screenshots/r008-mcp-write-confirm.png` | 「对方想在这台电脑执行操作」「运行一条命令」「允许」「拒绝」；命令 `echo r008-propose` |
| `docs/screenshots/r008-mcp-write-denied.png` | 「最近审计」顶行「已拒绝，未做任何改动」；信任档「每次询问」；已确认列表不含 bypass |
| `docs/screenshots/r008-agent-accept-no-mcp.png` | 「接受协助 · 把会话码和验证密码发给对方」「复制码+密码」；首屏无「如何安装 MCP」 |

另：`docs/screenshots/r008-mcp-tools-session.txt` 为同次 MCP 工具原文复核。父级取图目录：`%USERPROFILE%\Downloads\r008-shots\`。

### 本轮不做（已遵守）

- 未开 R009；未改 `relay/`；未重做 R006/R007 UI；未做 Mac/Linux。

