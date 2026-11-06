from pathlib import Path
p = Path("docs/roadmap.md")
lines = p.read_text(encoding="utf-8").splitlines(True)
out = []
replaced = False
for line in lines:
    if (not replaced) and line.startswith("| R033 |") and "官网安装包" in line:
        out.append("| R033 | 高频：Windows 接受协助端补「结束这次协助」；打磨：结束前确认对方将连不上 | 鹊桥-客户端 | **delivered** | `docs/round-033.md` |\n")
        out.append("| R034 | 高频：官网安装包跟上含 R031–R033 的客户端；打磨：替换后首页旁注与导语不回退 | 鹊桥-客户端 / 鹊桥-服务端 | **ready** | `docs/round-034.md` |\n")
        replaced = True
        continue
    if "下一步 R033：官网安装包跟上含 R031–R032；未开再下一轮。" in line:
        line = line.replace(
            "下一步 R033：官网安装包跟上含 R031–R032；未开再下一轮。",
            "R033 已交付（Windows 接受协助端补「结束这次协助」，见 `docs/round-033.md`）；下一步 R034：官网安装包跟上；未开再下一轮。",
        )
    if "R033 ready 跟上 R031–R032" in line:
        line = line.replace("R033 ready 跟上 R031–R032", "R034 ready 跟上 R031–R033")
    out.append(line)
p.write_text("".join(out), encoding="utf-8", newline="\n")
print("replaced", replaced)

p34 = Path("docs/round-034.md")
t = p34.read_text(encoding="utf-8")
t = t.replace("（R032）的 HEAD", "（R033）的 HEAD")
t = t.replace("还拿不到这两轮改动", "还拿不到 R031–R033 改动")
p34.write_text(t, encoding="utf-8", newline="\n")

# verify round-033 is end-assist
head = Path("docs/round-033.md").read_text(encoding="utf-8").splitlines()[0]
print("r033 title:", head)
print("r034 title:", p34.read_text(encoding="utf-8").splitlines()[0])
for i,l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R033" in l or "R034" in l:
        if l.startswith("| R0") or "下一步" in l or "R034 ready" in l or "R033 已交付" in l:
            print(f"{i}:{l[:140]}")
