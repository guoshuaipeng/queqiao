# 公网中继（Go MVP）

中继**只做隧道打通**：协助端与被控端各自主动出连，按会话（code+密码或账号设备）配对后透明双向转发。  
**产品默认走中继**；局域网直连为高级/调试。设计见仓库 `docs/relay.md`。

## 快速跑

```powershell
cd E:\IdeaProjects\remote-assist\relay
$env:RELAY_ADDR = "127.0.0.1:17891"
$env:RELAY_DATA = "$pwd\data"
go run .\cmd\relay
```

```powershell
go test .\internal\relay\ -count=1
go run .\cmd\smoketest -addr 127.0.0.1:17891
```

## 控制协议摘要

- `REGISTER <code> <agent|helper> <password>`
- `AUTH REGISTER|LOGIN <user> <pass>` → `OK token …`
- `DEVICE ONLINE|CONNECT|LIST|UPSERT …`

配对后透明 pipe，不解析业务帧。
