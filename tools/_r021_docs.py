from pathlib import Path

md5 = {
"r021-install-entry.png": "8ED52115F70AF18402F98BABF64CF47B",
"r021-install-copied.png": "ED902C943BD0D4906580AA772E8E054A",
"r021-clipboard-preview.png": "57E0DA16C431DA7A6AF3E94EE36135FE",
}

clip = """请打开官网下载「鹊桥远程协助」：http://121.196.45.81/
Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）
macOS 尚未提供正式下载。
如果你手头有安装包，也可以一起发出去。
装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。
如果电脑问是否允许连网，点「允许」。"""

hint = "说明里已带上官网下载地址，发给对方即可"

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- 共享常量 `RemoteAssist.Protocol.WriteConfirmCopy.InstallInstructions` / `InstallInstructionsClipboard` / `InstallCopiedHint` 已更新；**Windows WinForms（Helper.UI）与 Linux Avalonia（HelperWindow）共用同一常量**，两端人话一致，无各写一套。
- 剪贴板主路径改为发给对方官网下载地址（含 Windows / Linux 直链）；保留「如果你手头有安装包，也可以一起发出去」；**macOS 写「尚未提供正式下载」**，未写成已可下。
- 复制反馈改为「{hint}」；只写预览行一次，不写状态行（对齐 R017 不叠字）。
- 「连接」仍是唯一实心绿主按钮；「复制安装说明」保持轻按钮（白底蓝字）。
- **未改**「如何安装 MCP」三步、「复制 MCP 配置」及反馈；未改 R013–R016 已验收原文；未改 `docs/trust-levels.md`「才问你」。

### 最终逐字文案

**InstallCopiedHint（复制反馈）**

> {hint}

**InstallInstructions / InstallInstructionsClipboard（剪贴板）**

```
{clip}
```

### 截图（`docs/screenshots/r021-*`，已复制 `%USERPROFILE%\\Downloads\\r021-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r021-install-entry.png` | 「复制安装说明」轻按钮；「连接」实心绿；ui-shot | {md5["r021-install-entry.png"]} |
| `r021-install-copied.png` | 复制后反馈「{hint}」只出现一次；ui-shot | {md5["r021-install-copied.png"]} |
| `r021-clipboard-preview.png` | 预览区展示剪贴板正文：官网入口 + Win/Linux 直链；macOS 尚未提供正式下载；ui-shot | {md5["r021-clipboard-preview.png"]} |

三张 MD5 互异。坦白：`r021-install-entry.png` 与 `r017-install-entry.png` **字节/MD5 相同**（入口控件本轮未改视觉，仅剪贴板与反馈文案变了；入口图复用同一渲染结果）。

本轮截图为 **Avalonia ui-shot**；Windows 已改到同一共享常量，未单独拍 WinForms 窗（交付说明在此写明）。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release；安装说明断言已跟新文案）。

### 明确未做

- 未开下一轮；未改 `relay/`、`site/`、AI-SSH-Pro；未上生产机
- 未把 Windows 迁到 Avalonia；未把 macOS 写成已可下
- 未改 R013–R016 已验收原文；未改 MCP 三步 /「复制 MCP 配置」反馈；未改 `docs/trust-levels.md`「才问你」
- 未写端口号、17891、TLS、stdio、内部磁盘路径
- 现场公网双机非本轮必须，未做

'''

p = Path("docs/round-021.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**wip**", "> 状态：**ready-for-product**", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-021 ok")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt = rt.replace(
    "| R021 | 高频：复制安装说明带上官网下载地址；打磨：复制后人话反馈点明下载地址 | 鹊桥-客户端 | **wip** | `docs/round-021.md` |",
    "| R021 | 高频：复制安装说明带上官网下载地址；打磨：复制后人话反馈点明下载地址 | 鹊桥-客户端 | **ready-for-product** | `docs/round-021.md` |",
)
old = "已开 R021 去协助端「复制安装说明」带上官网下载地址（见 `docs/round-021.md`）。"
new = "R021 去协助端「复制安装说明」带上官网下载地址已交付待产品验收（见 `docs/round-021.md`）；未开下一轮。"
if old in rt:
    rt = rt.replace(old, new)
else:
    print("WARN: roadmap phrase not found")
rm.write_text(rt, encoding="utf-8")
print("roadmap ok")
