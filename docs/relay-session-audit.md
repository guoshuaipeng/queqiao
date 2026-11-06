# 中继侧会话码审计（R046 · 架构定稿）

> 架构拍板：2026-11-06 Asia/Shanghai。供产品 R046 与「鹊桥-服务端」实现；**本文为正式约定，实现前勿提前写代码以外的旁路方案。**  
> 对照：`relay-protocol.md` 访客生命周期、`relay/internal/relay/store.go`、`relay-deploy.md`、`architecture.md` §6、`tls-trust.md`。  
> 官网接单后台见 `site.md` / `site-deploy.md`（分库分进程；**非**本审计 MVP 必做入口）。

---

## 1. 目标与非目标

### 目标

- 中继记录访客**会话码生成 + 使用/失败/结束**，运维可查「某码何时生成、是否 join、成败、何时结束、时长」。
- 数据落**中继** SQLite；经**中继同机旁路只读 HTTP**列表查询（独立鉴权）。
- 不改 `guest_*` 客户端协议语义。

### 非目标 / 禁记（写死）

| 禁记项 | 说明 |
|--------|------|
| `pwd` / 验证码明文 | 不得写入审计行、旁路响应、普通日志 |
| `password_hash` 副本 | 仅留在瞬时表 `guest_sessions` |
| 完整 `remoteassist:pair…` 载荷 | 禁止 |
| `fp` | 禁止 |
| 业务内容 | pipe / 命令 / MCP 载荷禁止 |
| RemoteAddr / IP（v1） | **审计表不存、管理面不展**；排障继续靠现有 slog。若日后要弱标识，另开可选列且**默认关闭** |
| 账号 token | 禁止写入审计 |

审计**不替代** `tls-trust.md`：配对事实 ≠ 业务通道安全。短码不能单独建安全业务通道。

---

## 2. 现网生命周期（事件裁剪）

1. `guest_host` → 签发 code（约 2h 过期）→ `guest_sessions` 等待。  
2. `guest_join` 成功 → `paired` → pipe。  
3. `guest_join` 失败：`invalid code or password` / `guest session expired` / `peer not online` 等。  
4. Agent 等待超时 → `wait timeout`，`DeleteGuest`。  
5. Pipe 结束 → `session closed`，`DeleteGuest`。

`DeleteGuest` **只清等待态** `guest_sessions`，**不动**审计历史表。

**现网尚无审计表** → R046 新增（表名见 §5）。

---

## 3. 存储归属（定稿）

| 项 | 约定 |
|----|------|
| 库文件 | 仅中继 `RELAY_DATA`；生产 `/var/lib/remote-assist-relay/relay.db` |
| 与等待态关系 | 与 `guest_sessions` **同库不同表**；无外键 |
| site 接单库 | **不存**会话码审计（`SITE_DATA` / `site.db` 仍只服务接单） |
| 谁写 | 仅中继进程在控制路径追加插入 |
| 谁读 | 旁路只读 HTTP（及运维本机 sqlite）；site 若日后统一入口，只**代理**旁路，**不**写入 site SQLite |

依据：`site-deploy.md` 已规定与中继分文件、分用户、分进程；官网挂不得拖死中继。

---

## 4. 管理面选型（MVP · 定稿）

| 项 | 约定 |
|----|------|
| 形态 | **中继同机旁路只读 HTTP**（独立监听，**不**占用/改语义 TCP `17891` JSONL） |
| 鉴权 | **独立**运维配置（如 `RELAY_AUDIT_TOKEN` Bearer，或同等运维口令）；**不**复用中继 `signup`/`token`；**不**把官网 `/admin` 的 `SITE_ADMIN_PASSWORD` 当本 MVP 必依赖 |
| 能力 | 只读 list（按时间/code/事件筛选）；无改删审计行的对外 API（清理只走保留任务） |
| 协议 | **不**增加客户端 `guest_*` / audit op；不进 `relay-protocol.md` 消费方契约 |
| 官网 `/admin` | **非 MVP 必做**。后续若要统一入口：site `/admin` **代理**旁路即可，仍不直连/写入中继库文件 |
| 隔离理由 | 现网中继无 HTTP；site 与中继分库。旁路把「接单后台」与「中继审计读面」拆开，官网故障不影响中继配对 |

公网安全组：旁路默认只绑 `127.0.0.1`（或等价本机）；若绑非本机，须防火墙显式收紧，**不要**随 17891 对公网敞开。

---

## 5. 事件、字段与表

### 5.1 事件

