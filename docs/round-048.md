# 第 48 轮 — net48 协助端补 MCP 复制安装说明 / JSON

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 实现：鹊桥-客户端（仅 **src-net48** Helper / 去协助 UI；可迁 Protocol 常量和 snippet 辅助类）  
> 状态：**编译/文档核对 = PASS_WITH_NOTES（`2d427d4`）；运行验收仍 blocked（未验收）**  
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



## 交付（net48 Helper · 编译）

> 时间：2026-11-06 Asia/Shanghai · 仅 src-net48 Helper + Protocol 常量/辅助；**不**改 Avalonia/连接格式/信任模型；**不**自动对外监听 MCP。

### 改动文件

- `RemoteAssist.Protocol/McpInstallGuide.cs`（新）
- `RemoteAssist.Protocol/WriteConfirmCopy.cs`（新，安装说明；「复制连接凭证」口径）
- `RemoteAssist.Protocol/McpExeLocator.cs`（自 net8 Protocol 迁入，供 Helper）
- `RemoteAssist.Helper.UI/MainForm.cs`：主屏「复制安装说明」+ MCP 卡三步 +「复制 MCP 配置」+「复制 Skill 说明」

### 接线要点

- 「复制 MCP 配置」：`TryBuildOptions` → 要求合法 fp 且非 Insecure → `PairQrPayload.BuildRelay/Lan(..., useTls:true)` → `McpConfigSnippet.BuildClipboardBundle`；**禁止**缺指纹降明文
- 「复制安装说明」：`WriteConfirmCopy.InstallInstructionsClipboard`；成功 `InstallCopiedHint`
- 反馈经 `RedactSecureMaterial`；不把完整载荷写进状态/失败原文
- 无 MCP 监听/服务启动代码

### 编译产物

- 路径：`artifacts/app-net48-fd-r048/RemoteAssist.Net48.exe`（因 `artifacts/app-net48-fd` 被已运行的 RemoteAssist.Net48 占用未能覆盖；**本轮未启动/未结束用户进程**）
- **19968** 字节 · **2026-11-06 10:25:51 Asia/Shanghai**
- SHA256 `53974730E878F07C031212CC20338945E5C4B898FDE6C7B201A35D0AA5F4A20D`

### 未做 / 未验证

- 用户本机 UI 运行、联调、截图、剪贴板实贴 Cursor
- Avalonia/CLI；R046/R047；包装部署；真连 PASS



## 产品验收 · 编译/文档核对（2026-11-06 Asia/Shanghai）

**结论：编译/文档核对 = PASS_WITH_NOTES；真实连接 / 运行验收 = 仍 blocked（未验收）。**  
**不是**可公网使用的已验证客户端。

| 项 | 结果 |
|----|------|
| 证据 commit | `2d427d4c6eaa8ce0b5b699cc3cf43bc41b3700ac`（短 `2d427d4`） |
| 范围 | 只读源码 + 文档 + 产物元数据核对；**未**在用户本机启动 UI / 联调 / 截图 / 剪贴板实贴 Cursor |
| 改动范围 | Helper.UI + Protocol（`McpInstallGuide` / `WriteConfirmCopy` / `McpExeLocator`）；**未**改 Agent / 接受协助逻辑；无 MCP 监听/启动 |
| net48 无 UI 编译 | 客户端交付产物存在；产品另目录 `artifacts/app-net48-fd-r048-verify` publish **0 错误**（默认 `app-net48-fd` 仍可能被已运行进程占锁，**未杀进程**） |
| 交付产物 | `artifacts/app-net48-fd-r048/RemoteAssist.Net48.exe`，**19968** 字节，**2026-11-06 10:25:51 Asia/Shanghai**，SHA256 `53974730E878F07C031212CC20338945E5C4B898FDE6C7B201A35D0AA5F4A20D`（与客户端声明一致） |
| 核对重编产物 | `artifacts/app-net48-fd-r048-verify/RemoteAssist.Net48.exe`，**19968** 字节，**2026-11-06 10:29:01 Asia/Shanghai**，SHA256 `A4409597AC3892F2AD40FCCB63E0024D600B61272641A8E65BDB672BF1460DFB`（同尺寸；PE 时间戳导致哈希与交付物不同，属预期） |
| 界面入口（源码） | 主屏「复制安装说明」+ MCP 卡三步 +「复制 MCP 配置」+「复制 Skill 说明」；「连接」仍为主按钮 |
| 复制 MCP 配置 | `TryBuildOptions` → 要求合法 fp 且 **非** Insecure → `PairQrPayload.BuildRelay/Lan(..., useTls:true)` → `McpConfigSnippet.BuildClipboardBundle`；缺凭证用人话 `NeedCredentialHint` |
| 复制安装说明 | `WriteConfirmCopy.InstallInstructions` 含「复制连接凭证」口径；成功 `InstallCopiedHint` |
| 凭证格式 / 信任模型 / guest_* | **未改**；禁缺指纹降明文 MCP |
| Avalonia / CLI / R046 / R047 | **本轮未改** |
| 真实连接 / UI 运行 | **blocked / 未验收** |
| 包装部署 | **未做**（不在范围） |

### NOTES

1. 默认 `artifacts/app-net48-fd` 曾被本机已运行 `RemoteAssist.Net48` 占锁，交付改发 `app-net48-fd-r048`；产品核对未杀进程。
2. **未**做 UI/剪贴板/Cursor 连通/真连验收；本结论仅为编译/文档核对。
3. 包装部署不在本轮范围（见 R047）。
4. R046 / R047 仍并行 open，互不阻塞。

## 参考

- docs/round-005.md、docs/round-017.md、docs/round-021.md、docs/round-027.md
- docs/round-043.md～docs/round-045.md、docs/quality-connection-credential.md、docs/tls-trust.md
- docs/round-046.md、docs/round-047.md（并行）
- src/RemoteAssist.Protocol/McpInstallGuide.cs、McpConfigSnippet.cs
- src/RemoteAssist.Helper.UI/MainForm.cs
- src-net48/RemoteAssist.Agent/McpConfigCopyHelper.cs
