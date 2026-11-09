# 鹊桥（Remote Assist）Design System

## Product
- **Name**: 鹊桥
- **Type**: Windows desktop remote-assist client for trusted family/friends
- **Core jobs**: (1) 请人帮我 — share a credential and accept help; (2) 我帮别人 — paste credential and connect
- **Audience**: Non-technical Chinese users; copy must stay plain, short, and reassuring
- **Window**: Client area 840×480; title bar + content; desktop app chrome, not a website

## Brand & tone
- Trust, calm, family/friends — not enterprise SOC, not flashy consumer SaaS
- Safety-first: consent dialogs, default reject focus, clear “only share with people you trust”
- Chinese UI copy; avoid jargon (no “session token”, use 连接凭证)

## Visual foundation (current baseline)
- **Primary**: `#2563eb`
- **Ink**: `#0f172a`
- **Muted**: `#64748b`
- **Line**: `#e2e8f0`
- **Surface**: white window on soft gray page backdrop
- **Success / warn / danger**: `#16a34a` / `#ea580c` / `#dc2626`
- **Radius**: window ~10px, zones/cards ~12px, buttons ~8px
- **Type**: system Chinese UI fonts (Microsoft YaHei UI / PingFang SC); monospace for codes/credentials
- **Logo mark**: small rounded square, blue→cyan gradient

## Layout patterns
- Home: two equal zones side-by-side (请人帮我 | 我帮别人)
- Active zone gets blue border + soft blue ring; inactive can dim
- Session pages: status bar, scroll body, fixed end bar with 结束本次协助
- Modals: centered dialog on dim mask; default focus on 拒绝 for safety

## Interaction states to preserve
- Waiting for peer (amber pulsing dot)
- Connecting / waiting allow (spinner)
- Pair confirm countdown on 允许协助
- Trust banner when 完全信任
- Action log with allow/deny outcomes

## Redesign goals for this trial
- Keep the two-zone mental model and all Chinese copy intent
- Elevate visual craft: clearer hierarchy, calmer density, less generic blue SaaS feel
- Stay desktop-app sized (840×480), readable for older relatives
- Avoid purple gradients, Inter/Roboto defaults, floating marketing badges, noisy shadows
