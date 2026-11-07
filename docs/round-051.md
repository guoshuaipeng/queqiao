# 第 51 轮 — PV-001 官网 /admin 会话码只读管理（最小闭环）

> 产品/实现：鹊桥-持续迭代 · 2026-11-06 13:14 Asia/Shanghai  
> 状态：**完成（本地验收）**（2026-11-07；证据 docs/acceptance/pv-001-session-codes-20261107.md；**公网未做/未授**）
> 实现记录更新：2026-11-06 13:23 Asia/Shanghai  
> 依据：docs/PRODUCT-VERSIONS.md **PV-001**（用户原文不改）；对接 docs/round-046.md / docs/relay-audit.md  
> **不上生产**；无单独对话授权不 push / 不改公网机。

## 最小闭环（本轮只做这些）

验收对齐 PV-001：

1. 登录现有 `/admin` 后能看到 **会话码** 管理入口（与接单留言同后台，不另起后台）。
2. 能 **列表** 与 **按 code 过滤** 查看会话码相关记录。
3. **永不展示**：密码明文、`password_hash`、完整连接凭证、`fp`、业务载荷。
4. 现有接单（新留言 / 已联系 / 已结束）能力不破坏。

本轮 **不做**：作废码、结束会话、改删审计、公网上机、透视汇总大盘。

## 对接方式（相对 R046 的增量）

| 层 | 约定 |
|----|------|
| 数据 | 仍只在中继 `relay.db` 的 `guest_audit`（R046）；**site.db 不存**会话码 |
| R046 | 若尚未实现：本轮一并落地 MVP（表 + 事件挂钩 + `RELAY_AUDIT_TOKEN` 鉴权的 **loopback** 只读 HTTP 事件流水，可按 code 过滤） |
| site | `/admin` 新增只读 UI + `GET /admin/api/session-codes`；服务端 **HTTP 代理**到中继审计旁路（`SITE_RELAY_AUDIT_URL` + 同 token 头），**禁止** site 进程直接打开中继库文件 |
| 鉴权 | 运营仍用 `SITE_ADMIN_PASSWORD` 进 /admin；中继旁路仍要 `RELAY_AUDIT_TOKEN`；二者独立 |
| 公网 | 生产中继旁路默认仍仅 loopback；site 与中继同机或经安全内网可达时由 site 代理。**本轮只本地验证**，公网部署另授权 |

## 展示

- 形态：事件流水（最新在前），列：时间、code、event/status；可选 fail_reason 等非敏感字段。
- 过滤：query `code=`。

## 验证计划

- `go test`：`relay/...` 审计写入与管理 API；`site/...` 代理与未登录 401、响应无敏感字段。
- 本机起 site + 带 audit 的 relay（loopback），浏览器或 curl：登录 /admin → 见入口 → 列表/按码过滤。
- **编译/单测 PASS ≠ 公网验收**；公网 `121.196.45.81/admin` 未授权不上机。

## 非目标

- 不改 `guest_*` 协议字段；不改 AI-SSH-Pro；不绕过被控写确认；不自动验收 R049。

## 实现记录（2026-11-06 13:23 Asia/Shanghai）

- R046 MVP：`guest_audit` 表；generated/paired/join_failed/wait_timeout/ended/abandoned；`RELAY_AUDIT_ADDR`/`RELAY_AUDIT_TOKEN`；`GET /audit/events`。
- site：`GET /admin/api/session-codes` HTTP 代理；`admin.html` 会话码只读区 + code 过滤。
- 验证：`go test` relay+site PASS；本机起 relay+site curl 列表/过滤 PASS；**未**浏览器手工点验；**未**上生产。

## 本地验收收口（2026-11-07）

- 证据：`docs/acceptance/pv-001-session-codes-20261107.md`。
- go test relay+site PASS；隔离本机进程冒烟 PASS。
- **公网未验收、未部署**。
