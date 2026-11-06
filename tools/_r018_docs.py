from pathlib import Path

md5 = {
"r018-mcp-entry.png": "ED8DB4C33BC008E1761D2A31C732C599",
"r018-mcp-copied.png": "FB678C85C256C3F8BB554B0CDAAD411C",
"r018-copy-primary.png": "2B59E34B1BBB38A7FA32AFC2E5C8F2DC",
}

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- **接受协助端 `MainWindow` 高级**：增加与去协助端同等的 `McpInstallGuide`「如何安装 MCP」三步 + 轻按钮「复制 MCP 配置」。
- **可选 / 不自动监听**：高级内人话「可选。接受协助时不会自动对外开启 MCP；只有你主动复制配置给智能体时才用得上。」（无端口号 / stdio / TLS）。本轮未启动任何 MCP 监听。
- **复制反馈**：`McpInstallGuide.CopiedOkHint` / `CopiedPlaceholderHint` 只写入 `_mcpFeedback` 一行，不写状态行，避免重复。
- **主按钮权重**：「复制码+密码」仍为实心主按钮；「复制 MCP 配置」白底蓝字轻按钮，放在高级里。
- **未改** R013–R017 已验收原文。
- **osx**：与 Linux 同树可出包；本轮未重跑 publish、**未上真机**，不写成 Mac 已验收。

### 截图（`docs/screenshots/r018-*`，已复制 `%USERPROFILE%\\Downloads\\r018-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r018-mcp-entry.png` | 高级内「如何安装 MCP」三步 + 可选/不自动开启说明 + 轻按钮「复制 MCP 配置」；ui-shot | {md5["r018-mcp-entry.png"]} |
| `r018-mcp-copied.png` | 复制后「已复制 MCP 配置。请粘贴到 Cursor 的 MCP 设置并保存。」只出现一次；ui-shot | {md5["r018-mcp-copied.png"]} |
| `r018-copy-primary.png` | 「复制码+密码」实心主按钮仍在；MCP 为高级轻按钮；ui-shot | {md5["r018-copy-primary.png"]} |

截图为 **ui-shot**。MD5 互异，且不同于 r015/r016/r017。亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号、stdio、deviceId、offline。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开 R019 / 下一轮
- 未改 `relay/`、官网下载、AI-SSH-Pro
- 未把 Windows 迁到 Avalonia；未重做 R006–R017
- 未改 R013–R017 已验收文案；未改 `docs/trust-levels.md`「才问你」
- 接受协助时未自动启动 MCP 监听
- 现场公网双机非本轮必须，未做

'''

p = Path("docs/round-018.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**wip**", "> 状态：**ready-for-product**")
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-018 ok")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt = rt.replace(
    "| R018 | 高频：接受协助端可选 MCP 说明；打磨：复制 MCP 配置后人话反馈 | 鹊桥-客户端 | **wip** | `docs/round-018.md` |",
    "| R018 | 高频：接受协助端可选 MCP 说明；打磨：复制 MCP 配置后人话反馈 | 鹊桥-客户端 | **ready-for-product** | `docs/round-018.md` |",
)
old = "下一步 R018 接受协助端可选 MCP 说明。"
new = "R018 接受协助端可选 MCP 说明已交付待产品验收（见 `docs/round-018.md`）；未开下一轮。"
if old in rt:
    rt = rt.replace(old, new)
rm.write_text(rt, encoding="utf-8")
print("roadmap ok")
