# 鹊桥远程协助

桌面客户端只保留 **Tauri**（`apps/queqiao-tauri`）。旧的 .NET 8 / .NET Framework 4.8 客户端、WinForms / Avalonia 工程、安装器和对应的迭代记录已从仓库删除。

- `apps/queqiao-tauri` — Windows / macOS 桌面客户端（Tauri 2）
- `relay/` — Go 公网中继
- `site/` — 官网与 `/admin`
- `docs/` — 产品决策、界面规格、中继协议
- `private.example/` — 私有部署信息模板（复制为 `private/`，勿提交）

界面规格见 [docs/CLIENT-UI.md](docs/CLIENT-UI.md)。需求队列（迭代入口）见 [docs/PRODUCT-VERSIONS.md](docs/PRODUCT-VERSIONS.md)，文档地图见 [docs/architecture.md](docs/architecture.md)。客户端 **GitHub 打包 → 官网换包**（macOS / Linux 主路径）见 [docs/release.md](docs/release.md)。

## 客户端

在 `apps/queqiao-tauri`：

```bat
npm install
npm run check
npm run dev
```

默认连接本机中继 `127.0.0.1:17891`。正式发版默认地址见 `private.example/deploy.md`。

## 中继

```powershell
.\scripts\relay-smoke.ps1
```

协议与部署见 [docs/relay.md](docs/relay.md)、[docs/relay-protocol.md](docs/relay-protocol.md)、[docs/relay-deploy.md](docs/relay-deploy.md)。

## 私有信息

服务器 IP、口令、探活号、带生产链接的营销稿等放在本机 `private/`（gitignore）。开源仓库只保留占位符与模板。

## 安全底线

- 写操作必须在被协助的这台电脑上确认。
- 连接主路径仍是完整安全载荷（含证书指纹）。短码单独连接（R050）未授权。
- 不改 AI-SSH-Pro。
