# 公网中继（Go）

> **控制协议以 `docs/relay-protocol.md` 为准（JSON Lines）**。本文件只写部署与产品原则。

## 产品原则

1. 中继只做隧道打通（JSONL 控制 → 透明 pipe）。
2. **默认路径是中继**；局域网直连为高级/调试。
3. 业务 TLS/JSON 端到端；中继不解析业务帧。
4. SQLite：`RELAY_DATA`（默认 `./data/relay.db`）。

## 本机跑

```powershell
cd E:\IdeaProjects\remote-assist\relay
$env:RELAY_ADDR = "127.0.0.1:17891"
$env:RELAY_DATA = "$pwd\data\relay.db"
go run .\cmd\relay
go test .\internal\relay\ -count=1
go run .\cmd\smoketest -addr 127.0.0.1:17891
```

## 客户端

- `RemoteAssist.Protocol.RelayClient`：JSONL（`guest_host` / `guest_join` / `login` / `device_*`）。
- 被控：`guest_host` 后展示服务端签发的 code + 本地密码；或账号 `device_online`。
- 控制：`guest_join` 或 `connect_device`。

详见 `docs/relay-protocol.md`、`docs/PRODUCT.md`、`docs/dev-rules.md`。

## 生产部署

正式环境约定见 **`docs/relay-deploy.md`**（单机单实例、`:17891`、systemd、SQLite `/var/lib/remote-assist-relay`、防火墙仅 TCP 17891；默认公网 `121.196.45.81:17891`）。

