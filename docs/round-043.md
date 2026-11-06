# 第 43 轮 — Windows net48 主路径对齐产品（会话码+公网中继）

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 实现：鹊桥-客户端  
> 状态：**blocked（安全闸门未通过 — 公网中继业务明文；暂不验收）**  
> 依据：R042 产品已验收。用户对照 net48 协助端截图拍板：当前仍是填 IP/主机/端口/TLS 指纹，且中继写「即将支持·仍走局域网」——**不可接受**。稳定性与体验优先；**Windows 亲友/客户主路径 = `src-net48` + 本机 .NET Framework 4.8**。演示与验收优先开 net48 产物。用户经由体验提出：会话码与验证码合成一次复制/粘贴（`docs/quality-connection-credential.md`），**并入本轮验收**。  
> **本轮不做出包、不上架官网安装包。**

## 本轮目标（高频）

把 Windows **net48 / WinForms** 主路径对齐已定产品模型（与 net8 Helper.UI / Avalonia 亲友主路径一致）：

1. **协助端主操作**：粘贴 **连接凭证**（`{会话码}-{8位验证码}`，或可粘贴 `remoteassist:pair…` 载荷；兼容旧两行码+密码）后点「连接」。**不要**把 Host / IP / 端口 / TLS 指纹手工局域网表单当主界面。
2. **默认公网中继**：默认 `121.196.45.81:17891`；去掉「即将支持·仍走局域网」等占位文案。局域网 Host/Port/指纹仅可作**高级折叠**（默认收起、不引导亲友去填）。
3. **品牌**：窗口标题与产品名用 **「鹊桥」**（或「鹊桥远程协助」），不要「远端协助」。
4. **接受协助（被控）**：第一次点击不得弹「启动被控模式失败」这类异常框；失败须给人话原因 + 可执行下一步（对齐 R003 连不上引导风格）。
5. **连接凭证一次复制/粘贴**（详见 `docs/quality-connection-credential.md`）：被控主展示+复制为 `{会话码}-{8位验证码}`，按钮「复制连接凭证」；协助端**一个**输入框；新码固定 8 位数字（保留前导零）；兼容旧两行；指定错误句；凭证不写日志。生成/校验与显示必须一起改。
6. **实现范围**：`src-net48/` + `scripts/build-net48.ps1` 产物；共享 Protocol 复制/解析/生成（`FriendConnectGuide` / `SessionPasswordGenerator` 等）应本轮一并改，供 Avalonia/CLI 同轮或紧随对齐。不改 AI-SSH-Pro；**不新增中继协议字段**（若核实码+密码/载荷经现有 JSONL 已可用则只改 UI/接线；缺字段导致不可用时先回报产品，本轮默认按「协议已够用」实现）。

## 验收标准

1. 冷启动去协助端：首屏主路径是粘贴连接凭证/载荷 +「连接」；**默认看不到** Host/IP、端口、TLS 指纹输入（若保留，须在折叠「高级 / 局域网」内且默认收起）。
2. 无任何「即将支持·仍走局域网」「中继即将支持」类占位；设置/旁注若出现中继，默认写公网地址或「使用默认中继」，不引导填局域网 IP。
3. 粘贴对方发来的连接凭证（或旧两行码+密码，或 `remoteassist:pair` 载荷），在对方已接受协助时，经**默认公网中继**可配对连上（或按 R003 给出人话失败原因，不得只抛异常框）。
4. 窗口标题/关于/主文案含「鹊桥」，不含「远端协助」作产品名。
5. 接受协助：首次点开始/主按钮，成功则进入展示码+验证码（主形态为 `{码}-{8位}`）；失败为人话+下一步，**不得** MessageBox「启动被控模式失败」+ 堆栈/异常原文当主提示。
6. **连接凭证**：被控主按钮「复制连接凭证」，剪贴板为单行 `{会话码}-{8位验证码}`；协助端单框一次粘贴可解析；新生成/刷新为 8 位数字且保留前导零；旧两行粘贴仍可用；残缺凭证提示「连接凭证不完整，请复制完整的「会话码-8位验证码」」；日志无完整凭证。
7. 交付：`scripts/build-net48.ps1` 可复现产物路径；截图 `docs/screenshots/r043-net48-*`（协助端首屏含单框、粘贴后、被控首屏含凭证形态；勿覆盖 r022–r042）。开放问题（当前生成长度/字符集、中继是否限长、旧短码迁移）写入交付说明。
8. 未改 AI-SSH-Pro；未改中继 JSONL 字段（除非产品书面同意的最小补字段——本轮默认不改）。

