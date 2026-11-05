# 第 27 轮 — Skill 薄封装（如何安装 MCP）

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**done（产品已验收）**
> 依据：R026 产品已验收（官网包已含 R025「通常不会下载」指引）；P2-5「Skill 薄封装（仅安装 MCP 指引，非控制面）」界面三步已在 R005/R017 落地，技能说明本身仍 backlog。亲友或协助方把配置交给智能体时，还缺少一段可直接保存为技能的白话说明。

官网下载与安装说明这条线（R019–R026）已经闭环。本轮补上产品文档里写明的 Skill 薄封装：只告诉智能体怎么安装鹊桥的 MCP、怎么填参数，不把 Skill 做成控制面，也不能绕过对方本机确认。

## 本轮目标

1. **高频**：去协助端「如何安装 MCP」卡片增加「复制 Skill 说明」，复制内容只讲如何安装 MCP / 如何填参数。
2. **打磨**：复制后给人话反馈，只出现一次。

## A. 复制 Skill 说明（高频 · 鹊桥-客户端）

在去协助端「如何安装 MCP」卡片内（已有三步和「复制 MCP 配置」旁边）增加轻按钮「复制 Skill 说明」。

剪贴板正文用下面这段（逐字，换行保留）：

把下面这段保存成智能体的技能说明（只用来告诉它怎么安装鹊桥的 MCP，不能代替对方本机确认）。

1. 在鹊桥里点「复制 MCP 配置」。
2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。
3. 刷新或重启后再用智能体协助。

对方电脑上要改动的操作仍会弹确认，智能体不能绕过。不要用这段说明去远程控制电脑。

验收：

- 「复制 Skill 说明」是轻按钮（白底蓝字一类），不要做成实心绿；该屏唯一实心绿仍是「连接」。「复制 MCP 配置」「复制安装说明」保持已验收轻按钮。
- Windows 去协助端与 Linux Avalonia 去协助端都要有（共用文案常量更好）。
- 剪贴板就是上面这段，不要加端口、17891、Assist、ownerLink、TLS、证书、签名、HTTP、「不安全」、SmartScreen。
- 不要因为展示或复制这段说明，就自动对外监听 MCP。
- 不要改「如何安装 MCP」已验收三步、「复制 MCP 配置」及反馈、不要改「复制安装说明」/ `InstallInstructions`、不要改 `docs/trust-levels.md` 的「才问你」。
- 接受协助端本轮不做（与 R017→R018 相同，先做去协助）。
- 截图放到 `docs/screenshots/r027-*`（至少：入口能看到新按钮、复制反馈、剪贴板预览能读到上文）。**勿覆盖** `r022-*`～`r026-*`。

## 打磨：复制后人话反馈

复制成功后提示（只出现一次）：

已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。

不要和已验收的「已复制 MCP 配置。请粘贴到 Cursor 的 MCP 设置并保存。」或「说明里已带上官网下载地址，发给对方即可」写成同一句。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 把 Skill 做成控制面或远程控制
- 替换公网安装包
- 接受协助端的 Skill 入口（可交后续轮）
- 把未上真机的 macOS 包写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS
- 改信任等级「才问你」

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-06 Asia/Shanghai）

### 实现摘要

- 去协助端「如何安装 MCP」卡增加轻按钮「复制 Skill 说明」；Windows WinForms（Helper.UI）与 Linux Avalonia（HelperWindow）共用 `McpInstallGuide.CopySkillButton` / `SkillInstructions` / `SkillInstructionsClipboard` / `SkillCopiedHint`。
- 剪贴板正文与 round-027.md **逐字一致**；反馈「已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。」只出现一次（Avalonia 写 `_mcpHint`；WinForms 写预览行；均不叠到状态行）。
- 「连接」仍是唯一实心绿；「复制 MCP 配置」「复制安装说明」保持轻按钮与已验收反馈/三步/安装说明原文。
- **未改** 接受协助端（MainWindow）；未自动对外监听 MCP；未改 `docs/trust-levels.md`「才问你」；**未换包**。

### 最终逐字文案

**SkillCopiedHint**

> 已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。

**SkillInstructions / SkillInstructionsClipboard**

```
把下面这段保存成智能体的技能说明（只用来告诉它怎么安装鹊桥的 MCP，不能代替对方本机确认）。

1. 在鹊桥里点「复制 MCP 配置」。
2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。
3. 刷新或重启后再用智能体协助。

对方电脑上要改动的操作仍会弹确认，智能体不能绕过。不要用这段说明去远程控制电脑。
```

### 截图（`docs/screenshots/r027-*`，已复制 `%USERPROFILE%\Downloads\r027-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r027-skill-entry.png` | 「复制 Skill 说明」轻按钮与「复制 MCP 配置」并列；「连接」实心绿；三步未改；ui-shot | DE73CF0744C043EC96C1C353717A2FA4 |
| `r027-skill-copied.png` | 反馈「已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。」只一次；状态仍「未连接」；ui-shot | 1D4BF356D9CB4C319167C8618FBD4B61 |
| `r027-skill-clipboard-preview.png` | 预览含 Skill 说明逐字正文；ui-shot | A4471DB93526EC54F51EC7E933AD7708 |

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

## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r027-*` 与交付说明独立验收通过（实现 `65f2e2d`）。

独立核对：

- `r027-skill-entry.png`：去协助端「如何安装 MCP」内「复制 Skill 说明」为轻按钮，与「复制 MCP 配置」并列；该屏唯一实心绿仍是「连接」；三步原文未改。
- `r027-skill-copied.png`：反馈「已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。」只出现一次；状态仍「未连接」。
- `r027-skill-clipboard-preview.png`：预览正文与 round-027 剪贴板要求逐字一致；未见端口 / 17891 / Assist / ownerLink / TLS / 证书 / 签名 / HTTP /「不安全」/ SmartScreen。
- 交付说明与 `McpInstallGuide` 常量披露 Windows WinForms 与 Linux Avalonia 共用文案；本轮截图以 Avalonia ui-shot 为主（与 R017 同类）。
- 未改接受协助端、未换公网安装包、未自动对外监听 MCP、未改 trust-levels「才问你」、未改中继/官网/AI-SSH-Pro。

缺口：未单独拍 WinForms 截图（同源常量已披露，不挡验收）；接受协助端 Skill 入口本轮明确不做，交 R028；公网包仍为 R026。

已开 R028（接受协助端 Skill 薄封装：复制一段只讲如何安装 MCP 的技能说明；不改中继/官网/AI-SSH-Pro）。
