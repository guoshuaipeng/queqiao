# -*- coding: utf-8 -*-
"""Align R032 accept + R033 package-sync per product task."""
from pathlib import Path
import subprocess

def write_utf8(path, text):
    if not text.endswith("\n"):
        text += "\n"
    Path(path).write_text(text, encoding="utf-8", newline="\n")

# --- round-032: restore from HEAD then replace product-accept with exact task text ---
r32 = subprocess.check_output(
    ["git", "show", "HEAD:docs/round-032.md"], text=True, encoding="utf-8"
)
# ensure status
r32 = r32.replace("> 状态：**delivered**", "> 状态：**done（产品已验收）**")
# strip any existing 产品验收 section(s) from first heading to EOF
marker = "\n## 产品验收（鹊桥-产品"
idx = r32.find(marker)
if idx == -1:
    # maybe no leading newline
    idx = r32.find("## 产品验收（鹊桥-产品")
    if idx != -1:
        r32 = r32[:idx].rstrip() + "\n"
else:
    r32 = r32[:idx].rstrip() + "\n"

accept = """
## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r032-*`、`r032-balloon-copy-proof.txt` 与源码独立验收通过（`b78fc1e`）。

独立核对：

- `TrayStayCopy.BalloonDefault` / `BalloonWithReturnHome` 沿用原有两句，无第三种说法；`HiddenToTrayFeedback` =「已隐藏到托盘。双击托盘图标可重新打开。」
- Windows 接受协助端底栏默认空白：`docs/screenshots/r032-accept-statusbar-empty.png` 底栏无「就绪。关闭窗口后会继续在托盘运行。」；「复制码+密码」仍是唯一实心主按钮；R031 主状态「已连上网络，可以发给对方了」仍在。
- `docs/screenshots/r032-accept-hidden-to-tray-feedback.png`：底栏一次性「已隐藏到托盘。双击托盘图标可重新打开。」（ui-shot 注入同款文案，窗仍可见，规格允许）。
- 启动气泡未能真拍 NotifyIcon；以 `TrayStayCopy` 常量 + `AgentTrayContext` 接线 + `r032-balloon-copy-proof.txt` 证明未改气泡口径、未另造第三种说法。
- 未改 Avalonia 关窗提示、`InstallInstructions`、Skill/MCP、`docs/trust-levels.md`「才问你」。未覆盖 `r022-*`～`r031-*`。

缺口：未实拍启动气泡本体；关窗反馈图为 ui-shot 注入。公网安装包仍为 R030（交 R033）。

已开 R033（官网 Windows / Linux 安装包跟上含 R031–R032 的客户端；替换后首页旁注与导语不回退；不改中继/AI-SSH-Pro）。
"""
write_utf8("docs/round-032.md", r32 + accept)
assert Path("docs/round-032.md").read_text(encoding="utf-8").count("## 产品验收") == 1
print("round-032 fixed")

# --- round-033 already written with package sync; verify ---
r33 = Path("docs/round-033.md").read_text(encoding="utf-8")
assert "状态：**ready**" in r33
assert "官网安装包跟上含 R031–R032" in r33
print("round-033 ok")

# --- roadmap ---
rm = Path("docs/roadmap.md").read_text(encoding="utf-8")
old_r033 = "| R033 | 高频：Windows 接受协助端补「结束这次协助」；打磨：结束前确认对方将连不上 | 鹊桥-客户端 | **ready** | `docs/round-033.md` |"
new_r033 = "| R033 | 高频：官网安装包跟上含 R031–R032 的客户端；打磨：替换后首页旁注与导语不回退 | 鹊桥-客户端 / 鹊桥-服务端 | **ready** | `docs/round-033.md` |"
assert old_r033 in rm, "old R033 row missing: " + repr([l for l in rm.splitlines() if "R033" in l][:2])
rm = rm.replace(old_r033, new_r033, 1)

# R032 row should already be done
assert "| R032 |" in rm and "**done（产品已验收）**" in [l for l in rm.splitlines() if "| R032 |" in l][0]

old_para = "R032 已产品验收（`b78fc1e`，2026-11-06）；下一步 R033：Windows 接受协助端补「结束这次协助」；未开再下一轮。"
new_para = "R032 已产品验收（`b78fc1e`）；下一步 R033：官网安装包跟上含 R031–R032；未开再下一轮。"
if old_para not in rm:
    # try alternate endings
    cands = [l for l in rm.splitlines() if "R032 已" in l and "下一步 R033" in l]
    print("PARA CAND:", cands[0][-120:] if cands else "NONE")
    raise SystemExit("para not found")
rm = rm.replace(old_para, new_para, 1)

# install package row
old_cell = "**done（R019–R030）**"
# may already have been changed
cells = []
for l in rm.splitlines():
    if "安装包下载（Windows / Linux 真实文件" in l:
        cells.append(l)
print("INSTALL ROW:", cells[0][-150:] if cells else "NONE")
if old_cell in rm:
    count = rm.count(old_cell)
    assert count == 1, count
    rm = rm.replace(old_cell, "**done（R019–R030）；R033 ready 跟上 R031–R032**", 1)
