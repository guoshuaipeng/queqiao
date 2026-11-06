from pathlib import Path
import hashlib, re

shots = sorted(Path("docs/screenshots").glob("r041-*.png"))
rows = []
for p in shots:
    h = hashlib.md5(p.read_bytes()).hexdigest().upper()
    rows.append((p.as_posix().replace("\\","/"), h, p.stat().st_size))
print("SHOTS", len(rows), "unique", len({r[1] for r in rows}))

descriptions = {
    "r041-helper-connected-no-pair-ok.png": ("Win", "刚连上；结果区人话，无 PAIR_OK"),
    "r041-helper-propose-waiting.png": ("Win", "正在等对方在本机确认…"),
    "r041-helper-propose-allowed.png": ("Win", "对方已允许，改动已执行"),
    "r041-helper-propose-denied.png": ("Win", "已拒绝，未做任何改动"),
    "r041-helper-propose-timed-out.png": ("Win", "确认超时，未做任何改动"),
    "r041-avalonia-helper-connected-no-pair-ok.png": ("Avalonia", "刚连上；结果区人话，无 PAIR_OK"),
    "r041-avalonia-helper-propose-waiting.png": ("Avalonia", "正在等对方在本机确认…"),
    "r041-avalonia-helper-propose-allowed.png": ("Avalonia", "对方已允许，改动已执行"),
    "r041-avalonia-helper-propose-denied.png": ("Avalonia", "已拒绝，未做任何改动"),
    "r041-avalonia-helper-propose-timed-out.png": ("Avalonia", "确认超时，未做任何改动"),
}

shot_lines = []
for path, h, n in rows:
    name = Path(path).name
    plat, desc = descriptions.get(name, ("?", "?"))
    shot_lines.append(f"| `{path}` | {plat} | {desc} | {h} | {n} |")

p = Path("docs/round-041.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**wip**", "> 状态：**delivered**", 1)
delivery = f"""

## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- 新增 `ProposeChangeCopy`：`WaitingStatus` / `AllowedStatus` / `ConnectedResultHint`；`FailureStatus(deniedByUser, error)`（DeniedByUser 时优先非空 Error，超时不再盖成拒绝句）
- Windows `Helper.UI.MainForm` 与 Avalonia `HelperWindow`：`RunToolAsync` 对 `RunCommand` 显示等待/允许人话；失败走 `FailureStatus`；刚连上结果区改为 `ConnectedResultHint`（去掉 `PAIR_OK`）
- 拒绝/超时原文仍用 `WriteConfirmCopy.DeniedMessage` / `TimedOutMessage`（未改字）
- 未改五键顺序/显隐、R035–R037、R031、EndAssistCopy / InstallInstructions / Skill / 「才问你」；未改中继/官网包；未开下一轮

### 常量
| 常量 | 取值 |
|------|------|
| `ProposeChangeCopy.WaitingStatus` | 正在等对方在本机确认… |
| `ProposeChangeCopy.AllowedStatus` | 对方已允许，改动已执行 |
| `WriteConfirmCopy.DeniedMessage` | 已拒绝，未做任何改动（未改） |
| `WriteConfirmCopy.TimedOutMessage` | 确认超时，未做任何改动（未改） |
| `ProposeChangeCopy.ConnectedResultHint` | 已连上。建议先点「查看正在运行的程序」。 |

### 测试
- `RemoteAssist.Protocol.Tests`：**196** 通过（+4 `ProposeChangeCopyTests`）

### 截图（均为 **ui-shot**，非真实本机确认流）
| 文件 | 平台 | 显示 | MD5 | 字节 |
|------|------|------|-----|------|
{chr(10).join(shot_lines)}

亦复制到 `%USERPROFILE%\\Downloads\\r041-shots\\`（`C:\\Users\\user\\Downloads\\r041-shots\\`）。十张 MD5 互不相同；未覆盖 r022–r040。

### 缺口 / 披露
- 十张均为 ui-shot 注入状态，非真实中继配对与对方点允许/拒绝/超时的实拍。
- Windows 超时帧控件校验确认主状态为超时句且不含拒绝句。
"""
if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-041 OK")
else:
    print("delivery exists")

rp = Path("docs/roadmap.md")
text = rp.read_text(encoding="utf-8")
out = []
for line in text.splitlines(True):
    if line.startswith("| R041 |"):
        line = "| R041 | 高频：S004「提议一项改动」等待/结果四态人话；打磨：连上后结果区去掉 PAIR_OK | 鹊桥-客户端 | **delivered** | `docs/round-041.md` |\n"
    out.append(line)
text = "".join(out)
if "R041 已交付" not in text:
    if "R041 已开：" in text:
        text = text.replace(
            "> R041 已开：见 `docs/round-041.md`（依据 `docs/scene-s004-propose-wait.md` + `docs/quality-after-connect-status.md`）。",
            "> R041 已交付：见 `docs/round-041.md`。",
            1,
        )
    # scene footer
    old = "R040 客户端已出包（含 R039，见 `docs/round-040.md`）；服务端待上架；未开下一轮。"
    # may already say R040 已产品验收
    if "下一步 R041" in text or "R041 已开" in text:
        text = re.sub(
            r"R041 已开[^。]*。",
            "R041 已交付（提议四态 + 去掉 PAIR_OK，见 `docs/round-041.md`）；未开下一轮。",
            text,
            count=1,
        )
    if re.search(r"R040 已产品验收[^；]*；", text) and "R041 已交付" not in text:
        text = re.sub(
            r"(R040 已产品验收[^；]*；)([^\\n]*)",
            r"\1R041 已交付（提议四态 + 去掉 PAIR_OK，见 `docs/round-041.md`）；未开下一轮。",
            text,
            count=1,
        )
    if "R041 已交付" not in text:
        i = text.rfind("R041")
        print("FOOTER", repr(text[i:i+160] if i>=0 else text[-200:]))
rp.write_text(text, encoding="utf-8", newline="\n")
print("roadmap OK")
for i,l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R041" in l and (l.startswith("| R041") or "交付" in l or "已开" in l or "下一步" in l):
        print(f"{i}:{l[:200]}")
