# 第 13 轮 — Avalonia 已确认改动回看 + 刷新会话码确认

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（Avalonia 被控端；不改 `relay/`，不迁 Windows 到 Avalonia，不重做 R006–R012）
> 状态：**done（实现已推；待产品验收）**
> 依据：docs/mac-linux-scenes.md「客户支持」验收；docs/quality.md §1；Windows R007 已确认列表行为；docs/PRODUCT.md。

R012 已产品验收 Avalonia 信任三档 + Windows P2-7。客户支持场景仍要求：客户不进终端也能在本机看到这次已确认的改动；Avalonia 被控端还缺这份只读回看（Windows R007 已有）。同时 Avalonia「刷新会话码」仍可能与「复制码+密码」抢点击，对齐 quality.md 先确认再失效。

## 本轮目标

1. **高频**：Linux Avalonia **被控端**补上「本次协助已确认的改动」只读列表（对齐 Windows R007 / P2-4 最小版）。
2. **打磨**：Avalonia 被控端「刷新会话码」先确认旧码失效，避免与主按钮抢点击（对齐 `docs/quality.md` §1）。

## A. 本次协助已确认的改动（高频）

验收（对照 Windows R007 行为，不要自创说法）：

- 被控端本机只读列表，标题「本次协助已确认的改动」。
- 仅展示本会话已确认（含宽松/完全信任自动允许）的写操作，用人话；拒绝 / 未确认 / 超时拒绝的提议不得显示为已执行，也不进此列表。
- 入口：被协助端「高级」附近或现有审计区即可；亲友主路径首屏不要被列表挤占。
- 不做云端工单；不做跨会话云端同步；协助端 / MCP / 远程没有入口改这份列表。
- 界面不出现 Assist、Admin、ownerLink、TLS、端口号。
- osx 能出包即可；未上真机就注明，不写成 Mac 已验收。

截图放到 `docs/screenshots/r013-*`（至少：已确认列表有人话条目；一次拒绝项出现在最近审计但不在已确认列表，或同屏对照说明）。

## B. 刷新会话码先确认（打磨）

- 「复制码+密码」保持该屏唯一实心主按钮；「刷新会话码」为轻按钮，不得与主按钮同等权重并排抢点击。
- 点刷新先确认：「刷新后旧码会失效，已经发出去的码对方将连不上。」按钮「刷新会话码 / 取消」。取消则码不变。
- 确认后旧码旧密失效，界面展示新码；完全信任若在本次会话中开启，刷新后必须失效（沿用 trust-levels.md）。

截图：`r013-refresh-confirm.png`（或等价命名）。

## 本轮不做

- 改中继、改官网、改 AI-SSH-Pro、完整远程桌面
- 把 Windows 迁到 Avalonia
- Avalonia 账号登录 / 设备上线 / 免密（可后续）
- 重做 R006–R012 已验收路径
- 把「打开终端复制码」写给亲友主路径
- Linux `.deb` / 压到个位数 MB 硬指标

## 完成时

- 更新本文交付说明、`docs/roadmap.md`（本轮标 done）
- 截图放到 `docs/screenshots/r013-*`
- 回告产品验收；未开下一轮

## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要
- **Avalonia 已确认改动回看**：被控窗「高级」内标题「本次协助已确认的改动」，复用 `ConfirmedChangeList.Filter/DisplayLine/HumanizeDetail` + `AuditLog`；含已确认与宽松/完全信任自动允许；拒绝/未确认不进列表。旁附「最近审计」便于对照拒绝项（人话，剥掉 tool=/source=）。
- **刷新会话码先确认**：`RefreshSessionConfirmWindow` 原文对齐 `FriendConnectGuide` / quality.md §1；取消码不变；确认后 `RefreshSessionCode`（旧码旧密失效，完全信任经 `OnSessionRefreshed` 失效）。「复制码+密码」仍为唯一实心主按钮，刷新为轻按钮。
- osx 未上真机，不写成 Mac 已验收。未开下一轮。

### 截图（`docs/screenshots/r013-*`，已复制 `%USERPROFILE%\Downloads\r013-shots\`）
| 文件 | 屏幕可见要点 |
|------|-------------|
| r013-confirmed-list.png | 「本次协助已确认的改动」含人话「已确认 · 运行一条命令 · 命令：echo r013-ok」；会话码为**文档例** AB12CD/3471 |
| r013-denied-not-in-confirmed.png | 最近审计可见「已拒绝，未做任何改动」；已确认列表仍只有已确认项、无拒绝 |
| r013-refresh-confirm.png | 「刷新后旧码会失效，已经发出去的码对方将连不上。」按钮「刷新会话码 / 取消」 |
| r013-refresh-cancel-keeps-code.png | 取消后仍为文档例 AB12CD |
| r013-fulltrust-before-refresh.png | 完全信任横幅「结束完全信任」 |
| r013-refresh-clears-fulltrust.png | 确认刷新后新码（现场 host 码，非文档例）+ 完全信任横幅消失 |

### 测试
`dotnet test`：168 通过 / 0 失败。
