from pathlib import Path
import re
p = Path("docs/round-043.md")
t = p.read_text(encoding="utf-8")
t = re.sub(
    r"> 状态：\*\*.*?\*\*",
    "> 状态：**blocked（完整安全载荷已接线并 push；架构已废止官方指纹文 `2d6c0b2`；暂不验收真实连接/联调）**",
    t,
    count=1,
)
old = """### 文档冲突披露（不覆盖）

本地存在 `e7d012c` / 工作区改动中的 `docs/tls-trust.md` 仍写「默认公网中继内置官方业务指纹」。该说法与产品 09:15 及本实现（每台 Agent 临时业务证）**冲突**。本提交**未修改/未覆盖** `docs/tls-trust.md`（检测到他人可能正在改），等待架构按产品拍板撤回修正。本轮实现以完整安全载荷为准，**不**接线不存在的官方指纹。"""
new = """### 文档冲突披露（已由架构纠正）

本安全载荷提交 `2a3e618` **未覆盖** `docs/tls-trust.md`（当时工作区有他人改动）。随后架构 `2d6c0b2` 已按产品 09:15 废止「内置官方指纹」，与本实现（Agent 临时业务证 → 载荷 `fp=`）一致。实现侧**从未**接线不存在的官方指纹常量。"""
if old in t:
    t = t.replace(old, new)
    print("conflict section updated")
else:
    print("conflict section NOT found; dumping nearby")
    i = t.find("文档冲突")
    print(repr(t[i:i+200] if i>=0 else "missing"))

# roadmap soften
rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
rt2 = rt.replace(
    "R043 blocked（完整安全载荷已接线；tls-trust 官方指纹文冲突待架构改；不验收真连）",
    "R043 blocked（完整安全载荷已接线 `2a3e618`；信任模型文 `2d6c0b2` 已对齐；不验收真连）",
    1,
)
if rt2 != rt:
    rp.write_text(rt2, encoding="utf-8", newline="\n")
    print("roadmap OK")
else:
    print("roadmap no hit")

p.write_text(t, encoding="utf-8", newline="\n")
print("round done")
