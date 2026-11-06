# 第 44 轮 — 完整连接凭证逐字文案扫尾

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 体验规格：鹊桥-体验（已定，权威字符串见下 / `quality-connection-credential.md`）  
> 实现：鹊桥-客户端  
> 状态：**编译/文档核对 = PASS_WITH_NOTES（`7cad0ab`）；运行验收仍 blocked（未验收）**  
> 依据：R043 编译/文档 **PASS_WITH_NOTES**（`0122127`）；真连仍 blocked。本轮落地 net48 七句逐字文案。  
> **本轮不改**凭证格式、信任模型、中继协议字段；**不**把编译通过写成真连验收通过。

## 本轮目标（高频）

1. **逐字文案**落地到 Windows net48 亲友可见界面，与规范一致。（Avalonia/CLI 本轮**未改**——尚未移植 R043 完整安全载荷主路径，只改文案会文实不符；跨端对齐另轮。）
2. 亲友默认说明 / 错误 / 复制反馈：**不得**出现「指纹」「fp」「TLS」「安全载荷」；改用「完整连接凭证」「安全验证信息」。
3. 缺失安全验证信息 ≠ 验证不匹配；**无**「忽略并连接」入口。
4. 有条件时：本机外运行/真实连接验收（不打断用户本机）；未具备条件则明确仍 blocked。
5. 备忘：net8 Helper「无指纹 force Insecure」债（本轮不修，须写入交付说明，不得扩大）。

## 逐字文案（权威）

| 位置 | 字符串 |
|------|--------|
| 协助端输入框标签 | 粘贴完整连接凭证 |
| 协助端旁注 | 把对方通过「复制连接凭证」发来的整段粘贴到这里。 |
| 协助端主操作 | 连接 |
| 被协助端复制按钮 | 复制连接凭证 |
| 复制成功 | 连接凭证已复制，请完整发给对方。 |
| 复制失败 | 未能复制，请选中完整连接凭证，手动复制后发给对方。 |
| 缺少安全验证信息阻断 | 连接凭证缺少安全验证信息，暂时无法连接。请让对方点击「复制连接凭证」，重新发送完整内容。 |

## 验收标准

1. 协助端单框标签与旁注**逐字**匹配；一次粘贴完整串，无需拆分。
2. 复制真正成功才显示成功句；失败显示失败句，界面仍可手动复制完整串。
3. 无安全验证信息的短码或旧两行被阻断，提示**逐字**匹配；无忽略入口。
4. 亲友默认界面的标签、提示与错误不含「指纹」「fp」「TLS」「安全载荷」。
5. 验证不匹配仍拒绝连接，不用缺失提示覆盖，不降级。
6. 文档状态不把未完成的运行验收写成已通过；R043 真连 blocked 结论不变，直至另有运行验收。
7. **不改** `remoteassist:pair` 字段形态、业务 TLS 钉扎规则、中继 JSONL。

## 本轮不做

- 改信任模型 / 发明新凭证编码 / TOFU / 自动降明文
- 改 Avalonia / CLI（功能未对齐完整载荷前不借文案轮扩功能）
- 官网换包、改 AI-SSH-Pro、完整远程桌面
- 在用户本机弹窗实测（优先本机外或用户明确允许）

## 完成时

- 客户端：改动文件列表；对照上表逐字核对说明；Avalonia/CLI 覆盖范围；net8 债是否动刀。
- 产品：文案逐字 + 亲友 UI 无技术词；运行验收另计或按条件执行。

## 交付（net48 文案 · 编译）

> 时间：2026-11-06 Asia/Shanghai · 范围仅 `src-net48` 亲友可见文案；**不改**凭证格式 / 信任模型 / 阻断条件。

### 改动文件

- `RemoteAssist.Protocol/FriendConnectGuide.cs` — R044 逐字常量；Need*Hint 对齐 MissingSecurityInfoHint
- `RemoteAssist.Protocol/ConnectDraftParser.cs` — 非加密通道人话；阻断条件未改
- `RemoteAssist.Protocol/AssistClient.cs` — 缺安全验证信息抛错用逐字句
- `RemoteAssist.Helper.UI/MainForm.cs` — 标签/旁注/预览/连接/失败提示
- `RemoteAssist.Agent/MainForm.cs` — 引导/短码旁注/复制成功失败；失败时展示完整串供手选
- `RemoteAssist.App/ModeSelectForm.cs` — 模式副文去技术词

