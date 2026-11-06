# -*- coding: utf-8 -*-
"""Lock strict sole credential decision into product docs (2026-11-06 10:35 Asia/Shanghai)."""
from pathlib import Path

ROOT = Path(r"E:\IdeaProjects\remote-assist\docs")

# ---------- PRODUCT.md ----------
prod = ROOT / "PRODUCT.md"
pt = prod.read_text(encoding="utf-8")

old_acct = (
    "3. **账号免密（现网能力；与「唯一凭证」评估冲突，去留未拍板）**：简单注册/登录 → 电脑加入账号（设备上线）→ "
    "同账号机器列表 → 连接在线设备（无需再输会话密码）。若用户确认 code+password 为唯一控制凭证，本路径与之冲突——"
    "**等体验 A/B，禁止自行拍板兼容或废止**；见「用户目标（开放评估）」。"
)
new_acct = (
    "3. **账号免密 / 设备密钥快捷连（产品意图已废止 · 2026-11-06 10:35 Asia/Shanghai）**："
    "曾支持简单注册/登录 → 设备上线 → 同账号机器列表 → `connect_device` / `ownerLink` 免输会话密码。"
    "用户已拍板 **严格唯一凭证**，本旁路**取消**（自有设备也须输入 code+密码）；"
    "现网代码若仍存在，属待收口遗留，**不得**继续宣传为产品能力。"
    "见「产品拍板：严格唯一凭证」。"
)
if old_acct not in pt:
    raise SystemExit("PRODUCT: 账号免密 bullet missing/changed")
pt = pt.replace(old_acct, new_acct, 1)

OLD_EVAL = """## 用户目标（开放评估 · 非出货拍板 · 2026-11-06）

> **USER GOAL + OPEN EVALUATION** — 用户把标准抬高为评估项；**不是**已定出货路径。未完成评估与用户 A/B 选择前，**不得**按此改主路径实现或宣称已交付。

### 目标陈述（评估中）

- **控制面凭证目标**：会话 **code + password** 作为**唯一**控制凭证（sole control credential）；亲友主路径只靠短码完成授权与连接意图。
- **服务端不知明文密码**：中继 / 服务端**不得**持有可用明文 password（评估方向；与今日中继存 password_hash 做 guest_join 校验的现状可能冲突——属评估范围，**未拍板**怎么改）。
- **写确认档位正交**：Guest / Assist / Admin 与信任等级（每次询问 / 宽松 / 完全信任）的**本机写确认**规则保持正交，不因短凭证或「唯一凭证」评估而削弱或取消。

### 已知冲突（未拍板 · 等用户经体验选 A/B）

- **冲突**：若 code+password 为**唯一**控制凭证，则与现网 **账号免密**（login → token → `connect_device` / `ownerLink` 跳过配对确认）直接冲突——免密路径**不**靠访客 password。
- **产品尚未决定**是否保留旧免密 / owner-token 旁路。
- **禁止本轮拍板兼容**：不得自行写成「免密与唯一凭证并存兼容」或「废止免密」；等用户经 **鹊桥-体验** 给出 A/B 选择后再落文档与排期。
- 评估记录可并列选项，但**不选默认**。

### 架构侧（指针）

- 架构正在**单独评估 PAKE**（及短凭证 E2E）；方案入库前本文仅作指针，**不**把 PAKE 写成已选方案或出货依赖。
- 短凭证可连仍见 **R050** **blocked**；本评估抬高目标，**不**解除 R043 完整载荷主路径，也**不**授权实现。

"""

