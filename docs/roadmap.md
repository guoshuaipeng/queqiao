# 鹊桥 — 路线图

> 以 `docs/PRODUCT.md` 为准；需求队列见 `docs/PRODUCT-VERSIONS.md`（标「待开发」才推进）。
> 桌面客户端只有 Tauri（`apps/queqiao-tauri`）；.NET 客户端已删除，不恢复。

## 当前版本状态（v0.2）

| 项 | 状态 |
|----|------|
| PV-001 官网 /admin 会话码只读 | 完成（仅本地验收）；公网未授权 |
| PV-002 首页 / 协助端观感 | 待验收（留白缺口） |
| PV-003 安装包图标 | 完成（仅本地验收） |
| PV-004 安装界面语言 | 部分完成（可视像素缺口） |

细节与最新状态以 `PRODUCT-VERSIONS.md` 为准。

## 仍有效 / 后续

| 项 | 状态 | 文档 |
|----|------|------|
| 公网中继访客配对 + 透明转发 | 保持（生产已验收） | `docs/relay-protocol.md`、`docs/relay-deploy.md` |
| 账号 / 设备列表 / 免密连接 / 无人值守 | 2026-11-09 拍板恢复，随当前版本迭代 | `docs/PRODUCT.md` |
| 中继会话码审计（R046） | 本地已落地；公网未部署 | `docs/relay-audit.md` |
| 严格唯一凭证 / 短码可连（R050） | **blocked**：产品意图已锁定（code+8位密码唯一凭证、成熟 PAKE、服务端不知明文），未授权实现；安全验收通过前不得宣称 | `docs/PRODUCT.md`、`docs/tls-trust.md` |
| MCP 控制面 | **完成（仅本地验收 · PV-005）**：Rust stdio `RemoteAssist.Mcp`；`publish-mcp.ps1` 后可用；未进安装包 / 未公网发版 | `docs/MCP.md`、`docs/architecture.md` §4 |

## 明确不做

完整远程桌面、计费/组织、改 AI-SSH-Pro、恢复 .NET 客户端。