# feat/tauri-client-migration · 建分支回执

- **时间**：2026-11-06 15:13 Asia/Shanghai
- **执行**：鹊桥-迭代（唯一建分支）
- **基点**：7d7178c4a7d2232089d137ee5a91fc7c6b4e39bc（7d7178c · 当时本地 master tip；origin/master 仍落后本地 ahead 批）
- **分支**：feat/tauri-client-migration
- **工作树**：E:\IdeaProjects\remote-assist-wt-tauri（干净检出；非 E:\IdeaProjects\remote-assist 脏主树）
- **远端跟踪**：**未 push**（remote-assist 站立授权仅本地 commit；要 push -u 须用户另说）
- **本提交内容**：仅 docs/tauri-migration/（边界/决策/授权/探针笔记）；不删 net48/net8；不解 R050；不混官网；探针 ≠ 迁移完成
- **客户端**：须另开 worktree 只跟踪本分支；禁止抢脏主树或自建第二迁移分支

## 工作树交接（2026-11-06 15:15 Asia/Shanghai）

- **唯一编码写入者**：**鹊桥-客户端**
- **唯一写入路径**：E:\IdeaProjects\remote-assist-wt-tauri（本 worktree，分支 feat/tauri-client-migration）
- **鹊桥-迭代**：建分支 + docs 落盘已完成；**此后不再**在本 worktree 提交业务/壳代码；需要再改迁移 docs 时与客户端协调，避免抢写。
- **禁止**：第二 worktree 检出同一分支；--force / --ignore-other-worktrees 绕过。
- **主树** E:\IdeaProjects\remote-assist（master，脏）：官网/其它角色文档用；**不是** Tauri 编码树。
