# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(r"E:\IdeaProjects\remote-assist\docs")

# ---------- roadmap.md ----------
roadmap = (ROOT / "roadmap.md").read_text(encoding="utf-8-sig")

old_r044_row = "| R044 | 高频：完整连接凭证**逐字文案**扫尾（亲友 UI 无指纹/TLS 技术词）+ Avalonia/CLI 凭证 UI；有条件时本机外运行验收；备忘 net8 Insecure 债 | 鹊桥-体验（规格已定）/ 鹊桥-客户端（实现） | **compiled**（net48 文案+编译；运行未验收；Avalonia/CLI 未改） | `docs/round-044.md`、`docs/quality-connection-credential.md` |"
new_r044_row = "| R044 | 高频：完整连接凭证**逐字文案**扫尾（亲友 UI 无指纹/TLS 技术词）；Avalonia/CLI 本轮未改（完整载荷未移植） | 鹊桥-体验（规格已定）/ 鹊桥-客户端（实现） | **PASS_WITH_NOTES**（编译/文档 `7cad0ab`；运行 blocked） | `docs/round-044.md`、`docs/quality-connection-credential.md` |"
new_r045_row = "| R045 | 高频：亲友失败文案残留（码+密码→完整连接凭证）+ Helper 缺安全验证提示收窄（仅缺 fp/短凭证）+ 文档状态行清理；稳定性/UX，不扩功能 | 鹊桥-客户端 | **open** | `docs/round-045.md` |"

if old_r044_row not in roadmap:
    raise SystemExit("R044 row not found exact")
roadmap = roadmap.replace(old_r044_row, new_r044_row + "\n" + new_r045_row, 1)

old_cur = (
    "> **当前**：R044 **compiled**（net48 逐字文案+编译；运行未验收；Avalonia/CLI 未改）——运行与跨端仍 open；有条件时本机外运行验收；net8 Insecure 债备忘。"
    "R043 编译/文档 **PASS_WITH_NOTES**（`0122127`），真连仍 blocked；完整连接凭证载荷已接线；实际连接、安全校验与界面运行验收另计。"
    "见 `docs/round-044.md`、`docs/round-043.md`。R042 已产品验收（出包 `8d61398`，上架 `f90bd8f`，2026-11-06）。"
)
new_cur = (
    "> **当前**：R045 **open**（亲友失败文案残留 + Helper 缺安全验证提示收窄 + 文档状态清理；仅 net48；不扩功能）。"
    "R044 编译/文档 **PASS_WITH_NOTES**（`7cad0ab`；产物 19968 B / 2026-11-06 09:46:59 Asia/Shanghai / SHA256 `a78c0f24…d81e`），运行仍 blocked；Avalonia/CLI 完整载荷另轮。"
    "R043 编译/文档 **PASS_WITH_NOTES**（`0122127`），真连仍 blocked。见 `docs/round-045.md`、`docs/round-044.md`。"
    "R042 已产品验收（出包 `8d61398`，上架 `f90bd8f`，2026-11-06）。"
)
if old_cur not in roadmap:
    # show nearby for debug
    idx = roadmap.find("**当前**：R044")
    print("CUR SNIP:", repr(roadmap[idx:idx+400]) if idx>=0 else "no")
    raise SystemExit("current blurb not found")
roadmap = roadmap.replace(old_cur, new_cur, 1)

old_plat = "R043 编译/文档 PASS_WITH_NOTES（`0122127`；真连 blocked）；R044 打开（逐字文案 + Avalonia/CLI；见 `docs/round-044.md`）。"
new_plat = (
    "R043 编译/文档 PASS_WITH_NOTES（`0122127`；真连 blocked）；"
    "R044 编译/文档 PASS_WITH_NOTES（`7cad0ab`；运行 blocked；见 `docs/round-044.md`）；"
    "R045 打开（失败文案残留 + 缺安全提示收窄；见 `docs/round-045.md`）。"
)
if old_plat not in roadmap:
    raise SystemExit("platform paragraph tail not found")
roadmap = roadmap.replace(old_plat, new_plat, 1)

