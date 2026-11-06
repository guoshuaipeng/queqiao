# 第 41 轮 — 「提议一项改动」等待/结果四态人话 + 连上后去掉 PAIR_OK

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端
> 状态：**wip**
> 依据：R040 产品已验收。场景 S004（`docs/scene-s004-propose-wait.md`）与体验质感续篇（`docs/quality-after-connect-status.md`）已收口。官网包已含 R039；本轮修连上之后协助方仍像控制台的两处人话。

## 本轮目标

1. **高频（S004）**：去协助端点「提议一项改动」后的四态人话（Windows + Avalonia）：
   - 等待：`正在等对方在本机确认…`（不要出现 `RunCommand`）
   - 允许成功：`对方已允许，改动已执行`（不要用「已提议一项改动」表示成功）
   - 拒绝：继续 `已拒绝，未做任何改动`
   - 超时：必须 `确认超时，未做任何改动`（`DeniedByUser` 时优先展示非空 `Error`，不要无条件盖成拒绝句）
2. **打磨**：配对刚成功、尚未点操作时，结果区不要显示 `PAIR_OK`；改为例如 `已连上。建议先点「查看正在运行的程序」。`；点操作后再显示可读结果。

## 行为要点

- 文案进共用常量，Windows Helper.UI 与 Avalonia HelperWindow 都接到。
- 只读四个常用操作不要套「正在等对方确认」等待句。
- 不新增中继/协议字段；不改被控端确认窗原文；不改五键文案与顺序、显隐；不改 R035–R039 已锁主状态/按钮。
- 不改 EndAssistCopy / InstallInstructions / Skill / trust-levels「才问你」。

## 本轮不做

- 改中继 JSONL、改 AI-SSH-Pro、完整远程桌面
- 换公网安装包、改官网 HTML
- 把 macOS 写成已经可下；买证书或上 HTTPS

## 完成时

- 更新本文交付说明
- 截图放到 `docs/screenshots/r041-*`（至少：刚连上无 PAIR_OK；等待确认句；允许成功句；拒绝句；超时句；Windows 与 Avalonia 都要覆盖四态与刚连上）。可用 ui-shot，须标明。**勿覆盖** r022-*～r040-*。
- 回告产品验收；未开下一轮
