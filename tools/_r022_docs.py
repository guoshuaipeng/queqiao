from pathlib import Path

md5 = {
"r022-install-entry.png": "8ED52115F70AF18402F98BABF64CF47B",
"r022-install-copied.png": "ED902C943BD0D4906580AA772E8E054A",
"r022-clipboard-preview.png": "1AB69573818494B172CE7E67518247DE",
}

clip = """请打开官网下载「鹊桥远程协助」：http://121.196.45.81/
Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）
macOS 尚未提供正式下载。
如果你手头有安装包，也可以一起发出去。
装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。
如果系统问起是否允许联网，选择允许。"""

hint = "说明里已带上官网下载地址，发给对方即可"

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- 打磨：共享常量 `WriteConfirmCopy.InstallInstructions` 末句由「如果电脑问是否允许连网，点「允许」」改为「如果系统问起是否允许联网，选择允许。」；Windows WinForms 与 Linux Avalonia **共用同一常量**。
- 保留 R021 已验收：官网入口与 Win/Linux 直链、macOS「尚未提供正式下载」、手头安装包可选句、反馈「{hint}」、轻按钮「复制安装说明」、唯一实心绿「连接」。
- 高频出包：Windows Inno `RemoteAssist-Setup-0.1.0.exe` + Linux FD `queqiao-linux-x64-fd.tar.gz`（上架公网文件名去掉 `-fd` → `queqiao-linux-x64.tar.gz`）。
- **未改** MCP 三步 /「复制 MCP 配置」反馈、R013–R020 原文、`docs/trust-levels.md`「才问你」；未改 `site/`、未上生产机；未把 macOS 当正式产物；Linux 为 FD 非 SC。

### 最终逐字文案

**InstallCopiedHint**

> {hint}

**InstallInstructions / InstallInstructionsClipboard**

```
{clip}
```

### 本机产物

| 项 | Windows | Linux |
|------|---------|-------|
| 绝对路径 | `E:\\IdeaProjects\\remote-assist\\artifacts\\installer\\RemoteAssist-Setup-0.1.0.exe` | `E:\\IdeaProjects\\remote-assist\\artifacts\\queqiao-linux-x64-fd.tar.gz` |
| 公网文件名 | `RemoteAssist-Setup-0.1.0.exe` | `queqiao-linux-x64.tar.gz`（**去掉内部 `-fd`**） |
| 字节 | 75,582,719 | 7,999,909 |
| MB | 72.08 | 7.63 |
| mtime | 2026-11-05 23:58:02 Asia/Shanghai | 2026-11-05 23:57:35 Asia/Shanghai |
| SHA256 | `6AC3CE69AD0FB40C245CF2071D7E8E3EA454E97637595F6D68F4B20484400CD1` | `B90E3B28F323174F3C1CB35D3CACDB017C5C47F368D10CFF8D5571DECD0AB1CE` |
| 构建命令 | `scripts\\build-installer.ps1` | `scripts\\publish-agent-avalonia.ps1 -Runtimes linux-x64` |
| 形态 | Inno Setup（与现官网同形态） | framework-dependent（含 `RemoteAssist.Agent.Avalonia` + `运行说明.txt`） |

对照旧包：Windows 旧 75,503,088 / `78175CDC…ABF33AB8`；Linux 旧 7,999,640 / `EC1C7211…9DCE3650` — **大小与 SHA256 均不同**。出包前源码/产物 DLL 已核对含新联网句、不含旧句。

出包工作树对应本交付提交（含 R021 `632928a` + 本轮连网话术）；见本 commit。

### 截图（`docs/screenshots/r022-*`，已复制 `%USERPROFILE%\\Downloads\\r022-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r022-install-entry.png` | 「复制安装说明」轻按钮；「连接」实心绿；ui-shot | {md5["r022-install-entry.png"]} |
| `r022-install-copied.png` | 反馈「{hint}」只一次；ui-shot | {md5["r022-install-copied.png"]} |
| `r022-clipboard-preview.png` | 预览含官网下载地址 + 新句「如果系统问起是否允许联网，选择允许。」；macOS 尚未提供正式下载；ui-shot | {md5["r022-clipboard-preview.png"]} |

三张 MD5 互异。坦白：`r022-install-entry.png` 与 r017/r021 入口图字节相同；`r022-install-copied.png` 与 r021 反馈图字节相同（入口与反馈句本轮未改视觉/文案）；**`r022-clipboard-preview.png` 为新字节**（含新联网句）。

公网替换与下载页核对：**交鹊桥-服务端**（客户端只交出包路径）。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未把 macOS 当正式可下；未打 Linux SC 冒充本轮产物
- 未重打 `RemoteAssist-Agent-Setup-0.1.0.exe` 当官网主包
- 未改 MCP 三步 /「复制 MCP 配置」反馈；未改 R013–R020 原文；未改 trust-levels「才问你」

'''

p = Path("docs/round-022.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**todo**", "> 状态：**ready-for-product**（客户端出包+话术已交付，待服务端上架）", 1)
# also handle wip if any
text = text.replace("> 状态：**wip**", "> 状态：**ready-for-product**（客户端出包+话术已交付，待服务端上架）", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-022 ok")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt2 = rt.replace(
    "| R022 | 高频：官网安装包跟上 R021；打磨：安装说明连网话术中性化 | 鹊桥-客户端 + 鹊桥-服务端 | **todo** | `docs/round-022.md` |",
    "| R022 | 高频：官网安装包跟上 R021；打磨：安装说明连网话术中性化 | 鹊桥-客户端 + 鹊桥-服务端 | **ready-for-product**（客户端已出包，待服务端） | `docs/round-022.md` |",
)
if rt2 == rt:
    # try other patterns
    import re
    m = re.search(r"\| R022 \|.*?\|", rt)
    print("roadmap R022 line:", m.group(0) if m else "NOT FOUND")
    # broader replace status cell
    rt2 = re.sub(
        r"(\| R022 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*",
        r"\1**ready-for-product**（客户端已出包，待服务端）",
        rt,
        count=1,
    )
old = "已开 R022 官网安装包跟上 R021 + 安装说明连网话术中性化（见 `docs/round-022.md`）。"
new = "R022：客户端已改连网话术并打出 Win/Linux 新包（见 `docs/round-022.md`），待服务端上架；未开下一轮。"
if old in rt2:
    rt2 = rt2.replace(old, new)
else:
    # try from R021 acceptance open line
    alt_old = "已开 R022"
    if "已开 R022" in rt2:
        import re
        rt2 = re.sub(r"已开 R022[^。]*。", new, rt2, count=1)
    else:
        print("WARN: roadmap open phrase not found")
rm.write_text(rt2, encoding="utf-8")
print("roadmap ok")
