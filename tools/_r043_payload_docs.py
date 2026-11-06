from pathlib import Path
import re

p = Path("docs/round-043.md")
t = p.read_text(encoding="utf-8")
t = re.sub(
    r"> 状态：\*\*.*?\*\*",
    "> 状态：**blocked（完整安全载荷已接线；短码仅口述；与 docs/tls-trust.md「内置官方指纹」冲突待架构撤回；暂不验收真实连接）**",
    t,
    count=1,
)

sec = """

## 完整安全载荷接线（产品 09:15 拍板 · 同轮纠错）

**主路径「复制连接凭证」= 完整安全载荷**（现有 `PairQrPayload.BuildRelay`，含**本机 Agent 业务**指纹），协助端单框一次粘贴。口述短码 `{会话码}-{8位}` 可展示，**不可**默认可连。缺 fp / 残缺 → 安全阻断人话；无 Insecure/skipTls 静默降级。旧两行若无合法指纹则引导完整载荷。

### 实际载荷形态（字段顺序 · 仅占位，非 secret）

Scheme 与实现一致（`src-net48/.../PairQrPayload.cs` → `BuildRelay`）：

```
remoteassist:pair?v=2&via=relay&relay={host:port}&code={SESSION_CODE}&pwd={PASSWORD}&tls=1&fp={AGENT_BUSINESS_CERT_SHA256_HEX64}
```

- `fp`：被控进程临时自签业务证书的 SHA-256（大写十六进制 64 字符，无分隔符），**不是**中继控制面证书。
- 示例（掩码）：`remoteassist:pair?v=2&via=relay&relay=121.196.45.81:17891&code=ABCDEF&pwd=********&tls=1&fp=********************************************`

### 文档冲突披露（不覆盖）

本地存在 `e7d012c` / 工作区改动中的 `docs/tls-trust.md` 仍写「默认公网中继内置官方业务指纹」。该说法与产品 09:15 及本实现（每台 Agent 临时业务证）**冲突**。本提交**未修改/未覆盖** `docs/tls-trust.md`（检测到他人可能正在改），等待架构按产品拍板撤回修正。本轮实现以完整安全载荷为准，**不**接线不存在的官方指纹。

### 安全核对（源码）

| 项 | 结果 |
|----|------|
| 复制主按钮 | `MainForm.CopySecureCredential` → `_host.QrPayload`（= BuildRelay + 业务 fp） |
| 口述展示 | `FormatConnectionCredential` 仅 UI 短码 |
| 指纹合法性 | `TlsCertUtil.IsWellFormedFingerprint`：规范化后 64 hex；`BuildRelay(useTls:true)` 强制 |
| 协助端无/坏 fp | `ConnectDraftParser` + Helper 阻断；`NeedSecurePayloadHint` / `NeedFingerprintHint` |
| 日志回显 | `RedactSecureMaterial` 掩码 `pwd=`/`fp=`；错误提示不回显完整载荷 |
| 中继字段 | 未改 |

"""

if "## 完整安全载荷接线" not in t:
    if "## 参考" in t:
        t = t.replace("## 参考", sec + "\n## 参考", 1)
    else:
        t += sec

t = t.replace(
    "主分享仍为短凭证 `{码}-{8位}`（**未**擅自改成另一主格式）；并写明短凭证**不携带**业务指纹 → 默认协助主路径仍 **blocked**（安全默认，非明文放行）。",
    "主分享「复制连接凭证」已改为**完整安全载荷**；UI 仍可显示口述短码（不可单独加密连接）。",
)

p.write_text(t, encoding="utf-8", newline="\n")
print("round-043 updated")

rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
old = "R043 blocked（P0 明文已撤；短凭证无业务 fp，主路径未闭环）"
new = "R043 blocked（完整安全载荷已接线；tls-trust 官方指纹文冲突待架构改；不验收真连）"
if old in rt:
    rp.write_text(rt.replace(old, new, 1), encoding="utf-8", newline="\n")
    print("roadmap OK")
else:
    print("roadmap no hit")
