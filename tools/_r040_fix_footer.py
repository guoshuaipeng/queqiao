from pathlib import Path
p = Path("docs/roadmap.md")
t = p.read_text(encoding="utf-8")
old = "R040 官网安装包跟上含 R039（wip，见 `round-040.md`）。"
new = "R040 客户端已出包（含 R039，见 `docs/round-040.md`）；服务端待上架；未开下一轮。"
if old in t:
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("fixed")
elif "R040 客户端已出包" in t:
    print("already fixed")
else:
    i = t.find("R040")
    while i >= 0:
        snippet = t[i:i+100]
        if "wip" in snippet or "出包" in snippet or "官网" in snippet:
            print(repr(snippet))
        i = t.find("R040", i+1)
