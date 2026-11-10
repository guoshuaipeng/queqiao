# 鹊桥 Design System

## Product
鹊桥是亲友远程协助客户端：**不是远程桌面**，而是「先查看、改动需对方本机确认」的排障工具。协助端连上后应像轻量运维控制台：查进程/服务/目录/日志/文件，再提议改动。

## Brand voice
亲友话术、短句、少术语。主操作动词明确：「查看」「提议」「结束」「复制」。

## Visual language (keep across redesigns)
- **Palette**: primary `#2563eb` / hover `#1d4ed8`; success `#16a34a`; danger `#dc2626`; ink `#0f172a`; muted `#64748b`; line `#e2e8f0`; bg `#f8fafc`; surface `#fff`; wait `#f59e0b`; selected fill `#eff6ff`.
- **Type**: UI `"Microsoft YaHei UI", "PingFang SC", "Noto Sans SC", system-ui, sans-serif`. Results/mono `Consolas, "Cascadia Mono", monospace`.
- **Radius**: controls 10–12px; compact panels 8–12px; logo mark 8px with blue→cyan gradient `135deg #2563eb → #06b6d4`.
- **Chrome**: clean white desktop window, soft slate dividers, minimal shadow. No purple gradients, no cream/serif editorial, no dark neon.
- **Buttons**: height 32–40px; primary solid blue; danger outline/red text; selected tool blue border + `#eff6ff`.

## Home waiting IA (approved B)
1. After「开启接受协助」: left zone shows status「等待对方连接」.
2. **短码 / 密码分行**：短码大字；密码默认遮挡 + 眼睛；「复制连接凭证」在短码同行。
3. Copy once = full short credential `CODE-########` (code + password together).
4. Bottom secondary「停止接受」; right zone dimmed; home MCP strip may remain visible.

## Host session IA (approved B)
1. Status bar: green「正在接受协助 · 已连接」+ mono duration「已连接 MM:SS」(or HH:MM:SS).
2. **安全等级** inline segment on session page: 每次询问 / 宽松 / 完全信任 — wired to same `set_trust` as settings (full still needs confirm dialog).
3. Non-default trust keeps warning banners (完全信任 / 宽松) with quick revert.
4. Audit groups「查看」vs「改动」; empty:「对方还没有做任何操作」.
5. Endbar danger「结束本次协助」. Not a helper console / MCP / tool rail.

## Helper session IA (approved 1B + 2A)
1. Top brand bar + session status (connected green).
2. **Left tool rail**: groups「查看」vs「改动」; 改动 visually stronger but not equal to dump of 6 chips.
3. **Main workspace**: tool params + structured results + copy; session action timeline secondary.
4. End session always available, clear but not a empty full-width footer dominating the layout.
5. **「复制 MCP 配置」主入口在首页右区 + 设置**：未连接也可复制安装包（仅 command + 默认中继）。首页「我帮别人」在步骤与「连接」之间放浅色条（`#eff6ff` +「复制 MCP 配置」）。会话页可保留次要入口。短码交给 Agent 用 `connect`，不要绑死在 mcp.json。

## Motion
Subtle only: status dot pulse when waiting; selected tool state change. Prefer `prefers-reduced-motion` quiet.

## Do not
Invent new brand colors/fonts. Do not turn this into remote-desktop screen share UI. Do not hide confirmation-first mental model.
