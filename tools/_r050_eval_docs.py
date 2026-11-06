# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(r"E:\IdeaProjects\remote-assist\docs")

EVAL = """## 用户目标（开放评估 · 非出货拍板 · 2026-11-06）

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

# ---- PRODUCT ----
prod = ROOT / "PRODUCT.md"
pt = prod.read_text(encoding="utf-8")
if "## 用户目标（开放评估" not in pt:
    marker = "## 鉴权与安全底线"
    if marker not in pt:
        raise SystemExit("PRODUCT marker missing")
    # Insert after 安全实现前置条件 block (before 鉴权)
    pt = pt.replace(marker, EVAL + marker, 1)
    # Soften 账号免密 bullet slightly with pointer — do NOT remove existing feature description
    old_acct = "3. **账号免密**：简单注册/登录 → 电脑加入账号（设备上线）→ 同账号机器列表 → 连接在线设备（无需再输会话密码）。"
    new_acct = "3. **账号免密（现网能力；与「唯一凭证」评估冲突，去留未拍板）**：简单注册/登录 → 电脑加入账号（设备上线）→ 同账号机器列表 → 连接在线设备（无需再输会话密码）。若用户确认 code+password 为唯一控制凭证，本路径与之冲突——**等体验 A/B，禁止自行拍板兼容或废止**；见「用户目标（开放评估）」。"
    if old_acct not in pt:
        raise SystemExit("PRODUCT 账号免密 bullet missing")
    pt = pt.replace(old_acct, new_acct, 1)
    prod.write_text(pt, encoding="utf-8", newline="\n")
    print("PRODUCT updated")
else:
    print("PRODUCT already has open eval")

# ---- credential ----
cred = ROOT / "quality-connection-credential.md"
ct = cred.read_text(encoding="utf-8")
if "## 用户目标（开放评估" not in ct:
    marker = "## 当前安全主路径"
    if marker not in ct:
        raise SystemExit("cred marker missing")
    # Slimmer eval for credential doc + pointer to PRODUCT
    cred_eval = """## 用户目标（开放评估 · 非出货拍板 · 2026-11-06）

> **USER GOAL + OPEN EVALUATION**，详见 `docs/PRODUCT.md` 同名节。短码 / 唯一凭证**不是**已定出货路径。

- **评估目标**：code + password 为**唯一**控制凭证；服务端**不知**明文密码。
- **冲突**：与现网账号免密 / owner-token（`connect_device` / `ownerLink`）冲突；产品**未**决定保留旧免密；**禁止拍板兼容**；等用户经体验选 A/B。
- **正交**：写操作本机确认档位（权限三级 + 信任等级）不因本评估改动。
- **架构**：另评 PAKE（指针）；**R050** 仍 blocked。当前安全主路径（完整载荷 + `fp=`）不变。

"""
    ct = ct.replace(marker, cred_eval + marker, 1)
    # tweak 排期 line
    if "唯一凭证开放评估" not in ct:
        ct = ct.replace(
            "- **体验目标 / R049–R050**：",
            "- **用户目标开放评估**：code+password 唯一控制凭证 + 服务端不知明文密码——评估中，非出货；与免密冲突等 A/B（见 PRODUCT）。\n- **体验目标 / R049–R050**：",
            1,
        )
    cred.write_text(ct, encoding="utf-8", newline="\n")
    print("credential updated")
else:
    print("credential already has open eval")

# ---- round-050 ----
r050 = ROOT / "round-050.md"
r050.write_text("""# 第 50 轮 — 短凭证安全连接（blocked）+ 唯一凭证开放评估

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 状态：**blocked** — 等架构短凭证 E2E；另：**用户目标开放评估**（非出货拍板）  
> 依据：用户重申短码复制；并抬高评估标准为「code+password 为唯一控制凭证、服务端不知密码」。

## 体验目标（意图 · 已写入产品）

亲友主路径：复制 / 粘贴 `{会话码}-{8位验证码}` 一次即可连接；不转发完整 `remoteassist:pair` 长串；中继地址默认内置。

## 用户目标（开放评估 · 非出货）

> **USER GOAL + OPEN EVALUATION** — 见 `docs/PRODUCT.md`「用户目标（开放评估）」。

1. code + password 作为**唯一**控制凭证（sole control credential）。
2. 服务端 / 中继**不得**知明文 password（评估方向；与今日 guest 密码校验实现可能冲突，未拍板改法）。
3. **写确认档位正交**：不因本评估削弱本机写确认 / 信任等级。

### 冲突（未拍板 · 等体验 A/B）

- 与现网 **账号免密** / owner-token（`login` → `connect_device` / `ownerLink`）冲突。
- 产品**尚未决定**是否保留旧免密。
- **禁止拍板兼容**或擅自废止；等用户经 **鹊桥-体验** 选 A/B 后再改文档与排期。

### 架构指针

- 架构正在单独评估 **PAKE**（及短凭证 E2E）；方案入库前仅指针，**不**写成已选方案。

