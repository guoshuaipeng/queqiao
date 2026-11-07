# PV-001 本地验收 · 官网 /admin 会话码只读列表 + 按码过滤

> 时间：2026-11-07 2026-11-07 08:24 Asia/Shanghai  
> 执行：鹊桥-服务端（产品收口派验收）  
> 实现 commit：`b964b8a`（`feat(relay,site): R046 audit MVP + PV-001 /admin session-code read view`）  
> HEAD 当时：含该 commit（`git merge-base --is-ancestor b964b8a HEAD` = true）  
> **公网 / 生产：未验收、未部署、未授权**（禁止把本文件当作上机依据）

## 范围

对照 `docs/PRODUCT-VERSIONS.md` PV-001 与 `docs/round-051.md` 最小闭环：

| # | 项 | 本轮 |
|---|----|------|
| 1 | relay `guest_audit` + 事件挂钩 + loopback audit HTTP（`RELAY_AUDIT_TOKEN`） | 验收 |
| 2 | site `GET /admin/api/session-codes` 代理；`admin.html` 会话码区（列表 + 按码过滤） | 验收 |
| 3 | 敏感字段不展示（pwd / hash / fp / 完整凭证 / RemoteAddr） | 验收 |
| 4 | 单测 + 可复现本机命令证据 | 验收 |
| — | 公网点验 / 上生产 / 新功能 / 改协议 / 改客户端 | **不做** |

## 代码锚点（相对 b964b8a）

- `relay/internal/relay/audit.go`、`audit_http.go`、`audit_test.go`
- `site/internal/site/server.go`（`handleSessionCodes` 敏感键剥离）
- `site/internal/site/server_test.go`（`TestSessionCodesRequiresAuthAndProxies` 等）
- `site/admin.html`（「会话码」区 + code 过滤）

## 测试矩阵

### A. Go 单测（强制重跑，非缓存）

`	ext
cd relay && go test -count=1 ./internal/relay/
# PASS · ok .../relay/internal/relay ~2.45s
# 含 TestGuestAuditLifecycleAndHTTP / TestAuditHTTPDisabledWithoutToken / TestClassifyJoinFail
# 及既有 guest/account 回归

cd site && go test -count=1 ./internal/site/
# PASS · ok .../site/internal/site ~1.30s
# 含 TestSessionCodesRequiresAuthAndProxies / TestSessionCodesUnconfigured
# 及既有接单/登录/下载回归（未破坏）
`

环境：本机 Go `go1.27.3 windows/amd64`；仓库 `E:\IdeaProjects\remote-assist` · `master`。

### B. 隔离本机进程冒烟（临时目录 + 随机 loopback 端口；测完杀进程并删临时目录）

| 步骤 | 结果 |
|------|------|
| 起 relay（`RELAY_AUDIT_*`）+ site（`SITE_RELAY_AUDIT_*` + `SITE_ADMIN_PASSWORD`） | 两进程存活 |
| `GET /audit/events` 无 token | **401** |
| TCP `guest_host` → 错密 `guest_join` → 正确 `guest_join` | generated / join_failed / paired / ended 入审计 |
| `GET /audit/events?code=PV001A` + Bearer | **200**；事件流水最新在前 |
| site `POST /admin/login` → `GET /admin/api/session-codes?code=PV001A` | **200**；`configured:true`；事件与中继一致 |
| `?code=NOEXIST` | `events:[]` |
| `GET /admin/api/session-codes` 未登录 | **401** |
| 登录后 `GET /admin` HTML | 含「会话码」区块 |

### C. 敏感字段检查

对 audit HTTP 与 `/admin/api/session-codes` 响应体抽查：

- 未见：`password_hash`、明文密码（本测 `testpass`）、`pwd`、`RemoteAddr`/`remote_addr`、`fp`、完整 pair 凭证
- 可见且预期：`code`、`event`、`ts`、`fail_reason`（短枚举如 `bad_credentials`）、`paired_at`/`closed_at` 等非敏感字段
- 单测侧：site 代理对假数据注入的 `password_hash`/`fp` 会剥离（`SHOULD_NOT_LEAK` 不得出现）

## 结论

- **本地验收：PASS**
- **公网 / 生产：未验收、未部署**（`121.196.45.81` 仍未授权）
- 依据用户先前答复（iteration-status Q1）：本地最小闭环原则通过；点验满意后可自标 PRODUCT-VERSIONS「完成」。本轮以单测 + 隔离 curl/进程冒烟作为本地点验证据，将 PV-001 标为 **完成（仅本地验收语义）**。

## 未做 / 阻塞

- 浏览器人手点验观感（文案/空态）：未阻塞本地收口；公网另授前可不挡。
- **未** SSH / 改生产 / push。