# 鹊桥 — 路线图

> 以 `docs/PRODUCT.md` 为准。桌面客户端只有 Tauri（`apps/queqiao-tauri`）。
> .NET 客户端各轮（R001–R045、R047–R049）的实现记录已随代码删除，不再作为维护入口。

## 仍有效

| 项 | 负责 | 状态 | 文档 |
|----|------|------|------|
| 公网中继访客配对 + 透明转发 | 鹊桥-服务端 | 保持 | `docs/relay-protocol.md`、`docs/relay-deploy.md` |
| 会话码审计只读列表（R046） | 鹊桥-服务端 | 本地已落地；公网未部署 | `docs/round-046.md`、`docs/relay-audit.md` |
| 官网 /admin 会话码只读（PV-001） | 鹊桥-服务端 | 完成（仅本地） | `docs/round-051.md` |
| 严格唯一凭证 / 短码可连（R050） | 架构评估后另授权 | **blocked**，不得当已实现 | `docs/round-050.md`、`docs/pake-unique-credential-eval.md` |
| Tauri 客户端按 `CLIENT-UI.md` | 鹊桥-客户端 | 进行中 | `docs/CLIENT-UI.md`、`apps/queqiao-tauri` |

## 明确不做

完整远程桌面、计费/组织、改 AI-SSH-Pro、恢复 .NET 客户端。
