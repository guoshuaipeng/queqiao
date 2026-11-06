# 中继会话码审计（R046 产品规格）

> 状态：**MVP 已落地本地**（2026-11-06 13:23 Asia/Shanghai）；未上生产。实现归鹊桥-服务端 / 持续迭代。  
> 轮次：`docs/round-046.md`。详细事件草案可另见 `docs/relay-session-audit.md`（若存在）。

## 要解决什么

运维要能回答：某会话码**何时生成**、是否有人 **join（成功/失败）**、会话**何时结束**。列表在**管理台只读**可查。

## 现网缺口（只读评估）

| 项 | 现状 |
|----|------|
| 等待态 | `guest_sessions`：`SaveGuest` 写入；结束后 **`DeleteGuest` 物理删除** → 无历史 |
| 日志 | stdout slog：`agent waiting` / `session paired` / `session closed` / `wait timeout`（`key`=code）；**无持久审计表** |
| join 失败 | 仅 JSONL 错误回客户端；**未结构化入库存档** |
| 管理台 | 中继**无** HTTP；`site/admin` 只管官网接单，**与中继库不通** |

## 设计锁（产品拍板）

### 存储

1. **表**：`relay.db`.`guest_audit`（追加型事件流水；与 `guest_sessions` 分离）。**site 库不存**。
2. **`DeleteGuest` 不删审计行**。
3. **保留默认 90 天**。
4. **v1 不存 RemoteAddr**。
5. **禁记 / 禁展**：pwd、password_hash（审计表不复制）、fp、完整 pair 载荷、业务内容。等待态表仍可暂存 bcrypt 哈希供鉴权；管理 API/UI **不得返回** hash。
6. **不改** `guest_*` 协议字段。

### MVP 事件（必做）

| event | 含义 |
|-------|------|
| `generated` | 会话码签发成功 |
| `paired` | join 成功并配对 |
| `join_failed` | join 失败（短枚举 `fail_reason`，不写 password） |
| `wait_timeout` | 等待超时 |
| `ended` | pipe / 会话结束 |

- `abandoned`：同轮可选（便宜则做）。
- 审计 INSERT 失败只 slog，**不阻断**配对。

### 管理面

1. **`RELAY_AUDIT_ADDR` 默认仅 loopback**；生产经 **SSH 隧道**访问。
2. **`RELAY_AUDIT_TOKEN` 必填才启用**管理 HTTP；未配置 = 不开。
3. **绝不**裸暴露公网，**绝不**挂在业务口 **17891**。
4. **UI**：事件流水（**最新在前**），可按 **code 过滤**；**不做**按 code 透视表。
5. **site `/admin` 本轮不做**（接单与中继会话分离）。

### 挂钩（不改协议）

`opGuestHost`/`SaveGuest` 成功 → `generated`；`guest_join` 失败 → `join_failed`；配对成功 → `paired`；wait timeout → `wait_timeout`；pipe 结束 → `ended`。  
`guest_host` / `guest_join` JSON **保持** code + password。

## 验收要点

- `DeleteGuest` 后审计历史仍在；失败 join 有 `join_failed` 行。  
- 旁路仅 loopback + token；事件流水最新在前，可按 code 过滤；未鉴权不可读。  
- grep/抽查无 pwd、hash、fp、完整凭证、v1 RemoteAddr。  
- 现网控制面兼容；客户端与信任模型不动。

## 明确不做

业务 TLS/写操作审计搬中继；TOFU/降明文；客户端 `audit_*` op；完整后台产品；site 接单台改会话台；公网裸管理口；按 code 透视表。
