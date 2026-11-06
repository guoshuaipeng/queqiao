from pathlib import Path
p = Path('docs/round-043.md')
t = p.read_text(encoding='utf-8')
t = t.replace(
    '> 状态：**client-done（待产品验收；本交付未含用户本机运行截图）**',
    '> 状态：**blocked（安全闸门未通过 — 公网中继业务明文；暂不验收）**',
    1,
)
gate = '''

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

'''
if '## 安全核查' not in t:
    if '## 参考' in t:
        t = t.replace('## 参考', gate + '\n## 参考', 1)
    else:
        t += gate
p.write_text(t, encoding='utf-8', newline='\n')
print('round-043 security gate written')

# roadmap one-liner
rp = Path('docs/roadmap.md')
rt = rp.read_text(encoding='utf-8')
rt2 = rt.replace('R043 client-done待验收', 'R043 blocked（安全闸门：公网业务明文未过）', 1)
if rt2 != rt:
    rp.write_text(rt2, encoding='utf-8', newline='\n')
    print('roadmap updated')
else:
    print('roadmap no replace; check manually')
