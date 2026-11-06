from pathlib import Path
import re, hashlib

md5 = {}
for name in ["r029-skill-menu.png","r029-skill-copied.png","r029-skill-clipboard-preview.png"]:
    md5[name] = hashlib.md5(Path(f"docs/screenshots/{name}").read_bytes()).hexdigest().upper()

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- Windows 接受协助端 `RemoteAssist.Agent.MainForm` 的「更多」菜单里，在「复制 MCP 配置」旁增加「复制 Skill 说明」（`McpInstallGuide.CopySkillButton`）。
- 剪贴板 / 反馈复用 `SkillInstructionsClipboard` / `SkillCopiedHint`；反馈只写状态栏一次。
- **未新开**「如何安装 MCP」卡片；未改 Avalonia R028 / 去协助端 R027；未改 Skill 常量原文；未自动监听 MCP；未换包。

### 菜单位置

- 会话卡「更多」按钮 → ContextMenuStrip：复制载荷 / 复制 MCP 配置 / **复制 Skill 说明** / 复制会话码

### 截图（`docs/screenshots/r029-*`，已复制 `%USERPROFILE%\\Downloads\\r029-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r029-skill-menu.png` | 「更多」菜单展开，可见「复制 Skill 说明」在「复制 MCP 配置」旁；WinForms UiShot 实拍 | {md5["r029-skill-menu.png"]} |
| `r029-skill-copied.png` | 状态栏反馈「已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。」只一次；UiShot | {md5["r029-skill-copied.png"]} |
| `r029-skill-clipboard-preview.png` | 预览叠层展示与 R027/R028 相同的 Skill 正文；UiShot | {md5["r029-skill-clipboard-preview.png"]} |

三张 MD5 互异，且与 r022–r028 不同（未覆盖旧文件）。

### 改动文件

- `src/RemoteAssist.Agent/MainForm.cs`
- `tools/UiShot/R029Shots.cs`（新增）
- `tools/UiShot/Program.cs`（`--r029`）
- `docs/round-029.md` / `docs/roadmap.md` / `docs/screenshots/r029-*`

### 测试

`dotnet test`：170 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开下一轮；未换包；未改 Avalonia MainWindow / HelperWindow / Helper.UI 去协助 Skill 入口
- 未改 MCP 三步 /「复制 MCP 配置」反馈 / 安装说明 / Skill 常量原文 / trust-levels「才问你」
- 未新开 MCP 卡；未自动监听 MCP

'''

p = Path("docs/round-029.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**ready**", "> 状态：**ready-for-product**", 1)
text = text.replace("> 状态：**open**", "> 状态：**ready-for-product**", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-029 ok", md5)

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt2, n = re.subn(r"(\| R029 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*", r"\1**ready-for-product**", rt, count=1)
print("table", n)
rt2, n2 = re.subn(r"已开 R029[^。]*。", "R029：Windows 接受协助端菜单补复制 Skill 说明已交付待产品验收（见 `docs/round-029.md`）；未开下一轮。", rt2, count=1)
print("narrative", n2)
rm.write_text(rt2, encoding="utf-8")

# ensure McpInstallGuide / Avalonia / Helper untouched in working tree for those paths
import subprocess
for f in ["src/RemoteAssist.Protocol/McpInstallGuide.cs",
          "src/RemoteAssist.Agent.Avalonia/Views/MainWindow.cs",
          "src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs",
          "src/RemoteAssist.Helper.UI/MainForm.cs"]:
    out = subprocess.check_output(["git", "diff", "--", f], text=True)
    print(f, "diff_empty" if not out.strip() else "HAS DIFF")