NEW_LOCKED = """## 产品拍板：严格唯一凭证（2026-11-06 10:35 Asia/Shanghai）

> **LOCKED PRODUCT DECISION** — 用户经鹊桥-体验明确选择：**严格只认 code+密码，自己的设备也输入，不另设设备密钥快捷连接。**  
> 此前「开放评估 / 等 A/B」**已关闭**。本拍板是产品意图与排期依据；**不是**现网已实现、也**不是**安全 PASS。在架构正式方案落地并通过恶意中继等运行验证前，**不得**宣称已安全满足。

### 拍板内容

- **唯一连接凭证** = 会话 **code + 8 位密码**（sole connection credential）。
- **取消旁路**：账号 / 同 owner 服务器声明免密；设备密钥快捷连接；凡仅凭服务器 `token` / owner 声明即可授权控制的路径（含 `connect_device` / `ownerLink` 免密产品意图）。
- **自有设备也输入** code+密码；不另设设备密钥快捷连。
- **写确认档位正交不变**：Guest / Assist / Admin 与信任等级（每次询问 / 宽松 / 完全信任）的本机写确认规则保持正交；不得把「服务器不可冒充」落实成强制每次写弹窗，也不重置既定三档。
- **服务端不知明文密码**（目标）：中继不得持有可用明文 password；实现路径由架构评估成熟 **PAKE**（禁止自造密码学），见 `docs/pake-unique-credential-eval.md`。
- **安装 ≠ 可被随时控制**：安装/启动/上线本身不授予远程控制；须持有 code+密码并经端点验证 + 本机授权。

### 与现网 / 排期

- 今日安全主路径仍为完整 `remoteassist:pair`（含 `fp=`）；**R043** 信任模型在运行验证完成前**不得削弱**。
- **R049**（单主窗口 + 高级中继地址）：UX，与 PAKE **独立**，规格 draft，**未授权实现**（可另授权）。
- **R050**（严格唯一凭证 / 短码可连）：决策已锁定；状态仍 **blocked** — 等架构正式评估收口 → 产品对照验收标准 → **再授权实现**。未授权前不得改主路径实现。
- 账号注册/登录若保留，仅可用于发现、计费等**非授权**用途；**不得**再作为控制授权旁路。

"""

if OLD_EVAL not in pt:
    raise SystemExit("PRODUCT: open eval section missing/changed")
pt = pt.replace(OLD_EVAL, NEW_LOCKED, 1)

# Soften remaining "账号免密" product capability phrasing in 鉴权 / 形态 / 闭环
replacements = [
    (
        "- **账号免密连自己的机器**：可跳过「配对确认窗」（`ownerLink` 自动批准 Assist 会话）；**写操作本机确认不可绕过**（除非对方主动授予 Admin）。中继配对成功 ≠ 授权写操作。",
        "- **账号免密连自己的机器（产品意图已废止）**：不得再作为出货能力宣传或新增；遗留 `ownerLink` / `connect_device` 免密须在 **R050** 实现收口时移除或隔离。"
        "**写操作本机确认不可绕过**（除非对方主动授予 Admin）。中继配对成功 ≠ 授权写操作。",
    ),
    (
        "3. **Admin**：可跳过部分确认（仍审计）；仅对方主动授予；账号免密默认 Assist",
        "3. **Admin**：可跳过部分确认（仍审计）；仅对方主动授予；会话默认 Assist（不再有账号免密默认路径）",
    ),
    (
        "- 控制：code+密码或载荷经中继连接；设置里填中继地址；可注册/登录、刷设备列表、免密连接。",
        "- 控制：code+密码或完整连接凭证经中继连接；设置里填中继地址（目标：默认内置、仅高级可改）；"
        "可注册/登录、刷设备列表作发现用途——**连接控制须 code+密码**，不再免密连。",
    ),
    (
        "2. 远端安装后，把 **完整连接凭证**（「复制连接凭证」得到的整段安全载荷；或账号免密场景下的设备已在线）回传协助方。",
        "2. 远端安装后，把 **完整连接凭证**（当前：「复制连接凭证」得到的整段安全载荷；目标主路径：会话码+8位密码）回传协助方。"
        "**不再**以账号免密「设备已在线」替代凭证。",
    ),
    (
        "配对与 Assist 写操作默认本机确认窗（账号自有设备可跳过配对确认，见上）；串行排队；审计写本地日志。写操作必须在被控本机确认，中继配对成功 ≠ 授权写操作。亲友可见界面不出现 Assist、MCP、ownerLink、TLS。",
        "配对与 Assist 写操作默认本机确认窗（**严格唯一凭证**下不得再以账号自有设备跳过身份/配对）；串行排队；审计写本地日志。"
        "写操作必须在被控本机确认，中继配对成功 ≠ 授权写操作。亲友可见界面不出现 Assist、MCP、ownerLink、TLS。",
    ),
]
for a, b in replacements:
    if a not in pt:
        raise SystemExit(f"PRODUCT soft replace miss: {a[:60]}...")
    pt = pt.replace(a, b, 1)

# Update R050 status mention in long platform paragraph if present
pt = pt.replace(
    "**R050**（短凭证可连，**blocked** 等架构）",
    "**R050**（严格唯一凭证 / 短码可连，决策已锁定；**blocked** 等架构 PAKE 评估与授权实现）",
)

prod.write_text(pt, encoding="utf-8", newline="\n")
print("PRODUCT.md OK")

# ---------- quality-connection-credential.md ----------
cred = ROOT / "quality-connection-credential.md"
ct = cred.read_text(encoding="utf-8")

