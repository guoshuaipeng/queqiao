from pathlib import Path
import re

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
# Replace any R022 ... 已开 ... 未开下一轮 sentence fragment after R021验收
pat = r"R022[^；\n]*已开（见 `docs/round-022\.md`）；未开下一轮。"
m = re.search(pat, rt)
print("roadmap match:", m.group(0) if m else None)
if m:
    rt = re.sub(pat, "R022：客户端已改连网话术并打出 Win/Linux 新包（见 `docs/round-022.md`），待服务端上架；未开下一轮。", rt, count=1)
    rm.write_text(rt, encoding="utf-8")
    print("roadmap fixed")

p = Path("docs/round-022.md")
t = p.read_text(encoding="utf-8")
# find 出包 line
for line in t.splitlines():
    if "出包" in line or "HEAD" in line or "71893" in line:
        print("ROUND:", line[:160])

old = "出包工作树对应本交付提交（含 R021 `632928a` + 本轮连网话术）；见本 commit。"
if old in t:
    t = t.replace(old, "出包 HEAD：`71893d223d5c6894864fa17f49e30dd03973e7fd`（含 R021 `632928a` + 本轮连网话术；包由该提交对应源码打出）。")
    p.write_text(t, encoding="utf-8")
    print("round fixed via exact")
elif "出包 HEAD" not in t:
    # insert after 对照旧包 paragraph
    needle = "出包前源码/产物 DLL 已核对含新联网句、不含旧句。"
    if needle in t:
        t = t.replace(needle, needle + "\n\n出包 HEAD：`71893d223d5c6894864fa17f49e30dd03973e7fd`（含 R021 `632928a` + 本轮连网话术；包由该提交对应源码打出）。")
        p.write_text(t, encoding="utf-8")
        print("round inserted head note")
    else:
        print("needle missing")
else:
    print("round already has 出包 HEAD")
