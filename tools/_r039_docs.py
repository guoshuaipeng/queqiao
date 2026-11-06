from pathlib import Path
import re

p = Path("docs/round-039.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**ready**", "> 状态：**delivered**", 1)
delivery = """

## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- Avalonia `HelperWindow`：`body` 子控件顺序改为 `pairCard` → `_toolCard` → `accountCard` → `mcpCard`（R039：常用操作紧挨主状态下方，账号 / MCP 之前）
- `_toolCard.IsVisible` 改为显式调用 `HelperSessionCopy.ShowCommonTools(connected)`（规则不变）
- 五键文案改为读 `HelperSessionCopy.CommonToolButtonLabels`（顺序/原文锁定）；未改 Windows Helper.UI 布局与账号区
- 未改 R035 三句主状态 / R036 按钮文案 / R037 粘贴提示原文与显隐；未改 EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」；未改 relay/site；未换公网包；未开下一轮

### 测试
- `RemoteAssist.Protocol.Tests`：**192** 通过（新增 `CommonToolButtonLabels_OrderLocked`）

### 截图（均为 **ui-shot**，非真连中继配对）
| 文件 | 平台 | 显示 | MD5 | 字节 |
|------|------|------|-----|------|
| `docs/screenshots/r039-avalonia-helper-connected-tools-first-screen.png` | Avalonia | 同屏「已连接对方电脑」+ 灰禁「已连接」+ 五个常用操作 | 2B8444538D7307AEEE79BFD255F84E6B | 60202 |
| `docs/screenshots/r039-avalonia-helper-peer-ended-tools-hidden.png` | Avalonia | 「对方已结束这次协助。」；五键隐藏 | A5D5B996AC38C3AD417A2D453E5E5518 | 58388 |
| `docs/screenshots/r039-avalonia-helper-self-disconnect-tools-hidden.png` | Avalonia | 「状态：未连接」；五键隐藏 | 991BF347449A1D5A885A0578509B2916 | 57641 |
| `docs/screenshots/r039-helper-connected-tools-visible.png` | Win | 已连上五键仍可见（确认未回归） | 4823519DDC78D758059472703A2A6ABD | 45077 |

亦复制到 `%USERPROFILE%\\Downloads\\r039-shots\\`（`C:\\Users\\user\\Downloads\\r039-shots\\`）。

### 缺口 / 披露
- 四张均为 ui-shot。
- Avalonia 对方结束 / 自己断开两张与对应 `r037-avalonia-*-hint-back.png` **MD5 相同**（那些帧五键本就隐藏）。
- Windows 已连上张与 `r037-helper-connected-no-paste-hint.png` **MD5 相同**（Win 布局未改，五键本就在首屏）。
- Avalonia 已连上张 MD5 **不同于** r037（可证五键进入首屏）。r039 四张 MD5 互不相同。未覆盖 r022–r038。
"""
if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-039 OK")
else:
    print("delivery exists")

rp = Path("docs/roadmap.md")
text = rp.read_text(encoding="utf-8")
lines = []
for line in text.splitlines(True):
    if line.startswith("| R039 |"):
        line = "| R039 | 高频：Avalonia 去协助端连上后五个常用操作出现在首屏；打磨：未连上时五键仍隐藏 | 鹊桥-客户端 | **delivered** | `docs/round-039.md` |\n"
    lines.append(line)
text = "".join(lines)
if "下一步 R039：" in text:
    text = re.sub(
        r"下一步 R039：[^；]*；未开再下一轮。",
        "R039 已交付（Avalonia 连上后五键首屏，见 `docs/round-039.md`）；未开下一轮。",
        text,
        count=1,
    )
elif "R038 客户端已出包" in text and "R039 已交付" not in text:
    text = text.replace(
        "R038 客户端已出包（含 R035–R037，见 `docs/round-038.md`）；服务端待上架；未开下一轮。",
        "R038 已产品验收；R039 已交付（Avalonia 连上后五键首屏，见 `docs/round-039.md`）；未开下一轮。",
        1,
    )
# Also handle product acceptance of R038 footer
if "R039 已交付" not in text:
    m = re.search(r"下一步 R039：[^。]*。", text)
    if m:
        text = text[:m.start()] + "R039 已交付（Avalonia 连上后五键首屏，见 `docs/round-039.md`）；未开下一轮。" + text[m.end():]
    else:
        m = re.search(r"R038 已产品验收[^；]*；下一步 R039：[^；]*；未开再下一轮。", text)
        if m:
            text = text[:m.start()] + "R038 已产品验收；R039 已交付（Avalonia 连上后五键首屏，见 `docs/round-039.md`）；未开下一轮。" + text[m.end():]
        else:
            idx = text.rfind("R039")
            print("FOOTER CONTEXT:", repr(text[idx:idx+220] if idx>=0 else text[-250:]))
rp.write_text(text, encoding="utf-8", newline="\n")
print("roadmap OK")
for i,l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R039" in l and (l.startswith("| R039") or "交付" in l or "下一步" in l):
        print(f"{i}:{l[:200]}")