OLD_CRED_EVAL = """## 用户目标（开放评估 · 非出货拍板 · 2026-11-06）

> **USER GOAL + OPEN EVALUATION**，详见 `docs/PRODUCT.md` 同名节。短码 / 唯一凭证**不是**已定出货路径。

- **评估目标**：code + password 为**唯一**控制凭证；服务端**不知**明文密码。
- **冲突**：与现网账号免密 / owner-token（`connect_device` / `ownerLink`）冲突；产品**未**决定保留旧免密；**禁止拍板兼容**；等用户经体验选 A/B。
- **正交**：写操作本机确认档位（权限三级 + 信任等级）不因本评估改动。
- **架构**：另评 PAKE（指针）；**R050** 仍 blocked。当前安全主路径（完整载荷 + `fp=`）不变。

"""

NEW_CRED_LOCKED = """## 产品拍板：严格唯一凭证（2026-11-06 10:35 Asia/Shanghai）

> **LOCKED** — 详见 `docs/PRODUCT.md`「产品拍板：严格唯一凭证」。用户选择严格只认 code+8位密码；取消免密与设备密钥旁路；自有设备也输入。

- **唯一连接凭证**：会话 code + 8 位密码。
- **废止产品意图**：`connect_device` / `ownerLink` 账号免密、设备密钥快捷连；遗留实现待 **R050** 收口，不得继续宣传。
- **正交**：写操作本机确认三档不变（不因本拍板强制每次弹窗）。
- **架构**：继续正式评估成熟 PAKE（`docs/pake-unique-credential-eval.md`）；**R050** 决策锁定但仍 **blocked**（未授权实现）。当前安全主路径（完整载荷 + `fp=`）在运行验证前不变。
- **不得**把现网实现宣称已安全满足本拍板；须恶意中继等运行测试后方可安全 PASS。

"""

if OLD_CRED_EVAL not in ct:
    raise SystemExit("credential: open eval missing/changed")
ct = ct.replace(OLD_CRED_EVAL, NEW_CRED_LOCKED, 1)

old_sched = (
    "- **用户目标开放评估**：code+password 唯一控制凭证 + 服务端不知明文密码——评估中，非出货；与免密冲突等 A/B（见 PRODUCT）。\n"
    "- **体验目标 / R049–R050**：短码复制与单主窗口为产品意图（见上节）；**R049** 可做单主窗口 + 高级中继 UI（**不改**短凭证安全）；"
    "**R050** 短凭证可连 **blocked** 等架构。未满足前置条件前，主按钮与剪贴板仍按本规范「当前安全主路径」。"
)
new_sched = (
    "- **严格唯一凭证（已拍板）**：code+password 唯一连接凭证；取消免密/设备密钥旁路；服务端不知明文密码为目标（见 PRODUCT）。\n"
    "- **体验目标 / R049–R050**：**R049** draft — 单主窗口 + 高级中继 UI（与 PAKE **独立**，未授权实现）；"
    "**R050** — 决策锁定、仍 **blocked**（等架构 PAKE 正式评估收口后再授权实现）。"
    "未满足前置条件前，主按钮与剪贴板仍按本规范「当前安全主路径」。"
)
if old_sched not in ct:
    raise SystemExit("credential: schedule bullets missing")
ct = ct.replace(old_sched, new_sched, 1)

# Touch target path note about sole credential
if "S1 **不是**「服务器不知道密码」" in ct and "严格唯一凭证已拍板" not in ct:
    ct = ct.replace(
        "S1 **不是**「服务器不知道密码」。身份认证与本机三档写确认正交。",
        "S1 **不是**「服务器不知道密码」，也**不是**最终严格唯一凭证目标（最终见 PAKE 评估）。"
        "身份认证与本机三档写确认正交。严格唯一凭证产品意图已拍板，现网未宣称已满足。",
    )

cred.write_text(ct, encoding="utf-8", newline="\n")
print("quality-connection-credential.md OK")

# ---------- roadmap.md ----------
rm = ROOT / "roadmap.md"
rt = rm.read_text(encoding="utf-8")

old_r049 = (
    "| R049 | 高频：首页与「去协助/接受协助」**单主窗口**页内切换（不叠窗、保位置/大小；有会话返回/切角色须本机确认结束；安全确认窗不在合并范围）；"
    "打磨：中继地址仅高级设置可改、默认内置 | 鹊桥-客户端（主） | **draft / 规格**（未授权实现） | `docs/round-049.md` |"
)
new_r049 = (
    "| R049 | 高频：首页与「去协助/接受协助」**单主窗口**页内切换（不叠窗、保位置/大小；有会话返回/切角色须本机确认结束；安全确认窗不在合并范围）；"
    "打磨：中继地址仅高级设置可改、默认内置（与 PAKE/**R050** **独立**，可另授权） | 鹊桥-客户端（主） | "
    "**draft / 规格**（未授权实现；可授权后开干，不阻塞于 PAKE） | `docs/round-049.md` |"
)
if old_r049 not in rt:
    raise SystemExit("roadmap: R049 row missing")
