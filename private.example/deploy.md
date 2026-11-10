# 私有部署信息模板

复制本目录为仓库根下的 `private/`，填入你的真实值。`private/` 已被 gitignore，不会进开源仓库。

```powershell
Copy-Item -Recurse private.example private
# 编辑 private/deploy.md
```

## 生产主机

| 项 | 值 |
|---|---|
| 云厂商 | （填写） |
| 公网 IP / 域名 | `YOUR_RELAY_HOST` |
| 中继 | `YOUR_RELAY_HOST:17891` |
| 官网 | `http://YOUR_SITE_HOST/` |
| 后台 | `http://YOUR_SITE_HOST/admin` |
| 官网口令文件 | `/etc/remote-assist-site.env`（不进 git） |
| 审计 token | 服务器环境变量 `RELAY_AUDIT_TOKEN`（不进 git） |

## 客户端发版默认值

开源默认是本机 `127.0.0.1`。正式包可在编译前设置：

```powershell
$env:QUEQIAO_DEFAULT_RELAY = "YOUR_RELAY_HOST:17891"
$env:QUEQIAO_SITE = "http://YOUR_SITE_HOST/"
```

## 客户端发版 / 公网换包

完整步骤（GitHub Actions → 下载 Artifact → 上传 `SITE_DOWNLOADS` → `latest.json`）见仓库 **`docs/release.md`**。主路径：**macOS + Linux**；Windows 可选。

发新版安装包后，把同目录的 `latest.json` 放到 `SITE_DOWNLOADS`（客户端读 `{官网}/downloads/latest.json`）。

模板见仓库 `docs/update-latest.example.json`：把 `YOUR_SITE_HOST` 换成真实官网，版本号与各平台文件名与实际上传文件一致。

## 建议 gitignore 的本机目录

- `private/` — 本文件的真实副本
- `site-pub/`、`site-v2/`、`site-v3/` — 带生产链接的营销稿
- `artifacts/` — 交叉编译产物
