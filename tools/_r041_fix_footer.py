from pathlib import Path
import re
p = Path("docs/roadmap.md")
t = p.read_text(encoding="utf-8")
if "R041 已交付（提议四态" not in t:
    # common product open line
    patterns = [
        ("R041 已开：见 `docs/round-041.md`（依据 `docs/scene-s004-propose-wait.md` + `docs/quality-after-connect-status.md`）。",
         "R041 已交付（提议四态 + 去掉 PAIR_OK，见 `docs/round-041.md`）；未开下一轮。"),
    ]
    for a,b in patterns:
        if a in t:
            t = t.replace(a, b, 1)
            print("replaced open line")
            break
    # scene line ending
    m = re.search(r"R040 已产品验收（[^）]+）；[^\n]*", t)
    if m and "R041 已交付" not in m.group(0):
        t = t.replace(m.group(0), m.group(0).rstrip("。") + "；R041 已交付（提议四态 + 去掉 PAIR_OK，见 `docs/round-041.md`）；未开下一轮。", 1)
        print("patched scene")
    p.write_text(t, encoding="utf-8", newline="\n")
else:
    print("footer has R041 delivered")
i = t.rfind("R040 已产品")
print(t[i:i+220] if i>=0 else "no")
