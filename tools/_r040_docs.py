from pathlib import Path
import re

facts = {}
for line in Path("artifacts/_r040_facts.txt").read_text(encoding="utf-8").splitlines():
    if "=" in line:
        k, v = line.split("=", 1)
        facts[k] = v

pack = facts["PACK_HEAD"]
win_b = int(facts["WIN_BYTES"])
lin_b = int(facts["LIN_BYTES"])

p = Path("docs/round-040.md")
t = p.read_text(encoding="utf-8")
t = t.replace("> 状态：**wip**", "> 状态：**delivered**", 1)
t = t.replace("> 状态：**ready**", "> 状态：**delivered**", 1)

delivery = f"""

## 交付说明（客户端出包 · 2026-11-06 Asia/Shanghai）

### 出包 HEAD
- `{pack}` — `docs: 收口 S004 为 R041 高频，PAIR_OK 为人话打磨`
- 祖先确认：`git merge-base --is-ancestor 5c276ec HEAD` / `0405491` / `d24fb44` / `b707804` 均为真（含 R039 功能及 R037/R036/R035）。

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

### 对照 R038（上一正式公网包）
| | R038 | 本轮 R040 |
|--|------|-----------|
| Windows 字节 | 75,585,944 | **{win_b:,}** |
| Windows SHA256 | `8B777AE3…6416` | `{facts["WIN_SHA256"][:8]}…{facts["WIN_SHA256"][-4:]}` |
| Linux 字节 | 8,005,019 | **{lin_b:,}** |
| Linux SHA256 | `AB2DF29F…ABD2` | `{facts["LIN_SHA256"][:8]}…{facts["LIN_SHA256"][-4:]}` |

字节与 SHA256 **均不同**（代码已含 R039）。

### 未做 / 确认
- 未改业务代码 / UI / 文案；未改 InstallInstructions / Skill / EndAssistCopy / R031–R039 已验收文案 / trust-levels「才问你」
- 未打 Linux SC；未把 macOS 当正式可下；未改 site/ / relay/；未上生产机；未自己上公网；未开下一轮
- 包已就绪，交给服务端替换公网两个下载文件
"""

if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-040 OK")
else:
    print("delivery exists — not overwriting")

rp = Path("docs/roadmap.md")
text = rp.read_text(encoding="utf-8")
out = []
for line in text.splitlines(True):
    if line.startswith("| R040 |"):
        line = "| R040 | 高频：官网安装包跟上含 R039 的客户端；打磨：首页旁注不改、补公网下载截图 | 鹊桥-客户端出包 / 鹊桥-服务端上架 | **delivered** | `docs/round-040.md` |\n"
    out.append(line)
text = "".join(out)

if "R040 客户端已出包" not in text and "R040 已交付" not in text:
    if "下一步 R040：" in text:
        text = re.sub(
            r"下一步 R040：[^；]*；未开再下一轮。",
            "R040 客户端已出包（含 R039，见 `docs/round-040.md`）；服务端待上架；未开下一轮。",
            text,
            count=1,
        )
    elif re.search(r"R039 已产品验收[^；]*；下一步 R040：", text):
        text = re.sub(
            r"R039 已产品验收[^；]*；下一步 R040：[^；]*；未开再下一轮。",
            "R039 已产品验收；R040 客户端已出包（含 R039，见 `docs/round-040.md`）；服务端待上架；未开下一轮。",
            text,
            count=1,
        )
    elif "R039 已交付" in text and "R040" in text:
        # footer may already mention R041 candidate — insert R040 client packaged before R041
        text2 = re.sub(
            r"(R039 已交付[^；]*；)(R041|下一步 R041|未开下一轮)",
            r"\1R040 客户端已出包（含 R039，见 `docs/round-040.md`）；服务端待上架；\2",
            text,
            count=1,
        )
        if text2 == text:
            # try product acceptance form
            text2 = re.sub(
                r"(R039 已产品验收[^；]*；)([^；]*R041|未开下一轮|下一步)",
                r"\1R040 客户端已出包（含 R039，见 `docs/round-040.md`）；服务端待上架；\2",
                text,
                count=1,
            )
        text = text2
    if "R040 客户端已出包" not in text:
        idx = text.rfind("R040")
        print("FOOTER_CTX:", repr(text[max(0,idx-80):idx+200]))

rp.write_text(text, encoding="utf-8", newline="\n")
print("roadmap written")
for i, l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R040" in l and (l.startswith("| R040") or "出包" in l or "交付" in l or "下一步" in l):
        print(f"{i}:{l[:220]}")
