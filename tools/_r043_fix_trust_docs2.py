# -*- coding: utf-8 -*-
from pathlib import Path
import re

root = Path(r"E:\IdeaProjects\remote-assist")

# ---- PRODUCT.md ----
p = (root / "docs/PRODUCT.md").read_text(encoding="utf-8")

# Fix item 2 visitor assist
p = p.replace(
    "2. **访客协助**：被控端展示 **code + password**，主路径复制为 **`{会话码}-{8位验证码}`**（「复制连接凭证」）；协助端**一个**输入框粘贴完整凭证（兼容旧两行码+密码）或 `remoteassist:pair` 载荷后经中继配对。**Windows 主路径（net48）不得以填局域网 IP/端口/TLS 指纹为主操作。** 规范见 `docs/quality-connection-credential.md`（并入 R043）。",
    "2. **访客协助**：被控端展示会话码与验证码（短形态可展示/口述）；主按钮 **「复制连接凭证」** 复制**完整安全载荷**（既有 `remoteassist:pair`，含 code + password + **Agent 业务证书指纹 `fp=`**）。协助端**一个**输入框粘贴该整段后点「连接」。短码或无指纹旧两行可识别但**必须阻断**并人话请对方重发完整凭证。**Windows 主路径（net48）不得以填局域网 IP/端口/TLS 指纹为主操作。** 规范见 `docs/quality-connection-credential.md`（并入 R043）。",
)

# Fix item 5/6 trust bullets
p = p.replace(
    "5. 中继控制面：**JSONL**（见 `docs/relay-protocol.md`）；配对后透明转发。业务层默认 TLS + 证书指纹钉扎（端到端，中继只转发）。`--insecure` 仅显式调试。\n"
    "6. **短凭证与指纹分工**（见 `docs/tls-trust.md`）：默认公网中继由客户端内置官方指纹钉扎，`{会话码}-{8位验证码}` 只授权会话、不带指纹；自定义中继必须完整安全载荷含指纹（或显式配置）；禁止 TOFU；缺指纹安全阻断，禁止自动降明文/skipTls。",
    "5. 中继控制面：**JSONL**（见 `docs/relay-protocol.md`）；配对后透明转发。业务层默认 **TLS + Agent 临时自签证书指纹钉扎**（端到端，中继只转发、**不终结**业务 TLS；中继 `:17891` 为明文 TCP）。`--insecure` 仅显式调试。\n"
    "6. **连接凭证与指纹**（见 `docs/tls-trust.md`）：「复制连接凭证」= 完整 `PairQrPayload`（含 Agent 业务 `fp=`）；短「会话码-8位」仅展示/口述、不能单独连接；**禁止**内置「官方中继业务指纹」；禁止 TOFU；缺指纹安全阻断；禁止自动降明文/`skipTls`。",
)

# Fix auth visitor bullet
p = p.replace(
    "- **访客**：中继 `guest_host` / `guest_join`（code + 密码；**新生成/刷新验证码固定 8 位数字**，字符串保留前导零；协助端仍兼容未刷新的旧短码，下限仍 ≥4）。人话复制/粘贴主形态为单行连接凭证 `{会话码}-{8位验证码}`（见 `docs/quality-connection-credential.md`）；刷新会话码须同时换密码。",
    "- **访客**：中继 `guest_host` / `guest_join`（code + 密码；**新生成/刷新验证码固定 8 位数字**，字符串保留前导零）。人话主路径复制/粘贴为**完整安全载荷**（含业务指纹）；短码仅展示/口述；无指纹输入须阻断（见 `docs/quality-connection-credential.md`）；刷新会话码须同时换密码。",
)

# Fix assist loop step 2
p = p.replace(
    "2. 远端安装后，把 **连接凭证**（`{会话码}-{8位验证码}`，或账号免密场景下的设备已在线）回传协助方。",
    "2. 远端安装后，把 **完整连接凭证**（「复制连接凭证」得到的整段安全载荷；或账号免密场景下的设备已在线）回传协助方。",
)

# Soft-update R043 blurb if still saying short credential only
p = p.replace(
    "R043 进行中：**Windows net48 主路径对齐产品**（会话码+公网中继 + **连接凭证一次复制/粘贴**；见 `docs/round-043.md`、`docs/quality-connection-credential.md`；本轮不包装）。",
    "R043 进行中：**Windows net48 主路径对齐产品**（会话码+公网中继 + **连接凭证=完整安全载荷一次复制/粘贴**；见 `docs/round-043.md`、`docs/quality-connection-credential.md`；P0 明文已修，分享侧改完整载荷前仍 blocked；本轮不包装）。",
)

(root / "docs/PRODUCT.md").write_text(p, encoding="utf-8", newline="\n")
assert "内置官方" not in p
assert "官方指纹" not in p
print("PRODUCT.md ok")

