# 公网中继（Go）

**默认连接路径**：双方出连中继 → JSONL 鉴权/配对 → 透明双向字节转发。  
支持：**访客 code+密码**、**账号登录 + 设备列表免密**。  
Assist 写操作仍由被控本机确认；中继不绕过。完整契约见仓库 `docs/relay-protocol.md`。

## 快速跑

```powershell
cd relay
$env:RELAY_ADDR = ":17891"
$env:RELAY_DATA = ".\data\relay.db"
go run .\cmd\relay
```

```powershell
go run .\cmd\smoketest -addr 127.0.0.1:17891
go test .\internal\relay\ -count=1
```

或：`..\scripts\relay-smoke.ps1`

## 环境变量

| 变量 | 默认 | 含义 |
|---|---|---|
| `RELAY_ADDR` | `:17891` | TCP 监听 |
| `RELAY_DATA` | `./data/relay.db` | SQLite 文件路径 |

## 控制协议摘要（JSONL）

每行一个 JSON，例如访客：

```text
{"op":"guest_host","password":"test"}
{"op":"guest_join","code":"AB12CD","password":"test"}
```

账号：`signup` / `login` / `auth_token` / `device_online` / `list_devices` / `connect_device`。  
配对成功两边收到 `{"ok":true,"op":"paired","mode":"guest|account",...}` 后进入原始管道。

> 注意：旧的空格分隔 `REGISTER` / `DEVICE ONLINE` 文本行**已移除**；请以 `docs/relay-protocol.md` 为准。

## 部署（2C2G 纯 IP）

```powershell
$env:GOOS = "linux"; $env:GOARCH = "amd64"
go build -o relay-linux .\cmd\relay
```

服务器：`RELAY_ADDR=:17891 RELAY_DATA=/var/lib/remote-assist/relay.db ./relay-linux`，安全组放行 TCP 17891。
