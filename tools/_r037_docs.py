from pathlib import Path
import re

p = Path("docs/round-037.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**ready**", "> 状态：**delivered**", 1)
delivery = """

## 交付说明（客户端 · 2026-11-06 Asia/Shanghai）

### 做了什么
- `HelperSessionCopy` 增补：`PasteThenConnectHint`=「粘贴后点「连接」」（原文锁定）、`ShowPasteThenConnectHint(sessionConnected)`（已连上 → false）
- Windows `Helper.UI.MainForm`：`SetConnectedUi` → `UpdatePasteThenConnectHintVisibility`；`RefreshPreview` 已连上（真连或 ui-shot「已连接」按钮）不再刷回该句
- Avalonia `HelperWindow`：同上；`RefreshPreview` 在 `_client` / `_uiShotConnected` 时不再刷回
- 未改 `ConnectedButtonText` / `ConnectButtonText` / R035 三句主状态 / R003 失败原因句
- 未改五个常用操作显隐、账号区「连接这台电脑」/ 免密灰「已连接」
- 未改 EndAssistCopy / R031 / InstallInstructions / Skill / trust-levels「才问你」；未改 relay/site；未换公网包；未开下一轮

### 粘贴提示显隐
| 情景 | 「粘贴后点「连接」」 | R035 主状态 | R036 主按钮 |
|------|-------------------|------------|------------|
| 已连上 | 隐藏 | 已连接对方电脑 | 不可点「已连接」 |
| 对方结束 | 再显示原文 | 对方已结束这次协助。 | 实心「连接」 |
| 自己断开 | 再显示原文 | 状态：未连接 | 实心「连接」 |
| 未连上 / 失败 | 显示原文 | （失败时「连接失败」等） | 实心「连接」 |

### 测试
- `RemoteAssist.Protocol.Tests`：**191** 通过（含 `PasteThenConnectHint_HiddenWhenConnected_ExactCopyUnchanged`）

### 截图（均为 **ui-shot**）
| 文件 | 平台 | 显示 | MD5 | 字节 |
|------|------|------|-----|------|
| `docs/screenshots/r037-helper-connected-no-paste-hint.png` | Win | 「已连接」灰禁 +「已连接对方电脑」；**不见**「粘贴后点「连接」」 | 4823519DDC78D758059472703A2A6ABD | 45077 |
| `docs/screenshots/r037-helper-peer-ended-hint-back.png` | Win | 「对方已结束这次协助。」+ 实心「连接」+ 粘贴提示再出现 | 4054F5B75E449DE032AF37C9CFB890E8 | 41071 |
| `docs/screenshots/r037-helper-self-disconnect-hint-back.png` | Win | 「状态：未连接」+ 实心「连接」+ 粘贴提示 | 46D5815D703A7AA50796A529B6EF2EAA | 40760 |
| `docs/screenshots/r037-avalonia-helper-connected-no-paste-hint.png` | Avalonia | 「已连接」灰禁；**不见**粘贴提示 | E9BA7E3CCCF466F10CB0FF60844408FC | 57587 |
| `docs/screenshots/r037-avalonia-helper-peer-ended-hint-back.png` | Avalonia | 对方结束句 + 实心「连接」+ 粘贴提示 | A5D5B996AC38C3AD417A2D453E5E5518 | 58388 |
| `docs/screenshots/r037-avalonia-helper-self-disconnect-hint-back.png` | Avalonia | 未连接句 + 实心「连接」+ 粘贴提示 | 991BF347449A1D5A885A0578509B2916 | 57641 |

亦复制到 `%USERPROFILE%\\Downloads\\r037-shots\\`（`C:\\Users\\user\\Downloads\\r037-shots\\`）。

### 缺口 / 披露
- 六张均为 **ui-shot**（非真连中继实拍）；Win 连上态用控件校验：按钮「已连接」/禁用/非绿，且可见文本不含粘贴提示原文。
- 对方结束 / 自己断开四张与对应 `r036-*-reconnect.png` **MD5 相同**（那些图里粘贴提示本来就在；本轮连上态两张 MD5 已与 r036 区分）。未覆盖任何 r022–r036 截图文件。
- r037 六张 MD5 互不相同。
"""
if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-037 OK")
else:
    print("round-037 already has delivery")

rp = Path("docs/roadmap.md")
text = rp.read_text(encoding="utf-8")
lines = text.splitlines(True)
out = []
for line in lines:
    if line.startswith("| R037 |"):
        line = "| R037 | 高频：去协助端已连上后不再显示「粘贴后点「连接」」；打磨：结束后这句提示再出现 | 鹊桥-客户端 | **delivered** | `docs/round-037.md` |\n"
    out.append(line)
text = "".join(out)
# footer: replace 下一步 R037 ...
if "下一步 R037：" in text:
    text = text.replace(
        "下一步 R037：去协助端已连上后不再提示点「连接」；未开再下一轮。",
        "R037 已交付（去协助端已连上后不再提示「粘贴后点「连接」」，见 `docs/round-037.md`）；未开下一轮。",
        1,
    )
elif "R036 已产品验收" in text and "R037 已交付" not in text:
    # try alternate footer from product acceptance line
    text = re.sub(
        r"下一步 R037：[^；]*；未开再下一轮。",
        "R037 已交付（去协助端已连上后不再提示「粘贴后点「连接」」，见 `docs/round-037.md`）；未开下一轮。",
        text,
        count=1,
    )
rp.write_text(text, encoding="utf-8", newline="\n")
print("roadmap OK")
for i, l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R037" in l:
        print(f"{i}:{l[:200]}")
