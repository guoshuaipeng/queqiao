# PV 复验产品收口 · 2026-11-07 Asia/Shanghai

> 产品：鹊桥-产品  
> 仓库：`E:\IdeaProjects\remote-assist` · **master only**（未触碰 Tauri worktree 业务码）  
> **不 push / 不上公网 / 不部署生产 / 不授本阶段编码 / 不启 CloudAgent**

## 证据来源

| 项 | 值 |
|---|---|
| 复验全文（Tauri wt） | `docs/tauri-migration/acceptance/ACCEPTANCE-REVERIFY-20261107.md` |
| 复验 commit（wt） | **`4dbfe52`** |
| 主仓状态指针 | **`69892c6`**（`docs(status): test reverify matrix back; awaiting product closeout`） |
| Soft QA | node **53/53** PASS；cargo **25/25** PASS；无独立 typecheck（N/A） |
| 前序收口 | PV-001 `pv-001-product-closeout-20261107.md`；PV-002～004 初收 `pv-002-004-product-closeout-20261107.md`（本文件覆盖其可更新结论） |

> 抽查：证据文件与测试交回摘要一致；无致命造假/口径冲突。

## 产品决定（锁定 · 2026-11-07）

### PV-001 · 保持完成（仅本地验收）

- 不重开；公网/生产仍未授。

### PV-002 · 维持待验收

- net48 首页双卡留白**仍缺口**；不可视为验收完成；**不得伪 PASS**。
- Tauri 协助旁证=部分，**不替代** net48。
- 旧 docs/shots 误拍=无效。
- **不开**体验执行轮；**不授** net48 编码。

### PV-003 · 完成（仅本地验收）

- Setup 包图标：**PASS**（artifacts SHA256 前缀 **`71CE3908300A8046`**）。
- 开始菜单 `.lnk`：**PASS**（补证）。
- 桌面 `.lnk`：**PASS**（`/TASKS=desktopicon` 隔离重装；默认未勾选则无桌面项属预期）。
- 「完成」=**仅本地验收**；**≠** 公网换包 / 生产发布。
- **不派**改码 / **不上公网换包**。

### PV-004 · 部分完成

- iss 配置已备 ≠ 向导全量 PASS。
- 向导语言实测：**PASS（UIA）**，本机 zh-CN。
- 向导可视像素截图：**缺口**（PrintWindow 黑图 / CopyFromScreen 锁屏污染；禁止冒充）。
- **不派**安装器编码；可视图若产品日后坚持，另定取证手段（人工跟屏等）。

### Tauri 会话主路径 · Soft QA + 对抗复验 PASS

- Idle→Waiting / 停→Idle / 完整 pair（拒绝+允许+结束）：**PASS**。
- 开启失败文案 +「重试开启」按钮：**PASS（现状）**；「重试→等待」**未实现**（未授权，只记）。
- 复制失败不谎报：**PASS**（err=5）；成功路径不得报修好。
- **无功能回归须拦回客户端**。

### 编码边界（不变）

- **不放行**剪贴板成功写入路径编码。
- **不放行**「重试开启」→等待编码。
- **R050** blocked，不解。
- **不启** CloudAgent 本轮（下一编码轮仍暂无立项；SCM Origin 未通亦不开 bc-）。

## 状态一览（产品口径 · 覆盖前序 PV-002～004 初收）

| 编号 | 产品状态 | 要点 |
|---|---|---|
| PV-001 | **完成（仅本地验收）** | 保持；公网未授 |
| PV-002 | **待验收（留白缺口）** | 维持；不开体验/不授 net48 编码 |
| PV-003 | **完成（仅本地验收）** | 图标+.lnk PASS；≠公网换包 |
| PV-004 | **部分完成** | 语言 UIA PASS；可视像素缺口；不派安装器编码 |
| Tauri 会话 | Soft QA+对抗 **PASS** | 无功能回归拦回；剪贴板/重试边界不变 |

## 下一项

- 正式放编码：**暂无**。
- 本机：可继续真实验收取证；开发编码仍走 CloudAgent（待产品另授 + SCM 通畅）。