rt = rt.replace(old_r049, new_r049, 1)

old_r050 = (
    "| R050 | 高频：亲友主路径**短码**安全可连；另：**开放评估** code+password **唯一**控制凭证 + 服务端不知明文密码"
    "（非出货；与账号免密冲突等体验 A/B，禁止拍板兼容） | 架构（PAKE/短凭证评估）→ 产品收口 → 实现 | "
    "**blocked** + **open eval**（非出货拍板） | `docs/round-050.md`、`docs/PRODUCT.md`、`docs/quality-connection-credential.md` |"
)
new_r050 = (
    "| R050 | 高频：亲友主路径**短码**安全可连 + **严格唯一凭证**（code+8位密码；取消账号免密/`connect_device`/`ownerLink` 与设备密钥快捷连；自有设备也输入；"
    "服务端不知明文密码；成熟 PAKE） | 架构（PAKE 正式评估）→ 产品对照验收 → **授权后**实现 | "
    "**blocked**（决策已锁定；未授权实现；勿宣称现网已安全满足） | "
    "`docs/round-050.md`、`docs/PRODUCT.md`、`docs/pake-unique-credential-eval.md` |"
)
if old_r050 not in rt:
    raise SystemExit("roadmap: R050 row missing")
rt = rt.replace(old_r050, new_r050, 1)

old_cur = (
    "> **当前**：并行——**R046** open（规格已拍板）；**R047** open（规格已开）；**R048** compiled（运行未验收）。"
    "另锁定体验规格：**R049** draft（单主窗口 + 高级中继 UI，未授权实现）；**R050** **blocked** + **open eval**"
    "（短凭证等架构；唯一凭证/服务端不知密码为评估非出货；与免密冲突等体验 A/B，禁止拍板兼容；见 `round-050.md`）。"
    "用户重申短码+单主窗口；另抬高评估标准；**不得**未验证称短码可安全连接。R045/R044 运行仍 blocked；R042 已产品验收。"
)
new_cur = (
    "> **当前**：并行——**R046** open（规格已拍板）；**R047** open（规格已开）；**R048** compiled（运行未验收）。"
    "**R049** draft / 规格（单主窗口 + 高级中继 UI；与 PAKE 独立；**未授权实现**）。"
    "**R050** **blocked**（**严格唯一凭证已拍板** 2026-11-06 10:35；取消免密/设备密钥旁路；仍等架构 PAKE 正式评估收口后授权实现；"
    "**不得**宣称现网已安全满足；见 `round-050.md`）。"
    "R045/R044 运行仍 blocked；R042 已产品验收。"
)
if old_cur not in rt:
    raise SystemExit("roadmap: current blurb missing")
rt = rt.replace(old_cur, new_cur, 1)

rm.write_text(rt, encoding="utf-8", newline="\n")
print("roadmap.md OK")

# ---------- round-050.md ----------
r50 = ROOT / "round-050.md"
t50 = r50.read_text(encoding="utf-8")

