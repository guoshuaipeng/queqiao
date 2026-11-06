# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(r"E:\IdeaProjects\remote-assist\docs")

# ---------- quality-connection-credential.md ----------
cred = ROOT / "quality-connection-credential.md"
cred_text = cred.read_text(encoding="utf-8")

ux_block = """# 连接凭证（完整安全载荷）· 产品规范

> 产品拍板：2026-11-06 Asia/Shanghai（鹊桥-体验提出；产品收口并入 R043；R044 逐字文案扫尾）  
> 适用范围：Windows **src-net48**（主路径）、net8 WinForms / Avalonia、CLI 复制与解析。  
> **不要只改显示**：生成、校验、复制格式、粘贴解析、缺安全验证信息阻断必须一致。  
> 信任模型：`docs/tls-trust.md`（业务 TLS 钉扎 **Agent 临时自签证书指纹**，禁止「中继官方指纹」内置钉扎）。  
> **状态**：完整连接凭证载荷已接线；实际连接、安全校验与界面运行验收另计。

---

## 体验目标（用户重申 · 2026-11-06）

> **产品意图**，与下方「当前安全主路径」分开。短码本身**不等于**已可安全连接。

1. **复制内容目标**：主路径只要 **`{会话码}-{8位验证码}`**（会话码 + 密码；验证码仍 8 位数字、保留前导零）。中继地址默认内置，仅在**高级设置**修改；亲友**不**复制服务器地址或长串载荷作为主路径。协助端一次粘贴短码即可连接——**仅当**「安全实现前置条件」满足之后。
2. **口述/展示**：短码仍可展示与口述；目标是复制与粘贴也走同一短形态，不再逼亲友转发完整 `remoteassist:pair` 长串。

## 安全实现前置条件（短凭证 · blocker）

- **当前**安全连接仍要求完整 `remoteassist:pair` 载荷（含 **Agent 业务 TLS 指纹 `fp=`** / `PairQrPayload`）。见下文「当前安全主路径」与 `docs/tls-trust.md`（R043 信任模型**不变**）。
- `{会话码}-{8位验证码}` 今日只授权中继会话，**不能单独**建立安全业务通道。
- **R050**（短凭证可连）**blocked**：待架构给出保留端到端身份验证的短凭证方案与迁移/兼容边界；未经验证**不得**宣称短码可安全连接。
- **禁止**：TOFU；跳过 / 降级校验；把中继回传、未经可信绑定的指纹当独立认证；为体验删掉地址或指纹后仍称短码可连。
- 体验目标与安全前置条件**分节落档**；实现轮不得把体验文案先行改成「只复制短码」却仍依赖完整载荷暗连。

## 当前安全主路径（R043 · 在前置条件满足前仍有效）

亲友/客户仍然 **一次复制、一次粘贴** 即可连接。剪贴板里必须是**可安全连接的完整字符串**（既有 `remoteassist:pair` 安全载荷形态，含会话码 + 密码 + **Agent 业务证书指纹**），不得发明新编码，也不得跳过指纹校验。

"""

# Replace from title through "## 目标" section with new structure
import re
# Find start through end of "## 目标" paragraph (before "## 规则")
m = re.search(r"^## 规则（已拍板", cred_text, re.M)
if not m:
    raise SystemExit("quality-connection-credential: ## 规则 not found")
rest = cred_text[m.start():]
# Update 排期 section in rest
rest = rest.replace(
    "## 排期\n\n- **R043**：",
    "## 排期\n\n- **体验目标 / R049–R050**：短码复制与单主窗口为产品意图（见上节）；**R049** 可做单主窗口 + 高级中继 UI（**不改**短凭证安全）；**R050** 短凭证可连 **blocked** 等架构。未满足前置条件前，主按钮与剪贴板仍按本规范「当前安全主路径」。\n- **R043**：",
)
cred.write_text(ux_block + rest, encoding="utf-8", newline="\n")
print("updated quality-connection-credential.md")

