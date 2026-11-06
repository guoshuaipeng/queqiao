from pathlib import Path
import re

delivery = '''
## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 基于已推送 HEAD `c7dc792`（含 R029 `5909d99`，覆盖 R027–R029 Skill 入口）打出 Windows Inno 与 Linux framework-dependent 包；**未改**业务代码与已验收安装说明 / Skill 常量 / trust-levels「才问你」。
- Linux 产物 DLL 已核对含 Skill 说明正文。
- **未改** `site/`、未上生产机；公网上架交鹊桥-服务端。未把 macOS 当正式可下；Linux 为 FD 非 SC。

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\installer\\RemoteAssist-Setup-0.1.0.exe` | `E:\\IdeaProjects\\remote-assist\\artifacts\\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,582,164 | 8,001,712 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-06 02:09:18 Asia/Shanghai | 2026-11-06 02:08:54 Asia/Shanghai |
| SHA256 | `EEA41DA2474DED60C3DA5C72F718ADEFC2C56C5B0AACF53757E719F530E9C1E5` | `D659BF24602C3ACEF6F896071FA3A9B17EC7791D23B10C393E7B3A8CFB69080D` |
| 构建命令 | `scripts\\build-installer.ps1` | `scripts\\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

出包 HEAD：`c7dc792f8a8676759e85535512c70ae4fb36455e` — `docs(R029): 产品验收通过并开 R030（官网安装包跟上含 Skill 的客户端）`（祖先含 `5909d99`）。

对照 R026 旧包：Windows 75,580,750 / `2A8EE24D…9E26` → 新 `EEA41DA2…C1E5`；Linux 8,000,125 / `14B0C7EF…2F76` → 新 `D659BF24…080D` — **大小与 SHA256 均不同**。

公网替换与首页不回退核对：**交鹊桥-服务端**。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未改安装说明 / Skill 常量 / trust-levels「才问你」；未改业务功能
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物

'''

p = Path("docs/round-030.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**ready**", "> 状态：**ready-for-product**（客户端已出包，待服务端上架）", 1)
text = text.replace("> 状态：**open**", "> 状态：**ready-for-product**（客户端已出包，待服务端上架）", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-030 ok")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt2, n = re.subn(r"(\| R030 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*", r"\1**ready-for-product**（客户端已出包，待服务端）", rt, count=1)
print("table", n)
rt2, n2 = re.subn(r"已开 R030[^。]*。", "R030：客户端已打出含 R027–R029 Skill 入口的 Win/Linux 新包（见 `docs/round-030.md`），待服务端上架；未开下一轮。", rt2, count=1)
print("narrative", n2)
rm.write_text(rt2, encoding="utf-8")
