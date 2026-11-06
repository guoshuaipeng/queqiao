from pathlib import Path
import re, hashlib

md5 = {}
for name in ["r028-skill-entry.png","r028-skill-copied.png","r028-skill-clipboard-preview.png"]:
    md5[name] = hashlib.md5(Path(f"docs/screenshots/{name}").read_bytes()).hexdigest().upper()

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- Avalonia 接受协助端 `MainWindow`「如何安装 MCP」卡增加轻按钮「复制 Skill 说明」，**复用** `McpInstallGuide.CopySkillButton` / `SkillInstructions` / `SkillInstructionsClipboard` / `SkillCopiedHint`（未改常量原文）。
- 保留 R018 导语「可选。接受协助时不会自动对外开启 MCP；只有你主动复制配置给智能体时才用得上。」与附注「详细参数见产品文档；写操作仍须本机确认」。
- 反馈只写 `_mcpFeedback` 一次，不叠状态行。
- **Windows 接受协助端**（`RemoteAssist.Agent.MainForm`）**没有**「如何安装 MCP」卡片（仅有菜单「复制 MCP 配置」），按文档**未新开卡**；去协助端 R027 入口未改。
- 未自动对外监听 MCP；未换包。

### 截图（`docs/screenshots/r028-*`，已复制 `%USERPROFILE%\\Downloads\\r028-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r028-skill-entry.png` | 高级内「复制 Skill 说明」轻按钮；R018 导语/三步/附注仍在；ui-shot | {md5["r028-skill-entry.png"]} |
| `r028-skill-copied.png` | 反馈「已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。」只一次；状态仍「已连上网络…」；ui-shot | {md5["r028-skill-copied.png"]} |
| `r028-skill-clipboard-preview.png` | 反馈区展示与 R027 相同的 Skill 正文；ui-shot | {md5["r028-skill-clipboard-preview.png"]} |

三张 MD5 互异，且与 r022–r027 不同（未覆盖旧文件）。

### 改动文件

- `src/RemoteAssist.Agent.Avalonia/Views/MainWindow.cs`
- `src/RemoteAssist.Agent.Avalonia/UiShotRunner.cs`
- `docs/round-028.md` / `docs/roadmap.md` / `docs/screenshots/r028-*`

### 测试

`dotnet test`：170 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开下一轮；未换包；未改 `relay/`、`site/`
- 未改去协助端 Skill 入口；未改 MCP 三步 /「复制 MCP 配置」反馈 / 安装说明 / Skill 常量原文 / trust-levels「才问你」
- 未给 Windows 接受协助端新开 MCP 卡；未自动监听 MCP

'''

p = Path("docs/round-028.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**ready**", "> 状态：**ready-for-product**", 1)
text = text.replace("> 状态：**open**", "> 状态：**ready-for-product**", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-028 ok", md5)

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt2, n = re.subn(r"(\| R028 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*", r"\1**ready-for-product**", rt, count=1)
print("table", n)
rt2, n2 = re.subn(r"已开 R028[^。]*。", "R028：接受协助端可选 Skill 说明已交付待产品验收（见 `docs/round-028.md`）；未开下一轮。", rt2, count=1)
print("narrative", n2)
rm.write_text(rt2, encoding="utf-8")
