# 第 14 轮 — Avalonia 账号登录免密连自有设备 + 审计人话

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（Avalonia；不改 `relay/`，不迁 Windows 到 Avalonia，不重做 R006–R013）
> 状态：**产品已验收**
> 依据：Windows 已验收的账号登录→设备列表→免密连自有在线设备（PRODUCT.md）；docs/quality.md（账号/密码有可见标签；未选中设备时禁用连接）；R013 已验收；round-013 本轮不做里写过「Avalonia 账号登录 / 设备上线 / 免密（可后续）」。

R013 已产品验收 Avalonia「本次协助已确认的改动」+ 刷新会话码先确认。Windows 侧账号登录与免密连自有设备早已验收；Linux Avalonia 双模式仍缺同等路径。同时 R013 验收记下「最近审计」仍出现 `preference=AskEveryTime` 内部名，本轮一并打磨成人话。

## 本轮目标

1. **高频**：Linux Avalonia 补上与 Windows 同等的「账号登录 → 已上线的自有设备列表 → 免密连接」。
2. **打磨**：「最近审计」改成人话，不要出现 `preference=AskEveryTime` 这类内部名。

## A. Avalonia 账号登录免密（高频）

验收（对照 Windows 已验收界面与 quality.md，不要自创说法）：

- 默认走公网中继。
- 账号、密码字段有可见标签；未选中设备时「连接」不可用。
- 登录后能看到自己已上线的设备；被控端登录后应能出现在该账号的设备列表里（按 Windows 已有约定，不要另做一套）。
- ownerLink / 免密连自有设备可跳过配对确认窗；写操作确认不可因免密而绕过，仍走本机信任等级（默认每次询问）。
- 亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号。品牌「鹊桥」。
- osx 能出包即可；未上真机就注明，不写成 Mac 已验收。

截图放到 `docs/screenshots/r014-*`（至少：登录标签可见；设备列表且未选中时连接不可用；选中已上线设备后可免密连；最近审计无 preference=英文内部名）。

## B. 最近审计人话（打磨）

- 「最近审计」改成人话，不要出现 `preference=AskEveryTime` 这类内部名。
- 信任等级变更应写「每次询问」/「宽松」/「完全信任」。
- 不要改 R013 已验收的已确认列表标题和刷新确认原文。

## 本轮不做

- 改中继、改官网下载、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- 重做 R006–R013 已验收路径
- 把「打开终端复制码」写给亲友主路径
- 改 trust-levels.md 里宽松一行说明的「才问你」

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- **协助端 `HelperWindow`**：在「连接对方」下方增加「账号（可选）」——可见「账号」「密码」标签；注册 / 登录 / 刷新设备；设备列表；未选中时「连接这台电脑」禁用并提示「请先点选一台自己的电脑」；选中后按钮改为「连接：{名称}」，文案「将免密连接「…」」。免密走既有 `AssistConnectOptions.OwnerLink` + `AccountToken` + `DeviceId` + 默认公网中继（与 Windows Helper.UI 同协议）。
- **被控端 `MainWindow` 高级**：账号绑定登录/注册，写入 `HelperUiSettings` 并 `AgentHost.BindAccount`，拉起 `device_online` 循环，使本机出现在同账号设备列表（约定与 Windows `AgentDesktopController` + settings 一致，未另起一套）。
- **`Program.CreateHost`**：加载 settings 的 `AccountToken` / `DeviceId` / `DeviceDisplayName`。
- **打磨**：`TrustLevelCopy.HumanizeAuditDetail` + `MainWindow.FormatAuditLine`——「最近审计」信任变更显示「每次询问」/「宽松」/「完全信任」/「改回每次询问」/「结束完全信任」，不再外露 `preference=AskEveryTime`。未改 R013「本次协助已确认的改动」标题与刷新确认原文。
- **osx**：`scripts/publish-agent-avalonia.ps1` 已出 `osx-x64` / `osx-arm64` FD 包；**未上真机**，不写成 Mac 已验收。

