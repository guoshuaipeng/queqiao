# 鹊桥 — 迭代复盘

> 每轮结束后追加一节。通用规则晋升到 `docs/dev-rules.md`（≤15 条）。

## R000 — 启动持续迭代（2026-11-05）

- **触发**：用户要求产品 bot 自主迭代，保证易用性，可与架构师沟通并指定开发计划。
- **动作**：建立 `round-001.md` 选题机制；P2 按易用性重排；`ownerLink` 从 todo 改为待验收。
- **预防规则**：每轮必须 1 高频 + 1 小打磨；决策只进 `docs/`，再 @ 实现 bot。

## R001 — 一键复制码+密码 + 品牌「鹊桥」（2026-11-05）

- **交付**：复制码+密码主按钮；鹊桥品牌文案/安装显示名；截图三张；测试 103 绿。
- **踩坑**：统一 App 已创建 ModeSelect 后再 SetCompatibleTextRenderingDefault 会炸，--accept 进被控失败；需吞掉 InvalidOperation / 由宿主先初始化。
- **踩坑**：桌面 CopyFromScreen 易被其它窗口遮挡；验收截图改用 WinForms DrawToBitmap（	ools/UiShot）。
- **规则候选**：有 UI 的轮次优先 DrawToBitmap/专用 shot 工具，不依赖当前桌面前景。