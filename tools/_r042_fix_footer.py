from pathlib import Path
p = Path("docs/roadmap.md")
t = p.read_text(encoding="utf-8")
# Find long scene paragraph end about R041/R042
needle = "R042 已开（见 `round-042.md`）。"
if needle in t:
    t = t.replace(needle, "R042 客户端已出包（含 R041，见 `docs/round-042.md`）；服务端待上架；未开下一轮。", 1)
    print("replaced remaining 已开")
# Also in scene闭环 line
import re
m = re.search(r"R041 已产品验收（`aeafeeb`，2026-11-06）；R042[^\n]*", t)
if m:
    print("SCENE:", m.group(0)[:200])
    if "客户端已出包" not in m.group(0):
        t = t.replace(m.group(0), "R041 已产品验收（`aeafeeb`，2026-11-06）；R042 客户端已出包（含 R041，见 `docs/round-042.md`）；服务端待上架；未开下一轮。", 1)
        print("scene fixed")
p.write_text(t, encoding="utf-8", newline="\n")
