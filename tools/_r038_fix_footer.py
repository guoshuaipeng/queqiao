from pathlib import Path
rp = Path("docs/roadmap.md")
text = rp.read_text(encoding="utf-8")
if "R038 客户端已出包" not in text:
    # Find the long scene line and append/replace near end
    # Common pattern after product opened R038:
    markers = [
        "下一步 R038：官网安装包跟上含 R035–R037 的客户端；未开再下一轮。",
        "下一步 R038：官网安装包跟上含 R035-R037 的客户端；未开再下一轮。",
        "R037 已产品验收（`0405491`，2026-11-06）；下一步 R038：官网安装包跟上含 R035–R037 的客户端；未开再下一轮。",
        "R037 已产品验收（`0405491`，2026-11-06）；下一步 R038：官网安装包跟上含 R035-R037 的客户端；未开再下一轮。",
    ]
    replaced = False
    for m in markers:
        if m in text:
            text = text.replace(m, "R037 已产品验收；R038 客户端已出包（含 R035–R037，见 `docs/round-038.md`）；服务端待上架；未开下一轮。", 1)
            replaced = True
            break
    if not replaced:
        # fuzzy: find 下一步 R038
        import re
        m = re.search(r"下一步 R038：[^。]*。", text)
        if m:
            text = text[:m.start()] + "R037 已产品验收；R038 客户端已出包（含 R035–R037，见 `docs/round-038.md`）；服务端待上架；未开下一轮。" + text[m.end():]
            replaced = True
            print("fuzzy replaced:", m.group(0)[:120])
        else:
            # try R037 已产品验收 ... 未开再下一轮
            m = re.search(r"R037 已产品验收[^；]*；下一步 R038：[^；]*；未开再下一轮。", text)
            if m:
                text = text[:m.start()] + "R037 已产品验收；R038 客户端已出包（含 R035–R037，见 `docs/round-038.md`）；服务端待上架；未开下一轮。" + text[m.end():]
                replaced = True
                print("fuzzy2:", m.group(0)[:160])
            else:
                # dump tail snippet containing R037
                idx = text.rfind("R037")
                print("NO REPLACE; context:", repr(text[idx:idx+200] if idx>=0 else text[-300:]))
    rp.write_text(text, encoding="utf-8", newline="\n")
    print("replaced=", replaced)
else:
    print("footer already has R038 客户端已出包")

# Align round status: packaging-only rounds in past used delivered; keep delivered for round-038
# Align roadmap cell: use delivered for consistency with round status OR keep client-packaged
# Prefer matching R034 table which used delivered after client pack. Change client-packaged -> delivered for table consistency with round md.
t = rp.read_text(encoding="utf-8")
t2 = t.replace("| **client-packaged** | `docs/round-038.md` |", "| **delivered** | `docs/round-038.md` |", 1)
# Actually R034 after client alone might have said delivered while server pending - OK
# But parent may prefer clarity. Looking at task "只更新交付说明" - client-packaged is honest. Round md says delivered which is inconsistent.
# Make round-038 status match: use wording from R034 packaging - they said delivered.
p = Path("docs/round-038.md")
rt = p.read_text(encoding="utf-8")
# keep delivered
rp.write_text(t2 if "**client-packaged**" in t else t, encoding="utf-8", newline="\n")
print("table status:", "delivered" if "| **delivered** | `docs/round-038.md` |" in rp.read_text(encoding="utf-8") else "other")
idx = rp.read_text(encoding="utf-8").rfind("R038")
print("footer snippet:", rp.read_text(encoding="utf-8")[idx:idx+180] if idx>0 else "no")
# better find last R038 client
for i,l in enumerate(rp.read_text(encoding="utf-8").splitlines()):
    if "R038 客户端" in l or (l.startswith("| R038") ):
        print(i, l[:200])