elif "R033 ready 跟上 R031–R032" in rm:
    print("install row already updated")
else:
    # maybe different form from concurrent commit
    row = cells[0]
    if "R019–R030" in row and "R033" not in row:
        new_row = row.replace("**done（R019–R030）**", "**done（R019–R030）；R033 ready 跟上 R031–R032**", 1)
        rm = rm.replace(row, new_row, 1)
    else:
        raise SystemExit("cannot update install row")
write_utf8("docs/roadmap.md", rm)
print("roadmap fixed")

# --- PRODUCT ---
prod = Path("docs/PRODUCT.md").read_text(encoding="utf-8")
# concurrent version may already say R032 accepted with wrong next
replacements = [
    (
        "R032 关闭窗口后留在托盘的说明只留一处已于 2026-11-06 产品对照截图验收通过（`b78fc1e`）。下一步 R033：Windows 接受协助端补「结束这次协助」。",
        "R032 已于 2026-11-06 产品对照截图验收通过（`b78fc1e`）。下一步 R033：官网安装包跟上含 R031–R032 的客户端。",
    ),
    (
        "下一步 R032：关闭窗口后留在托盘的说明只留一处。",
        "R032 已于 2026-11-06 产品对照截图验收通过（`b78fc1e`）。下一步 R033：官网安装包跟上含 R031–R032 的客户端。",
    ),
]
ok = False
for a, b in replacements:
    if a in prod:
        prod = prod.replace(a, b, 1)
        ok = True
        break
if not ok:
    # show ending
    for l in prod.splitlines():
        if "R032" in l or "下一步 R03" in l:
            print("PROD LINE END:", l[-180:])
    raise SystemExit("PRODUCT pattern missing")
write_utf8("docs/PRODUCT.md", prod)
print("PRODUCT fixed")

# --- retro: replace wrong R032 accept block with exact task text ---
retro = Path("docs/retro.md").read_text(encoding="utf-8")
marker = "## R032 产品验收（2026-11-06 Asia/Shanghai）"
idx = retro.find(marker)
if idx == -1:
    # append
    block = """
## R032 产品验收（2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r032-*` 与源码验收通过（`b78fc1e`）：

- Windows 接受协助端底栏默认不再写「就绪。关闭窗口后会继续在托盘运行。」；启动气泡口径保留；关窗后底栏可出现一次性「已隐藏到托盘。双击托盘图标可重新打开。」
- 缺口：未实拍 NotifyIcon 气泡；关窗反馈图为 ui-shot；公网包仍为 R030。

已开 R033（官网安装包跟上含 R031–R032 的客户端；替换后首页旁注与导语不回退；客户端出包，服务端替换上架；不改中继/AI-SSH-Pro）。
"""
    if not retro.endswith("\n"):
        retro += "\n"
    retro = retro + block
else:
    # replace from marker to EOF (assuming it's the last section)
    block = """## R032 产品验收（2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r032-*` 与源码验收通过（`b78fc1e`）：

- Windows 接受协助端底栏默认不再写「就绪。关闭窗口后会继续在托盘运行。」；启动气泡口径保留；关窗后底栏可出现一次性「已隐藏到托盘。双击托盘图标可重新打开。」
- 缺口：未实拍 NotifyIcon 气泡；关窗反馈图为 ui-shot；公网包仍为 R030。

已开 R033（官网安装包跟上含 R031–R032 的客户端；替换后首页旁注与导语不回退；客户端出包，服务端替换上架；不改中继/AI-SSH-Pro）。
"""
    retro = retro[:idx].rstrip() + "\n\n" + block
write_utf8("docs/retro.md", retro)
print("retro fixed")

for f in ["docs/round-032.md","docs/round-033.md","docs/PRODUCT.md","docs/roadmap.md","docs/retro.md"]:
    raw = Path(f).read_bytes()[:3]
    print(f, "BOM" if raw == b"\\xef\\xbb\\xbf" else "no-BOM")

# quick asserts
r32t = Path("docs/round-032.md").read_text(encoding="utf-8")
assert "状态：**done（产品已验收）**" in r32t
assert "公网安装包仍为 R030（交 R033）" in r32t
assert "结束这次协助" not in r32t.split("## 产品验收")[-1]
r33t = Path("docs/round-033.md").read_text(encoding="utf-8")
assert r33t.splitlines()[4] == "> 状态：**ready**"
rmt = Path("docs/roadmap.md").read_text(encoding="utf-8")
assert "官网安装包跟上含 R031–R032 的客户端" in [l for l in rmt.splitlines() if "| R033 |" in l][0]
assert "下一步 R033：官网安装包跟上含 R031–R032" in rmt
prodt = Path("docs/PRODUCT.md").read_text(encoding="utf-8")
assert "下一步 R033：官网安装包跟上含 R031–R032 的客户端。" in prodt
retrot = Path("docs/retro.md").read_text(encoding="utf-8")
assert "官网安装包跟上含 R031–R032 的客户端" in retrot.split("## R032 产品验收")[-1]
assert "结束这次协助" not in retrot.split("## R032 产品验收")[-1]
print("ALL ASSERTS OK")
