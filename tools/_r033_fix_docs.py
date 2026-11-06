from pathlib import Path
import re
bt = chr(96)
root = Path(r"E:/IdeaProjects/remote-assist")

pm = (root / "docs/PRODUCT.md").read_text(encoding="utf-8")
i = pm.find("R032 已于")
end = pm.find("\n\n- **Windows / macOS / Linux**", i)
new = (
    "R032 已于 2026-11-06 产品对照截图验收通过（" + bt + "b78fc1e" + bt + "）。"
    "R033 Windows 接受协助端「结束这次协助」已于 2026-11-06 产品对照截图验收通过（" + bt + "3b01e7f" + bt + "）。"
    "下一步 R034：官网安装包跟上含 R031–R033 的客户端。"
)
pm = pm[:i] + new + pm[end:]
(root / "docs/PRODUCT.md").write_text(pm, encoding="utf-8", newline="\n")
print("PRODUCT ok:", repr(pm[i:i+160]))

rm = (root / "docs/roadmap.md").read_text(encoding="utf-8")
rm = rm.replace("| docs/round-033.md |", "| " + bt + "docs/round-033.md" + bt + " |")
rm = rm.replace("| docs/round-034.md |", "| " + bt + "docs/round-034.md" + bt + " |")
# platform paragraph: find R032 已产品验收 and rewrite tail through 未开再下一轮
m = re.search(r"R032 已产品验收.*未开再下一轮。", rm)
if m:
    rep = (
        "R032 已产品验收（" + bt + "b78fc1e" + bt + "，2026-11-06）；"
        "R033 已产品验收（" + bt + "3b01e7f" + bt + "，2026-11-06）；"
        "下一步 R034：官网安装包跟上含 R031–R033；未开再下一轮。"
    )
    rm = rm[:m.start()] + rep + rm[m.end():]
    print("platform replaced")
else:
    print("platform pattern missing")
# site table
rm = rm.replace(
    "**done（R019–R030）；R033 ready 跟上 R031–R032**",
    "**done（R019–R030）；R034 ready 跟上 R031–R033**",
)
rm = rm.replace(
    "**done（R019–R030）；R034 ready 跟上 R031–R032**",
    "**done（R019–R030）；R034 ready 跟上 R031–R033**",
)
(root / "docs/roadmap.md").write_text(rm, encoding="utf-8", newline="\n")
for l in rm.splitlines():
    if l.startswith("| R033") or l.startswith("| R034") or ("R034 ready" in l and "安装包" in l):
        print(l)
if "下一步 R034" in rm:
    print("roadmap has next R034")