# ---------- PRODUCT.md ----------
prod = ROOT / "PRODUCT.md"
pt = prod.read_text(encoding="utf-8")

# Insert UX goals + security precondition after "## 连接（2026-11-05 起）" block's item 6,
# before "## 鉴权与安全底线". Also lightly annotate connection item 2.

marker = "## 鉴权与安全底线"
if marker not in pt:
    raise SystemExit("PRODUCT: 鉴权 marker missing")

insert = """## 体验目标（用户重申 · 2026-11-06）

> **产品意图**，与「安全实现前置条件」分节。短码本身**不等于**已可安全连接；在架构短凭证方案落地前，连接仍走完整安全载荷（见上节与 `quality-connection-credential.md`）。

1. **复制内容 = 会话码 + 密码（短码）**：主路径目标为 `{会话码}-{8位验证码}`（8 位验证码要求仍有效）。中继地址默认内置，只在**高级设置**修改；亲友**不**复制地址或长串载荷。协助端一次粘贴短码连接——**前提**见下节。
2. **单主窗口**：首页与点击「去协助 / 接受协助」后共用**一个**主窗口，页内切换，不多开主窗口；返回首页不叠窗，保持位置/大小。有活动会话时返回首页或切换角色**不得**暗中断开、留下隐藏会话或自动扩大权限；确需结束须本机确认。**安全确认对话框不在单窗口合并范围**——不得为单窗口取消写操作本机确认。

排期：**R049**（单主窗口 + 高级中继 UI，UX，不改短凭证安全）；**R050**（短凭证可连）**blocked** 等架构。见 `docs/roadmap.md`、`docs/round-049.md`。

## 安全实现前置条件（短凭证）

- **当前**安全连接仍要求完整 `remoteassist:pair`（含 Agent 业务 TLS 指纹 `fp=` / `PairQrPayload`）。R043 信任模型见 `docs/tls-trust.md`，**不得削弱**。
- 短码今日只授权中继会话，**不能单独**建安全业务通道。
- **禁止**：TOFU；跳过校验；把中继回传、未经可信绑定的指纹当独立认证；未验证前把主路径改成「只复制短码却暗连」。
- 短凭证 E2E 方案与迁移/兼容边界由架构另出；未落地前 **R050** 不得开实现。

"""

# Soften connection bullet 2 to point at split without claiming short code is live-secure
old_conn2 = """2. **访客协助**：被控端展示会话码与验证码（短形态可展示/口述）；主按钮 **「复制连接凭证」** 复制**完整安全载荷**（既有 `remoteassist:pair`，含 code + password + **Agent 业务证书指纹 `fp=`**）。协助端**一个**输入框粘贴该整段后点「连接」。短码或无指纹旧两行可识别但**必须阻断**并人话请对方重发完整凭证。**Windows 主路径（net48）不得以填局域网 IP/端口/TLS 指纹为主操作。** 规范见 `docs/quality-connection-credential.md`（并入 R043）。"""

new_conn2 = """2. **访客协助（当前安全主路径）**：被控端展示会话码与验证码（短形态可展示/口述）；主按钮 **「复制连接凭证」** 复制**完整安全载荷**（既有 `remoteassist:pair`，含 code + password + **Agent 业务证书指纹 `fp=`**）。协助端**一个**输入框粘贴该整段后点「连接」。短码或无指纹旧两行可识别但**必须阻断**并人话请对方重发完整凭证。**Windows 主路径（net48）不得以填局域网 IP/端口/TLS 指纹为主操作。** 规范见 `docs/quality-connection-credential.md`（并入 R043）。**体验目标**是主路径只复制短码、地址进高级设置——见下节「体验目标」；在「安全实现前置条件」满足前**不得**把短码当已可安全连接。"""

if old_conn2 not in pt:
    raise SystemExit("PRODUCT: connection bullet 2 not found exact")
