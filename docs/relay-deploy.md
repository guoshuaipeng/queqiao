# 公网中继 — 生产部署约定

> 控制协议见 `docs/relay-protocol.md`；本机开发见 `docs/relay.md`。  
> **真实主机地址、验收记录**写在本机 `private/deploy.md`（从 `private.example/` 复制），不要提交到开源仓库。

## 拓扑

| 项 | 约定 |
|---|---|
| 形态 | **单机单实例**（MVP；不做多实例/负载均衡） |
| 主机 | 自备 VPS / 云主机（示例：`YOUR_RELAY_HOST`） |
| 系统 | 常见 Linux x86_64 发行版即可 |
| 规格建议 | 2C2G 即可 |
| 监听 | `RELAY_ADDR=:17891`（全网卡） |
| 进程 | **systemd** 常驻，建议 unit 名 `remote-assist-relay` |
| 数据 | SQLite：`RELAY_DATA=/var/lib/remote-assist-relay/relay.db`（目录属主 = 运行用户） |
| 防火墙 / 安全组 | **中继入站放行 TCP 17891**；同机官网另放行 TCP 80，见 `docs/site-deploy.md`。SSH 保持既有管理通道，不在本文展开 |

中继只做 JSONL 控制 → 透明 pipe；业务 TLS 端到端，中继不解析业务帧。

## 二进制与目录

建议布局（可微调，但环境变量名固定）：

```
/usr/local/bin/remote-assist-relay     # go build 产物
/var/lib/remote-assist-relay/          # 数据目录（chmod 750）
/etc/systemd/system/remote-assist-relay.service
```

构建（在仓库 `relay/`）：

```bash
cd relay
CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build -o remote-assist-relay ./cmd/relay
# 推荐 modernc SQLite（CGO=0）。若改用 CGO SQLite：CGO_ENABLED=1，需 gcc
```

## systemd 示例

```ini
[Unit]
Description=Remote Assist public relay
After=network.target

[Service]
Type=simple
User=relay
Group=relay
Environment=RELAY_ADDR=:17891
Environment=RELAY_DATA=/var/lib/remote-assist-relay/relay.db
ExecStart=/usr/local/bin/remote-assist-relay
Restart=always
RestartSec=3
StartLimitIntervalSec=0
LimitNOFILE=65535

[Install]
WantedBy=multi-user.target
```

`Restart=always` 而不是 `on-failure`：中继正常退出（含升级脚本 kill 后的干净退出）也要自己起来，否则无人值守端会一直连不上。`StartLimitIntervalSec=0` 避免连续失败后被 systemd 放弃重启。

### 连接与容量（可选环境变量）

| 变量 | 默认 | 说明 |
|---|---|---|
| `RELAY_IDLE_TIMEOUT` | `10m` | Go duration（如 `30m`）。同时作用于「等待配对」与「已配对管道空闲」：超时即断开，客户端会自动重新注册。协助会话经常长时间无字节往来时可调大 |
| `RELAY_MAX_SESSIONS` | `64` | 等待 + 转发中的会话槽上限，超出报 `too many sessions` |

两端都开了 TCP keepalive（客户端 30s 空闲 / 10s 探测，中继 `SetKeepAlivePeriod(30s)`），用于刷新 NAT 空闲映射并尽快发现半开连接；不要只靠 `RELAY_IDLE_TIMEOUT` 兜底掉线。

上机前创建专用用户与目录，例如：

```bash
useradd --system --home /var/lib/remote-assist-relay --shell /sbin/nologin relay
mkdir -p /var/lib/remote-assist-relay
chown -R relay:relay /var/lib/remote-assist-relay
```

## 验收

1. `systemctl enable --now remote-assist-relay` 后 `ss -lntp | grep 17891` 可见监听。
2. 本机或外网：`go run ./cmd/smoketest -addr YOUR_RELAY_HOST:17891`（或同等访客 guest_host / guest_join）通过。
3. 安全组 / 本机 firewalld 或 nftables：中继对公网开放 **TCP 17891**（按运维习惯二选一或都配）。同机官网另放行 TCP 80，见 `docs/site-deploy.md`。
4. 客户端默认中继：开源包默认 `127.0.0.1:17891`；正式发版用 `QUEQIAO_DEFAULT_RELAY=YOUR_RELAY_HOST:17891`（见 `private.example/deploy.md`），用户仍可在设置里改。

## 会话码审计（R046）

> 细则见 `docs/relay-session-audit.md`。此处只固定部署侧约定；**实现前勿提前上机改二进制以外的临时方案。**

| 项 | 约定 |
|---|---|
| 存储 | 与中继同库：`RELAY_DATA`（生产 `/var/lib/remote-assist-relay/relay.db`）表 `guest_audit`；与 `guest_sessions` 同库不同表。官网 `site.db` **不存**审计 |
| 旁路只读 HTTP | 中继进程另开**同机旁路**只读 list（独立鉴权，如 `RELAY_AUDIT_TOKEN` Bearer）。**不**改 `17891` JSONL / `guest_*`。默认绑 `127.0.0.1`（端口由实现写入 unit/环境变量，选定后补进本表） |
| 鉴权 | 与官网 `SITE_ADMIN_PASSWORD` **独立**；不与中继用户 `signup`/`token` 打通 |
| 保留 | 默认 **90 天**（`RELAY_AUDIT_RETENTION_DAYS`，可配）；超期删 `guest_audit` 行。`DeleteGuest` 只清等待态，不动审计历史 |
| IP | v1 **不存、不展** RemoteAddr/IP；排障用现有 slog |
| 官网 `/admin` | **非**本 MVP 必做入口；若后续统一，由 site **代理**旁路，禁止直接打开中继库文件 |

环境变量示意（值由运维在服务器配置，**口令/token 不进仓库**）：

```
RELAY_ADDR=:17891
RELAY_DATA=/var/lib/remote-assist-relay/relay.db
RELAY_AUDIT_ADDR=127.0.0.1:<port>   # 实现选定端口后写死示例
RELAY_AUDIT_TOKEN=                  # 旁路 Bearer；勿提交 git
RELAY_AUDIT_RETENTION_DAYS=90
```

安全组：旁路不对公网随 17891 放开；本机/SSH 隧道访问即可。

## 明确不做（本阶段）

- 多实例、域名与正式 CA、中继侧 TLS 终结业务流量
- 在文档或仓库中保存 SSH 密码 / 私钥 / `RELAY_AUDIT_TOKEN` / `SITE_ADMIN_PASSWORD`
- 中继代执行写操作或绕过被控本机确认
- 会话码审计写入官网 SQLite，或把官网 `/admin` 当作 R046 MVP 必做读面（统一入口仅后续代理）
- v1 审计表存储客户端 IP / RemoteAddr

## 变更流程

改监听端口、数据路径或拓扑：先改本文 + `docs/relay.md` / `PRODUCT.md`，再改 systemd / 客户端默认，最后上机。