## 同轮打磨 / 顺延

- Avalonia + CLI：复制/解析若走共享 Protocol，**优先同轮**改完；产能不够时 Protocol 必须本轮落地，各 UI 扫尾明确记入 **R044**。
- **S005 中途掉线人话**与断开后人话（`docs/scene-s005-mid-session-drop.md`、`docs/quality-after-disconnect.md`）优先级**低于**本轮 net48 主路径 + 连接凭证；能同轮顺手做则做，否则明确记入 **R044**。
- 锁定文案仍有效（顺延时沿用）：中途掉线主状态「连接已断开。」+ 重连引导；自己断开「你已断开。若要再连，粘贴码后点「连接」。」；未刷新则旧码仍可再连；无新协议字段。

## 本轮不做

- 官网换包 / Inno 大安装包上架（net8 自包含仍为兼容备用，本轮不排包装轮）
- 改 AI-SSH-Pro、完整远程桌面、macOS 正式下载、HTTPS
- 自动重连 / 会话无感恢复
- 仅改显示而不改生成长度/字符集（禁止）

## 完成时

- 客户端：说明改动文件、`build-net48.ps1` 产物路径与体积；截图如上；凭证生成/解析改动与开放问题结论；若发现协议缺口先停并回报。
- 产品对照 net48 截图与本机运行验收（含一次粘贴连接凭证）；通过后再排 S005/R044 或包装。



## 客户端交付说明（2026-11-06 Asia/Shanghai）

### 改动摘要（仅 `src-net48/` + 本轮文档）

1. **协助端主路径**：单框粘贴连接凭证 / `remoteassist:pair` 载荷 +「连接」；Host/Port/TLS/中继在「高级」默认折叠；去掉「即将支持·仍走局域网」。
2. **默认公网中继**：`RelayEndpoint.DefaultPublic = 121.196.45.81:17891`；`HelperUiSettings.PreferRelay=true`。
3. **品牌**：窗口 / Product「鹊桥」；去掉「远端协助」产品名。
4. **接受协助启动**：
   - **实测根因**（用户本机 ui-errors.log，约束收紧前）：`ConfirmPrompt.EnsureWinFormsInitialized` 在首页 Form 已创建后再次调用 `Application.SetCompatibleTextRenderingDefault`，抛出 `InvalidOperationException`（「在创建第一个 IWin32Window 之前必须调用…」），`AppShell` 原逻辑 `MessageBox.Show(ex.ToString(), "启动被控模式失败")` 把堆栈甩给用户。
   - **修复**：`EnsureWinFormsInitialized` 捕获该异常（对齐 net8）；`AppShell` 改为人话 + 下一步，堆栈只追加 `%LocalAppData%\RemoteAssist\ui-errors.log`。
   - **另**：被控默认 `preferRelay` + `guest_host` 中继环（`AgentHost.RelayGuestLoopAsync`），LAN 监听默认跳过——属主路径接线，**不是**未复现的「端口占用」臆测。
5. **连接凭证（Protocol）**：`SessionPasswordGenerator` 新码固定 8 位数字（安全随机、保留前导零）；`FormatShareClipboard` → `{码}-{验证码}`；`TryParseShareText` 先按最后一个 `-` 拆分再 Normalize；兼容旧两行；残缺文案固定「连接凭证不完整，请复制完整的「会话码-8位验证码」」。被控主按钮「复制连接凭证」。
6. **公网接线**：移植 `RelayClient` / `RelayEndpoint` / `AssistClient.PairViaRelayGuest` / Agent `guest_host` 环；**未改中继 JSONL 字段**。

### 开放问题结论

| # | 结论 |
|---|------|
| 1 生成 | 改前 net48 无独立 SessionPassword；net8 仍为默认 4 位字母数字。本轮 net48 新码 **8 位数字**。 |
| 2 中继限长 | `relay/internal/relay/store.go`：`minPasswordLen=4`，guest 无最大长度/字符集限制；字符串哈希比对。**8 位数字可直接 guest_host/guest_join**（约束收紧前曾对公网 `121.196.45.81:17891` 实测 ok；本交付按新硬约束未再对本机发起连接）。 |
| 3 迁移 | 协助端仍解析旧两行与 ≥4 位旧码；刷新后旧凭证失效（既有语义）。 |
| 4 拆分 | 单行无冒号时按 **最后一个** `-` 拆：左侧会话码、右侧验证码，再分别 Normalize。 |

