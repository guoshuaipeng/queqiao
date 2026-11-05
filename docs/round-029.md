# 第 29 轮 — Windows 接受协助端菜单补复制 Skill 说明

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**done（产品已验收）**
> 依据：R028 产品已验收（Avalonia 接受协助端已能复制同一段 Skill 说明）。Windows 接受协助端 `RemoteAssist.Agent.MainForm` 没有「如何安装 MCP」卡片，只有菜单「复制 MCP 配置」，本轮不新开卡，只在已有菜单旁补上同一入口。

## 本轮目标

1. **高频**：Windows 接受协助端在已有「复制 MCP 配置」菜单旁增加「复制 Skill 说明」，复用 R027/R028 已验收正文。
2. **打磨**：复制后给人话反馈，只出现一次。

## A. 菜单项「复制 Skill 说明」（高频 · 鹊桥-客户端）

在 `RemoteAssist.Agent.MainForm` 已有的「复制 MCP 配置」菜单旁增加菜单项「复制 Skill 说明」。

剪贴板正文、菜单文案、反馈文案一律复用 `McpInstallGuide.CopySkillButton` / `SkillInstructions` / `SkillInstructionsClipboard` / `SkillCopiedHint`，不要另写一份。已验收正文是：

把下面这段保存成智能体的技能说明（只用来告诉它怎么安装鹊桥的 MCP，不能代替对方本机确认）。

1. 在鹊桥里点「复制 MCP 配置」。
2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。
3. 刷新或重启后再用智能体协助。

对方电脑上要改动的操作仍会弹确认，智能体不能绕过。不要用这段说明去远程控制电脑。

验收：

- 不要新开「如何安装 MCP」卡片，以免把 Windows 接受协助主路径变复杂。
- 不要改 Avalonia 接受协助端 R028 已验收入口，不要改去协助端 R027 已验收入口。
- 不要改「如何安装 MCP」三步、「复制 MCP 配置」及反馈、不要改「复制安装说明」/ `InstallInstructions`、不要改 `docs/trust-levels.md` 的「才问你」。
- 不要因为增加菜单项就自动对外监听 MCP。
- 截图放到 `docs/screenshots/r029-*`（至少：能看到菜单里的「复制 Skill 说明」、复制反馈、剪贴板预览能读到上文）。**勿覆盖** `r022-*`～`r028-*`。

## 打磨：复制后人话反馈

复制成功后提示（只出现一次，复用已有常量）：

已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。

不要和已有的「已复制 MCP 配置」反馈写成同一句，也不要叠出两行相同提示。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 把 Skill 做成控制面或远程控制
- 为 Windows 接受协助端新开一张「如何安装 MCP」卡片
- 替换公网安装包
- 把未上真机的 macOS 包写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS
- 改信任等级「才问你」
- 改 Avalonia R028 / 去协助端 R027 已验收入口与文案

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- Windows 接受协助端 `RemoteAssist.Agent.MainForm` 的「更多」菜单里，在「复制 MCP 配置」旁增加「复制 Skill 说明」（`McpInstallGuide.CopySkillButton`）。
- 剪贴板 / 反馈复用 `SkillInstructionsClipboard` / `SkillCopiedHint`；反馈只写状态栏一次。
- **未新开**「如何安装 MCP」卡片；未改 Avalonia R028 / 去协助端 R027；未改 Skill 常量原文；未自动监听 MCP；未换包。

### 菜单位置

- 会话卡「更多」按钮 → ContextMenuStrip：复制载荷 / 复制 MCP 配置 / **复制 Skill 说明** / 复制会话码

### 截图（`docs/screenshots/r029-*`，已复制 `%USERPROFILE%\Downloads\r029-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r029-skill-menu.png` | 「更多」菜单展开，可见「复制 Skill 说明」在「复制 MCP 配置」旁；WinForms UiShot 实拍 | 8C120721CF7B5F5C7A0831195AC27189 |
| `r029-skill-copied.png` | 状态栏反馈「已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。」只一次；UiShot | 932135806F785EEC133F5BAB22B2A217 |
| `r029-skill-clipboard-preview.png` | 预览叠层展示与 R027/R028 相同的 Skill 正文；UiShot | F951A1F00A3D7F1E985392538C33CD55 |

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


## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r029-*` 与源码独立验收通过（实现 `5909d99`）。本轮未换包。

独立核对：

- `RemoteAssist.Agent.MainForm` 在会话卡「更多」菜单、已有「复制 MCP 配置」下方增加 `McpInstallGuide.CopySkillButton`（「复制 Skill 说明」），顺序为：复制载荷 / 复制 MCP 配置 / 复制 Skill 说明 / 复制会话码。未新开「如何安装 MCP」卡片。
- 复制使用 `SkillInstructionsClipboard`，反馈 `SetStatus(SkillCopiedHint)` 只写状态栏一次。`McpInstallGuide` / `WriteConfirmCopy` / Avalonia `MainWindow` / 去协助端 `HelperWindow` 与 `Helper.UI` / `docs/trust-levels.md` 相对本轮提交无改动。
- `docs/screenshots/r029-skill-menu.png`（MD5 `8C120721CF7B5F5C7A0831195AC27189`）：弹出菜单可见「复制 Skill 说明」在「复制 MCP 配置」正下方。
- `docs/screenshots/r029-skill-copied.png`（MD5 `932135806F785EEC133F5BAB22B2A217`）：主窗状态栏「已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。」只出现一次；主路径仍是「复制码+密码」，未见新开 MCP 卡。
- `docs/screenshots/r029-skill-clipboard-preview.png`（MD5 `F951A1F00A3D7F1E985392538C33CD55`）：预览为与 R027/R028 相同的逐字正文（含「不能代替对方本机确认」「不要用这段说明去远程控制电脑」）。未覆盖 `r022-*`～`r028-*`。

缺口：菜单图是弹出菜单裁切，背后能看到官网深色页面；剪贴板预览状态栏为 UiShot 标注「状态栏仅提示复制结果；下方为剪贴板正文预览。」（真实反馈见 copied 图）。不挡验收。

已开 R030（官网安装包跟上含 R027–R029 的客户端；替换后首页旁注与导语不回退）。
