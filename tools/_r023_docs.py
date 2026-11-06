from pathlib import Path

md5 = {
"r023-install-entry.png": None,
"r023-install-copied.png": None,
"r023-clipboard-preview.png": None,
}
import hashlib
for name in list(md5):
    b = Path(f"docs/screenshots/{name}").read_bytes()
    md5[name] = hashlib.md5(b).hexdigest().upper()

r022c = hashlib.md5(Path("docs/screenshots/r022-clipboard-preview.png").read_bytes()).hexdigest().upper()
r017e = hashlib.md5(Path("docs/screenshots/r017-install-entry.png").read_bytes()).hexdigest().upper()
r021c = hashlib.md5(Path("docs/screenshots/r021-install-copied.png").read_bytes()).hexdigest().upper()

clip = """请打开官网下载「鹊桥远程协助」：http://121.196.45.81/
Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）
macOS 尚未提供正式下载。
如果你手头有安装包，也可以一起发出去。
装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。
如果系统问起是否允许联网，选择允许。
若打开时提示缺少 .NET，先安装「.NET 8 运行时」（用来跑这个程序的，不是开发工具）。
打开 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 ，在「运行时」那一栏按系统下载，不要选 SDK。"""

hint = "说明里已带上官网下载地址，发给对方即可"

reuse_notes = []
if md5["r023-install-entry.png"] == r017e:
    reuse_notes.append("`r023-install-entry.png` 与 r017/r021/r022 入口图字节相同（入口控件未改视觉）")
if md5["r023-install-copied.png"] == r021c:
    reuse_notes.append("`r023-install-copied.png` 与 r021/r022 反馈图字节相同（反馈句未改）")
if md5["r023-clipboard-preview.png"] == r022c:
    reuse_notes.append("ERROR clipboard same as r022")
else:
    reuse_notes.append("`r023-clipboard-preview.png` 为新字节（含 .NET 8 运行时短指引）")

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 共享常量 `WriteConfirmCopy.InstallInstructions` 在 R022 已验收全文后补 **2 行** .NET 8 运行时短指引（对齐 `docs/first-run-mac-linux.md` 语气：运行时不是开发工具；打开微软 .NET 8 下载页，选「运行时」不要选 SDK）。
- Windows WinForms 与 Linux Avalonia **共用同一常量**；`InstallCopiedHint` 仍为「{hint}」；反馈只出现一次。
- **原样保留** R022：官网入口与 Win/Linux 直链、macOS「尚未提供正式下载」、手头安装包可选句、中性联网句、轻按钮「复制安装说明」、唯一实心绿「连接」。
- **未改** MCP 三步 /「复制 MCP 配置」反馈、R013–R022 其余原文、`docs/trust-levels.md`「才问你」；未做自动下载器；未改 `site/`、未上生产机；本轮未重打/上架安装包。

### 最终逐字文案

**InstallCopiedHint**

> {hint}

**InstallInstructions / InstallInstructionsClipboard**

```
{clip}
```

### 截图（`docs/screenshots/r023-*`，已复制 `%USERPROFILE%\\Downloads\\r023-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r023-install-entry.png` | 「复制安装说明」轻按钮；「连接」实心绿；ui-shot | {md5["r023-install-entry.png"]} |
| `r023-install-copied.png` | 反馈「{hint}」只一次；ui-shot | {md5["r023-install-copied.png"]} |
| `r023-clipboard-preview.png` | 预览含 .NET 8 运行时短指引（微软下载页 + 不要选 SDK）+ 官网/直链/macOS 尚未提供正式下载/联网中性句；ui-shot | {md5["r023-clipboard-preview.png"]} |

三张 MD5 互异。坦白：{ "；".join(reuse_notes) }。

官网旁注对齐与 R022 公网下载截图：**交鹊桥-服务端**（本交付仅为客户端 A 节）。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未做缺运行时时的自动下载器 / 内嵌安装器
- 未重打/上架安装包（本轮文案截图先行）
- 未改 MCP 三步 /「复制 MCP 配置」反馈；未改 trust-levels「才问你」
- 未把 macOS 写成已可下

'''

p = Path("docs/round-023.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**open**", "> 状态：**ready-for-product**（客户端 A 节已交付，待服务端 B 节）", 1)
text = text.replace("> 状态：**wip**", "> 状态：**ready-for-product**（客户端 A 节已交付，待服务端 B 节）", 1)
text = text.replace("> 状态：**todo**", "> 状态：**ready-for-product**（客户端 A 节已交付，待服务端 B 节）", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-023 ok", md5)

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
import re
rt2, n = re.subn(
    r"(\| R023 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*",
    r"\1**ready-for-product**（客户端 A 已交付，待服务端）",
    rt,
    count=1,
)
print("roadmap table subs", n)
# narrative
rt2, n2 = re.subn(
    r"已开 R023[^。]*。",
    "R023：客户端已补安装说明 .NET 8 运行时短指引（见 `docs/round-023.md`），待服务端旁注/截图；未开下一轮。",
    rt2,
    count=1,
)
print("roadmap narrative subs", n2)
rm.write_text(rt2, encoding="utf-8")
