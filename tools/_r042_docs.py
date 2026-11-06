from pathlib import Path
import re

facts = {}
for line in Path("artifacts/_r042_facts.txt").read_text(encoding="utf-8").splitlines():
    if "=" in line:
        k, v = line.split("=", 1)
        facts[k] = v

pack = facts["PACK_HEAD"]
win_b = int(facts["WIN_BYTES"])
lin_b = int(facts["LIN_BYTES"])

p = Path("docs/round-042.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**wip**", "> 状态：**delivered**", 1)

delivery = f"""

## 交付说明（客户端出包 · 2026-11-06 Asia/Shanghai）

### 出包 HEAD
- `{pack}` — `docs(R041): 产品验收通过并开 R042（官网包跟上含 R041）`
- 祖先确认：`git merge-base --is-ancestor aeafeeb HEAD` / `5c276ec` 均为真（含 R041 功能及 R039）。

### Windows
| 项 | 值 |
|----|-----|
| 路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\installer\\RemoteAssist-Setup-0.1.0.exe` |
| 字节 | {win_b:,} |
| SHA256 | `{facts["WIN_SHA256"]}` |
| mtime | {facts["WIN_MTIME"]} |
| 命令 | `scripts\\build-installer.ps1` |

### Linux（framework-dependent，非 SC）
| 项 | 值 |
|----|-----|
| 本地路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `queqiao-linux-x64.tar.gz`（去掉 `-fd`） |
| 字节 | {lin_b:,} |
| SHA256 | `{facts["LIN_SHA256"]}` |
| mtime | {facts["LIN_MTIME"]} |
| 命令 | `scripts\\publish-agent-avalonia.ps1 -Runtimes linux-x64` |

包内：`RemoteAssist.Agent.Avalonia` + `运行说明.txt`；`RemoteAssist.Agent.Avalonia.runtimeconfig.json` → `Microsoft.NETCore.App` 8.0.0；无 hostfxr/coreclr 等 SC 痕迹。

### 对照 R040（上一正式公网包）
| | R040 | 本轮 R042 |
|--|------|-----------|
| Windows 字节 | 75,585,615 | **{win_b:,}** |
| Windows SHA256 | `C403FA6D…2104` | `{facts["WIN_SHA256"][:8]}…{facts["WIN_SHA256"][-4:]}` |
| Linux 字节 | 8,005,149 | **{lin_b:,}** |
| Linux SHA256 | `A2F1E187…2467` | `{facts["LIN_SHA256"][:8]}…{facts["LIN_SHA256"][-4:]}` |

字节与 SHA256 **均不同**（代码已含 R041）。

### 未做 / 确认
- 未改业务代码 / UI / 文案；未改 InstallInstructions / Skill / EndAssistCopy / R031–R041 已验收文案 / trust-levels「才问你」
- 未打 Linux SC；未把 macOS 当正式可下；未改 site/ / relay/；未上生产机；未自己上公网；未开下一轮
- 包已就绪，交给服务端替换公网两个下载文件
"""

if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-042 OK")
else:
    print("delivery exists")

rp = Path("docs/roadmap.md")
text = rp.read_text(encoding="utf-8")
out = []
for line in text.splitlines(True):
    if line.startswith("| R042 |"):
        line = "| R042 | 高频：官网安装包跟上含 R041 的客户端；打磨：首页旁注不改、补公网下载截图 | 鹊桥-客户端出包 / 鹊桥-服务端上架 | **delivered** | `docs/round-042.md` |\n"
    out.append(line)
text = "".join(out)

# open banner
text = text.replace(
    "> R041 已产品验收（`aeafeeb`，2026-11-06）；R042 已开（见 `round-042.md`）。",
    "> R041 已产品验收（`aeafeeb`，2026-11-06）；R042 客户端已出包（含 R041，见 `docs/round-042.md`）；服务端待上架。",
    1,
)

# scene footer
if "R042 客户端已出包" not in text or "服务端待上架" not in text:
    old = "R041 已交付（提议四态 + 去掉 PAIR_OK，见 `docs/round-041.md`）；未开下一轮。"
    # may already be product acceptance form
    if "R041 已产品验收" in text and "R042 客户端已出包" not in text.split("场景闭环")[-1] if "场景闭环" in text else True:
        pass
    m = re.search(r"R041 已产品验收（`aeafeeb`，2026-11-06）；[^\n]*", text)
    if m and "R042 客户端已出包" not in m.group(0):
        # might be the banner we already replaced
        pass
    # scene line often ends with R041 已交付 or R041 已产品验收；R042 已开
    scene = re.search(r"R041 已产品验收[^；\n]*；R042[^\n]*", text)
    if scene and "客户端已出包" not in scene.group(0):
        text = text.replace(scene.group(0), "R041 已产品验收（`aeafeeb`，2026-11-06）；R042 客户端已出包（含 R041，见 `docs/round-042.md`）；服务端待上架；未开下一轮。", 1)
        print("scene patched")
    elif "R042 客户端已出包" not in text:
        # try R041 已交付 footer
        old2 = "R041 已交付（提议四态 + 去掉 PAIR_OK，见 `docs/round-041.md`）；未开下一轮。"
        if old2 in text:
            text = text.replace(old2, "R041 已产品验收；R042 客户端已出包（含 R041，见 `docs/round-042.md`）；服务端待上架；未开下一轮。", 1)
            print("delivered footer patched")
        else:
            i = text.rfind("R042")
            print("FOOTER_CTX", repr(text[i:i+180] if i>=0 else "none"))

rp.write_text(text, encoding="utf-8", newline="\n")
print("roadmap written")
for i, l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R042" in l and (l.startswith("| R042") or "出包" in l or "已开" in l or "R041 已产品" in l):
        print(f"{i}:{l[:220]}")
