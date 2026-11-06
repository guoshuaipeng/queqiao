# 第 46 轮 — 会话码生成/使用审计 + 管理台只读查看

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 实现：鹊桥-服务端（主）；架构对齐文档  
> 状态：**open（规格已拍板）** — 只读评估结论已合入；**尚未实现代码**  
> 依据：用户要求「服务端记录生成的 code，以及使用记录；管理台可以查看」。只读评估要点（现网 `DeleteGuest` 抹历史、join 失败未入审计、无中继管理 HTTP、site/admin 接单分离）已收口进本轮与 `docs/relay-audit.md`。  
> **本轮主题**：中继 SQLite **审计表** + **鉴权只读管理列表**；**不**改客户端亲友 UI、**不**改 `guest_*` 协议字段、**不**在用户本机跑应用。

## 产品拍板（设计锁死）

1. **存储**：在中继 SQLite（现网 `RELAY_DATA` / 生产 `relay.db`）新增**审计表**（与瞬时等待态 `guest_sessions` **分离**）。记录至少四类：
   - **code generated**（`guest_host` / `SaveGuest` 成功签发登记）
   - **join success**（`guest_join` 校验通过并配对）
   - **join fail**（`guest_join` 失败；短枚举 reason，**不**写入用户输入的 password）
   - **session end**（pipe 结束 / 等待超时 / 放弃清理）
2. **管理台只读列表**：列至少 `created_at`（或事件时间）、`code`、`status`（覆盖生成 / 使用成败 / 结束；可用事件枚举或汇总状态）；可选协议已有、经确认允许的弱参与者标识（**不**把 IP 当实名）。**须鉴权**；公网匿名 / 普通客户端 / 持码访客 / MCP **不可**读。
3. **协议**：`guest_host` / `guest_join` JSON 字段**不改**；审计为服务端副作用。挂钩：`SaveGuest`、join 成败路径、pipe / timeout 结束。
4. **严禁存储或展示**：`pwd` / 验证码明文、`password_hash`（审计表不复制）、业务证书指纹 `fp`、完整 `remoteassist:pair` / 完整连接凭证、业务管道内容。
5. **与官网接单后台分离**：现有 `site/` `/admin` 只管 customers/messages，**与中继库分文件、不互通**。本轮管理面 = **新建中继侧只读 HTTP（或本机运维查询）**，或**谨慎挂靠** site 只读代理中继审计表；**不要**把接单工单体系做成中继会话管理台。
6. **现网缺口（评估确认）**：`DeleteGuest` 在配对结束/超时后**抹掉等待态历史**；`guest_join` 失败仅回客户端错误串、**无结构化审计行**；中继**无** HTTP 管理面；stdout slog 有 `key`=code 的配对/关闭信息，**不能**单独满足管理台列表。

权威短规格：`docs/relay-audit.md`。更细事件/字段草案可参考同目录架构稿（若有 `relay-session-audit.md`）。

## 建议表形态（实现可微调列名，语义不变）

```text
guest_audit / session_audit_events（追加型）
  id, code, created_at|ts, status|event,
  paired_at?, closed_at?, fail_reason?,
  -- 禁止：password, password_hash, fp, payload, full remoteassist:pair
```

- 等待态表 `guest_sessions` 仍可在结束后 `DeleteGuest`；**审计表行保留**（可另设保留天数，默认建议 30 天，实现时写入 `relay-deploy.md`）。
- 审计 `INSERT` 失败**不得**阻断配对主路径（只记服务日志）。

## 管理面切片（MVP）

| 优先级 | 内容 |
|--------|------|
| 必做 | 审计表落库 + 挂钩四点事件 + 鉴权只读查询（文档化 `sqlite3` 例，或本机-only 只读 HTTP / 子命令） |
| 同轮可选 | `127.0.0.1`（或独立管理端口，**非** `17891`）+ 运维 token 的 JSON 列表 |
| 不做 | 完整账号体系、图表大盘、改删审计行的对外 API；不把管理 op 写进客户端 JSONL 契约 |

展示列：`created_at`/`ts`、`code`、`status`（及可选 `fail_reason` / 结束时间）。**永不展示**验证码、hash、fp、完整凭证。

## 验收标准（产品）

1. 本地/测试库在一次 `guest_host` →（成功或失败）`guest_join` → 结束 后，审计表可见对应**生成 / 使用成败 / 结束**行；`DeleteGuest` 后历史仍在。
2. 管理台或本机只读查询能列出至少 `created_at`、`code`、`status`；未鉴权不可读。
3. 库与管理响应中**看不到** pwd、`password_hash`、完整凭证、fp、业务载荷（可用 grep / 只读抽查）。
4. `guest_*` 协议字段与现网 `121.196.45.81:17891` 控制面行为兼容；不改客户端分享形态与业务 TLS+Agent 指纹钉扎；不引入 TOFU/自动降明文。
5. **不**在用户本机启动客户端 UI；本轮**无**客户端功能交付。生产上机另授权。
6. 文档：`round-046` / `relay-audit` / roadmap·PRODUCT 简述一致；未实现前状态保持 **open（规格已拍板）**，不得写成已上线。

## 本轮不做

- 客户端亲友 UI / Avalonia / CLI 改动
- 改 `guest_*` 协议字段或扩客户端 `audit_*` op
- 在用户本机弹窗实测客户端
- 存储或展示验证码、完整连接凭证、指纹、业务载荷、审计表内 `password_hash`
- 把 `site/admin` 接单系统改造成中继会话台（除非极窄只读代理且不混账号）
- 改写 git 历史 / force-push；官网换包 / S005

## 完成时（实现轮）

- 服务端：改动文件；表 DDL；挂钩点对照；管理只读入口与鉴权；未记字段清单；保留策略。
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