# ---- architecture.md: fix business face + hard rules + §15 ----
a = (root / "docs/architecture.md").read_text(encoding="utf-8")
a = a.replace(
    "2. **业务面**：管道内业务帧（4 字节 LE 长度 + UTF-8 JSON）+ **TLS（自签 + 证书指纹钉扎）**。`--insecure` 仅显式调试。中继只转发，不终结业务 TLS。短凭证与官方/自定义中继的取指纹规则见 **`tls-trust.md`** 与本文 §15。",
    "2. **业务面**：管道内业务帧（4 字节 LE 长度 + UTF-8 JSON）+ **TLS（Agent 临时自签 + 证书指纹钉扎）**。`--insecure` 仅显式调试。中继只转发，不终结业务 TLS。连接凭证与取指纹规则见 **`tls-trust.md`** 与本文 §15。",
)
a = a.replace(
    "- **短凭证不携带指纹**；默认公网中继靠客户端内置官方指纹钉扎；自定义中继必须完整安全载荷含指纹或显式配置；**禁止 TOFU**；**禁止**因缺指纹自动降明文 / skipTls。细则见 `tls-trust.md` / §15。",
    "- **「复制连接凭证」须含 Agent 业务指纹**（完整 `PairQrPayload`）；短码仅展示；**禁止**内置官方中继业务指纹；**禁止 TOFU**；**禁止**因缺指纹自动降明文 / skipTls。细则见 `tls-trust.md` / §15。",
)

old15 = '''## 15. 短凭证与业务 TLS 指纹（2026-11-06 拍板）

产品纠错 R043 后的信任分工，全文见 **`docs/tls-trust.md`**。摘要：

- **默认公网中继**：客户端内置官方业务 TLS 指纹；`{会话码}-{8位验证码}` 只做会话授权，不带指纹。
- **自定义中继**：必须完整安全载荷含 `fp=`（或显式配置）；不得用官方内置指纹顶替。
- **缺指纹 → 安全阻断**；禁止 TOFU；禁止自动 skipTls / Insecure 降明文。`--insecure` 仅显式调试。
- **与现网**：不改 `relay-deploy` 拓扑与 `guest_*` 字段；指纹在客户端业务 TLS，中继仍 opaque pipe。
- 实现：「鹊桥-客户端」；不改 `relay/` 协议字段。
'''
new15 = '''## 15. 连接凭证与业务 TLS 指纹（2026-11-06 拍板）

产品纠错 R043 后的信任分工，全文见 **`docs/tls-trust.md`**。摘要：

- **指纹对象**：被控 Agent **临时自签业务证书**（非中继证书）；经 `PairQrPayload` 的 `fp=` 分享。
- **「复制连接凭证」**：完整安全载荷（code + password + Agent `fp=`）；短「会话码-8位」仅展示/口述，不能单独连接。
- **禁止**客户端内置「官方中继 / 官方业务指纹」；中继 `:17891` 明文 TCP，不终结业务 TLS。
- **缺指纹 → 安全阻断**；禁止 TOFU；禁止自动 skipTls / Insecure 降明文。`--insecure` 仅显式调试。
- **与现网**：不改 `relay-deploy` 拓扑与 `guest_*` 字段；中继仍 opaque pipe。
- 实现：「鹊桥-客户端」；不改 `relay/` 协议字段。
'''
if old15 not in a:
    raise SystemExit('architecture §15 block not found exactly')
a = a.replace(old15, new15)
assert "内置官方" not in a
assert "官方业务 TLS" not in a
(root / "docs/architecture.md").write_text(a, encoding="utf-8", newline="\n")
print("architecture.md ok")

# ---- relay-protocol.md ----
r = (root / "docs/relay-protocol.md").read_text(encoding="utf-8")
r = r.replace(
    "2. 访客 `code`+`password` 等同会话密钥，展示给用户时注意防肩窥；过期约 2 小时。人话短凭证 `{会话码}-{8位验证码}` 只映射到这两项，**不携带业务 TLS 指纹**（见 `tls-trust.md`）。\n"
    "3. 账号 `token` 应安全存储（DPAPI/凭据保管库）；泄漏可免密连本账号在线设备。\n"
    "4. 进入 pipe 后，业务层默认 **TLS + 证书指纹钉扎**：默认公网中继用客户端内置官方指纹；自定义中继必须载荷/`fp` 显式配置。**禁止 TOFU**；**禁止**缺指纹时自动降明文或 `skipTls`。`--insecure` 仅显式调试。\n"
    "5. 进入 pipe 后，建议业务层仍做会话内鉴权（现有短时令牌/配对确认可保留）。",
    "2. 访客 `code`+`password` 等同会话密钥，展示给用户时注意防肩窥；过期约 2 小时。人话主路径「复制连接凭证」为完整 `remoteassist:pair`（含业务 `fp=`）；短 `{会话码}-{8位验证码}` 仅展示/口述，**不能单独安全连接**（见 `tls-trust.md`、`quality-connection-credential.md`）。\n"
    "3. 账号 `token` 应安全存储（DPAPI/凭据保管库）；泄漏可免密连本账号在线设备。\n"
    "4. 进入 pipe 后，业务层默认 **TLS + Agent 临时自签证书指纹钉扎**（载荷 `fp=`）。**禁止**内置官方中继业务指纹；**禁止 TOFU**；**禁止**缺指纹时自动降明文或 `skipTls`。`--insecure` 仅显式调试。\n"
    "5. 进入 pipe 后，建议业务层仍做会话内鉴权（现有短时令牌/配对确认可保留）。",
)
assert "内置官方" not in r
(root / "docs/relay-protocol.md").write_text(r, encoding="utf-8", newline="\n")
print("relay-protocol.md ok")
print("phase2 ok")