## 安全实现前置条件（未满足则不得实现「短码可连」）

- 当前安全路径仍依赖完整载荷内 **Agent 业务 TLS 指纹**（`PairQrPayload` / `fp=`）。见 `docs/tls-trust.md`、`docs/quality-connection-credential.md`。
- 短码今日只授权中继会话，**不能单独**建安全业务通道。
- 架构须给出：如何在短凭证流程中保留 E2E 身份验证；迁移边界另议——**不**在评估未完成时拍板「与免密兼容」。
- **禁止**：TOFU；跳过校验；把中继回传、未经可信绑定的指纹当独立认证；未验证宣称短码已可安全连接；把本开放评估当成出货授权。

## 本轮不做（直至 blocker 解除 + 评估收口）

- 客户端把主复制改成只复制短码却仍「能连」
- 任何削弱 R043 信任模型或本机写确认的实现
- 自行拍板免密去留 / 兼容方案
- 与 **R049**（单主窗口）混为一谈——R049 可先做 UX

## 解除 blocked / 收口评估后

1. 架构方案入库（含是否采用 PAKE；tls-trust / credential 更新）。
2. 用户经体验完成免密 A/B 选择 → 产品落文档。
3. 产品对照方案改本 round 验收标准并授权实现。

## 参考

- `docs/PRODUCT.md`（体验目标 / 安全实现前置条件 / **用户目标开放评估**）
- `docs/quality-connection-credential.md`
- `docs/tls-trust.md`
- `docs/round-049.md`
""", encoding="utf-8", newline="\n")
print("round-050 rewritten")

# ---- roadmap ----
rm = ROOT / "roadmap.md"
rt = rm.read_text(encoding="utf-8")
old_r050 = "| R050 | 高频：亲友主路径**短码**（会话码+8位密码）一次复制/粘贴即可**安全**连接 | 架构方案 → 客户端/中继按方案 | **blocked**（等架构短凭证 E2E；禁止 TOFU/跳校验/未绑定指纹当认证） | `docs/quality-connection-credential.md`、`docs/tls-trust.md` |"
new_r050 = "| R050 | 高频：亲友主路径**短码**安全可连；另：**开放评估** code+password **唯一**控制凭证 + 服务端不知明文密码（非出货；与账号免密冲突等体验 A/B，禁止拍板兼容） | 架构（PAKE/短凭证评估）→ 产品收口 → 实现 | **blocked** + **open eval**（非出货拍板） | `docs/round-050.md`、`docs/PRODUCT.md`、`docs/quality-connection-credential.md` |"
if old_r050 in rt:
    rt = rt.replace(old_r050, new_r050, 1)
    print("roadmap R050 row updated")
elif "| R050 |" in rt and "open eval" in rt:
    print("roadmap R050 already open eval")
else:
    # fuzzy replace the R050 line
    lines = rt.splitlines(keepends=True)
    out = []
    hit = False
    for line in lines:
        if line.startswith("| R050 |") and "open eval" not in line:
            out.append(new_r050 + "\n")
            hit = True
        else:
            out.append(line)
    if not hit:
        raise SystemExit("roadmap R050 line not found")
    rt = "".join(out)
    print("roadmap R050 fuzzy updated")

# update current note to mention open eval
if "唯一凭证开放评估" not in rt:
    rt = rt.replace(
        "**R050** **blocked**（短凭证安全可连，等架构；见 `quality-connection-credential.md`）。用户重申：复制目标=短码；单主窗口；**不得**未验证称短码可安全连接。",
        "**R050** **blocked** + **open eval**（短凭证等架构；唯一凭证/服务端不知密码为评估非出货；与免密冲突等体验 A/B，禁止拍板兼容；见 `round-050.md`）。用户重申短码+单主窗口；另抬高评估标准；**不得**未验证称短码可安全连接。",
        1,
    )
    print("roadmap current note updated")
else:
    print("roadmap current note already mentions open eval")

rm.write_text(rt, encoding="utf-8", newline="\n")
print("roadmap written")

# ---- retro ----
retro = ROOT / "retro.md"
rtro = retro.read_text(encoding="utf-8")
if "唯一控制凭证开放评估" not in rtro:
    rtro = rtro.rstrip() + "\n\n## 唯一控制凭证开放评估（2026-11-06 Asia/Shanghai）\n\n- **触发**：用户抬高评估标准——code+password 为**唯一**控制凭证，服务端不知明文密码；非已定出货路径。\n- **收口**：标为 **USER GOAL + OPEN EVALUATION** 写入 PRODUCT / credential / round-050；写确认档位正交；架构另评 PAKE（指针）。\n- **冲突**：与现网账号免密/owner-token 冲突；产品**未**决定保留旧免密；**禁止拍板兼容**；等用户经体验 A/B。\n"
    retro.write_text(rtro, encoding="utf-8", newline="\n")
    print("retro updated")
else:
    print("retro already has note")

print("ALL DONE")
