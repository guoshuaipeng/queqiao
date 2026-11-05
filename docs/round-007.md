# 第 7 轮 — 被协助端信任等级 + 本机会话已确认改动回看

> 产品负责人：鹊桥-产品 · 2026-11-05 Asia/Shanghai
> 实现：鹊桥-客户端（只改 src/ UI/确认逻辑，不改中继、不换 UI 框架）
> 状态：**done**
> 依据：docs/trust-levels.md（验收与原文以该文档为准）；打磨对齐 P2-4 / S001 最小版

## 本轮目标

1. **高频**：被协助端信任等级（每次询问 / 宽松 / 完全信任）— 整份验收与界面原文以 `docs/trust-levels.md` 为准，不要另发明文案。
2. **打磨**：P2-4 / S001 最小版——被协助端本机回看「本次协助已确认的改动」（只读列表）。

## A. 信任等级（高频）

必须覆盖 `docs/trust-levels.md` 中的闭环，摘要如下（实现时仍以该文档全文为准）：

- **入口**：被协助端 →「高级」→ 分组标题「对方要改这台电脑时」；三选一，默认「每次询问」。
- **三档名称与一行说明**：每次询问 / 宽松 / 完全信任（说明原文见 trust-levels.md 表）。
- **完全信任二次确认原文**：标题「这次协助不再询问了吗？」；正文含不再弹窗、会记下、可改回、换人不会沿用；按钮「确定，这次不再询问」/「取消」。未点确定则档位不变。
- **主界面提示**：宽松/完全信任时显示当前档 +「改回每次询问」（点了立即生效、不再确认）；默认「每次询问」时主界面不额外提示。
- **完全信任范围**：仅当前会话 + 当前这位已连接的协助者；会话结束、刷新会话码、断开后换人、新访客重新配对后失效；不能设为开机默认，也不能跨会话保存。
- **访客只读天花板**：即便本机开了完全信任，访客也不能写；对方不能远程打开此档。
- **界面不出现** Assist、Admin、ownerLink。

## B. 本机会话已确认改动回看（打磨，P2-4 / S001 最小版）

- 被协助端本机只读列表：「本次协助已确认的改动」。
- **未确认 / 已拒绝的提议不得显示为已执行**。
- 不做云端工单；不做跨会话云端同步。
- 入口：被协助端「高级」或现有审计区即可。

## 本轮不做

- 官网（外网已通，交鹊桥-服务端做浏览器验收）
- Mac/Linux 客户端
- 改中继协议
- 完整远程桌面
- 改 AI-SSH-Pro

## 交付说明（2026-11-05 Asia/Shanghai）

### 代码

- `TrustLevelCopy` / `TrustLevelPolicy` / `HighRiskWrite` / `ConfirmedChangeList`（Protocol，文案与 trust-levels.md 对齐）
- `TrustLevelSession`：偏好可持久化（每次询问/宽松）；完全信任仅会话+协助者令牌；刷新会话码/换人/断开清空完全信任
- `FullTrustConfirmDialog`：二次确认原文一字不改
- `AgentHost`：写操作按 `EffectiveForClient` + `NeedsLocalConfirm`；审计 `tool_auto_relaxed` / `tool_auto_fulltrust`
- 被协助端 `MainForm`：「高级」三选一 + 主界面信任提示条 +「本次协助已确认的改动」列表；未改 R006 已验收界面结构

### 测试

- 全量 `RemoteAssist.Protocol.Tests`：**164 通过**（含 TrustLevel / HighRisk / ConfirmedChange / Session 29 条）

### 截图

| 文件 | 说明 |
|------|------|
| `docs/screenshots/r007-advanced-ask.png` | 高级里三档，默认每次询问 |
| `docs/screenshots/r007-fulltrust-confirm.png` | 完全信任二次确认原文 |
| `docs/screenshots/r007-fulltrust-cancel-stays.png` | 取消后仍停在每次询问 |
| `docs/screenshots/r007-banner-relaxed.png` | 宽松主界面提示 +「改回每次询问」 |
| `docs/screenshots/r007-banner-fulltrust.png` | 完全信任主界面提示（警告色） |
| `docs/screenshots/r007-ask-no-banner.png` | 每次询问时主界面无信任提示 |
| `docs/screenshots/r007-confirmed-list.png` | 已确认改动回看列表 |
| `docs/screenshots/r007-denied-not-in-list.png` | 拒绝项不出现在已确认列表（同屏对照种子审计） |

### 验收修复（同日）

- 高级展开时托盘状态行钉在面板下方，不再叠住「宽松」。
- 已确认列表展示 WriteConfirmCopy 人话，剥离 tool=/RunCommand。
- 四张验收图 MD5 互不相同；拒绝项只出现在「最近审计」。