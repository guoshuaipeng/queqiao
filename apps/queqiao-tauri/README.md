# queqiao-tauri（D0 空壳）

路径约定：`apps/queqiao-tauri/`（见 `docs/tauri-migration/ARCHITECTURE-BOUNDARY.md` §4 D0）。

## 本阶段范围

- Tauri **v2** + **vanilla** 前端（未引入 React/Vue 等大型框架）
- Windows 本地可 **构建**；窗口设计预算 **840×480 DIP**，入口一屏双区布局方向（A-480-delta-v1.1）
- 冷启动 **零监听**；左「开启」/右「连接」等真实能力 **未实现且按钮禁用**，明确开发阶段文案

## 明确不做（D0）

- 不接真实会话 / relay / Agent
- 不解析 pair/`fp`、不改凭证安全
- 不截屏、不键鼠注入、不放开 shell 白名单
- 不解 R050、不扩 R049、不改官网/installer、不删改 net48
- `bundle.active=false`：D0 不打安装包

## 命令

```bat
cd apps\queqiao-tauri
npm install
npm run build
```

`npm run dev` 会打开 GUI 窗口；无用户机 GUI 授权时不要扰用户桌面，以构建通过为准。

## 探针 8 项

均属后续能力地板，**本空壳未测**；单项 PASS ≠ 迁移完成。