pt = pt.replace(old_conn2, new_conn2)
pt = pt.replace(marker, insert + marker)

# Update parallel rounds mention near end of platform paragraph - append R049/R050 note after R048 mention
old_parallel = "当前并行打开：**R046**"
if old_parallel not in pt:
    raise SystemExit("PRODUCT: parallel R046 mention missing")
# Find the sentence and extend after R048 paren
pt = pt.replace(
    "后续迭代优先稳定性和用户体验；本机验收勿打断用户工作，优先本机外路径。",
    "另已锁定体验目标并开规格桩：**R049**（单主窗口 + 高级中继 UI，draft）；**R050**（短凭证可连，**blocked** 等架构）。后续迭代优先稳定性和用户体验；本机验收勿打断用户工作，优先本机外路径。",
)

# Add brief after net48 MCP R048 paragraph
old_r048_prod = "**net48 协助端 MCP（R048）**：去协助界面须有「复制安装说明」与「复制 MCP 配置」（可贴进 Cursor 等的 JSON，载荷为完整连接凭证）；对齐 net8 Helper / Avalonia；缺完整凭证阻断，禁止明文降级。见 docs/round-048.md。"
new_r048_prod = old_r048_prod + "\n\n**短码复制与单主窗口（体验目标）**：复制内容目标为会话码+8位密码；单主窗口页内切换。安全短凭证未落地前主路径仍复制完整连接凭证。见「体验目标」「安全实现前置条件」；`docs/round-049.md`（R049 draft）；R050 blocked。"
if old_r048_prod not in pt:
    raise SystemExit("PRODUCT: R048 para missing")
pt = pt.replace(old_r048_prod, new_r048_prod)

prod.write_text(pt, encoding="utf-8", newline="\n")
print("updated PRODUCT.md")

# ---------- roadmap.md ----------
rm = ROOT / "roadmap.md"
rt = rm.read_text(encoding="utf-8")

# Add R049 R050 rows before the "> **当前**：并行三轮" note
r048_row = "| R048 | 高频：net48 **去协助 / Helper** 补「复制安装说明」+ MCP「复制 MCP 配置」JSON（对齐 net8/Avalonia）；打磨：缺完整凭证阻断、禁明文降级 | 鹊桥-客户端（主） | **compiled**（运行未验收） | `docs/round-048.md` |"
if r048_row not in rt:
    raise SystemExit("roadmap: R048 row missing")

r049_050 = """| R048 | 高频：net48 **去协助 / Helper** 补「复制安装说明」+ MCP「复制 MCP 配置」JSON（对齐 net8/Avalonia）；打磨：缺完整凭证阻断、禁明文降级 | 鹊桥-客户端（主） | **compiled**（运行未验收） | `docs/round-048.md` |
| R049 | 高频：首页与「去协助/接受协助」**单主窗口**页内切换（不叠窗、保位置/大小；有会话返回/切角色须本机确认结束；安全确认窗不在合并范围）；打磨：中继地址仅高级设置可改、默认内置 | 鹊桥-客户端（主） | **draft / 规格**（未授权实现） | `docs/round-049.md` |
| R050 | 高频：亲友主路径**短码**（会话码+8位密码）一次复制/粘贴即可**安全**连接 | 架构方案 → 客户端/中继按方案 | **blocked**（等架构短凭证 E2E；禁止 TOFU/跳校验/未绑定指纹当认证） | `docs/quality-connection-credential.md`、`docs/tls-trust.md` |"""

rt = rt.replace(r048_row, r049_050)

