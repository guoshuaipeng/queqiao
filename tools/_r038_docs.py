from pathlib import Path
import re

p = Path("docs/round-038.md")
t = p.read_text(encoding="utf-8")
# Only mark client packaging delivered; server still pending — keep ready or say client-delivered?
# R034 used delivered after both; for A-only client often wrote 交付说明 with status still ready or partial.
# Looking at R034 client docs commit pattern from earlier - they set delivered when client finished packaging section.
# Spec says update round-038 delivery; product/server will accept later. R030/R034 client docs often set status delivered for packaging then server adds. Or keep ready until full.
# Parent said: 只更新 docs/round-038.md 和若需要的 docs/roadmap.md 交付说明
# For R034 client packing, they updated delivery while status went delivered after packaging docs.
# I'll add client delivery section and set status to note client packaging done. Looking at R034:
# Actually for split A/B, round often stays ready until server, OR client marks partial.
# Check R030 pattern... Parent says 出包后只更新交付说明. I'll add ## 交付说明（客户端出包） and change status to something like delivered for client portion, or keep ready.
# R034 client docs said 状态：**delivered** after packaging before server? From earlier extract, round-034 had client delivery then server. I'll set **delivered** for packaging docs like R034 client did, but roadmap might say client delivered / server ready.
# Safer: keep status **ready** until product acceptance of full round, but add 客户端出包交付. Or mirror R034.
# From R034 extract: client wrote delivery with paths; later product acceptance. I'll change to delivered for client packaging like prior packaging rounds, and roadmap note 客户端已出包 / 服务端待上架.

t = t.replace("> 状态：**ready**", "> 状态：**client-packaged**", 1)
# if that status word is odd, use delivered for client section only
# Looking at prior rounds R022/R024/R030/R034 - they used **delivered** after client packaging docs.
t = t.replace("> 状态：**client-packaged**", "> 状态：**delivered**", 1)

delivery = """

## 交付说明（客户端出包 · 2026-11-06 Asia/Shanghai）

### 出包 HEAD
- `0efc3c4f2e8e254f7bf8834f69252509c2dc970a` — `docs(R037): 产品验收通过并开 R038（官网安装包跟上含 R035-R037）`
- 祖先确认：`git merge-base --is-ancestor 0405491 HEAD` / `d24fb44` / `b707804` 均为真（含 R037 功能、R036、R035）。

### Windows
| 项 | 值 |
|----|-----|
| 路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\installer\\RemoteAssist-Setup-0.1.0.exe` |
| 字节 | 75,585,944 |
| SHA256 | `8B777AE324AB2528A434BB407018A6048708F10BF5E9220F2C5281B1E6866416` |
| mtime | 2026-11-06 03:40:40 Asia/Shanghai |
| 命令 | `scripts\\build-installer.ps1` |

### Linux（framework-dependent，非 SC）
| 项 | 值 |
|----|-----|
| 本地路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `queqiao-linux-x64.tar.gz`（去掉 `-fd`） |
| 字节 | 8,005,019 |
| SHA256 | `AB2DF29FE1606D94A270D98D358B11F086B636BD649E47BDE5C6999D81C2ABD2` |
| mtime | 2026-11-06 03:40:53 Asia/Shanghai |
| 命令 | `scripts\\publish-agent-avalonia.ps1 -Runtimes linux-x64` |

包内：`RemoteAssist.Agent.Avalonia` + `运行说明.txt`；`RemoteAssist.Agent.Avalonia.runtimeconfig.json` → `Microsoft.NETCore.App` 8.0.0；无 hostfxr/coreclr 等 SC 痕迹。

### 对照 R034（当前公网）
| | R034 | 本轮 R038 |
|--|------|-----------|
| Windows 字节 | 75,584,964 | **75,585,944** |
| Windows SHA256 | `64A32316…BCD0` | `8B777AE3…6416` |
| Linux 字节 | 8,002,690 | **8,005,019** |
| Linux SHA256 | `B20257AD…9108` | `AB2DF29F…ABD2` |

字节与 SHA256 **均不同**（代码已含 R035–R037）。

### 未做 / 确认
- 未改业务代码；未改 InstallInstructions / Skill / EndAssistCopy / R031–R037 已验收文案 / trust-levels「才问你」
- 未打 Linux SC；未把 macOS 当正式可下；未改 site/ / relay/；未上生产机；未自己上公网；未开下一轮
- 包已就绪，交给服务端替换公网两个下载文件
"""

if "## 交付说明" not in t:
    t = t.rstrip() + "\n" + delivery
    p.write_text(t, encoding="utf-8", newline="\n")
    print("round-038 OK")
else:
    print("round-038 already has delivery — skip overwrite")

rp = Path("docs/roadmap.md")
text = rp.read_text(encoding="utf-8")
lines = text.splitlines(True)
out = []
for line in lines:
    if line.startswith("| R038 |"):
        line = "| R038 | 高频：官网安装包跟上含 R035–R037 的客户端；打磨：首页旁注不改、补公网下载截图 | 鹊桥-客户端出包 / 鹊桥-服务端上架 | **client-packaged** | `docs/round-038.md` |\n"
    out.append(line)
text = "".join(out)
# footer
old_footer_bits = [
    "R037 已交付（去协助端已连上后不再提示「粘贴后点「连接」」，见 `docs/round-037.md`）；未开下一轮。",
    "下一步 R038：",
]
# After product acceptance of R037 the footer may say R037 已产品验收 and 下一步 R038
if "下一步 R038：" in text:
    text = re.sub(
        r"下一步 R038：[^；]*；未开再下一轮。",
        "R038 客户端已出包（含 R035–R037，见 `docs/round-038.md`）；服务端待上架；未开下一轮。",
        text,
        count=1,
    )
elif "R037 已产品验收" in text and "R038 客户端已出包" not in text:
    text = re.sub(
        r"R037 已产品验收（`[^`]+`，2026-11-06）；下一步 R038：[^；]*；未开再下一轮。",
        "R037 已产品验收（`0efc3c4` 开轮文档祖先）；R038 客户端已出包（含 R035–R037，见 `docs/round-038.md`）；服务端待上架；未开下一轮。",
        text,
        count=1,
    )
elif "R037 已交付" in text and "R038 客户端已出包" not in text:
    text = text.replace(
        "R037 已交付（去协助端已连上后不再提示「粘贴后点「连接」」，见 `docs/round-037.md`）；未开下一轮。",
        "R037 已交付；R038 客户端已出包（含 R035–R037，见 `docs/round-038.md`）；服务端待上架；未开下一轮。",
        1,
    )
# Also handle product acceptance footer from 0efc3c4
if "R038 客户端已出包" not in text:
    # dump relevant tail for debug
    for i, l in enumerate(text.splitlines()):
        if "R037" in l or "R038" in l:
            if l.startswith("|") or "下一步" in l or "已交付" in l or "已产品" in l or "已出包" in l:
                print(f"LINE {i}: {l[:220]}")

rp.write_text(text, encoding="utf-8", newline="\n")
print("roadmap written")
for i, l in enumerate(Path("docs/roadmap.md").read_text(encoding="utf-8").splitlines()):
    if "R038" in l and (l.startswith("| R038") or "出包" in l or "下一步" in l or "服务端" in l):
        print(f"{i}:{l[:220]}")