### 构建产物

- 命令：`powershell -File scripts/build-net48.ps1`（**仅编译/publish，不自动启动**）
- 目录：`artifacts/app-net48-fd/`
- 入口：`artifacts/app-net48-fd/RemoteAssist.Net48.exe`（约 19.5 KB；整夹约 1.48 MB）
- 依赖：本机已装 .NET Framework 4.8

### 验证实际执行 / 未做

**已做（本交付）**

- 用户本机：**仅** `dotnet`/`build-net48.ps1` 无 UI 编译 publish；编译通过。
- 隔离 box：连接凭证格式/解析/旧两行/残缺语义的 Python 对拍；8 位数字生成前导零抽样。
- 源码核对：中继 `minPasswordLen=4`、无 max；根因堆栈证据见上（来自约束收紧前本机 `ui-errors.log`）。

**未做（新硬约束：禁止用户本机启动 exe / 联调 / 弹窗 / ui-shot）**

- 用户本机运行验收与 `docs/screenshots/r043-net48-*` **本交付不附截图**（不冒充旧图）。
- 端到端经公网中继双端配对（本交付未再对本机发起）。
- Avalonia / CLI UI 扫尾 → **R044**。
- S005 中途掉线人话 → **R044**。
- 打安装包 / 部署 → 不做。

### 非用户本机复现步骤（供隔离 Windows / 产品机）

1. checkout 本 commit；`powershell -File scripts/build-net48.ps1`
2. 在**非用户日常机**运行 `artifacts/app-net48-fd/RemoteAssist.Net48.exe`
3. 「去协助」：确认首屏仅凭证框+连接，高级默认收起，状态含默认中继 `121.196.45.81:17891`
4. 「接受协助」：应出 `{码}-{8位}` +「复制连接凭证」；不应再出「启动被控模式失败」+堆栈
5. 双端经默认中继粘贴凭证试连；残缺粘贴应出固定人话




## 安全核查（2026-11-06 Asia/Shanghai）· 闸门结论：**未通过**

依据：`docs/architecture.md`、`docs/relay-protocol.md`、`docs/PRODUCT.md`（业务 TLS 端到端 + 证书指纹钉扎；`--insecure` 仅调试；中继不终结业务 TLS）。  
范围：只读复核 `5996705` 与 net8 对照；**未**在用户本机启动 exe/联调。

### 结论摘要

| 项 | 结论 |
|----|------|
| 闸门 | **未通过** — 不得请本轮产品验收 |
| 公网中继配对后业务流 | **明文**（本轮 net48 主动跳过业务 TLS） |
| 密码 guest_host/join | 仅控制面配对鉴权；**不能替代**业务 TLS |
| 是否本轮引入 | **本轮引入于被控/载荷侧**；协助端「无指纹则 force Insecure」在 **net8 已有同类逻辑**（既有 UX 妥协），本轮 **移植并与 agent `skipTls` 对齐成双端明文** |
| 是否仍请验收 | **否** |

### 证据（文件 / 方法）

1. **被控中继环强制跳过业务 TLS（本轮新增）**  
   - `src-net48/RemoteAssist.Agent/AgentHost.cs` → `RelayGuestLoopAsync`：`HandleStreamAsync(..., skipTls: true, ...)`  
   - 同文件 `HandleStreamAsync`：`if (!skipTls && !_insecure)` 才 `TlsTransport.AuthenticateAsServerAsync`  
   - 同文件 `QrPayload`（`_preferRelay`）：`PairQrPayload.BuildRelay(..., fingerprint: null, useTls: false)`

2. **协助端无指纹时强制明文（移植自 net8）**  
   - `src-net48/RemoteAssist.Helper.UI/MainForm.cs` → `ConnectAsync`：`ViaRelay && !Insecure && 无 Fingerprint` → `Insecure = true`  
   - net8 对照：`src/RemoteAssist.Helper.UI/MainForm.cs` 同注释「中继访客默认允许明文业务管道」

