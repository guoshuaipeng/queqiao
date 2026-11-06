# 第 47 轮 — net48 安装包（发给别人安装）

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 实现：鹊桥-客户端（主；Inno 或 MSI）；官网上传可选后续轮  
> 状态：**open（规格已开）** — **尚未实现代码**  
> 依据：用户确认「可以打包成一个安装包发给别人安装」；当前 `artifacts/app-net48-fd` 约 1.5 MB 解压即用目录依赖本机 **.NET Framework 4.8**，尚无单文件 Setup。  
> **并行说明**：**R046**（会话码审计 + 管理台只读）仍由服务端推进、互不阻塞；本轮只做 Windows **net48** 安装包。

## 产品目标

把 Windows **主路径** net48 客户端打成**单个安装程序**，方便发给亲友/客户一键安装；对方机器需已有（或安装说明里指引安装）.NET Framework 4.8。

## 产品拍板（设计锁死）

1. **输入产物**：以 `scripts/build-net48.ps1` → `artifacts/app-net48-fd/`（含 `RemoteAssist.Net48.exe`）为安装内容；**不要**把 .NET 8 自包含 `app-win-x64-sc` 当本轮主包。
2. **安装器形态**：优先 **Inno Setup**（可复用/改编现有 `installer/RemoteAssist.iss` 思路）；若 Inno 不可用再评 **MSI**。输出建议：`artifacts/installer/RemoteAssist-Net48-Setup-*.exe`（名称可微调，须在文档标明 net48）。
3. **安装行为（对齐现有亲友友好约定）**：
   - 默认目录 `%LOCALAPPDATA%\Programs\RemoteAssist`（或明确区分的 net48 子目录，避免与 net8 自包含静默互覆盖——实现时二选一并写入 round 完成说明）
   - `PrivilegesRequired=lowest`（尽量不强制管理员）
   - 开始菜单「鹊桥远程协助」+ 卸载；桌面快捷方式可选
4. **运行时前提**：安装说明 / 向导旁注须写清：**需要 .NET Framework 4.8**；未装时给出官方安装指引短链或文案（不静默下载运行时进安装包）。
5. **功能范围**：安装后打开即现有双模式首页（接受协助 / 去协助）；**不**本轮改连接格式、信任模型、文案协议。
6. **官网**：本轮**可选不做**上传；需要上架时另开「官网换包」轮或与服务端协同。先保证本地/制品目录可分享 Setup。
7. **验收不打断**：无 UI 编译出 Setup 即可请「编译/文档核对」；**不**在用户本机强行跑安装向导除非用户明确要求；不包装部署到生产官网除非另授权。

## 与现有 Inno 的关系

- 现有 `installer/RemoteAssist.iss` + `scripts/build-installer.ps1` 面向 **.NET 8 自包含** `app-win-x64-sc`（体积约 72 MB 档），是**兼容备用**出包路径。
- **R047** 新增/改编一条 **net48 框架依赖**安装包路径（体积应接近现 publish 目录量级，远小于自包含），作为 **Windows 亲友主路径分发**。
- 两套 Setup 可并存；文档与文件名必须分清，避免发错包。

## 验收标准（产品）

1. 仓库内有可重复的构建脚本（或文档步骤）：publish net48 → 编出单个 Setup.exe。
2. Setup 安装后开始菜单可启动；首页为接受协助 / 去协助（与当前 net48 产物一致）。
3. 安装说明或旁注写明 **.NET Framework 4.8** 前提；未把完整连接凭证 / 验证码写进安装日志。
4. 文档：`round-047` / roadmap / PRODUCT 简述一致；未实现前状态保持 **open**，不得写成已上架官网。
5. **不**改 R046 范围；**不**改 Avalonia/CLI；**不**改写 git 历史。

## 本轮不做

- 会话码审计 / 管理台（仍属 **R046**）
- Avalonia/CLI 完整载荷移植
- 强制官网上传与公网换包（可选后续）
- 自包含 .NET 8 安装包重做（已有路径）
- 在用户本机无人值守弹窗安装；改连接/信任/guest_* 协议

## 完成时（实现轮）

- 客户端：iss/脚本路径、输出 Setup 路径与字节/SHA256、.NET 4.8 说明落点、与 net8 Setup 并存策略。
- 产品：对照本验收；需要时再开官网上传轮。

## 参考

- `docs/round-046.md`（并行：审计，不阻塞本轮）
- `docs/PRODUCT.md`（Windows 主路径 = net48）
- `installer/RemoteAssist.iss`、`installer/README.md`、`scripts/build-net48.ps1`
- `docs/round-043.md`～`docs/round-045.md`（当前 net48 功能水位）
