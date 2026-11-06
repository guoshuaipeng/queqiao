# 中继侧会话码审计（R046 · 架构定稿）

> 架构拍板：2026-11-06 Asia/Shanghai。与产品规格 `docs/relay-audit.md` / `docs/round-046.md` 对齐；实现归「鹊桥-服务端」。**尚未写业务代码、不上机。**  
> 部署侧摘要：`docs/relay-deploy.md`「会话码审计（R046）」。  
> 官网接单 `site/` `/admin`：**本轮不做**会话码读面（分库分进程）。

---

## 1. 目标与非目标

### 目标

- 中继持久记录访客会话码的**生成 / 配对成功 / join 失败 / 等待超时 / 结束**。
- 数据仅落中继 `relay.db`；经**同机旁路只读 HTTP**（独立 token）列表查询。
- **不改** `guest_*` 客户端协议字段。

### 非目标 / 禁记（写死）

| 禁记项 | 说明 |
|--------|------|
| `pwd` / 验证码明文 | 审计行、旁路响应、普通日志均不得出现 |
| `password_hash` 副本 | 仅留在瞬时表 `guest_sessions`；管理 API 不得返回 hash |
| 完整 `remoteassist:pair…` 载荷 | 禁止 |
| `fp` | 禁止 |
| 业务内容 | pipe / 命令 / MCP 载荷禁止 |
| RemoteAddr / IP（v1） | **表不存、管理面不展**；排障靠现有 slog。日后弱标识另开可选列且默认关 |
| 账号 token | 禁止写入审计 |

审计**不替代** `tls-trust.md`。短码不能单独建安全业务通道。

---

## 2. 现网生命周期与挂钩

1. `guest_host` / `SaveGuest` 成功 → 签发 code（约 2h）→ 等待。  
2. `guest_join` 成功配对 → `paired` → pipe。  
3. `guest_join` 失败 → 错误回客户端（现网未入库存档）。  
4. Agent 等待超时 → `wait timeout`，`DeleteGuest`。  
5. Pipe 结束 → `session closed`，`DeleteGuest`。

**`DeleteGuest` 只清 `guest_sessions` 等待态，不得删除审计历史。**  
现网无审计表 → R046 新增 `guest_audit`。

---

## 3. 存储归属（定稿）

| 项 | 约定 |
|----|------|
| 库 | 仅 `RELAY_DATA`；生产 `/var/lib/remote-assist-relay/relay.db` |
| 表 | **`guest_audit`**，与 `guest_sessions` **同库不同表**、无外键 |
| site | **`site.db` 不存**会话码审计 |
| 谁写 | 仅中继进程；`INSERT` 失败只 slog，**不阻断**配对主路径 |
| 谁读 | 旁路只读 HTTP（及运维本机工具）；后续若统一入口，site 只**代理**旁路，禁止直接打开中继库文件 |

依据：`site-deploy.md` 分文件 / 分用户 / 分进程；官网挂不得拖死中继。

---

## 4. 管理面选型（MVP · 定稿）

| 项 | 约定 |
|----|------|
| 形态 | **中继同机旁路只读 HTTP**；**不**占用、不改语义 TCP `17891` |
| 地址 | `RELAY_AUDIT_ADDR` **默认仅 loopback**；生产经 **SSH 隧道**访问；不得裸暴露公网 |
| 鉴权 | 配置 **`RELAY_AUDIT_TOKEN` 后才启用**管理 HTTP；未配置 = 不开。与 `SITE_ADMIN_PASSWORD`、中继 `signup`/`token` **均独立** |
| 能力 | 只读事件流水（**最新在前**），可按 **code** 过滤；无改删审计的对外 API |
| UI | **不做**按 code 透视表（产品锁） |
| 官网 `/admin` | **本轮不做**；非 MVP 必做读面 |
| 协议 | 不增加客户端 `audit_*` op；不进 `relay-protocol.md` 消费方契约 |

---

## 5. 事件与字段

### 5.1 MVP 事件（与产品锁同名）

| event | 挂钩 | 列表归类 |
|-------|------|----------|
| `generated` | `opGuestHost` / `SaveGuest` 成功 | 生成 |
| `paired` | join 校验通过并配对成功 | 使用 |
| `join_failed` | `opGuestJoin` 失败 | 失败 |
| `wait_timeout` | Agent 等待超时 | 失败 |
| `ended` | pipe / 会话结束 | 结束 |

- `abandoned`（未配对放弃）：同轮**可选**，便宜则做。  
- `join_failed.fail_reason` 短枚举：`bad_credentials` / `expired` / `peer_offline` / `busy` / `too_many_sessions` / `bad_request` / `other`；**不**写用户输入的 password。

### 5.2 表字段（可落 SQLite）

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | INTEGER PK AUTOINCREMENT | |
| `ts` | INTEGER | Unix 秒；展示转 Asia/Shanghai |
| `event` | TEXT | 上表 |
| `code` | TEXT | **可记** |
| `ok` | INTEGER | 可选；1/0，便于筛选 |
| `fail_reason` | TEXT | 仅失败类 |
| `peer_role` | TEXT | 可选：`agent` / `helper` |
| `session_key` | TEXT | 可选关联 id |
| `duration_sec` | INTEGER | 仅 `ended` |
| `expires_at` | INTEGER | 仅 `generated` |

**v1 不设** `client_addr` / IP 列。  
索引：`ts DESC`、`code`、`(event, ts)`。

### 5.3 「谁」最小集

- **要**：时间、`code`、事件（生成/使用/失败/结束）。  
- **建议**：`fail_reason`、`duration_sec`。  
- **不要（v1）**：IP、`device_id`（访客路径无）、中继 username、pwd/fp/载荷。

---

## 6. 保留策略（定稿）

| 项 | 约定 |
|----|------|
| 默认 | **90 天**（`RELAY_AUDIT_RETENTION_DAYS`，可配） |
| 动作 | 超期删除 **`guest_audit`** 行 |
| `DeleteGuest` | 只清等待态，**不动**审计历史 |

可选条数上限由实现评估，非拍板阻塞项。

---

## 7. 列表列（旁路 / 管理只读）

| 列 | 来源 |
|----|------|
| 时间 | `ts` |
| 会话码 | `code` |
| 生成 / 使用 / 失败 / 结束 | `event`（+ `fail_reason` / `duration_sec`） |

响应禁含：pwd、hash、完整载荷、fp、业务内容、IP。

---

## 8. Open questions（实现细节）

1. `RELAY_AUDIT_ADDR` 具体端口号（写入 unit 后回填 `relay-deploy.md`）。  
2. `session_key` / `peer_role` 是否首轮落地。  
3. 是否加条数上限作为第二保留维。

---

## 9. 文档地图

- 产品规格：`relay-audit.md`、`round-046.md`  
- 本文：架构定稿（存储/旁路/IP/保留/字段）  
- 部署：`relay-deploy.md`  
- 总览回链：`architecture.md` §6 / §9  
- **不要**把旁路读接口写入 `relay-protocol.md` 客户端 ops  

---

## 10. 变更记录

| 日期（Asia/Shanghai） | 说明 |
|----------------------|------|
| 2026-11-06 | 草案与 site 边界讨论 |
| 2026-11-06 | **定稿**：`relay.db`/`guest_audit`；旁路只读 HTTP；v1 不存 IP；保留 90 天；site `/admin` 本轮不做；事件名对齐产品锁 |