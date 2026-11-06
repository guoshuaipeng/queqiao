# 中继会话码审计（R046 产品规格）

> 状态：**规格已拍板**（2026-11-06 Asia/Shanghai）；实现归鹊桥-服务端。  
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

## 设计

1. **SQLite 审计表**（复用 `RELAY_DATA`/`relay.db`，与 `guest_sessions` 分离）：至少记录  
   `code_generated` · `join_success` · `join_fail` · `session_end`  
   （实现可用等价英文短码 / status 枚举。）
2. **挂钩（不改协议字段）**：`SaveGuest` / `opGuestHost` 成功；`guest_join` 成败；pipe 结束与 wait timeout。  
   `guest_host` / `guest_join` JSON **保持** code + password。
3. **管理只读列表**：`created_at`（或事件时间）、`code`、`status`；可选已有协议允许的弱标识。**必须鉴权**；管理监听**不要**混在匿名 `17891` TCP 上。
4. **禁记 / 禁展**：pwd、password_hash（审计表不复制）、fp、完整 pair 载荷、业务内容。  
   等待态表仍可暂存 bcrypt 哈希供鉴权；管理 API/UI **不得返回** hash。
5. **与 site 接单分离**：新建中继只读管理面，或谨慎只读挂靠；不做工单化。

## 验收要点

- `DeleteGuest` 后审计历史仍在；失败 join 有行。  
- 未鉴权不可读列表。  
- grep/抽查无 pwd、hash、fp、完整凭证。  
- 现网控制面兼容；客户端与信任模型不动。

## 明确不做

业务 TLS/写操作审计搬中继；TOFU/降明文；客户端 `audit_*` op；完整后台产品。