old_cur = "> **当前**：并行三轮——**R046** **open（规格已拍板）**（服务端：会话码审计 + 管理台只读；见 `docs/round-046.md`、`docs/relay-audit.md`）；**R047** **open（规格已开）**（客户端：net48 单文件安装包；见 `docs/round-047.md`）；**R048** **compiled**（net48 Helper MCP/安装说明；运行未验收；见 `docs/round-048.md`）。R045 编译/文档 **PASS_WITH_NOTES**（`39d6dc1`），运行仍 blocked。R044 编译/文档 **PASS_WITH_NOTES**（`7cad0ab`），运行仍 blocked；Avalonia/CLI 完整载荷另轮。R042 已产品验收（出包 `8d61398`，上架 `f90bd8f`，2026-11-06）。"

new_cur = "> **当前**：并行——**R046** open（规格已拍板）；**R047** open（规格已开）；**R048** compiled（运行未验收）。另锁定体验规格：**R049** draft（单主窗口 + 高级中继 UI，未授权实现）；**R050** **blocked**（短凭证安全可连，等架构；见 `quality-connection-credential.md`）。用户重申：复制目标=短码；单主窗口；**不得**未验证称短码可安全连接。R045/R044 运行仍 blocked；R042 已产品验收。"

if old_cur not in rt:
    raise SystemExit("roadmap: current note missing")
rt = rt.replace(old_cur, new_cur)

# Extend platform paragraph ending that lists R048
rt = rt.replace(
    "R048 打开（net48 协助端 MCP 复制安装说明/JSON；见 `docs/round-048.md`）。",
    "R048 打开（net48 协助端 MCP 复制安装说明/JSON；见 `docs/round-048.md`）；R049 draft（单主窗口；见 `docs/round-049.md`）；R050 blocked（短凭证等架构）。",
)

rm.write_text(rt, encoding="utf-8", newline="\n")
print("updated roadmap.md")

# ---------- round-049.md ----------
r049 = """# 第 49 轮 — 单主窗口 + 高级中继设置（体验）

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 依据：用户重申体验目标（经鹊桥-体验）；与短凭证安全**分节**  
> 实现：鹊桥-客户端（主；Windows net48 优先，Avalonia 对齐另述）  
> 状态：**draft / 规格** — **尚未授权实现**  
> **并行**：R046 / R047 / R048 仍各自推进；本轮**不改**连接安全载荷与信任模型。

## 产品目标

首页与「去协助 / 接受协助」共用**一个**主窗口（页内切换）；中继地址默认内置，仅高级设置可改。本轮是 **UX / 窗口与设置**，**不是**短码可安全连接（那是 **R050**，blocked）。

## 体验目标（锁死）

1. **单主窗口**：点击「去协助」或「接受协助」后**不新开**第二个主窗口；同窗内切换模式；返回首页不叠窗；保持窗口位置与大小。
2. **活动会话**：返回首页或切换角色时，**不得**暗中断开、留下隐藏会话、或自动扩大权限；若必须结束会话，先本机确认。
3. **安全确认对话框 out of scope**：写操作 / 配对等本机确认窗**不在**「多个主窗口」合并范围内；不得为单窗口取消本机确认。
4. **中继地址**：默认使用内置中继；地址只在**高级设置**修改并保存；亲友主路径不展示、不复制服务器地址。

## 与安全的边界（本轮明确不做）

- **不**把「复制连接凭证」改成只复制短码。
- **不**让短码单独建业务 TLS；不 TOFU、不跳校验、不把中继回传未绑定指纹当认证。
- 当前安全主路径仍见 `docs/quality-connection-credential.md`「当前安全主路径」与 `docs/tls-trust.md`。
- 短凭证可连见 **R050**（blocked 等架构方案）。

## 验收标准（授权实现后）

1. 从首页进两种模式再回首页：始终一个主窗口实例；位置/大小保持合理。
2. 有活动会话时返回/切角色：有本机确认或明确保持会话策略（文档写清）；无静默断连、无隐藏会话、无权限扩大。
3. 写操作本机确认仍在。
4. 默认路径不要求用户复制/填写中继地址；高级设置可改并持久化。
5. 文档与 roadmap 状态从 draft 改为实现中/验收时再更新；未授权前不得标 done。

## 本轮不做

- 短凭证协议 / 指纹绑定方案（**R050** / 架构）
- 削弱 R043 完整载荷或本机写确认
- 改 guest_* 中继协议字段（除非架构方案明确要求且另开轮）
- 未授权即开客户端实现

## 参考

- `docs/PRODUCT.md`（体验目标 / 安全实现前置条件）
- `docs/quality-connection-credential.md`
- `docs/tls-trust.md`
- `docs/roadmap.md`（R049 / R050）
"""
(ROOT / "round-049.md").write_text(r049, encoding="utf-8", newline="\n")
print("wrote round-049.md")

