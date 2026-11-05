# 官网与接单 — 生产部署约定

> 架构拍板（2026-11-05 Asia/Shanghai）。产品范围见 `docs/site.md`；本文件写进程、端口、数据路径、口令与安全组。  
> 实现 / 上机：交给「鹊桥-服务端」。架构侧不写业务代码、不上机。SSH/口令不进仓库。

## 原则

- **独立进程**，与中继 `remote-assist-relay` 分开；官网挂掉不得拖死中继。
- 与中继同机：阿里云 ECS `121.196.45.81`（约 2C2G），必须轻量。
- 单 Go 二进制 + 静态 HTML（可 `go:embed`）；**不装** Node / Java / PHP / MySQL / Redis / WordPress / Docker。
- SQLite 与中继库**分文件**，不共用连接，不打通账号。
- **禁止占用 TCP 17891**。

## 进程与路径

| 项 | 约定 |
|---|---|
| 形态 | 独立 systemd 单元 `remote-assist-site` |
| 二进制 | `/usr/local/bin/remote-assist-site`（`CGO_ENABLED=0 GOOS=linux GOARCH=amd64`） |
| 运行用户 | 系统用户 `rasite`（`nologin`），不要用 `relay` 用户跑官网 |
| 监听 | `SITE_ADDR=:80`（全网卡；MVP 用 `http://121.196.45.81`） |
| 绑 80 | systemd `AmbientCapabilities=CAP_NET_BIND_SERVICE`，进程不以 root 跑业务 |
| 数据 | `SITE_DATA=/var/lib/remote-assist-site/site.db`（目录 `chmod 750`，属主 `rasite`） |
| 环境文件 | `/etc/remote-assist-site.env`（`chmod 600`，不进 git） |
| 内存上限 | `MemoryMax=64M`（必要时可调到 128M）；`CPUQuota=30%` |
| 重启 | `Restart=on-failure`；**不要** `BindsTo=` / `PartOf=` 中继单元 |

仓库布局建议：`site/`（静态页 + Go 小服务）。构建示例：

```bash
cd site
CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build -o remote-assist-site ./cmd/site
```

## systemd 示例

```ini
[Unit]
Description=Queqiao public site and intake
After=network.target
# 不依赖 remote-assist-relay；任一方挂掉不影响另一方

[Service]
Type=simple
User=rasite
Group=rasite
EnvironmentFile=/etc/remote-assist-site.env
Environment=SITE_ADDR=:80
Environment=SITE_DATA=/var/lib/remote-assist-site/site.db
AmbientCapabilities=CAP_NET_BIND_SERVICE
CapabilityBoundingSet=CAP_NET_BIND_SERVICE
NoNewPrivileges=true
ExecStart=/usr/local/bin/remote-assist-site
Restart=on-failure
RestartSec=3
MemoryMax=64M
CPUQuota=30%
LimitNOFILE=4096

[Install]
WantedBy=multi-user.target
```

上机前：

```bash
useradd --system --home /var/lib/remote-assist-site --shell /sbin/nologin rasite
mkdir -p /var/lib/remote-assist-site
chown -R rasite:rasite /var/lib/remote-assist-site
chmod 750 /var/lib/remote-assist-site
```

## 运营口令

- 环境变量：`SITE_ADMIN_PASSWORD`（写在 `/etc/remote-assist-site.env`）。
- **不**写入仓库、文档示例、聊天转发或镜像。由运维在服务器上本地创建该文件。
- 后台 `/admin`：校验该口令（登录后可用短期 session cookie）；不做多用户权限、不与中继 `signup`/`token` 打通。
- 进程日志不得打印口令明文。

`/etc/remote-assist-site.env` 只需两行量级（值由运维填写）：

```
SITE_ADDR=:80
SITE_ADMIN_PASSWORD=
```

## 安全组 / 防火墙

在现有「入站 TCP 17891」之外，**额外放行入站 TCP 80**。本阶段不上 HTTPS，**先不放 443**。SSH 管理通道保持原样，本文不展开。