### 截图（`docs/screenshots/r014-*`，已复制 `%USERPROFILE%\Downloads\r014-shots\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r014-helper-account-labels.png` | 账号/密码可见标签；连接这台电脑禁用；ui-shot | 7FB55DA2FDECB7CA25C9A93092133F58 |
| `r014-helper-account-unselected.png` | 设备列表有「客厅电脑 在线」等；未选中；连接禁用 +「请先点选」；ui-shot | 9519BFAD7478C408ABD9B89DD8C2DA8F |
| `r014-helper-account-selected.png` | 选中在线「客厅电脑」；「连接：客厅电脑」可用；ui-shot | 847D7A7C277BAAF6AE208B96C679DE53 |
| `r014-helper-owner-connected.png` | 免密已连状态文案「已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）」；ui-shot | E4750D6A2212252282FF1FDBEF1A4866 |
| `r014-audit-human.png` | 最近审计：宽松 / 完全信任 / 改回每次询问 / 结束完全信任；无 preference=；已确认标题仍为「本次协助已确认的改动」；ui-shot | C20C77597556DD05DCDA838632A1F011 |
| `r014-osx-publish.png` | osx 出包体积说明；标明未上真机 | B7F7D9C77587B00882759CACF1458C39 |

登录/设备列表截图为 **ui-shot**（示例设备名「客厅电脑 / 书房电脑」），不是现场公网结对码。未做现场双机免密联调截图；协议与 Windows 已验收路径相同。

### 测试

`dotnet test`：169 通过 / 0 失败（含 `HumanizeAuditDetail_Maps_Preference_To_Chinese`）。

### 明确未做

- 未开 R015 / 下一轮
- 未改 `relay/`、官网下载、AI-SSH-Pro
- 未把 Windows 迁到 Avalonia；未重做 R006–R013
- 未改 R013 已验收的「本次协助已确认的改动」标题与刷新确认原文「刷新后旧码会失效，已经发出去的码对方将连不上。」
- 亲友界面未出现 Assist、Admin、ownerLink、TLS、端口号

## 产品验收（2026-11-05 Asia/Shanghai）

产品对照截图验收通过，提交 `edddc11`（`edddc11d8877079978b4b4e5f2c59e0417cc9281`）。交付说明记 `dotnet test` 169 通过 / 0 失败（含 HumanizeAuditDetail_Maps_Preference_To_Chinese）。

所见：
- `r014-helper-account-labels.png`：品牌「鹊桥远程协助 · 去协助别人」；「账号」「密码」可见标签；「连接这台电脑」禁用；提示「请先点选一台自己的电脑」；说明「登录后免密连自己的机器（无需再确认配对；写操作仍须对方本机确认）」；状态「请填写账号和密码后登录」。
- `r014-helper-account-unselected.png`：示例账号 demo；列表「dev-living  客厅电脑  在线」「dev-office  书房电脑  离线」；未选中；连接禁用 + 「请先点选一台自己的电脑」；状态「已登录 (截图)」。
- `r014-helper-account-selected.png`：选中「客厅电脑  在线」；按钮「连接：客厅电脑」；旁注「将免密连接「客厅电脑」」。按钮仍是灰色次要样式，与禁用态视觉接近，靠文案区分可用（不挡本轮，见 R015）。
- `r014-helper-owner-connected.png`：会话码输入区禁用；上方「连接」变灰；状态「已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）」；状态「免密已连接 (截图)」。「常用操作」仅见标题，按钮态未完整露出。
- `r014-audit-human.png`：最近审计可见「结束完全信任」「改回每次询问」「完全信任」「宽松」；无 `preference=`；已确认列表标题仍是「本次协助已确认的改动」；被控端高级里有账号绑定（「登录后，这台电脑会出现在该账号的设备列表里…」）。宽松一行说明仍是「才问你」（与 trust-levels.md 表一致，不要改成「才会问你」）。审计四条时间戳相同（2026-11-05 21:25:58），为界面示例数据，不挡。
- `r014-osx-publish.png`：osx-x64 26.40 MB / osx-arm64 26.43 MB，标明未上真机、不写成 Mac 已验收。

缺口（不挡本轮）：
- 登录/设备列表/免密已连接均为 ui-shot（示例账号 demo、示例设备客厅电脑/书房电脑），不是现场公网双机结对。未拍被控端登录后真实出现在该账号列表、免密连上后写操作仍弹出确认。文案已写明写操作仍须本机确认；协议与 Windows 已验收 OwnerLink 一致。
- 设备列表同时显示了内部 id（dev-living / dev-office）和人话名称（客厅电脑 / 书房电脑）——交 R015。
- 选中在线设备后「连接：客厅电脑」仍是灰色次要样式，与禁用态靠文案区分——交 R015。
- osx 已出包、未上真机，不写「Mac 已验收」。

下一轮见 `docs/round-015.md`。
