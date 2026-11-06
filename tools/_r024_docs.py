from pathlib import Path
import re

delivery = '''
## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 基于已推送 HEAD `bf078ac`（含 R023 `a5eb2df`）打出 Windows Inno 与 Linux framework-dependent 包；**未改**业务代码与已验收安装说明/MCP/trust-levels「才问你」。
- Linux 产物 DLL 已核对含 R023「若打开时提示缺少 .NET」两行指引与微软下载页。
- **未改** `site/`、未上生产机；公网上架交鹊桥-服务端。未把 macOS 当正式可下；Linux 为 FD 非 SC。

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\installer\\RemoteAssist-Setup-0.1.0.exe` | `E:\\IdeaProjects\\remote-assist\\artifacts\\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,583,762 | 8,000,095 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-06 00:57:06 Asia/Shanghai | 2026-11-06 00:56:39 Asia/Shanghai |
| SHA256 | `236EDEE7B5C1CC28BA6E4804D17CE0FEC07AACA916CB201C3C16CAC2BC76FB58` | `305F9E40057828C0354ED0C0057F6611A380541A962A15AACC8CAD2C414FE586` |
| 构建命令 | `scripts\\build-installer.ps1` | `scripts\\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

出包 HEAD：`bf078acfece3ada8875cb645740c59831ba95e95` — `docs(R023): 产品验收通过并开 R024（官网安装包跟上 R023）`（祖先含 `a5eb2df`）。

对照 R022 旧包：Windows 75,582,719 / `6AC3CE69…BC76` 对照新 `236EDEE7…FB58`；Linux 7,999,909 / `B90E3B28…B1CE` 对照新 `305F9E40…E586` — **大小与 SHA256 均不同**。

公网替换与首页不回退核对：**交鹊桥-服务端**。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未改安装说明 / MCP / trust-levels「才问你」；未改业务功能
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物

'''

p = Path("docs/round-024.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**open**", "> 状态：**ready-for-product**（客户端已出包，待服务端上架）", 1)
text = text.replace("> 状态：**wip**", "> 状态：**ready-for-product**（客户端已出包，待服务端上架）", 1)
text = text.replace("> 状态：**todo**", "> 状态：**ready-for-product**（客户端已出包，待服务端上架）", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-024 ok")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt2, n = re.subn(
    r"(\| R024 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*",
    r"\1**ready-for-product**（客户端已出包，待服务端）",
    rt,
    count=1,
)
print("table subs", n)
rt2, n2 = re.subn(
    r"已开 R024[^。]*。",
    "R024：客户端已打出含 R023 的 Win/Linux 新包（见 `docs/round-024.md`），待服务端上架；未开下一轮。",
    rt2,
    count=1,
)
print("narrative subs", n2)
rm.write_text(rt2, encoding="utf-8")
