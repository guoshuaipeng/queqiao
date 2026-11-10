# Theme — 鹊桥 Tauri 客户端

## Part 1 — Compact token summary

### Colors
| Token | Value | Use |
|-------|-------|-----|
| `--blue` / primary | `#2563eb` | CTA、选中、链接 |
| `--green` | `#16a34a` | 已连接、成功 |
| `--red` | `#dc2626` | 结束协助、危险 |
| `--ink` | `#0f172a` | 主文案 |
| `--sub` | `#64748b` | 次文案 |
| `--line` | `#e2e8f0` | 边框分割 |
| `--bg` | `#f8fafc` | 浅底 |
| surface | `#ffffff` | 主面板 |
| wait amber | `#f59e0b` | 等待中 |
| primary hover | `#1d4ed8` | 主按钮悬停 |
| chip / muted fill | `#f1f5f9` | 顶栏芯片 |
| selected op | `#eff6ff` | 工具按钮选中底 |

### Typography
- Family: `"Microsoft YaHei UI", "PingFang SC", system-ui, sans-serif`
- Mono (结果/日志): `Consolas, "Cascadia Mono", monospace`
- Logo title: 24px / 700 / letter-spacing -0.03em
- Body / buttons: 14px / 500
- Hints: 12–13px / `--sub`
- Result dump: 12px mono / line-height 20px

### Spacing & radius
- Page horizontal padding: 32px (tight: 16–24px)
- Button height: 40px default, 34px ops, 32px sm
- Radius: buttons/inputs 10–12px; logo mark 8px; result box 8px
- Session endbar height: 64px (tight 44px)

### Shadows
- Primary button: `0 1px 2px rgba(37,99,235,0.18)`
- Auth cards: `0 10px 28px rgba(15,23,42,0.05)`
- Focus ring: `0 0 0 4px rgba(37,99,235,0.08)`

### Layout shell
- Full-height column: `.client` → header `.top` → main views → footer `.foot` / `.endbar`
- Session: status bar + scroll tools/results + sticky endbar

## Part 2 — Raw source

Primary stylesheet: `apps/queqiao-tauri/src/styles.css` (~1030 lines).

```css
:root {
  --blue: #2563eb;
  --green: #16a34a;
  --red: #dc2626;
  --ink: #0f172a;
  --sub: #64748b;
  --line: #e2e8f0;
  --bg: #f8fafc;
}
```

Key session styles live near `.session`, `.sbar`, `.ops`, `.result`, `.endbar`, `.path-row`, `.propose` in the same file. No Tailwind config in this app — vanilla CSS only.