本机若启用 firewalld/nftables，同样只为官网增加 80，不要改中继 17891 规则。

## 页面、文案与订单状态

- 静态页在仓库 `site/`；上屏文案以 `docs/site-copy.md` 为准。
- 后台订单状态**只允许**以下三值（存储与展示用同一中文，不要另造英文枚举再映射丢词）：
  - `新留言`
  - `已联系`
  - `已结束`
- 访客提交的新单默认 `新留言`。运营可改状态、加备注。客户名单仍按联系方式聚合。

## 本阶段明确不做

- 与中继共用进程、端口或 SQLite 文件
- 安装重型 Web/数据库栈
- 域名、正式证书、中继侧终结 TLS（官网 HTTPS 以后单独立项）
- 完整 CRM / 计费 / 与中继账号打通

## 验收

1. `systemctl enable --now remote-assist-site` 后 `ss -lntp | grep ':80'` 可见由 `remote-assist-site` 监听（不是 relay）。
2. 浏览器打开 `http://121.196.45.81/` 可见介绍页；`/ask` 可提交；`/admin` 需口令。
3. `systemctl stop remote-assist-site` 后，中继 `17891` 与 `remote-assist-relay` 仍正常（可用既有 smoketest 抽检）。
4. 安全组已放行 TCP 80，且 17891 规则未回退。

## 变更流程

改端口、数据路径或是否上 443：先改本文 + `docs/site.md` / `docs/architecture.md`，再改单元与安全组，最后上机。
## 验收记录（2026-11-05 Asia/Shanghai）

- systemd `remote-assist-site` enabled + active，用户 rasite，监听 *:80；数据 /var/lib/remote-assist-site/site.db
- 口令仅在服务器 /etc/remote-assist-site.env（未进仓库）
- 本机：GET / 200；GET /ask 含「需要帮忙？留下联系方式」；POST /api/ask 303 -> /thanks
- 中继 remote-assist-relay 仍 active，公网 17891 TCP 仍通
- 公网 80 外网超时：待安全组额外放行入站 TCP 80（未改 17891 规则）
- 放行后访问 http://121.196.45.81/ ，后台 /admin


## 浏览器验收（2026-11-05 18:36 Asia/Shanghai）

对照 `docs/site.md`、`docs/site-copy.md`，由鹊桥-服务端在公网做浏览器验收：

- http://121.196.45.81/ 介绍页通过：品牌「鹊桥远程协助 · 智能体」；H1「用命令和智能体修电脑，不是把整块屏幕交给别人。」；导语含「目前以 Windows 为主」；导航与主 CTA「留下联系方式」指向 `/ask`；三步连上与安全边界（写操作需对方本机确认，中继不代执行）文案对齐；下载处 Windows 入口尚未提供真实安装包，macOS/Linux 标明「即将提供」。
- http://121.196.45.81/ask 通过：H1「需要帮忙？留下联系方式」；说明「不用注册。我们会通过你留下的方式回你。」；按钮「发给我们」；空表单提示「请填完后再发送。」；有称呼和问题但无联系方式时提示「请留下能联系到你的微信、电话或邮箱。」；无访客单号、进度查询或注册/中继账号入口。
- 例行探活两次提交（称呼「例行探活」，微信 `qiaoqiao_probe_20261105`，系统 Windows）：均 POST `/api/ask` 后到达 `/thanks`，标题「已收到，我们会联系你」，正文含「不用注册，也不用记住单号。」，次要操作「返回介绍」。此为验收探活，不要当成真客户跟进。
- http://121.196.45.81/admin 通过：单口令登录；两条探活归为同一客户（历史条数 2）；状态选项仅「新留言 / 已联系 / 已结束」；将「第一次提交」改为「已联系」并备注「例行探活验收，勿跟进」，刷新后仍保留。「第二次提交」仍为「新留言」。运营口令未写入仓库或聊天。
- 中继 `remote-assist-relay` 仍监听 `*:17891`，与官网进程分离。
