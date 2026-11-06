# -*- coding: utf-8 -*-
"""R027 product acceptance + open R028 docs updater (idempotent where noted)."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"

ACCEPT_SECTION = """## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r027-*` 与交付说明独立验收通过（实现 `65f2e2d`）。

独立核对：

- `r027-skill-entry.png`：去协助端「如何安装 MCP」内「复制 Skill 说明」为轻按钮，与「复制 MCP 配置」并列；该屏唯一实心绿仍是「连接」；三步原文未改。
- `r027-skill-copied.png`：反馈「已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。」只出现一次；状态仍「未连接」。
- `r027-skill-clipboard-preview.png`：预览正文与 round-027 剪贴板要求逐字一致；未见端口 / 17891 / Assist / ownerLink / TLS / 证书 / 签名 / HTTP /「不安全」/ SmartScreen。
- 交付说明与 `McpInstallGuide` 常量披露 Windows WinForms 与 Linux Avalonia 共用文案；本轮截图以 Avalonia ui-shot 为主（与 R017 同类）。
- 未改接受协助端、未换公网安装包、未自动对外监听 MCP、未改 trust-levels「才问你」、未改中继/官网/AI-SSH-Pro。

缺口：未单独拍 WinForms 截图（同源常量已披露，不挡验收）；接受协助端 Skill 入口本轮明确不做，交 R028；公网包仍为 R026。

已开 R028（接受协助端 Skill 薄封装：复制一段只讲如何安装 MCP 的技能说明；不改中继/官网/AI-SSH-Pro）。
"""

R028_CONTENT = """# 第 28 轮 — 接受协助端 Skill 薄封装（如何安装 MCP）

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**ready**
> 依据：R027 产品已验收（去协助端「复制 Skill 说明」）；R018 已在接受协助端落地可选 MCP 三步与「复制 MCP 配置」，但尚未有可直接保存为技能的白话 Skill 说明。亲友本机接受协助时，把 MCP 交给智能体仍缺这一段。

去协助端 Skill 薄封装（R027）已闭环。本轮对齐 R017→R018 的路径，把同一套「只讲如何安装 MCP」的 Skill 说明补到接受协助端，不把 Skill 做成控制面，也不能绕过本机确认。

## 本轮目标

1. **高频**：接受协助端「如何安装 MCP」卡片（R018 已有）增加「复制 Skill 说明」，复制内容只讲如何安装 MCP / 如何填参数。
2. **打磨**：复制后给人话反馈，只出现一次。

## A. 复制 Skill 说明（高频 · 鹊桥-客户端）

在接受协助端「如何安装 MCP」卡片内（已有三步和「复制 MCP 配置」旁边）增加轻按钮「复制 Skill 说明」。

剪贴板正文优先复用去协助端已验收的 `McpInstallGuide.SkillInstructions` / `SkillInstructionsClipboard`（逐字同一段）。若接受协助端文案必须区分「本机 / 对方」，仍不得加入端口、17891、Assist、ownerLink、TLS、证书、签名、HTTP、「不安全」、SmartScreen；默认推荐与 R027 完全同一段：

把下面这段保存成智能体的技能说明（只用来告诉它怎么安装鹊桥的 MCP，不能代替对方本机确认）。

1. 在鹊桥里点「复制 MCP 配置」。
2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。
3. 刷新或重启后再用智能体协助。

对方电脑上要改动的操作仍会弹确认，智能体不能绕过。不要用这段说明去远程控制电脑。

验收：

- 「复制 Skill 说明」是轻按钮（白底蓝字一类），不要做成实心绿；接受协助端主操作权重保持已验收样式。「复制 MCP 配置」保持已验收轻按钮。
- Windows（接受协助相关 UI）与 Linux Avalonia 接受协助端都要有（共用文案常量更好）。
- 剪贴板就是上面这段（或与 R027 常量同一引用），不要加端口、17891、Assist、ownerLink、TLS、证书、签名、HTTP、「不安全」、SmartScreen。
- 不要因为展示或复制这段说明，就自动对外监听 MCP（R018：仅接受协助时不自动对外监听仍成立）。
- 不要改 R018 已验收三步、「复制 MCP 配置」及反馈、不要改去协助端 R027 已验收行为、不要改 `docs/trust-levels.md` 的「才问你」。
- 截图放到 `docs/screenshots/r028-*`（至少：入口能看到新按钮、复制反馈、剪贴板预览能读到上文）。**勿覆盖** `r022-*`～`r027-*`。

