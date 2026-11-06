from pathlib import Path
import re, hashlib

md5 = {}
for name in ["r025-install-entry.png","r025-install-copied.png","r025-clipboard-preview.png"]:
    md5[name] = hashlib.md5(Path(f"docs/screenshots/{name}").read_bytes()).hexdigest().upper()

clip = """请打开官网下载「鹊桥远程协助」：http://121.196.45.81/
Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）
macOS 尚未提供正式下载。
如果你手头有安装包，也可以一起发出去。
装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。
如果系统问起是否允许联网，选择允许。
若打开时提示缺少 .NET，先安装「.NET 8 运行时」（用来跑这个程序的，不是开发工具）。
打开 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 ，在「运行时」那一栏按系统下载，不要选 SDK。
如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。"""

hint = "说明里已带上官网下载地址，发给对方即可"

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 在已验收 `WriteConfirmCopy.InstallInstructions` 全文末尾（R023 .NET 两句之后）**仅追加一句**：「如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。」
- Windows WinForms 与 Linux Avalonia **共用同一常量**；`InstallCopiedHint` 仍为「{hint}」；反馈只出现一次。
- **未改** MCP 三步 /「复制 MCP 配置」反馈、trust-levels「才问你」、R013–R024 其余原文；**未换包**；未改 `site/`。

### 最终逐字文案

**InstallCopiedHint**

> {hint}

**InstallInstructions / InstallInstructionsClipboard**

```
{clip}
```

### 截图（`docs/screenshots/r025-*`，已复制 `%USERPROFILE%\\Downloads\\r025-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r025-install-entry.png` | 「复制安装说明」轻按钮；「连接」实心绿；ui-shot | {md5["r025-install-entry.png"]} |
| `r025-install-copied.png` | 反馈「{hint}」只一次；ui-shot | {md5["r025-install-copied.png"]} |
| `r025-clipboard-preview.png` | 预览含新句 + 官网/直链/macOS 尚未提供正式下载/联网中性句/.NET 8 两行；ui-shot | {md5["r025-clipboard-preview.png"]} |

三张 MD5 互异。坦白：`r025-install-entry.png` / `r025-install-copied.png` 分别与 r023（及更早同源）入口图、反馈图**字节相同**；`r025-clipboard-preview.png` 为新字节（含不受信任怎么选那句），且未覆盖 r022/r023/r024 已有截图。

官网 Windows 旁注：**交鹊桥-服务端**（本交付仅为客户端 A 节）。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开下一轮；未换公网安装包；未改 `relay/`、`site/`、AI-SSH-Pro
- 未改 MCP / InstallCopiedHint / trust-levels「才问你」
- 未提 SmartScreen、证书、签名、HTTP、「不安全」、端口、17891

'''

p = Path("docs/round-025.md")
text = p.read_text(encoding="utf-8")
# status was ready from product open doc — mark client A ready-for-product pending server
text = text.replace("> 状态：**ready**", "> 状态：**ready-for-product**（客户端 A 节已交付，待服务端 B 节）", 1)
text = text.replace("> 状态：**open**", "> 状态：**ready-for-product**（客户端 A 节已交付，待服务端 B 节）", 1)
text = text.replace("> 状态：**wip**", "> 状态：**ready-for-product**（客户端 A 节已交付，待服务端 B 节）", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-025 ok", md5)

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt2, n = re.subn(r"(\| R025 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*", r"\1**ready-for-product**（客户端 A 已交付，待服务端）", rt, count=1)
print("table", n)
rt2, n2 = re.subn(r"已开 R025[^。]*。", "R025：客户端已补安装说明「下载/打开不受信任怎么选」（见 `docs/round-025.md`），待服务端旁注；未开下一轮。", rt2, count=1)
print("narrative", n2)
rm.write_text(rt2, encoding="utf-8")

# verify source final
src = Path("src/RemoteAssist.Protocol/WriteConfirmCopy.cs").read_text(encoding="utf-8")
assert "如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。" in src
assert 'InstallCopiedHint = "说明里已带上官网下载地址，发给对方即可"' in src
print("source verified")
