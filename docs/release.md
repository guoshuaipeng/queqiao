# 客户端发版与公网换包

> 桌面安装包**不进 git**。正式包由 **GitHub Actions 打包**，再上传到官网 `SITE_DOWNLOADS`，供首页 `/downloads/` 与客户端自动更新清单使用。  
> 真实主机与口令见本机 `private/deploy.md`（勿提交）。进程约定见 `docs/site-deploy.md`。

## 一句话流程

```text
改版本号 → 推到 GitHub → Actions「Release packages (MCP)」出包
  → 本机下载 Artifact → 上传官网 downloads/ → 写/更新 latest.json → 核对首页链接
```

**本机一键脚本（推荐日常换包）**：[`scripts/release-site.ps1`](../scripts/release-site.ps1)

```powershell
# 交互选择 windows / macos / linux
.\scripts\release-site.ps1

# 只发 Windows（本机可直接 NSIS 打包并上传）
.\scripts\release-site.ps1 -Platforms windows

# 多平台：Windows 本机打；mac/Linux 需先把产物放进 artifacts/release-<ver>/
.\scripts\release-site.ps1 -Platforms windows,macos,linux -Version 0.1.4

# 只打包不上传 / 只用已有产物上传
.\scripts\release-site.ps1 -Platforms windows -SkipUpload
.\scripts\release-site.ps1 -Platforms windows -SkipBuild -ArtifactsRoot .\artifacts\release-0.1.3
```

| 能力 | 说明 |
|------|------|
| `-Platforms` | `windows` / `macos` / `linux`（可多选）；省略则交互询问 |
| 本机构建 | **仅 Windows**（当前机是 Win 时打 NSIS + 随包 MCP） |
| mac / Linux | 不交叉编译；把 Actions 产物放进 `artifacts/release-<ver>/` 后同一脚本上传 |
| 上传 | `scp`/`ssh` → `QUEQIAO_SSH`（默认 `root@121.196.45.81`），目标 `…/downloads/` |
| 首页 | 改 `site/index.html` 后交叉编译并替换 `remote-assist-site`（`-SkipSite` 可关） |
| `latest.json` | 每平台写独立 `platforms.*.version`；顶层 `version` 取各平台最大值（旧客户端回退） |

需对本机 `ssh`/`scp` 到官网免密；否则 `-SkipUpload`，再用 ssh-connect MCP 上传同目录产物。

当前主路径：**macOS（arm64 + x64）与 Linux（AppImage / deb / MCP tar）**。Windows NSIS 为可选同工作流开关；本机 Win 发版可直接走脚本而不必开 Actions。

## 1. 版本号

发版前把下列处改成同一 SemVer（例 `0.1.1`）：

| 位置 | 字段 |
|------|------|
| `apps/queqiao-tauri/src-tauri/tauri.conf.json` | `version` |
| `apps/queqiao-tauri/src-tauri/Cargo.toml` | `version` |
| `apps/queqiao-tauri/package.json` | `version` |
| `.github/workflows/release-packages.yml` | 产物文件名里的版本段（`Queqiao-*-0.x.y.*`） |
| `site/index.html` | 下载链接与文案中的文件名 / 「版本 x.y.z」 |

客户端运行时显示的版本来自 `CARGO_PKG_VERSION`（即 `src-tauri/Cargo.toml`）。

## 2. GitHub Actions 打包

工作流：`.github/workflows/release-packages.yml`  
名称：**Release packages (MCP)**  
触发：仅 `workflow_dispatch`（手动）。

编译期已写死正式默认（与 `private.example/deploy.md` 一致）：

- `QUEQIAO_DEFAULT_RELAY=121.196.45.81:17891`
- `QUEQIAO_SITE=http://121.196.45.81/`

### 触发方式

在 GitHub 仓库 **Actions → Release packages (MCP) → Run workflow**：

| 输入 | 含义 | mac/Linux 换包建议 |
|------|------|-------------------|
| `include_windows` | 是否顺带打 Windows NSIS | 仅换 mac/Linux 时保持 **false**（省分钟、避免误换 Win） |

