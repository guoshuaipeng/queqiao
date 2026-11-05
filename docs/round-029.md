# 第 29 轮 — Windows 接受协助端菜单补复制 Skill 说明

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**ready**
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
