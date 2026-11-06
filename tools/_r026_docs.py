from pathlib import Path
import re

delivery = '''
## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 基于已推送 HEAD `73b2fea`（含 R025 `f423db1`）打出 Windows Inno 与 Linux framework-dependent 包；**未改**业务代码与已验收安装说明/MCP/trust-levels「才问你」。
- Linux 产物 DLL 已核对含 R025「如果浏览器提示通常不会下载，选择保留…选择仍要运行。」以及 R023 .NET 指引。
- **未改** `site/`、未上生产机；公网上架交鹊桥-服务端。未把 macOS 当正式可下；Linux 为 FD 非 SC。

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\installer\\RemoteAssist-Setup-0.1.0.exe` | `E:\\IdeaProjects\\remote-assist\\artifacts\\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,580,750 | 8,000,125 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-06 01:35:02 Asia/Shanghai | 2026-11-06 01:34:36 Asia/Shanghai |
| SHA256 | `2A8EE24DA2A91157986686D4F22BF8D1AD960CFD4CEB60E031864401B6F59E26` | `14B0C7EF5050050469FBF7ECA522001F06529527F38C27ACE657BB0069832F76` |
| 构建命令 | `scripts\\build-installer.ps1` | `scripts\\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

出包 HEAD：`73b2fea70185fae2bce7e0162006e57855fc2c01` — `docs(R025): 产品验收通过并开 R026（官网安装包跟上 R025）`（祖先含 `f423db1`）。

对照 R024 旧包：Windows 75,583,762 / `236EDEE7…FB58` → 新 `2A8EE24D…9E26`；Linux 8,000,095 / `305F9E40…E586` → 新 `14B0C7EF…2F76` — **大小与 SHA256 均不同**。

公网替换与首页不回退核对：**交鹊桥-服务端**。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未改安装说明 / MCP / trust-levels「才问你」；未改业务功能
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物

'''

p = Path("docs/round-026.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**ready**", "> 状态：**ready-for-product**（客户端已出包，待服务端上架）", 1)
text = text.replace("> 状态：**open**", "> 状态：**ready-for-product**（客户端已出包，待服务端上架）", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-026 ok")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt2, n = re.subn(r"(\| R026 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*", r"\1**ready-for-product**（客户端已出包，待服务端）", rt, count=1)
print("table", n)
rt2, n2 = re.subn(r"已开 R026[^。]*。", "R026：客户端已打出含 R025 的 Win/Linux 新包（见 `docs/round-026.md`），待服务端上架；未开下一轮。", rt2, count=1)
print("narrative", n2)
rm.write_text(rt2, encoding="utf-8")