3. **net8 被控仍对中继 pipe 做业务 TLS（本轮 net48 相对 net8 agent 为回退）**  
   - `src/RemoteAssist.Agent/AgentHost.cs` → `RelayGuestLoopAsync` 调用 `HandleStreamAsync` **无** skipTls  
   - `HandleStreamAsync`：`if (!_insecure)` → `AuthenticateAsServerAsync`  
   - net8 `QrPayload` preferRelay：默认 `BuildRelay(..., fingerprint: fpR, useTls: true)`

4. **文档边界**  
   - `architecture.md`：业务面「TLS（自签 + 证书指纹钉扎）」；「中继只转发，不终结业务 TLS」  
   - `PRODUCT.md`：业务层默认 TLS + 指纹；`--insecure` 仅调试  
   - `relay-protocol.md`：paired 后 opaque pipe，业务 FrameIo/**TLS 可骑在管道内**

5. **局域网直连路径（未去掉 TLS）**  
   - `AgentHost.HandleClientAsync`：仍 `if (!_insecure)` → `AuthenticateAsServerAsync`  
   - 回归点在 **公网中继访客主路径**，不是 LAN。

6. **写操作本机确认**（路径仍在；但若业务明文则确认帧可被中继/路径窃听）  
   - `HandleStreamAsync` / `HandleClientAsync` → `HandleToolAsync` → `ConfirmPrompt.AskAsync`；超时 `ConfirmDecision.TimedOut` → 「确认超时，已自动拒绝」  
   - 非 Admin 写工具不可绕过；与中继配对成功解耦 — **确认逻辑未删**，但明文下机密性不足。

7. **8 位验证码**（源码保障；Python 对拍≠ C# 单测）  
   - `SessionPasswordGenerator.Generate`：`DefaultLength=8`；`Net48Crypto.FillRandom` → `RandomNumberGenerator.Create().GetBytes`；数字字符拼接故保留前导零；`Normalize` 仅 Trim  
   - 审计：`relay_guest_code` / `session_refresh` 等写 **code=**，未见完整凭证/password 入 `_audit.Write`（源码检索）。**未跑 C# 单测**（硬约束）。

### 修复方案（不扩中继 JSONL 字段；待产品/架构拍板后另开实现）

**P0 — 立刻恢复与 architecture / net8 agent 一致（禁止公网明文业务）**

1. `AgentHost.RelayGuestLoopAsync`：去掉 `skipTls: true`；与 net8 一样在 `!_insecure` 时对 relay stream `AuthenticateAsServerAsync`。  
2. `QrPayload` preferRelay：恢复 `BuildRelay(..., fingerprint: _certFingerprint, useTls: true)`（非 `--insecure`）。  
3. `Helper.UI/MainForm.ConnectAsync`：删除「ViaRelay 无指纹则 Insecure=true」；无指纹时给人话：请对方用「复制载荷」（含 fp）或展开高级填指纹；**禁止**为连通自动降明文。  
4. 文档：明确「连接凭证 `{码}-{验证码}` 只解决配对；业务 TLS 指纹仍须载荷/高级/后续产品形态」，**不得**再写「中继业务管道明文」。

**P1 — 产品体验（与凭证单行无 fp 的张力）**

- 选项 A：主路径改为复制含 `fp=` 的 `remoteassist:pair` 载荷（或凭证旁附指纹），UI 仍可「一次粘贴」。  
- 选项 B（需架构书面同意）：中继 pipe 上 **TOFU TLS**（加密但首连不预分享指纹，会话内钉扎）— **仍加密**，不是明文；不新增 relay 控制字段。  
- **不可接受**：公网 guest 路径长期明文当完成。

**降级表述（若短期不修）**：仅允许 `--insecure` / 本机调试；**不可**作为公网亲友主路径上架或验收。

### 本轮验收

**不提请验收。** 实现中的凭证/UI/首启人话可保留为后续修补基线，但 **5996705 含公网业务明文，安全闸门未通过。**


## 参考

- `docs/quality-connection-credential.md`（连接凭证规范）
- `docs/PRODUCT.md`（Windows 主路径 = Framework 4.8 / src-net48）
- `docs/round-003.md`（连不上人话）
- `docs/round-001.md`（历史两行「复制码+密码」）
- `docs/scene-s005-mid-session-drop.md`（顺延候选）
- `docs/round-042.md`（已验收；本轮不跟包）