| event | 含义（列表归类） |
|-------|------------------|
| `code_created` | **生成** |
| `guest_join_ok` | **使用**（成功） |
| `guest_join_fail` | **失败** |
| `agent_wait_timeout` | **失败**（等待超时） |
| `session_end` | **结束** |

失败 `reason`：`bad_credentials` / `expired` / `peer_offline` / `busy` / `wait_timeout` / `other`；可选 `error_code`（英文短句，≤96，禁 password）。  
不单独造扫表型 `code_expired`（过期归入 fail+`expired`）。

### 5.2 表名与字段

表名：**`guest_audit`**（追加型历史；R046 新增）。

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | INTEGER PK AUTOINCREMENT | |
| `ts` | INTEGER | Unix 秒；UI 转 Asia/Shanghai |
| `event` | TEXT | 上表 |
| `code` | TEXT | **可记** |
| `mode` | TEXT | R046 固定 `guest` |
| `ok` | INTEGER | 1/0 |
| `reason` | TEXT | 可空 |
| `error_code` | TEXT | 可空 |
| `peer_role` | TEXT | `agent` / `helper`，可空 |
| `session_key` | TEXT | 建议；实现可弱化为 code+时间窗 |
| `duration_sec` | INTEGER | 仅 `session_end` |
| `expires_at` | INTEGER | 仅 `code_created` |

**v1 不设** `client_addr` / IP 列。

索引建议：`ts`、`code`、`(event, ts)`。

### 5.3 「谁」的最小范围

- **要**：`code`、事件结果（生成/使用/失败/结束）、时间；结束行建议时长。  
- **建议**：`peer_role`。  
- **不要（v1）**：IP/RemoteAddr、访客路径下的 `device_id`、中继账号 username、完整载荷/fp/pwd。

---

## 6. 保留策略（定稿）

| 项 | 约定 |
|----|------|
| 默认 | **90 天**（按 `ts` 删除更早的 `guest_audit` 行） |
| 配置 | 环境变量示意 `RELAY_AUDIT_RETENTION_DAYS=90`（`0` 表示关闭按天清理，须另有运维约定） |
| 条数上限 | 可选第二维（实现时可加 `RELAY_AUDIT_MAX_ROWS`）；未配置则仅按天 |
| `DeleteGuest` | **只**删/清 `guest_sessions` 等待态，**永不**删 `guest_audit` 历史（除非命中上表保留任务） |

---

## 7. 管理面列表列

| 列 | 来源 |
|----|------|
| 时间 | `ts` |
| 会话码 | `code` |
| 生成 / 使用 / 失败 / 结束 | `event`（+ `reason` / `duration_sec`） |

推荐先做事件流水；按码透视为增强。响应禁含 pwd、完整载荷、fp、业务内容、IP。

---

## 8. 与 site 管理台关系（摘要）

| | 中继审计 MVP | 官网 `/admin` |
|--|--------------|---------------|
| 库 | `relay.db` → `guest_audit` | `site.db` → `customers`/`messages` |
| 鉴权 | `RELAY_AUDIT_*`（旁路独立） | `SITE_ADMIN_PASSWORD` + `rasite_session` |
| MVP | **必做旁路只读** | **非必做**会话码列表 |
| 后续 | 不变 | 可选代理旁路，不共用 SQLite 文件 |

证据（只读）：`site/internal/site/store.go` 无 audit 表；`relay/internal/relay/store.go` 现仅有 `guest_sessions` 等待态；`docs/site-deploy.md` 明确不分 SQLite 文件。

---

## 9. Open questions（实现细节，不改拍板）

1. 旁路监听默认：仅 `127.0.0.1:端口` 还是 Unix socket？端口号写入 `relay-deploy.md` 时由服务端选定。  
2. `session_key` 首轮是否生成稳定 id？  
3. 可选条数上限默认值是否需要（仅 90 天是否足够）？

---

## 10. 文档地图回链

- `architecture.md` §6 / §9  
- `relay-deploy.md`（环境变量与旁路约定）  
- **不要**把 audit 读接口写进 `relay-protocol.md` 客户端 ops  

---

## 11. 变更记录

| 日期（Asia/Shanghai） | 说明 |
|----------------------|------|
| 2026-11-06 | 草案初稿与 site 边界讨论 |
| 2026-11-06 | **定稿**：落 `relay.db`/`guest_audit`；旁路只读 HTTP；v1 不存 IP；保留默认 90 天；官网 `/admin` 非 MVP 必做 |
