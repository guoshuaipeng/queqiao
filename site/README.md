# 鹊桥官网（Go 小服务 + 静态页）

独立进程，与中继分端口；SQLite 分文件。部署约定见 `docs/site-deploy.md`，文案见 `docs/site-copy.md`。

## 页面

| 路径 | 说明 |
|------|------|
| `/` | 介绍首页 |
| `/ask` | 接单表单 |
| `/thanks` | 提交成功（无单号） |
| `/admin` | 运营后台（口令） |
| `GET /downloads/<文件名>` | 安装包下载（来自 `SITE_DOWNLOADS`，不进二进制） |
| `POST /api/ask` | 接单 API |
| `GET /admin/api/stats` | 访问/下载统计（需登录；含次数、独立 IP、最近明细） |

后台状态仅：`新留言` / `已联系` / `已结束`。公开页打开与安装包下载会写入 SQLite（IP、路径/文件名、UA），后台「统计」页可查。

## 环境变量

| 变量 | 默认 | 说明 |
|------|------|------|
| `SITE_ADDR` | `:8088` | 监听地址。生产为 `:80` |
| `SITE_DATA` | `./data/site.db` | SQLite 路径 |
| `SITE_DOWNLOADS` | `./downloads` | 安装包目录；`GET /downloads/<文件名>` 从磁盘流式提供，缺文件返回 404，无目录列表 |
| `SITE_ADMIN_PASSWORD` | （见下） | 后台口令；**永不写入日志** |

口令规则：

- 显式设置 `SITE_ADMIN_PASSWORD` 时一律用该值。
- 未设置时：若 `SITE_ADDR` 为本地友好端口（如 `:8088`、`127.0.0.1:…`），默认口令为 `dev`，方便本机与测试。
- 未设置且监听 `:80` / `:443`（生产）时**拒绝启动**，必须配置口令。

## 本地运行

需要本机已装 Go（与中继相同）。

```powershell
cd site
$env:SITE_ADDR=":8088"
$env:SITE_DATA=".\data\site.db"
$env:SITE_DOWNLOADS=".\downloads"
$env:SITE_ADMIN_PASSWORD="dev"
go run .\cmd\site
```

浏览器打开 http://127.0.0.1:8088/  
后台 http://127.0.0.1:8088/admin （口令 `dev`）


把 Windows `.exe` / macOS `.dmg` / Linux `.AppImage` / `.tar.gz` 放到 `SITE_DOWNLOADS` 目录后即可通过 `/downloads/文件名` 下载。客户端自动更新还会读同目录的 `latest.json`（模板见仓库 `docs/update-latest.example.json`）。包文件**不要** `go:embed` 进二进制（生产 `MemoryMax=64M`）。缺文件或空目录返回 404，不提供目录浏览。

## 测试

```powershell
cd site
go test .\internal\site\ -count=1
```

## 交叉编译（不上机）

```powershell
cd site
$env:CGO_ENABLED=0
$env:GOOS="linux"
$env:GOARCH="amd64"
mkdir ..\artifacts -Force
go build -o ..\artifacts\remote-assist-site-linux-amd64 .\cmd\site
```

生产单元名 `remote-assist-site`，监听 80，数据 `/var/lib/remote-assist-site/site.db`，**禁止占用 17891**。
