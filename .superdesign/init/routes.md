# Routes — 鹊桥 Tauri 客户端

单页应用，无 URL 路由。视图靠 `hidden` 切换，逻辑在 `apps/queqiao-tauri/src/main.js`。

| View id | Role | Entry markup |
|---------|------|----------------|
| `#home` | 首页：请人帮我 / 我帮别人 | `main.zones` |
| `#host` | 被协助端会话（对方做过的事） | `section.session` |
| `#helper` | **协助端会话（连上远程后）** | `section.session` |
| `#account` | 个人中心 / 登录注册 | `section.set` |
| `#settings` | 设置（信任档、中继、MCP） | `section.set` |

## Helper session (`#helper`) — design target

1. Status bar: 正在协助对方 · 已连接 +「复制 MCP 配置」
2. 常用操作：ListProcesses / ListServices / ListDir / ReadLogTail / ReadFile / RunCommand
3. Conditional path row / propose form
4. 结果 + 复制结果 + 之前的结果
5. Footer endbar: 结束本次协助

Shell chrome always present: header brand / 个人中心 / 设置；session 时显示 `#endbar`，隐藏首页安全脚注。
