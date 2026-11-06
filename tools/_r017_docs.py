from pathlib import Path

md5 = {
"r017-install-entry.png": "8ED52115F70AF18402F98BABF64CF47B",
"r017-install-copied.png": "87BBE7532447153E3D7F0EFF529E445F",
}

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- **「复制安装说明」**：协助端「连接对方」卡内，与 Windows 同位置语义——「先让对方装好「鹊桥远程协助」」旁轻按钮（白底蓝字，非实心绿）。复制 `WriteConfirmCopy.InstallInstructionsClipboard`，反馈 `WriteConfirmCopy.InstallCopiedHint`（「请把安装包和这段说明一起发出去」）。
- **MCP 安装说明卡**：`McpInstallGuide.SectionTitle` / `StepsBlock` / 「复制 MCP 配置」轻按钮 +「详细参数见产品文档；写操作仍须对方本机确认」。复制走 `McpConfigSnippet.BuildClipboardBundle` + `McpExeLocator`，反馈 `CopiedOkHint` / `CopiedPlaceholderHint`。缺会话码时提示 `NeedCodePasswordHint`。
- **未改** R013–R016 已验收原文；未在接受协助路径自动对外监听 MCP（MainWindow 无 MCP 监听启动）。
- **osx**：与 Linux 同树可出包；本轮未重跑 publish、**未上真机**，不写成 Mac 已验收。

### 截图（`docs/screenshots/r017-*`，已复制 `%USERPROFILE%\\Downloads\\r017-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r017-install-entry.png` | 「复制安装说明」轻按钮 +「如何安装 MCP」卡与「复制 MCP 配置」轻按钮可见；ui-shot | {md5["r017-install-entry.png"]} |
| `r017-install-copied.png` | 复制后反馈「请把安装包和这段说明一起发出去」；ui-shot | {md5["r017-install-copied.png"]} |

截图为 **ui-shot**。MD5 互异，且不同于 r015/r016。亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号、deviceId、offline。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开 R018 / 下一轮
- 未改 `relay/`、官网下载、AI-SSH-Pro
- 未把 Windows 迁到 Avalonia；未重做 R006–R016
- 未改 R013–R016 已验收文案；未改 `docs/trust-levels.md`「才问你」
- 仅接受协助时不自动对外监听 MCP（本轮未新增监听）
- 现场公网双机非本轮必须，未做

'''

p = Path("docs/round-017.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**wip**", "> 状态：**ready-for-product**")
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-017 ok")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt = rt.replace(
    "| R017 | 高频：Avalonia MCP 安装说明；打磨：复制后人话反馈 | 鹊桥-客户端 | **wip** | `docs/round-017.md` |",
    "| R017 | 高频：Avalonia MCP 安装说明；打磨：复制后人话反馈 | 鹊桥-客户端 | **ready-for-product** | `docs/round-017.md` |",
)
old = "下一步 R017 Avalonia MCP 安装说明。"
new = "R017 Avalonia MCP / 复制安装说明已交付待产品验收（见 `docs/round-017.md`）；未开下一轮。"
if old in rt:
    rt = rt.replace(old, new)
rm.write_text(rt, encoding="utf-8")
print("roadmap ok")
