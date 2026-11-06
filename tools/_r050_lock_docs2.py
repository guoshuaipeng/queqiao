# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(r"E:\IdeaProjects\remote-assist\docs")

# ----- verify PRODUCT has locked section -----
pt = (ROOT / "PRODUCT.md").read_text(encoding="utf-8")
if "## 产品拍板：严格唯一凭证（2026-11-06 10:35 Asia/Shanghai）" not in pt:
    raise SystemExit("PRODUCT missing locked section")
if "开放评估 · 非出货拍板" in pt:
    raise SystemExit("PRODUCT still has open eval heading")
print("PRODUCT OK")

# ----- polish credential: remove stale OPEN EVALUATION quote -----
cred = ROOT / "quality-connection-credential.md"
ct = cred.read_text(encoding="utf-8")
old_q = (
    "## 用户目标（已拍板 · 严格唯一凭证 · 2026-11-06 10:35）\n\n"
    "> **USER GOAL + OPEN EVALUATION**，详见 `docs/PRODUCT.md` 同名节。短码 / 唯一凭证**不是**已定出货路径。\n\n"
    "- **评估目标**：code + password 为**唯一**控制凭证；服务端**不知**明文密码。\n"
    "- **拍板**：严格唯一凭证 = code + 8 位密码；**取消**账号免密 / owner-token 声明授权与设备密钥快捷连；自有设备也输入 code+密码。"
    "详见 [pake-unique-credential-eval.md](pake-unique-credential-eval.md) §0/§7 与 [short-credential-trust.md](short-credential-trust.md) §10。\n"
    "- **正交**：写操作本机确认档位（权限三级 + 信任等级）不因本评估改动。\n"
    "- **架构**：另评 PAKE（指针）；**R050** 仍 blocked。当前安全主路径（完整载荷 + `fp=`）不变。\n"
)
new_q = (
    "## 产品拍板：严格唯一凭证（2026-11-06 10:35 Asia/Shanghai）\n\n"
    "> **LOCKED** — 详见 `docs/PRODUCT.md`「产品拍板：严格唯一凭证」。"
    "用户选择严格只认 code+8位密码；取消免密与设备密钥旁路；自有设备也输入。"
    "**不是**现网已实现；**不是**安全 PASS。\n\n"
    "- **唯一连接凭证**：会话 code + 8 位密码；服务端不知明文密码为目标（PAKE）。\n"
    "- **废止产品意图**：`connect_device` / `ownerLink` 账号免密、设备密钥快捷连；遗留实现待 **R050** 收口，不得继续宣传。\n"
    "- **正交**：写操作本机确认三档不变（不因本拍板强制每次弹窗）。\n"
    "- **架构**：继续正式评估成熟 PAKE（`docs/pake-unique-credential-eval.md`）；"
    "**R050** 决策锁定但仍 **blocked**（未授权实现）。当前安全主路径（完整载荷 + `fp=`）在运行验证前不变。\n"
)
if old_q not in ct:
    raise SystemExit("credential section mismatch:\n" + ct[ct.find("## 用户目标"):ct.find("## 用户目标")+500])
ct = ct.replace(old_q, new_q, 1)

old_sched2 = (
    "- **体验目标 / R049–R050**：短码复制与单主窗口为产品意图（见上节）；"
    "**R049** 可做单主窗口 + 高级中继 UI（**不改**短凭证安全）；"
    "**R050** 短凭证可连 **blocked** 等架构。未满足前置条件前，主按钮与剪贴板仍按本规范「当前安全主路径」。"
)
new_sched2 = (
    "- **体验目标 / R049–R050**：**R049** draft — 单主窗口 + 高级中继 UI（与 PAKE **独立**，未授权实现，可另授权）；"
    "**R050** — 决策锁定、仍 **blocked**（等架构 PAKE 正式评估收口后再授权实现）。"
    "未满足前置条件前，主按钮与剪贴板仍按本规范「当前安全主路径」。"
)
if old_sched2 not in ct:
    raise SystemExit("credential schedule R049/R050 miss")
ct = ct.replace(old_sched2, new_sched2, 1)
cred.write_text(ct, encoding="utf-8", newline="\n")
print("credential OK")

# ----- roadmap -----
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
    raise SystemExit("roadmap R049 miss")
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
    raise SystemExit("roadmap R050 miss")
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
    raise SystemExit("roadmap current miss")
rt = rt.replace(old_cur, new_cur, 1)
rm.write_text(rt, encoding="utf-8", newline="\n")
print("roadmap OK")

# ----- round-050 -----
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
(ROOT / "round-050.md").write_text(NEW_R50, encoding="utf-8", newline="\n")
print("round-050 OK")

# ----- round-049 light cross-ref -----
r49 = ROOT / "round-049.md"
t49 = r49.read_text(encoding="utf-8")
needle = "- 短凭证可连见 **R050**（blocked 等架构方案）。"
repl = "- 短凭证 / 严格唯一凭证见 **R050**（决策已锁定；仍 blocked 等架构 PAKE；与本轮 UX **独立**，本轮可另授权）。"
if needle in t49:
    t49 = t49.replace(needle, repl, 1)
    r49.write_text(t49, encoding="utf-8", newline="\n")
    print("round-049 OK")
else:
    print("round-049 skip:", "R050" in t49)

# ----- retro -----
retro = ROOT / "retro.md"
rx = retro.read_text(encoding="utf-8")
if "## 严格唯一凭证拍板（2026-11-06 10:35 Asia/Shanghai）" not in rx:
    add = (
        "\n\n## 严格唯一凭证拍板（2026-11-06 10:35 Asia/Shanghai）\n\n"
        "- **拍板**：用户选严格只认 code+8位密码；自有设备也输入；取消账号免密与设备密钥快捷连；三档写确认正交；"
        "R049 可独立授权；R050 决策锁定仍 blocked（等 PAKE，未授权实现，勿称现网已满足）。\n"
    )
    marker = "## 唯一控制凭证开放评估（2026-11-06 Asia/Shanghai）"
    if marker in rx:
        idx = rx.find(marker)
        next_h = rx.find("\n## ", idx + 5)
        close_note = "\n- **后续（同日 10:35）**：用户经体验选**严格唯一**；开放评估**关闭**，见下节「严格唯一凭证拍板」。\n"
        if next_h == -1:
            rx = rx + close_note + add
        else:
            rx = rx[:next_h] + close_note + add + rx[next_h:]
    else:
        if not rx.endswith("\n"):
            rx += "\n"
        rx += add
    retro.write_text(rx, encoding="utf-8", newline="\n")
    print("retro OK")
else:
    print("retro already locked")

print("ALL DONE")