NEW_R50 = """# 第 50 轮 — 严格唯一凭证 / 短码安全连接（blocked · 决策已锁定）

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 状态：**blocked** — **产品决策已锁定**；仍等架构成熟 PAKE / 短凭证 E2E 正式评估收口 → 产品对照验收标准 → **再授权实现**  
> 依据：用户（经鹊桥-体验）于 **2026-11-06 10:35 Asia/Shanghai** 明确选择严格唯一凭证。  
> **本轮尚未授权实现**；不得改 C#/Go 主路径，不得宣称现网已安全满足。

## 产品拍板（LOCKED · 关闭此前开放评估）

用户原话要点：**「严格只认 code+密码，自己的设备也输入，不另设设备密钥快捷连接。」**

1. **唯一连接凭证** = 会话 code + **8 位**密码。
2. **取消**：账号 / 同 owner 服务器声明免密；设备密钥快捷连接；`connect_device` / `ownerLink` 等仅凭服务器 token/owner 声明授权控制的产品意图。
3. **自有设备也输入** code+密码。
4. **服务端不知明文密码**（目标）；实现评估成熟 **PAKE**（见 `docs/pake-unique-credential-eval.md`），禁止自造密码学。
5. **写确认三档正交不变**（每次询问 / 宽松 / 完全信任）；不得借「服务器不可冒充」强制每次写弹窗。
6. **单窗口 / 默认中继高级改地址**仍为体验目标，由 **R049** 承担（与本轮 PAKE **独立**）。

此前「开放评估 · 等体验 A/B」**已关闭**；不得再写「去留未拍板」或并列设备密钥例外为待选默认。

## 体验目标（意图 · 已写入产品）

亲友主路径：复制 / 粘贴 `{会话码}-{8位验证码}` 一次即可连接；不转发完整 `remoteassist:pair` 长串；中继地址默认内置。

## 安全实现前置条件（未满足则不得实现「短码可连」 / 不得安全 PASS）

- 当前安全路径仍依赖完整载荷内 **Agent 业务 TLS 指纹**（`PairQrPayload` / `fp=`）。见 `docs/tls-trust.md`、`docs/quality-connection-credential.md`。
- 短码今日只授权中继会话，**不能单独**建安全业务通道。
- 架构须完成：成熟 PAKE 选型与集成边界；密码不向中继明文/可逆回传；恶意中继 / 重放 / 跨会话绑定 / 端点限速与密码生命周期；旁路收口（免密与设备密钥）。
- **禁止**：TOFU；跳过校验；把中继回传、未经可信绑定的指纹当独立认证；未经验证宣称短码已可安全连接或现网已满足严格唯一凭证；把本拍板当成实现授权。

## 本轮不做（直至 blocker 解除 + 产品授权）

- 客户端把主复制改成只复制短码却仍「能连」
- 任何削弱 R043 信任模型或本机写确认的实现
- 静默保留账号免密 / 设备密钥旁路并继续宣传
- 与 **R049**（单主窗口）混为一谈——R049 可另授权先做 UX
- 未授权即开实现

## 解除 blocked 后

1. 架构正式评估收口入库（PAKE profile/库、tls-trust / credential 更新、旁路收口清单）。
2. 产品对照方案改本 round 验收标准（含恶意中继运行测试）并**明确授权**实现。
3. 实现后：编译/文档核对 ≠ 安全 PASS；安全 PASS 须运行验证。

## 参考

- `docs/PRODUCT.md`（产品拍板：严格唯一凭证）
- `docs/quality-connection-credential.md`
- `docs/pake-unique-credential-eval.md`
- `docs/short-credential-trust.md`
- `docs/tls-trust.md`
- `docs/round-049.md`
"""

r50.write_text(NEW_R50, encoding="utf-8", newline="\n")
print("round-050.md OK")

# ---------- round-049.md: light cross-ref only ----------
r49 = ROOT / "round-049.md"
t49 = r49.read_text(encoding="utf-8")
needle = "- 短凭证可连见 **R050**（blocked 等架构方案）。"
repl = (
    "- 短凭证 / 严格唯一凭证见 **R050**（决策已锁定；仍 blocked 等架构 PAKE；与本轮 UX **独立**，本轮可另授权）。"
)
if needle in t49:
    t49 = t49.replace(needle, repl, 1)
    r49.write_text(t49, encoding="utf-8", newline="\n")
    print("round-049.md OK")
else:
    print("round-049.md: skip (needle missing)")

# ---------- retro.md ----------
retro = ROOT / "retro.md"
rx = retro.read_text(encoding="utf-8")
if "## 严格唯一凭证拍板（2026-11-06 10:35 Asia/Shanghai）" not in rx:
    add = """

## 严格唯一凭证拍板（2026-11-06 10:35 Asia/Shanghai）

- **拍板**：用户选严格只认 code+8位密码；自有设备也输入；取消账号免密与设备密钥快捷连；三档写确认正交；R049 可独立授权；R050 决策锁定仍 blocked（等 PAKE，未授权实现，勿称现网已满足）。
"""
    # Append after the open-eval section if present, else end
    marker = "## 唯一控制凭证开放评估（2026-11-06 Asia/Shanghai）"
    if marker in rx:
        # insert after that section's last bullet block — append note that it closed
        idx = rx.find(marker)
        # find next ## after this section
        next_h = rx.find("\n## ", idx + 5)
        close_note = (
            "\n- **后续（同日 10:35）**：用户经体验选**严格唯一**；开放评估**关闭**，见下节「严格唯一凭证拍板」。\n"
        )
        if next_h == -1:
            rx = rx + close_note + add
        else:
            rx = rx[:next_h] + close_note + add + rx[next_h:]
    else:
        if not rx.endswith("\n"):
            rx += "\n"
        rx += add
    retro.write_text(rx, encoding="utf-8", newline="\n")
    print("retro.md OK")
else:
    print("retro.md already has locked section")

print("ALL DONE")