## 打磨：复制后人话反馈

复制成功后提示（只出现一次），与 R027 同一句：

已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。

不要和已验收的「已复制 MCP 配置。请粘贴到 Cursor 的 MCP 设置并保存。」写成同一句。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 把 Skill 做成控制面或远程控制
- 替换公网安装包（可交后续轮）
- 购买代码签名证书 / 给官网加 HTTPS
- 改信任等级「才问你」
- 把未上真机的 macOS 包写成已经可下

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮
"""

RETRO_SECTION = """## R027 产品验收（2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r027-*` 验收通过（`65f2e2d`）：

- 去协助端「复制 Skill 说明」轻按钮 + 人话反馈只一次；剪贴板为仅安装 MCP 的技能说明；未自动监听 MCP。
- 缺口：未单独拍 WinForms；接受协助端 Skill 交 R028；公网包仍为 R026。

已开 R028（接受协助端 Skill 薄封装；交给鹊桥-客户端；不改中继/官网/AI-SSH-Pro）。
"""

skips = []
changes = []


def write(path: Path, text: str, label: str):
    old = path.read_text(encoding="utf-8") if path.exists() else None
    if old == text:
        skips.append(f"{label}: already exact")
        return
    path.write_text(text, encoding="utf-8", newline="\n")
    changes.append(label)


# --- 1) round-027.md ---
r027 = DOCS / "round-027.md"
t = r027.read_text(encoding="utf-8")
orig = t

# status → done（产品已验收）
t2, n = re.subn(
    r"> 状态：\*\*[^*]+\*\*[^\n]*",
    "> 状态：**done（产品已验收）**",
    t,
    count=1,
)
t = t2
if n:
    changes.append("round-027 status→done（产品已验收）")
else:
    if "**done（产品已验收）**" in t.splitlines()[4] if len(t.splitlines()) > 4 else "":
        skips.append("round-027 status already done")
    else:
        # try broader
        if "done（产品已验收）" in t[:400]:
            skips.append("round-027 status already done（产品已验收）")
        else:
            print("WARN: could not find status line in round-027", file=sys.stderr)

marker = "## 产品验收（鹊桥-产品"
if marker in t:
    # replace existing section through EOF (or next ## at same level after it — section is last)
    idx = t.index(marker)
    # keep everything before; drop trailing blank before marker
    before = t[:idx].rstrip() + "\n\n"
    new_t = before + ACCEPT_SECTION.rstrip() + "\n"
    if new_t != orig and new_t != t:
        t = new_t
        changes.append("round-027 产品验收 section replaced to product brief")
    elif ACCEPT_SECTION.strip() in orig:
        skips.append("round-027 产品验收 already matches required")
        t = orig  # no change from original if already matched after status fix only
        # re-apply status on orig
        t2, n = re.subn(
            r"> 状态：\*\*[^*]+\*\*[^\n]*",
            "> 状态：**done（产品已验收）**",
            orig,
            count=1,
        )
        t = t2
    else:
        # section exists but different — already handled by replace above if new_t != t
        # if new_t == t after status change only, check content
        before = t[: t.index(marker)].rstrip() + "\n\n"
        t = before + ACCEPT_SECTION.rstrip() + "\n"
        if t != orig:
            changes.append("round-027 产品验收 section updated")
else:
    t = t.rstrip() + "\n\n" + ACCEPT_SECTION.rstrip() + "\n"
    changes.append("round-027 产品验收 section appended")

# Recompute cleanly to avoid logic tangle
t = r027.read_text(encoding="utf-8")
t2, nstat = re.subn(
    r"> 状态：\*\*[^*]+\*\*[^\n]*",
    "> 状态：**done（产品已验收）**",
    t,
    count=1,
)
t = t2
if marker in t:
    before = t[: t.index(marker)].rstrip() + "\n\n"
    t = before + ACCEPT_SECTION.rstrip() + "\n"
else:
    t = t.rstrip() + "\n\n" + ACCEPT_SECTION.rstrip() + "\n"
if t != r027.read_text(encoding="utf-8"):
    r027.write_text(t, encoding="utf-8", newline="\n")
    if "round-027" not in " ".join(changes):
        changes.append("round-027.md updated")
else:
    skips.append("round-027.md unchanged (already matches)")


# --- 2) round-028.md ---
r028 = DOCS / "round-028.md"
desired = R028_CONTENT.rstrip() + "\n"
if r028.exists():
    old = r028.read_text(encoding="utf-8")
    if old.replace("\r\n", "\n") == desired:
        skips.append("round-028.md already exact (existed)")
    else:
        # Product brief supersedes prior draft; update in place.
        r028.write_text(desired, encoding="utf-8", newline="\n")
        changes.append("round-028.md rewritten to product brief (existed; updated)")
else:
    r028.write_text(desired, encoding="utf-8", newline="\n")
    changes.append("round-028.md created")


# --- 3) roadmap.md ---
rm = DOCS / "roadmap.md"
rt = rm.read_text(encoding="utf-8")
rt0 = rt

# R027 row status
rt, n = re.subn(
    r"(\| R027 \|[^|\n]+\|[^|\n]+\| )\*\*[^*]+\*\*",
    r"\1**done（产品已验收）**",
    rt,
    count=1,
)
# R028 row: ensure present with required text
r028_row = "| R028 | 高频：接受协助端复制 Skill 说明（仅安装 MCP）；打磨：复制后人话反馈 | 鹊桥-客户端 | **ready** | `docs/round-028.md` |"
if re.search(r"\| R028 \|", rt):
    rt, n = re.subn(
        r"\| R028 \|[^\n]+",
        r028_row,
        rt,
        count=1,
    )
else:
    # insert after R027 row
    rt, n = re.subn(
        r"(\| R027 \|[^\n]+\n)",
        r"\1" + r028_row + "\n",
        rt,
        count=1,
    )

# P2-5
rt, n = re.subn(
    r"5\. \*\*P2-5\*\*[^\n]+",
    "5. **P2-5** Skill 薄封装（仅安装 MCP 指引，非控制面）→ 界面三步指引 **done（R005）**；去协助端 Skill 封装 **done（R027）**；接受协助端本轮 R028",
    rt,
    count=1,
)

# Platform paragraph ending — replace next-step clause after R027 mention
# Prefer replacing from "R027 已产品验收" through end of that sentence's next-step.
def patch_platform_next(text: str) -> str:
    # Common patterns from prior drafts
    patterns = [
        (
            r"R027 已产品验收（`65f2e2d`，2026-11-06）；下一步 R028：[^\n。]+；未开再下一轮。",
            "R027 已产品验收（`65f2e2d`，2026-11-06）；下一步 R028：接受协助端 Skill 薄封装；未开再下一轮。",
        ),
        (
            r"下一步 R027：[^\n。]+；未开再下一轮。",
            "R027 已产品验收（`65f2e2d`，2026-11-06）；下一步 R028：接受协助端 Skill 薄封装；未开再下一轮。",
        ),
        (
            r"下一步 R028：接受协助端可选 Skill 说明；未开再下一轮。",
            "下一步 R028：接受协助端 Skill 薄封装；未开再下一轮。",
        ),
    ]
    for pat, rep in patterns:
        text2, n = re.subn(pat, rep, text, count=1)
        if n:
            return text2
    # Fallback: if R027 acceptance present but next step wrong
    if "下一步 R028：接受协助端 Skill 薄封装" in text:
        return text
    if "R027 已产品验收" in text and "下一步 R028" in text:
        text2, n = re.subn(
            r"下一步 R028：[^\n。]+",
            "下一步 R028：接受协助端 Skill 薄封装",
            text,
            count=1,
        )
        return text2
    return text

rt = patch_platform_next(rt)

if rt != rt0:
    rm.write_text(rt, encoding="utf-8", newline="\n")
    changes.append("roadmap.md updated")
else:
    skips.append("roadmap.md unchanged")


# --- 4) PRODUCT.md ---
pm = DOCS / "PRODUCT.md"
pt = pm.read_text(encoding="utf-8")
pt0 = pt

# Ensure R027 acceptance + next R028 Skill 薄封装 in the long platforms paragraph
# Replace older next-step phrasings
repls = [
    (
        r"下一步 R027：[^\n。]+。",
        "R027 去协助端 Skill 薄封装已于 2026-11-06 产品对照截图验收通过（`65f2e2d`）。下一步 R028：接受协助端 Skill 薄封装（仅安装 MCP 的技能说明）。",
    ),
    (
        r"R027 去协助端 Skill 薄封装已于 2026-11-06 产品对照截图验收通过（`65f2e2d`）。下一步 R028：接受协助端可选 Skill 说明（复用同一段仅安装 MCP 的指引）。",
        "R027 去协助端 Skill 薄封装已于 2026-11-06 产品对照截图验收通过（`65f2e2d`）。下一步 R028：接受协助端 Skill 薄封装（仅安装 MCP 的技能说明）。",
    ),
    (
        r"下一步 R028：接受协助端可选 Skill 说明[^\n。]*。",
        "下一步 R028：接受协助端 Skill 薄封装（仅安装 MCP 的技能说明）。",
    ),
]
for pat, rep in repls:
    pt2, n = re.subn(pat, rep, pt, count=1)
    if n:
        pt = pt2

# If somehow R027 acceptance missing entirely but R026 ends with next R027
if "R027 去协助端 Skill 薄封装已于" not in pt and "下一步 R027" not in pt0:
    # append after R026 sentence if present
    pt2, n = re.subn(
        r"(R026 官网安装包跟上含 R025 的客户端已于 2026-11-06 产品对照公网与截图验收通过（出包 HEAD `73b2fea`，上架 `a97a2f5`）。)",
        r"\1R027 去协助端 Skill 薄封装已于 2026-11-06 产品对照截图验收通过（`65f2e2d`）。下一步 R028：接受协助端 Skill 薄封装（仅安装 MCP 的技能说明）。",
        pt,
        count=1,
    )
    if n:
        pt = pt2

if pt != pt0:
    pm.write_text(pt, encoding="utf-8", newline="\n")
    changes.append("PRODUCT.md updated")
else:
    if "下一步 R028：接受协助端 Skill 薄封装" in pt:
        skips.append("PRODUCT.md already has R028 Skill 薄封装 next step")
    else:
        skips.append("PRODUCT.md unchanged (pattern miss?)")
        print("WARN PRODUCT snippet around R027:", file=sys.stderr)
        i = pt.find("R027")
        if i >= 0:
            print(pt[i : i + 220], file=sys.stderr)


# --- 5) retro.md ---
rr = DOCS / "retro.md"
rtxt = rr.read_text(encoding="utf-8")
rtxt0 = rtxt
retro_marker = "## R027 产品验收（2026-11-06 Asia/Shanghai）"
if retro_marker in rtxt:
    # replace from marker to EOF (assuming last section) or until next ## R0 that isn't R027
    idx = rtxt.index(retro_marker)
    # find next ## after this section
    rest = rtxt[idx + len(retro_marker) :]
    m = re.search(r"\n## ", rest)
    if m:
        end = idx + len(retro_marker) + m.start()
        rtxt = rtxt[:idx].rstrip() + "\n\n" + RETRO_SECTION.rstrip() + "\n" + rtxt[end:]
    else:
        rtxt = rtxt[:idx].rstrip() + "\n\n" + RETRO_SECTION.rstrip() + "\n"
else:
    rtxt = rtxt.rstrip() + "\n\n" + RETRO_SECTION.rstrip() + "\n"

if rtxt != rtxt0:
    rr.write_text(rtxt, encoding="utf-8", newline="\n")
    changes.append("retro.md R027 section updated/appended")
else:
    skips.append("retro.md unchanged")

print("CHANGES:")
for c in changes:
    print(" -", c)
print("SKIPS:")
for s in skips:
    print(" -", s)
print("round-028 exists:", r028.exists())
print("round-027 status line:", [ln for ln in r027.read_text(encoding="utf-8").splitlines() if "状态" in ln][:1])