也可用 CLI（本机已装 `gh` 时）：

```bash
gh workflow run "Release packages (MCP)" --repo guoshuaipeng/remote-assist -f include_windows=false
gh run watch --repo guoshuaipeng/remote-assist
```

### 产物 Artifact（保留约 14 天）

| Artifact 名 | 主要文件（版本号随发版改） |
|-------------|---------------------------|
| `queqiao-macos-arm64` | `Queqiao-0.1.0-macos-arm64.dmg`、`RemoteAssist.Mcp-macos-arm64` |
| `queqiao-macos-x64` | `Queqiao-0.1.0-macos-x64.dmg`、`RemoteAssist.Mcp-macos-x64` |
| `queqiao-linux-x64` | `Queqiao-0.1.0-linux-x64.AppImage`、`.deb`、`Queqiao-MCP-0.1.0-linux-x64.tar.gz` |
| `queqiao-windows`（可选） | `Queqiao-Setup-0.1.0.exe`、`RemoteAssist.Mcp-windows-x64.exe` |

下载到本机建议目录：`artifacts/gh-build-<run_id>/`（已 gitignore）。

```bash
gh run download <run_id> --repo guoshuaipeng/remote-assist -D artifacts/gh-build-<run_id>
```

单独打 Linux、避免误耗 macOS 分钟时，可用备用工作流 `build-tauri-linux.yml`（见仓库 `.github/workflows/`）；**公网换包仍以本文件「上传官网」步骤为准**。

## 3. 上传官网（公网换包）

生产机：`121.196.45.81`（ssh-connect id：`queqiao-website`）。  
目标目录：`/var/lib/remote-assist-site/downloads/`（属主 `rasite:rasite`，权限目录 `750`）。

### macOS / Linux（本轮默认）

上传并覆盖同名文件（文件名须与 `site/index.html` 链接一致）：

| 本地产物 | 远程路径 |
|----------|----------|
| `Queqiao-*-macos-arm64.dmg` | `.../downloads/Queqiao-<ver>-macos-arm64.dmg` |
| `Queqiao-*-macos-x64.dmg` | `.../downloads/Queqiao-<ver>-macos-x64.dmg` |
| `Queqiao-*-linux-x64.AppImage` | `.../downloads/Queqiao-<ver>-linux-x64.AppImage` |
| `Queqiao-*-linux-x64.deb`（可选） | 同目录；首页可不链 |
| `Queqiao-MCP-*-linux-x64.tar.gz` | 同目录（首页「MCP 命令行」） |

Agent / 运维约定：生产上传用 MCP `ssh-connect`（`ssh_upload`，**`confirm=true`**），或本机等价 scp；**勿把口令写进仓库或聊天**。

上传后：

```bash
chown rasite:rasite /var/lib/remote-assist-site/downloads/Queqiao-*
# AppImage 建议可执行（若客户端侧 chmod 亦可）
chmod 644 /var/lib/remote-assist-site/downloads/*.dmg \
          /var/lib/remote-assist-site/downloads/*.deb \
          /var/lib/remote-assist-site/downloads/*.exe \
          /var/lib/remote-assist-site/downloads/*.tar.gz
chmod 755 /var/lib/remote-assist-site/downloads/*.AppImage 2>/dev/null || true
```

**不必**为换安装包而重启 `remote-assist-site`（静态从磁盘读）。

### Windows（可选）

仅当本轮明确要换 Win 包时：上传 `Queqiao-Setup-<ver>.exe`，并同步首页链接。未授权时不要覆盖现网 Setup。

## 4. 自动更新清单 `latest.json`

客户端读：`{QUEQIAO_SITE}/downloads/latest.json`（模板：`docs/update-latest.example.json`）。

换包后把清单放到**同一** `downloads/` 目录。各平台 `url` 必须是官网同源绝对地址。

