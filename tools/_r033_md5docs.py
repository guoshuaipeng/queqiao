from pathlib import Path
p = Path("docs/round-033.md")
t = p.read_text(encoding="utf-8")
repls = [
    ("7FB53C4A3C505001F71142E6897468B5", "AB25E54689FF5D95ACB5535C4736D045"),
    ("A8EB8FDABB0AEA852E8E6365A3962B4B", "202CD344237A3171A97D93AD112793D3"),
    ("F371996DD67F37CEEE4E7D489860C4CB", "AE8035C234B5DACC4B71EDBAC49AFD5A"),
]
for a,b in repls:
    t = t.replace(a, b)
p.write_text(t, encoding="utf-8", newline="\n")
print("md5 updated")
