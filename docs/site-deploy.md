# 官网与接单 — 生产部署约定

> 产品范围见 `docs/site.md`；本文件写进程、端口、数据路径、口令与安全组。  
> **真实主机地址、验收记录、探活号**写在本机 `private/deploy.md`（从 `private.example/` 复制），不要提交到开源仓库。SSH/口令不进仓库。

## 原则

- **独立进程**，与中继 `remote-assist-relay` 分开；官网挂掉不得拖死中继。
- 可与中继同机（约 2C2G 低配即可），必须轻量。
- 单 Go 二进制 + 静态 HTML（可 `go:embed`）；**不装** Node / Java / PHP / MySQL / Redis / WordPress / Docker。
- SQLite 与中继库**分文件**，不共用连接，不打通账号。
- **禁止占用 TCP 17891**。

## 进程与路径

| 项 | 约定 |
|---|---|
| 形态 | 独立 systemd 单元 `remote-assist-site` |
| 二进制 | `/usr/local/bin/remote-assist-site`（`CGO_ENABLED=0 GOOS=linux GOARCH=amd64`） |
| 运行用户 | 系统用户 `rasite`（`nologin`），不要用 `relay` 用户跑官网 |
| 监听 | `SITE_ADDR=:80`（全网卡；MVP 可用 `http://YOUR_SITE_HOST`） |
| 绑 80 | systemd `AmbientCapabilities=CAP_NET_BIND_SERVICE`，进程不以 root 跑业务 |
| 数据 | `SITE_DATA=/var/lib/remote-assist-site/site.db`（目录 `chmod 750`，属主 `rasite`） |
| 安装包 | `SITE_DOWNLOADS=/var/lib/remote-assist-site/downloads`（把 Windows `.exe` / macOS `.dmg` / Linux `.AppImage` 与 `latest.json` 拷到此目录；不进 git、不 `go:embed`） |
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
Environment=SITE_DOWNLOADS=/var/lib/remote-assist-site/downloads
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
mkdir -p /var/lib/remote-assist-site /var/lib/remote-assist-site/downloads
chown -R rasite:rasite /var/lib/remote-assist-site
chmod 750 /var/lib/remote-assist-site /var/lib/remote-assist-site/downloads
# 将 Windows 安装包与 Linux .tar.gz 拷入 downloads/（公开文件名见首页链接），不要把运营口令放进该目录
```

## 运营口令

- 环境变量：`SITE_ADMIN_PASSWORD`（写在 `/etc/remote-assist-site.env`）。
- **不**写入仓库、文档示例、聊天转发或镜像。由运维在服务器上本地创建该文件。
- 后台 `/admin`：校验该口令（登录后可用短期 session cookie）；不做多用户权限、不与中继 `signup`/`token` 打通。
- 进程日志不得打印口令明文。

`/etc/remote-assist-site.env` 只需两行量级（值由运维填写）：

```
SITE_ADDR=:80
SITE_DATA=/var/lib/remote-assist-site/site.db
SITE_DOWNLOADS=/var/lib/remote-assist-site/downloads
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
2. 浏览器打开 `http://YOUR_SITE_HOST/` 可见介绍页；`/ask` 可提交；`/admin` 需口令。
3. `systemctl stop remote-assist-site` 后，中继 `17891` 与 `remote-assist-relay` 仍正常（可用既有 smoketest 抽检）。
4. 安全组已放行 TCP 80，且 17891 规则未回退。

## 安装包换包（macOS / Linux 主路径）

客户端安装包由 **GitHub Actions 打包 → 上传本机 `SITE_DOWNLOADS`**，不要手搓进仓库。完整步骤、Artifact 名、`latest.json`、验收清单见 **[docs/release.md](release.md)**。

摘要：Actions 工作流 `Release packages (MCP)`（仅手动）→ 下载 Artifact → 覆盖 `/var/lib/remote-assist-site/downloads/` 下与首页同名的 dmg / AppImage（及 Linux MCP tar）→ 更新同目录 `latest.json` → 公网 HEAD 200。换包一般**不必**重启 `remote-assist-site`。

## 变更流程

改端口、数据路径或是否上 443：先改本文 + `docs/site.md` / `docs/architecture.md`，再改单元与安全组，最后上机。