**版本比较（0.1.3+ 客户端）**：优先用该平台的 `platforms.<key>.version`；缺省才回退顶层 `version`。因此可以只抬 Windows 而不触发 mac/Linux 更新。

顶层 `version` 仍建议写成各平台里的**最高**版本（给更旧客户端回退用）。

```json
{
  "version": "0.1.3",
  "notes": "托盘常驻；各平台可独立抬版本。",
  "pub_date": "2026-11-11T01:00:00Z",
  "platforms": {
    "windows-x86_64": {
      "version": "0.1.3",
      "url": "http://121.196.45.81/downloads/Queqiao-Setup-0.1.3.exe",
      "name": "Queqiao-Setup-0.1.3.exe"
    },
    "darwin-aarch64": {
      "version": "0.1.3",
      "url": "http://121.196.45.81/downloads/Queqiao-0.1.3-macos-arm64.dmg",
      "name": "Queqiao-0.1.3-macos-arm64.dmg"
    },
    "darwin-x86_64": {
      "version": "0.1.3",
      "url": "http://121.196.45.81/downloads/Queqiao-0.1.3-macos-x64.dmg",
      "name": "Queqiao-0.1.3-macos-x64.dmg"
    },
    "linux-x86_64": {
      "version": "0.1.3",
      "url": "http://121.196.45.81/downloads/Queqiao-0.1.3-linux-x64.AppImage",
      "name": "Queqiao-0.1.3-linux-x64.AppImage"
    }
  }
}
```

注意：

- **新客户端**：本机版本低于**本平台** `version` 才提醒/自动更新。
- **旧客户端**（无平台 version 逻辑）：仍只看顶层 `version`。
- 新功能进包 = 先合入 GitHub 再跑 Actions；只改 `latest.json` 不会给旧包打补丁。

## 5. 验收

1. 公网 HEAD/GET 为 200，且 `Content-Length` 与上传文件一致：  
   - `/downloads/Queqiao-<ver>-macos-arm64.dmg`  
   - `/downloads/Queqiao-<ver>-macos-x64.dmg`  
   - `/downloads/Queqiao-<ver>-linux-x64.AppImage`  
   - `/downloads/latest.json`（JSON 可解析，`version` 正确）
2. 首页下载按钮指向上述文件名（改版本时同步改 `site/` 并另授上线 site 二进制或静态页）。
3. 中继 `17891` 与 `remote-assist-relay` 未因换包被重启或误伤。

## 6. 授权与红线

- **公网换包、推送 GitHub、上生产机**须对话明确授权（见 `docs/dev-rules.md`、`PRODUCT-VERSIONS` 补充约定）。
- 口令、审计 token、`/etc/*.env` 不进 git、不复述。
- 旧 `.NET` 安装包可留在 `downloads/` 但不在首页链接；不要当现行主路径。

## 7. 记录落点

| 记什么 | 写哪 |
|--------|------|
| 流程与文件名约定 | **本文** `docs/release.md` |
| 官网进程 / 目录 | `docs/site-deploy.md` |
| 本机 IP、某次换包验收 | `private/deploy.md`（勿提交） |
| ssh 主机笔记 | ssh-connect `server-info/queqiao-website.md` |
| 产品需求状态 | `docs/PRODUCT-VERSIONS.md`（如 PV-006） |

## 变更记录（摘要）

| 日期 | 内容 |
|------|------|
| 2026-11-11 | `latest.json` 支持 `platforms.*.version`；客户端优先按平台版本比较。增加 `scripts/release-site.ps1`。 |
| 2026-11-11 | 增加 `scripts/release-site.ps1`：可选 windows/macOS/linux，本机打 Win + 上传 downloads / 更新 latest.json / 可选换 site。 |
| 2026-11-10 | 成文：GitHub `Release packages (MCP)` → 官网 `downloads/`；主路径 macOS + Linux；附 `latest.json`。当日现网已有 `Queqiao-0.1.0` mac/Linux/Win 包；补齐自动更新清单流程。 |
