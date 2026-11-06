from pathlib import Path
import re, hashlib

md5 = {}
for name in ["r027-skill-entry.png","r027-skill-copied.png","r027-skill-clipboard-preview.png"]:
    md5[name] = hashlib.md5(Path(f"docs/screenshots/{name}").read_bytes()).hexdigest().upper()

skill = """把下面这段保存成智能体的技能说明（只用来告诉它怎么安装鹊桥的 MCP，不能代替对方本机确认）。

1. 在鹊桥里点「复制 MCP 配置」。
2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。
3. 刷新或重启后再用智能体协助。

对方电脑上要改动的操作仍会弹确认，智能体不能绕过。不要用这段说明去远程控制电脑。"""

hint = "已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。"

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 去协助端「如何安装 MCP」卡增加轻按钮「复制 Skill 说明」；Windows WinForms（Helper.UI）与 Linux Avalonia（HelperWindow）共用 `McpInstallGuide.CopySkillButton` / `SkillInstructions` / `SkillInstructionsClipboard` / `SkillCopiedHint`。
- 剪贴板正文与 round-027.md **逐字一致**；反馈「{hint}」只出现一次（Avalonia 写 `_mcpHint`；WinForms 写预览行；均不叠到状态行）。
- 「连接」仍是唯一实心绿；「复制 MCP 配置」「复制安装说明」保持轻按钮与已验收反馈/三步/安装说明原文。
- **未改** 接受协助端（MainWindow）；未自动对外监听 MCP；未改 `docs/trust-levels.md`「才问你」；**未换包**。

### 最终逐字文案

**SkillCopiedHint**

> {hint}

**SkillInstructions / SkillInstructionsClipboard**

```
{skill}
```

### 截图（`docs/screenshots/r027-*`，已复制 `%USERPROFILE%\\Downloads\\r027-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r027-skill-entry.png` | 「复制 Skill 说明」轻按钮与「复制 MCP 配置」并列；「连接」实心绿；三步未改；ui-shot | {md5["r027-skill-entry.png"]} |
| `r027-skill-copied.png` | 反馈「{hint}」只一次；状态仍「未连接」；ui-shot | {md5["r027-skill-copied.png"]} |
| `r027-skill-clipboard-preview.png` | 预览含 Skill 说明逐字正文；ui-shot | {md5["r027-skill-clipboard-preview.png"]} |

三张 MD5 互异，且与 r022–r026 截图不同（未覆盖旧文件）。本轮以 Avalonia ui-shot 为主；Windows 同源常量，交付说明在此披露。

### 改动文件

- `src/RemoteAssist.Protocol/McpInstallGuide.cs`
- `tests/RemoteAssist.Protocol.Tests/McpInstallGuideTests.cs`
- `src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs`
- `src/RemoteAssist.Agent.Avalonia/UiShotRunner.cs`
- `src/RemoteAssist.Helper.UI/MainForm.cs`
- `docs/round-027.md` / `docs/roadmap.md` / `docs/screenshots/r027-*`

### 测试

`dotnet test`：170 通过 / 0 失败（RemoteAssist.sln Release；含新增 Skill 文案断言）。

### 明确未做

- 未开下一轮；未换公网安装包；未改 `relay/`、`site/`、AI-SSH-Pro
- 未改 MCP 三步 /「复制 MCP 配置」反馈 / 安装说明 / trust-levels「才问你」
- 未改接受协助端；未因展示/复制 Skill 而自动监听 MCP
- 未把 Skill 做成控制面或远程控制

'''

p = Path("docs/round-027.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**ready**", "> 状态：**ready-for-product**", 1)
text = text.replace("> 状态：**open**", "> 状态：**ready-for-product**", 1)
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-027 ok", md5)

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt2, n = re.subn(r"(\| R027 \|[^|]+\|[^|]+\| )\*\*[^*]+\*\*", r"\1**ready-for-product**", rt, count=1)
print("table", n)
rt2, n2 = re.subn(r"已开 R027[^。]*。", "R027：去协助端「复制 Skill 说明」已交付待产品验收（见 `docs/round-027.md`）；未开下一轮。", rt2, count=1)
print("narrative", n2)
rm.write_text(rt2, encoding="utf-8")
