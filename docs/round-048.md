# 第 48 轮 — net48 协助端补 MCP 复制安装说明 / JSON

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 实现：鹊桥-客户端（仅 **src-net48** Helper / 去协助 UI；可迁 Protocol 常量和 snippet 辅助类）  
> 状态：**open（规格已开）** — **尚未实现代码**  
> 依据：用户在 net48「去协助」界面找不到可贴给智能体的 MCP 安装 JSON；只读核对结论——**不是藏起来了，是 net48 Helper 从未迁过** MCP「复制安装说明 / 复制 MCP 配置」；net48 **Agent（接受协助）** 已有「复制 MCP 配置」，**Helper 没有**。  
> **并行说明**：**R046**（会话码审计）/ **R047**（net48 安装包）仍 open，互不阻塞。本轮只补 net48 协助端 MCP / 安装说明入口。

## 产品目标

让 Windows **net48「去协助」**界面具备与 net8 Helper.UI / Avalonia HelperWindow 同等的：

1. **亲友向**「复制安装说明」（发给对方怎么装鹊桥）；
2. **智能体向**「如何安装 MCP」三步 +「复制 MCP 配置」——剪贴板为**可直接粘贴**到 Cursor 等工具的 **mcp.json / MCP 设置 JSON**（含当前会话完整 
emoteassist:pair 载荷）。

不改连接格式、信任模型、guest_* 协议。

## 缺口（只读事实）

| 端 | MCP「复制配置 / 安装说明」 |
|----|---------------------------|
| net8 RemoteAssist.Helper.UI | 有：「复制安装说明」+ McpInstallGuide 卡 +「复制 MCP 配置」+「复制 Skill 说明」 |
| Avalonia 去协助（R017） | 有：同等入口与反馈 |
| net48 Agent | 有：「复制 MCP 配置」（托盘/主窗） |
| **net48 Helper** | **无**（主路径只有粘贴完整连接凭证 + 连接） |
| net48 Protocol | 有 McpConfigSnippet；**无** McpInstallGuide.cs；**无** WriteConfirmCopy 安装说明常量 |

## 产品拍板（设计锁死）

1. **范围**：仅 net48 **去协助 / Helper** UI（及为实现所必需的 src-net48/RemoteAssist.Protocol 常量/辅助类移植）。**不**改 Avalonia/CLI；**不**改 Agent 已有 MCP 复制逻辑（可复用其 McpConfigSnippet / locator 思路）。
2. **参考实现（文案与能力对齐，勿另起说法）**：
   - src/RemoteAssist.Protocol/McpInstallGuide.cs
   - src/RemoteAssist.Helper.UI/MainForm.cs（复制安装说明 / MCP 卡 / Skill）
   - Avalonia HelperWindow（R017 交付说明）
   - net48 Agent 侧 McpConfigCopyHelper / McpConfigSnippet（JSON 生成可复用）
3. **界面放置**：MCP 安装卡与「复制安装说明」在 **Helper 主屏常显**（对齐 net8：不必等连上才出现）。「连接」仍是唯一实心主按钮；安装说明 / MCP / Skill 为**轻按钮**，不抢主点击。
4. **复制安装说明（亲友向）**：
   - 按钮文案：「复制安装说明」
   - 剪贴板：移植/对齐 net8 WriteConfirmCopy.InstallInstructions（含官网下载地址等已验收内容）
   - **顺手口径**：若正文仍写「复制码+密码」，改为「复制连接凭证」，与 R043/R044 一致（不算改协议）
   - 成功反馈：对齐 InstallCopiedHint（或同等「说明里已带上官网下载地址…」）；失败不报成功
5. **如何安装 MCP + 复制 MCP 配置（智能体向）**：
   - 迁入 McpInstallGuide 到 net48 Protocol（SectionTitle / 三步 / CopyConfigButton / 反馈常量）
   - 「复制 MCP 配置」输出 McpConfigSnippet.BuildClipboardBundle(exe, payload)：**可粘贴 JSON**，REMOTE_ASSIST_PAYLOAD（或 args）须为**完整安全载荷**（含 	ls=1 与 Agent 业务 p=），与当前粘贴框解析结果一致
   - **禁止**移植 net8 Helper 里「缺指纹时勾 Insecure 再生成明文 MCP 配置」的退路；缺完整凭证则阻断并人话提示，对齐 R043 禁 TOFU / 禁自动降明文
   - 缺凭证提示改为完整连接凭证口径（例如：「请先粘贴完整连接凭证，再复制 MCP 配置」），勿再写「会话码和验证密码」为主
   - exe 找不到时可用占位路径 + CopiedPlaceholderHint；找到则 CopiedOkHint
6. **复制 Skill 说明（建议同轮）**：轻按钮「复制 Skill 说明」，正文/反馈用 McpInstallGuide.SkillInstructions*（R027）；若产能紧可记 NOTES 下轮，但优先同轮落地以免协助端再缺一块。
7. **亲友主路径边界**：被协助端（接受协助）默认首屏仍不把 MCP 当主路径；本轮是**协助端**补入口（与 PRODUCT「协助端须能看到怎么安装 MCP」及 R005 一致）。协助端可见「MCP」安装指引字样（智能体场景需要）；不在默认文案里堆 TLS/fp/端口。
8. **验收节奏**：无 UI 编译 + 源码/文档核对即可请产品「编译/文档核对」；**不**在用户本机强行跑 UI/联调除非用户要求；不包装部署；不改写历史。

## 验收标准（产品）

1. net48 去协助界面可见：「复制安装说明」、如何安装 MCP 三步、「复制 MCP 配置」（及若实现则「复制 Skill 说明」）；「连接」仍为主按钮。
2. 「复制 MCP 配置」剪贴板含可解析的 mcpServers JSON，且 payload 为完整 
emoteassist:pair（含 fp）；短码/缺 fp **不能**生成「可连」的降级明文配置。
3. 「复制安装说明」为亲友向纯文本；成功/失败反馈人话；安装说明口径含「复制连接凭证」而非过时「码+密码」主说法。
4. 无 UI publish 成功；产物路径/字节/时间写入 round；文档状态 **compiled** 前不得称运行 PASS。
5. **不**改 R046/R047 范围；**不**改 guest_* / 信任模型 / 完整载荷格式。

## 本轮不做

- 会话码审计 / 管理台（R046）
- net48 Setup 安装包（R047）
- Avalonia/CLI 完整载荷移植
- 官网上传换包
- 在用户本机无人值守弹窗；改写 git 历史
- 把 MCP 重新做成「仅接受协助」默认对外监听

## 完成时（实现轮）

- 客户端：改动文件列表、是否迁入 McpInstallGuide/WriteConfirmCopy 片段、publish 元数据、是否含 Skill 按钮。
- 产品：对照本验收做编译/文档核对；运行验收另计。

## 参考

- docs/round-005.md、docs/round-017.md、docs/round-021.md、docs/round-027.md
- docs/round-043.md～docs/round-045.md、docs/quality-connection-credential.md、docs/tls-trust.md
- docs/round-046.md、docs/round-047.md（并行）
- src/RemoteAssist.Protocol/McpInstallGuide.cs、McpConfigSnippet.cs
- src/RemoteAssist.Helper.UI/MainForm.cs
- src-net48/RemoteAssist.Agent/McpConfigCopyHelper.cs
