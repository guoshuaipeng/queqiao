# 第 46 轮 — 会话码生成/使用审计 + 管理台只读查看

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 实现：鹊桥-服务端（主）；架构对齐文档  
> 状态：**open（规格已拍板）** — 只读评估结论已合入；**产品锁已补齐（旁路本机 + 事件流水 MVP）**；**尚未实现代码**  
> 依据：用户要求「服务端记录生成的 code，以及使用记录；管理台可以查看」。只读评估要点（现网 `DeleteGuest` 抹历史、join 失败未入审计、无中继管理 HTTP、site/admin 接单分离）已收口进本轮与 `docs/relay-audit.md`。  
> **本轮主题**：中继 SQLite **审计表** + **鉴权只读管理列表**；**不**改客户端亲友 UI、**不**改 `guest_*` 协议字段、**不**在用户本机跑应用。

## 产品拍板（设计锁死）

### 存储与边界（锁）

1. **表**：仅 `relay.db` 内 **`guest_audit`**（与瞬时等待态 `guest_sessions` **分离**）。**site 库不存**会话码审计。
2. **`DeleteGuest` 不得删除审计行**；等待态可删，审计历史保留。
3. **保留期默认 90 天**（写入 `relay-deploy.md` / 实现配置）。
4. **v1 不存 `RemoteAddr` / IP**（不把 IP 当实名）。
5. **严禁存储或展示**：`pwd` / 验证码明文、`password_hash`（审计表不复制）、业务证书指纹 `fp`、完整 `remoteassist:pair` / 完整连接凭证、业务管道内容。
6. **协议**：`guest_host` / `guest_join` JSON 字段**不改**；审计为服务端副作用。不引入客户端 `audit_*` op。
7. **与官网接单后台分离**：现有 `site/` `/admin` **本轮不做**、与中继库分文件、不互通。本轮管理面 = **中继同机旁路只读 HTTP**，**不要**把接单工单体系做成中继会话管理台。

### MVP 事件（锁）

挂钩为**追加型事件行**（非按 code 透视表）。**MVP 必做**：

| event | 挂钩 |
|-------|------|
| `generated` | `opGuestHost` / `SaveGuest` 成功签发 |
| `paired` | `registerHelper` / join 校验通过并配对成功 |
| `join_failed` | `opGuestJoin` 失败路径；`fail_reason` 短枚举（如 bad_credentials / expired / peer_offline / busy / too_many_sessions / bad_request）；**不**写用户输入的 password |
| `wait_timeout` | 等待超时 |
| `ended` | pipe / 会话正常结束 |

- **`abandoned`（未配对放弃）**：同轮**可选**，实现成本低则一并做；非阻塞验收。
- 审计 `INSERT` 失败只打 slog，**不得阻断**配对主路径。

### 管理面（锁）

1. **旁路地址**：`RELAY_AUDIT_ADDR` **默认仅 loopback**（如 `127.0.0.1` 某端口）；**不得**裸暴露在公网，**不得**挂在 `17891` 业务 TCP 上。
2. **生产访问**：经 **SSH 隧道** 访问本机旁路；运维文档须写明该约定。
3. **鉴权**：设置 **`RELAY_AUDIT_TOKEN` 后才启用**管理 HTTP；未配置 token = 不开管理面。公网匿名 / 普通客户端 / 持码访客 / MCP **不可**读。
4. **UI / 列表形态**：**事件流水**（**最新在前**），可按 **`code` 过滤**；**不做**按 code 透视汇总表 / pivot。
5. **展示列**：事件时间、`code`、`event`/`status`；可选 `paired_at` / `closed_at` / `fail_reason` / `duration_sec` / `peer_role`。**永不展示**验证码、hash、fp、完整凭证。

### 现网缺口（评估确认）

`DeleteGuest` 在配对结束/超时后**抹掉等待态历史**；`guest_join` 失败仅回客户端错误串、**无结构化审计行**；中继**无** HTTP 管理面；stdout slog 有 `key`=code 的配对/关闭信息，**不能**单独满足管理台列表。

