# 公网中继 — 生产部署约定

> 架构拍板（2026-11-05）。控制协议见 `docs/relay-protocol.md`；本机开发见 `docs/relay.md`。  
> **实现 / 上机**：交给开发侧（「鹊桥-服务端」改 Go 与部署脚本；「鹊桥-客户端」填客户端默认中继地址）。架构 bot 不写业务代码、不上机。

## 拓扑

| 项 | 约定 |
|---|---|
| 形态 | **单机单实例**（MVP；不做多实例/负载均衡） |
| 主机 | 阿里云 ECS `121.196.45.81` |
| 系统 | Alibaba Cloud Linux 4 LTS 64 位（标准版，非 Pro / Agentic / 容器优化） |
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
Restart=on-failure
RestartSec=3
LimitNOFILE=65535

[Install]
WantedBy=multi-user.target
```

上机前创建专用用户与目录，例如：

```bash
useradd --system --home /var/lib/remote-assist-relay --shell /sbin/nologin relay
mkdir -p /var/lib/remote-assist-relay
chown -R relay:relay /var/lib/remote-assist-relay
```

## 验收

1. `systemctl enable --now remote-assist-relay` 后 `ss -lntp | grep 17891` 可见监听。
2. 本机或外网：`go run ./cmd/smoketest -addr 121.196.45.81:17891`（或同等访客 guest_host / guest_join）通过。
3. 安全组 / 本机 firewalld 或 nftables：中继对公网开放 **TCP 17891**（按运维习惯二选一或都配）。同机官网另放行 TCP 80，见 `docs/site-deploy.md`。
4. 客户端 / MCP 默认中继填：`121.196.45.81:17891`（写入设置默认值；用户仍可改）。

## 明确不做（本阶段）

- 多实例、域名与正式 CA、中继侧 TLS 终结业务流量
- 在文档或仓库中保存 SSH 密码 / 私钥
- 中继代执行写操作或绕过被控本机确认

## 变更流程

改监听端口、数据路径或拓扑：先改本文 + `docs/relay.md` / `PRODUCT.md`，再改 systemd / 客户端默认，最后上机。


## 验收记录（2026-11-05 Asia/Shanghai）

| 项 | 结果 |
|---|---|
| 主机 | 121.196.45.81（Alibaba Cloud Linux 4） |
| 二进制 | /usr/local/bin/remote-assist-relay（linux/amd64，CGO=0 / modernc） |
| systemd | emote-assist-relay enabled + active |
| 监听 | *:17891 |
| 数据 | RELAY_DATA=/var/lib/remote-assist-relay/relay.db（用户 elay） |
| 外网 smoketest | go run ./cmd/smoketest -addr 121.196.45.81:17891 → **SMOKE OK** |
| 本机 firewalld | 未运行；依赖阿里云安全组放行 **TCP 17891**（请运维确认） |

客户端 / MCP 默认中继建议填：121.196.45.81:17891。