(ROOT / "roadmap.md").write_text(roadmap, encoding="utf-8", newline="\n")
print("roadmap ok")

# ---------- PRODUCT.md ----------
product = (ROOT / "PRODUCT.md").read_text(encoding="utf-8-sig")
old_p = (
    "R043 编译/文档核对 **PASS_WITH_NOTES**（`0122127`；产物 net48 exe 19968 字节 / 2026-11-06 09:23:55 Asia/Shanghai）；"
    "**真实连接仍 blocked**。完整连接凭证载荷已接线；实际连接、安全校验与界面运行验收另计。见 `docs/round-043.md`。"
    "当前打开 **R044**：完整连接凭证逐字文案扫尾 + Avalonia/CLI UI（`docs/round-044.md`）。"
    "后续迭代优先稳定性和用户体验；本机验收勿打断用户工作，优先本机外路径。"
)
new_p = (
    "R043 编译/文档核对 **PASS_WITH_NOTES**（`0122127`；产物 net48 exe 19968 字节 / 2026-11-06 09:23:55 Asia/Shanghai）；"
    "**真实连接仍 blocked**。完整连接凭证载荷已接线；实际连接、安全校验与界面运行验收另计。见 `docs/round-043.md`。"
    "R044 编译/文档核对 **PASS_WITH_NOTES**（`7cad0ab`；产物 19968 字节 / 2026-11-06 09:46:59 Asia/Shanghai / SHA256 `a78c0f246c4532025cd5ed0407013ac1828e2e3bb379fc692a0a32604080d81e`）；"
    "**运行仍 blocked**；net48 七句逐字文案已接线，Avalonia/CLI 未改。见 `docs/round-044.md`。"
    "当前打开 **R045**：亲友失败文案残留（码+密码→完整连接凭证）+ Helper 缺安全验证提示收窄 + 文档状态清理（`docs/round-045.md`）。"
    "后续迭代优先稳定性和用户体验；本机验收勿打断用户工作，优先本机外路径。"
)
if old_p not in product:
    idx = product.find("R043 编译/文档核对")
    print("PROD SNIP:", repr(product[idx:idx+500]) if idx>=0 else "no")
    raise SystemExit("PRODUCT blurb not found")
product = product.replace(old_p, new_p, 1)
(ROOT / "PRODUCT.md").write_text(product, encoding="utf-8", newline="\n")
print("PRODUCT ok")

# ---------- retro.md ----------
retro = (ROOT / "retro.md").read_text(encoding="utf-8-sig")
if "已开 R044（体验规格已定；客户端实现逐字文案；不改格式/信任模型）。" not in retro:
    raise SystemExit("retro R044 open line not found")
addon = r'''

## R044 产品验收复盘（2026-11-06 Asia/Shanghai）

编译/文档核对 **PASS_WITH_NOTES**（证据 `7cad0ab`）；真实连接 / 运行 **仍 blocked**。

- **做对的**：net48 七句逐字权威文案落地；默认亲友路径去掉指纹/TLS/fp/安全载荷技术词；复制失败可手选完整串；范围自觉收窄为仅 net48（Avalonia 未接完整载荷，避免文实不符）；无 UI 编译、不打断用户本机。
- **NOTES → R045**：`FailWrongPassword` / `FailPeerNotAccepting` 仍「码+密码」；Helper catch 一律追加缺失安全验证句（过宽）；文档状态行曾混用 compiled/待实现；AgentHost 控制台技术词备忘。
- **规则候选**：缺失安全验证提示只绑缺 fp/短凭证分支；其他失败各自人话，禁止一律追加。

已开 R045（稳定性/UX：失败文案残留 + 提示收窄 + 文档状态清理；仅 net48；不扩功能；Avalonia/CLI 完整载荷另轮）。
'''
# append after the R044 open line once
needle = "已开 R044（体验规格已定；客户端实现逐字文案；不改格式/信任模型）。"
if "## R044 产品验收复盘" in retro:
    print("retro already has R044 accept section")
else:
    retro = retro.replace(needle, needle + addon, 1)
    (ROOT / "retro.md").write_text(retro, encoding="utf-8", newline="\n")
    print("retro ok")

print("phase2 done")
