# 鹊桥远程协助

桌面客户端只保留 **Tauri**（`apps/queqiao-tauri`）。旧的 .NET 8 / .NET Framework 4.8 客户端、WinForms / Avalonia 工程、安装器和对应的迭代记录已从仓库删除。

- `apps/queqiao-tauri` — Windows 桌面客户端（Tauri 2）
- `relay/` — Go 公网中继
- `site/` — 官网与 `/admin`
- `docs/` — 产品决策、界面规格、中继协议

界面规格见 [docs/CLIENT-UI.md](docs/CLIENT-UI.md)。

## 客户端

在 `apps/queqiao-tauri`：

```bat
npm install
npm run check
npm run dev
```

## 中继

```powershell
cd E:\IdeaProjects\remote-assist
.\scripts\relay-smoke.ps1
```

协议与部署见 [docs/relay.md](docs/relay.md)、[docs/relay-protocol.md](docs/relay-protocol.md)、[docs/relay-deploy.md](docs/relay-deploy.md)。

## 安全底线（仍然有效）

- 写操作必须在被协助的这台电脑上确认。
- 连接主路径仍是完整安全载荷（含证书指纹）。短码单独连接（R050）未授权。
- 不改 AI-SSH-Pro。默认不 push、不上公网，除非另授权。