### 逐字落点

| 规格句 | 源码 |
|--------|------|
| 粘贴完整连接凭证 | PasteCredentialLabel → Helper 输入区标签 |
| 旁注整段粘贴 | PasteCredentialHint → Helper 头/预览；ModeSelect 协助侧 |
| 连接 | ConnectButtonText → Helper 连接按钮 |
| 复制连接凭证 | CopyCredentialButtonText → Agent |
| 连接凭证已复制… | CopyCredentialSucceededHint |
| 未能复制… | CopyCredentialFailedHint + 手动选取对话框 |
| 缺少安全验证信息… | MissingSecurityInfoHint（Need*Hint）→ Parser/Helper/AssistClient |
| 引导 / 短码旁注 | AcceptGuideHint / OralShortCodeHint → Agent |

### 明确未做

- Avalonia / CLI / net8
- 用户本机运行 / 联调 / 截图 / 真连验收（R043 真连 blocked 不变）
- 包装部署；历史改写

### 编译

- `scripts/build-net48.ps1` publish-only（不自动启动）
- 产物：`E:\IdeaProjects\remote-assist\artifacts\app-net48-fd\RemoteAssist.Net48.exe`
- **19968** 字节 · **2026-11-06 09:46:59 Asia/Shanghai**
- SHA256 `a78c0f246c4532025cd5ed0407013ac1828e2e3bb379fc692a0a32604080d81e`

## 产品验收 · 编译/文档核对（2026-11-06 Asia/Shanghai）

**结论：编译/文档核对 = PASS_WITH_NOTES；真实连接 / 运行验收 = 仍 blocked（未验收）。**  
**不是**可公网使用的已验证客户端。

| 项 | 结果 |
|----|------|
| 证据 commit | `7cad0aba269f8c5a50c3b0fa50ef5123b6228dd4`（短 `7cad0ab`） |
| 范围 | 只读源码 + 文档 + 产物元数据核对；**未**在用户本机启动 UI / 联调 / 截图 |
| net48 无 UI 编译 | 客户端交付后无 UI publish 成功 |
| 产物 | `RemoteAssist.Net48.exe`，**19968** 字节，**2026-11-06 09:46:59 Asia/Shanghai**，SHA256 `a78c0f246c4532025cd5ed0407013ac1828e2e3bb379fc692a0a32604080d81e` |
| 七句逐字 | FriendConnectGuide / Helper / Agent / ModeSelect 主路径已对齐权威表 |
| 默认亲友路径技术词 | 标签/旁注/复制反馈/缺安全验证阻断已去「指纹/fp/TLS/安全载荷」；高级 TLS 诊断保留 |
| 凭证格式 / 信任模型 | **未改**；缺 fp 阻断条件未改 |
| Avalonia / CLI | **本轮未改**（完整载荷未移植，另轮） |
| 真实连接 | **blocked / 未验收** |

### NOTES（转入 R045）

1. **失败文案残留**：`FailWrongPassword` / `FailPeerNotAccepting` 等仍写「码+密码」类措辞，应改为「完整连接凭证」亲友人话（不改判定逻辑）。
2. **Helper 错误提示过宽**：连接失败 catch 路径**一律**追加 `MissingSecurityInfoHint`，会盖住错密码 / 指纹不匹配 / 其他失败；缺失提示**仅**用于缺 fp / 短凭证等缺安全验证信息场景。
3. **文档状态行**：本轮落地前 `round-044` / `roadmap` 曾残留「规格已定；待实现」与「compiled」混用——本验收文档已纠正；R045 再扫尾同类过时句。
4. **AgentHost 控制台**：仍可能出现面向开发者的技术词备忘（非亲友 UI）；R045 视情况脱敏或降噪，不扩功能。
5. Avalonia / CLI 完整安全载荷主路径、本机外运行验收、net8 Insecure 债 → **另轮**（不并入 R045 文案残留扫尾，除非极琐碎）。

## 参考

- `docs/quality-connection-credential.md`
- `docs/tls-trust.md`
- `docs/round-043.md`
- `docs/round-045.md`
