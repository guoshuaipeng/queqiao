# 第 45 轮 — 亲友失败文案残留与错误提示收窄

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 实现：鹊桥-客户端  
> 状态：**编译/文档核对 = PASS_WITH_NOTES（`39d6dc1`）；运行验收仍 blocked（未验收）**  
> 依据：R044 编译/文档 **PASS_WITH_NOTES**（`7cad0ab`）；运行仍 blocked。  
> **本轮主题**：稳定性 / UX 收尾，**不**扩功能、**不**改凭证格式与信任模型。  
> **范围默认仅 net48**（除非改动极琐碎且同文件可顺手）；仍**不**在用户本机跑 UI；Avalonia/CLI 完整载荷主路径**另轮**。

## 本轮目标（高频 · 产品拍板）

1. **失败文案残留**：亲友可见的 `FailWrongPassword` / `FailPeerNotAccepting` 等仍写「码+密码」的句子，改为「完整连接凭证」口径（与 R044 权威表一致：说「完整连接凭证 / 安全验证信息」，不说指纹/TLS/fp）。
2. **Helper 连接错误提示收窄**：`MissingSecurityInfoHint`（缺安全验证信息句）**仅**在缺 fp / 短凭证等「缺少安全验证信息」场景追加或展示；**禁止**在错密码、指纹不匹配、对端未接受、网络失败等其他失败上**一律**追加该句（不得用缺失提示覆盖其他错误）。
3. **文档状态行清理**：`round-044` / `roadmap`（及本轮相关）去掉过时「待实现 / compiled 混用」等状态句，与 R044 验收结论一致。
4. 有条件时顺手：AgentHost 控制台面向开发者的技术词备忘（脱敏/降噪），**不**扩功能。

## 验收标准

1. 亲友默认失败路径不再出现「码+密码」作为主连接物说明；改为完整连接凭证口径。
2. 缺安全验证信息时仍逐字使用 R044 阻断句；错密码 / 不匹配 / 其他失败各自人话，**不**附带缺失句。
3. 指纹不匹配不得与缺失混为一类；无「忽略并连接」入口；不降级校验。
4. 文档状态与 R044 PASS_WITH_NOTES / 运行 blocked 一致；本轮不把编译写成真连 PASS。
5. **不改** `remoteassist:pair` 字段、业务 TLS 钉扎、中继 JSONL、8 位验证码规则。
6. 默认不改 Avalonia/CLI；用户本机不跑应用/测试；不包装、不改写历史。

## 本轮不做

- 发明新凭证编码 / TOFU / 自动降明文 / 改信任模型
- Avalonia/CLI 完整安全载荷移植（另轮）
- 官网换包、S005 中途掉线大功能、改 AI-SSH-Pro
- 在用户本机弹窗实测

## 完成时

- 客户端：改动文件列表；错误分支对照（缺 fp vs 错密码 vs 不匹配）；无 UI 编译产物元数据；明确未做项。
- 产品：编译/文档核对；运行验收另计。



## 交付（net48 · 编译）

> 时间：2026-11-06 Asia/Shanghai · **不改**凭证格式 / 信任模型 / 阻断条件 / 脱敏逻辑。

### 改动

- `FriendConnectGuide.cs`：`FailWrongPassword` / `FailPeerNotAccepting` / `FailStaleCode` → 完整连接凭证口径
- `Helper.UI/MainForm.cs`：`ConnectAsync` 通用 catch **不再**追加 `MissingSecurityInfoHint`；仍 `RedactSecureMaterial(FriendlyOrRaw)`
- 保留：连接前缺 fp 预检、`ConnectDraftParser`/`Need*`、`AssistClient` 缺安全验证阻断

### 明确未做

- 用户本机运行 / 联调 / 截图；Avalonia/CLI；包装部署；真连验收；历史改写

### 编译

- `scripts/build-net48.ps1` publish-only（不自动启动）
- 产物：`E:\IdeaProjects\remote-assist\artifacts\app-net48-fd\RemoteAssist.Net48.exe`
- **19968** 字节 · **2026-11-06 09:57:04 Asia/Shanghai**
- SHA256 `57F9ADB65B4FEC7A647E9966EAA105A1F4B8B24F23B320FFF3A75265706C60BF`

## 产品验收 · 编译/文档核对（2026-11-06 Asia/Shanghai）

**结论：编译/文档核对 = PASS_WITH_NOTES；真实连接 / 运行验收 = 仍 blocked（未验收）。**  
**不是**可公网使用的已验证客户端。

| 项 | 结果 |
|----|------|
| 证据 commit | `39d6dc1ee96a1748cffb59ead4493e6171a8d3e8`（短 `39d6dc1`） |
| 范围 | 只读源码 + 文档 + 产物元数据核对；**未**在用户本机启动 UI / 联调 / 截图 |
| net48 无 UI 编译 | 客户端交付后无 UI publish 成功 |
| 产物 | `RemoteAssist.Net48.exe`，**19968** 字节，**2026-11-06 09:57:04 Asia/Shanghai**，SHA256 `57F9ADB65B4FEC7A647E9966EAA105A1F4B8B24F23B320FFF3A75265706C60BF` |
| 失败文案 | `FailWrongPassword` / `FailPeerNotAccepting` / `FailStaleCode` 已改为完整连接凭证口径 |
| Helper catch | 通用 catch **仅** `SetFailHint(RedactSecureMaterial(friendly))`，**不再**一律追加 `MissingSecurityInfoHint` |
| 缺 fp 预检 | 保留；`ConnectDraftParser` / `AssistClient` 缺安全验证阻断保留 |
| 凭证格式 / 信任模型 / 脱敏 / 8 位规则 | **未改** |
| Avalonia / CLI | **本轮未改** |
| 真实连接 | **blocked / 未验收** |

### NOTES（转入 R046 / 备忘）

1. 各失败场景仅源码核对，**未**实机验证文案弹出路径。
2. Avalonia/CLI 完整安全载荷主路径、本机外运行验收、net8 Insecure 债 → **另轮**。
3. 产品新需求（会话码生成/使用审计 + 管理台只读查看）→ **R046**（服务端轮；见 `docs/round-046.md`）。

## 参考

- `docs/round-044.md`（NOTES → 本轮）
- `docs/quality-connection-credential.md`
- `docs/tls-trust.md`