# ---------- round-050.md (blocked stub) ----------
r050 = """# 第 50 轮 — 短凭证安全连接（blocked）

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 状态：**blocked** — 等架构给出保留端到端身份验证的短凭证方案与迁移/兼容边界  
> 依据：用户重申复制内容只要「会话码-密码」；现有长串含业务指纹，不能只删地址/指纹就称短码可连。

## 体验目标（意图）

亲友主路径：复制 / 粘贴 `{会话码}-{8位验证码}` 一次即可连接；不转发完整 `remoteassist:pair` 长串；中继地址默认内置。

## 安全实现前置条件（未满足则不得实现「短码可连」）

- 当前安全路径仍依赖完整载荷内 **Agent 业务 TLS 指纹**（`PairQrPayload` / `fp=`）。见 `docs/tls-trust.md`、`docs/quality-connection-credential.md`。
- 短码今日只授权中继会话，**不能单独**建安全业务通道。
- 架构须给出：如何在短凭证流程中保留 E2E 身份验证；迁移与兼容边界（旧完整载荷如何过渡）。
- **禁止**：TOFU；跳过校验；把中继回传、未经可信绑定的指纹当独立认证；未验证宣称短码已可安全连接。

## 本轮不做（直至 blocker 解除）

- 客户端把主复制改成只复制短码却仍「能连」
- 任何削弱 R043 信任模型的实现
- 与 **R049**（单主窗口）混为一谈——R049 可先做 UX，R050 等架构

## 解除 blocked 后

1. 架构方案入库（architecture / tls-trust / credential 规范更新）。
2. 产品对照方案改本 round 验收标准并授权实现。
3. 客户端/中继按方案分轮落地；保留完整载荷迁移路径直至兼容窗口结束。

## 参考

- `docs/PRODUCT.md`（体验目标 / 安全实现前置条件）
- `docs/quality-connection-credential.md`
- `docs/tls-trust.md`
- `docs/round-049.md`（可并行的 UX，不含短凭证安全变更）
"""
(ROOT / "round-050.md").write_text(r050, encoding="utf-8", newline="\n")
print("wrote round-050.md")

# ---------- retro.md ----------
retro = ROOT / "retro.md"
rtro = retro.read_text(encoding="utf-8")
if "用户重申短码复制与单主窗口" not in rtro:
    rtro = rtro.rstrip() + "\n\n\n## 体验目标重申（2026-11-06 Asia/Shanghai）\n\n- **触发**：用户明确重申两项体验目标（经鹊桥-体验同步）：① 复制内容只要会话码+密码（8 位验证码仍有效），中继默认、地址仅高级设置；② 首页与去协助/接受协助共用一个主窗口，页内切换、不叠窗。\n- **收口**：体验目标与安全前置条件**分节**写入 PRODUCT / quality-connection-credential / roadmap；开 **R049** draft（单主窗口 UX）、**R050** blocked（短凭证等架构）。**不得**未验证称短码可安全连接；禁止 TOFU / 跳校验 / 未绑定指纹当认证。\n- **边界**：R049 不改短凭证安全；R043 完整载荷主路径在 R050 解除前仍有效。\n"
    retro.write_text(rtro, encoding="utf-8", newline="\n")
    print("updated retro.md")
else:
    print("retro already has note")

print("DONE")
