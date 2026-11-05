# 公网中继（Go）

> 状态：已接通 MVP（透明隧道 + code/密码 + 最小账号/设备）。目标部署：阿里云 2C2G，可用纯 IP。

## 产品原则

1. **中继只做隧道打通**（双方主动出连 → 按会话配对 → 双向字节转发）。
2. **默认路径是中继**；局域网直连保留高级/调试（`via=lan`）。
3. 不做完整远程桌面；排障流量仍应限速/空闲超时（默认 10 分钟，最大 64 会话）。
4. 业务 TLS/JSON **端到端**；中继 **不解析** 业务帧。
5. MVP 账号数据落本地 `RELAY_DATA/accounts.json`（非生产级安全）。

## 控制协议（一行 UTF-8，`\n` 结尾）

### 访客（code + 密码）

```text
REGISTER <code> <agent|helper> <password>
→ OK waiting | OK paired | ERR ...
```

### 账号

```text
AUTH REGISTER <user> <pass>   → OK token <tok>
AUTH LOGIN <user> <pass>      → OK token <tok>
DEVICE UPSERT <token> <deviceId> <name>
DEVICE LIST <token>           → OK n 然后 n 行：id\tname\tonline(0|1)
DEVICE ONLINE <token> <deviceId> [name...]   # 被控等待，等同 agent 隧道
DEVICE CONNECT <token> <deviceId>            # 控制端免密，等同 helper 隧道
```

配对成功后进入透明双向 pipe。

## 客户端对接（net8）

- `RemoteAssist.Protocol.RelayClient`：出连与控制行。
- `PairQrPayload.BuildRelay`：`remoteassist:pair?v=2&via=relay&relay=host:port&code=&pwd=&tls=&fp=`
- `AgentHost`：默认 `preferRelay=true`，循环 `REGISTER`/`DEVICE ONLINE`；仍监听 LAN 端口。
- `AssistClient`：`ViaRelay` / `OwnerLink` 路径。
- 设置：`%LOCALAPPDATA%\RemoteAssist\helper-ui.json`（中继地址、账号 token、DeviceId）。

## 本机跑

```powershell
cd E:\IdeaProjects\remote-assist\relay
$env:RELAY_ADDR = "127.0.0.1:17891"
$env:RELAY_DATA = "$pwd\data"
go run .\cmd\relay
# 测试：
go test .\internal\relay\ -count=1
go run .\cmd\smoketest -addr 127.0.0.1:17891
# 或
..\scripts\relay-smoke.ps1
```

客户端默认中继 `127.0.0.1:17891`，部署时在设置里改成公网 `IP:17891`。

## 分工

| 模块 | 负责 |
|---|---|
| `relay/` Go 中继 | 并行 bot 脚手架 + 本轮账号/密码扩展 |
| `src/` net8 对接 | 「远端协助」bot（本轮） |
| `src-net48` | 体积对比；「返回首页」等 UI 勿互相覆盖 |

## 非目标

- 计费 / OAuth / 多租户
- 中继终止 TLS
- 音视频远控
