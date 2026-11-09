# 鹊桥远程协助

桌面客户端只保留 **Tauri**。旧的 .NET 8 / .NET Framework 4.8 客户端、WinForms / Avalonia 工程、安装器和对应的迭代记录已从仓库删除。

本分支（`master`）目前是中继、官网和产品文档：

- `relay/` — Go 公网中继
- `site/` — 官网与 `/admin`
- `docs/` — 产品决策、界面规格、中继协议

Tauri 客户端在分支 `feat/tauri-client-migration`，目录 `apps/queqiao-tauri`（本机工作区 `E:\IdeaProjects\remote-assist-wt-tauri`）。界面规格见 [docs/CLIENT-UI.md](docs/CLIENT-UI.md)。

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