权威短规格：`docs/relay-audit.md`。更细事件/字段草案可参考同目录架构稿（若有 `relay-session-audit.md`）。

## 建议表形态（实现可微调列名，语义不变）

```text
guest_audit（追加型事件流水）
  id, code, created_at|ts, event|status,
  paired_at?, closed_at?, fail_reason?, duration_sec?, peer_role?,
  -- 禁止：password, password_hash, fp, payload, full remoteassist:pair, RemoteAddr(v1)
```

- 等待态表 `guest_sessions` 仍可在结束后 `DeleteGuest`；**审计表行保留**（默认 **90 天**）。
- 审计 `INSERT` 失败**不得**阻断配对主路径（只记服务日志）。

## 管理面切片（MVP）

| 优先级 | 内容 |
|--------|------|
| 必做 | `guest_audit` 落库 + MVP 五事件挂钩 + `RELAY_AUDIT_TOKEN` 鉴权的 loopback 只读 HTTP 事件流水（可按 code 过滤） |
| 同轮可选 | `abandoned`；文档化 `sqlite3` 例 / SSH 隧道步骤 |
| 不做 | 完整账号体系、图表大盘、改删审计行的对外 API；site `/admin`；按 code 透视表；把管理 op 写进客户端 JSONL 契约；公网裸暴露管理口 |

## 验收标准（产品）

1. 本地/测试库在一次 `guest_host` →（成功或失败）`guest_join` → 结束 后，`guest_audit` 可见对应 **generated / paired|join_failed / wait_timeout|ended** 行；`DeleteGuest` 后历史仍在。
2. 管理旁路（token + loopback）能以**事件流水（最新在前）**列出至少时间、`code`、`event`；可按 code 过滤；未鉴权不可读；**无**透视表要求。
3. 库与管理响应中**看不到** pwd、`password_hash`、完整凭证、fp、业务载荷、v1 RemoteAddr（可用 grep / 只读抽查）。
4. `guest_*` 协议字段与现网 `121.196.45.81:17891` 控制面行为兼容；不改客户端分享形态与业务 TLS+Agent 指纹钉扎；不引入 TOFU/自动降明文。
5. **不**在用户本机启动客户端 UI；本轮**无**客户端功能交付。生产上机另授权。
6. 文档：`round-046` / `relay-audit` / roadmap·PRODUCT 简述一致；未实现前状态保持 **open（规格已拍板）**，不得写成已上线。

## 本轮不做

- 客户端亲友 UI / Avalonia / CLI 改动
- 改 `guest_*` 协议字段或扩客户端 `audit_*` op
- 在用户本机弹窗实测客户端
- 存储或展示验证码、完整连接凭证、指纹、业务载荷、审计表内 `password_hash`、v1 RemoteAddr
- 把 `site/admin` 接单系统改造成中继会话台
- 公网裸暴露 `RELAY_AUDIT_ADDR` 或挂到 `17891`
- 按 code 透视汇总表（MVP 只要事件流水）
- 改写 git 历史 / force-push；官网换包 / S005

## 完成时（实现轮）

- 服务端：改动文件；表 DDL；挂钩点对照；`RELAY_AUDIT_ADDR`/`RELAY_AUDIT_TOKEN` 与 SSH 隧道约定；未记字段清单；90 天保留策略。
- 架构：与 `relay-protocol.md` / 现网部署无矛盾的书面结论（控制面仍 TCP JSONL；指纹仍在客户端业务 TLS）。
- 产品：对照本验收标准；不把客户端编译当本轮验收。

## 参考

- `docs/relay-audit.md`（本轮权威短规格）
- `docs/round-045.md`
- `docs/relay-protocol.md` §8–9
- `docs/relay-deploy.md`
- `docs/tls-trust.md`
- `docs/quality-connection-credential.md`
- `docs/architecture.md` §6（等待态 vs 本机写操作审计）
