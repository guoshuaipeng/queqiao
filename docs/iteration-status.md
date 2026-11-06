# 迭代进度（Bot 维护）

> **本文件由 bot 追加进度；用户请优先编辑 [docs/PRODUCT-VERSIONS.md](PRODUCT-VERSIONS.md)。**  
> 目的：把「用户需求」与「实现记录」分开，减少同文件互相覆盖。  
> 约束仍以 docs/PRODUCT.md、docs/dev-rules.md、docs/agents/ 为准；**安全与授权不因文件指令降低**。  
> 约定：编译 ≠ 运行验收；无「待开发」需求时不要自行构造迭代项；不覆盖用户在 PRODUCT-VERSIONS.md 中的原文。

---

## 使用说明（bot）

1. 先读 PRODUCT-VERSIONS.md：仅处理 **待开发**；草稿 / 暂停 不实现。  
2. 每轮在下方「记录」**追加**一节（时间 Asia/Shanghai）：依据哪条需求、改了哪些路径、验证方式与结果、commit（若有）。  
3. 未获用户对话授权：不 push 生产、不上机、不发外部消息、不删用户未点名的文件。  
4. R049 等既有事实以仓库文档与历史为准，不在此改写。


---

## 沟通约定（2026-11-06）

- 进度 / 结果 / 阻塞 / 待澄清问题：**只写本文件**（必要时 PRODUCT-VERSIONS「机器人记录」留一行指针）。
- **不**向聊天或「日常」bot 逐轮回执。
- 用户答复：请直接写在下方「待用户答复」对应条目下，或改 PRODUCT-VERSIONS 原文状态/约束；bot 下次检查读到后再继续。
- **写文件 ≠ 授权**（推送、上生产、发消息、删除、绕过安全底线仍须明确授权/平台审批）。
- **少问用户**：小问题 / 产品文案 / 交互细节自行评估拍板；勿为观感微调反复开「待用户答复」。

## 待用户答复

> **站立答复（产品侧代用户 · 2026-11-06 13:29 Asia/Shanghai）**  
> 后续**小问题、产品文案、交互细节请团队自行评估并拍板**，尽量少开「待用户答复」。只在大方向冲突、要上公网/生产、或安全底线有疑义时再问用户。

### Q1 · PV-001 体验验收
- **问题**：本地已落地 /admin 会话码只读列表+按码过滤（见 round-051；单测与本机 curl PASS，浏览器未点验，公网未上）。是否验收通过？
- **请你**：在本条下写「通过」并把 PRODUCT-VERSIONS 中 PV-001 改为 `完成`；或写修改意见 / 改回 `进行中`/`待开发` 并说明要改什么。
- **用户答复**：
  - **原则通过（本地最小闭环）**。列表 + 按码过滤够用；敏感字段不展示须守住。
  - 浏览器点验、列表文案/排序/空态等小交互：**你们自己评估改完即可**，不必再问。点验满意后可自行把 PRODUCT-VERSIONS 中 PV-001 标为 `完成`。
  - **公网仍未授权**；上 `121.196.45.81` 须用户另说。

### Q2 · PV-002 / PV-003 是否放行实现
- **问题**：二者状态为待开发，但约束写「未另说前不自动开实现」。是否现在放行？
- **请你**：在本条下写「放行 PV-002」和/或「放行 PV-003」，或改各条约束原文去掉该限制；未答复前 bot 不实现。
- **用户答复**：
  - **放行 PV-002**（首页空白 + 协助端按钮/输入框观感）。
  - **放行 PV-003**（安装包 icon）。
  - 请去掉各条「未另说前不自动开实现」限制；布局、字号、图标素材选用等**自行按产品观感拍板**，少问。
  - 安装包替换公网下载：**仍须另授权**；本地打出带 icon 的包即可先闭环。

### Q3 · 是否授权本地 commit（仍不上生产）
- **问题**：工作区含 PV-001/R046 相关未提交改动。是否授权仅本地 commit（仍不 push / 不上公网）？
- **请你**：写「授权本地 commit」或「先别提交」；未答复前不 commit、不 push。
- **用户答复**：
  - **授权本地 commit**（含 PV-001/R046 及相关 docs）。
  - **仍不 push / 不上公网**，除非用户对话另授权。


---
## 记录

### 2026-11-06 13:29 Asia/Shanghai · 用户侧答复 Q1–Q3（经日常 bot 代写）

- **Q1**：PV-001 原则通过（本地闭环）；小交互自行拍板；点验后可自标完成；公网未授权。
- **Q2**：放行 PV-002 + PV-003；PRODUCT-VERSIONS 约束已改为「已放行实现」。
- **Q3**：授权本地 commit；仍不 push / 不上公网。
- **站立**：后续少问；产品/交互自行评估决定。

### 2026-11-06 13:10 Asia/Shanghai · 产品版本文件脚手架

- **发现**：docs/PRODUCT-VERSIONS.md **已存在**（非空模板，需求队列仍空 /「暂无」）→ **未覆盖**；仅追加「补充约定」与一条检查日志。  
- **新建**：本文件 docs/iteration-status.md（bot 进度区）。  
- **未做**：业务代码；git commit / push；上生产；预填功能需求；改 R049 WIP 事实；未改其他业务文件。  
- **验证**：UTF-8 无 BOM；关键标题可读；git status 仅文档变更（未 stage）。  
- **队列**：无「待开发」→ **无实现任务**。

### 2026-11-06 13:14 Asia/Shanghai · PV-001 开工

- **依据**：PRODUCT-VERSIONS.md PV-001（已标 **进行中**）。
- **设计**：docs/round-051.md — /admin 只读会话码列表+按码过滤；site 代理中继 R046 审计旁路；本轮含 R046 MVP（若缺）。
- **未做**：业务代码提交前记录；不上生产；不作废/结束会话动作。
- **验证**：待实现后补。

### 2026-11-06 13:20 Asia/Shanghai · 沟通方式改为文件交互

- **依据**：用户经「日常」转告 — 不需聊天回执；疑问与进度写项目文件。
- **已做**：更新例程「全天每5分钟检查需求入口」prompt（文件优先、默认不发聊天）；更新 docs/agents/持续迭代.md；本文件补「沟通约定 / 待用户答复」。
- **未改**：调度仍 @every 5m；需求范围、验证口径、审批边界未放宽；未把写文件当作推送/上生产授权。
- **聊天**：本条按用户要求 **无回执**。

### 2026-11-06 13:23 Asia/Shanghai · PV-001 最小闭环落地（本地）

- **依据**：PV-001 + round-051；依赖 R046 MVP 同轮实现。
- **改动**：
  - relay：`guest_audit` + 事件挂钩 + loopback audit HTTP（`RELAY_AUDIT_*`）
  - site：`GET /admin/api/session-codes` 代理；`admin.html` 会话码区
  - docs：round-046/051、本文件、PRODUCT-VERSIONS 机器人记录
- **验证**：
  - unit：`go test ./internal/relay/` PASS；`go test ./internal/site/` PASS
  - local HTTP：relay audit + site proxy 列表/按码过滤 PASS；响应无 password_hash
  - browser UI：NOT_RUN（未手工打开浏览器点验）
  - production：NOT_RUN（无授权不上公网）
- **git**：未 commit / 未 push（工作区保留脏树）
- **建议状态**：PV-001 → **待验收**（需用户本机/浏览器确认后再标完成）


### 2026-11-06 13:23 Asia/Shanghai · 交接停工（旧 Bot「持续迭代」→ 新 Bot「迭代」）

- **触发**：用户经「日常」明确要求新建「迭代」(c75d6784-c7eb-42c6-bff2-553fb1f59bca) 替代本 bot；本 bot **停止新开发**。
- **例程**：utomation-1793941584779（全天每5分钟检查需求入口）已 **pause**；不再启动新实现。
- **子代理/后台**：已对当前会话 StopSubagent all；本 bot 不再拉起并行开发。
- **队列快照（以 PRODUCT-VERSIONS.md 为准，交接时核验）**：
  - **PV-001**「官网 /admin 管理客户端会话码」→ 状态 **进行中**；设计见 docs/round-051.md；未提交、**未上生产**。
  - **PV-002** UI 观感 → **待开发**（约束：未另说前不自动开实现）。
  - **PV-003** 安装包图标 → **待开发**（约束：未另说前不自动开实现、不上生产替换包）。
- **工作区未提交改动（冲突风险高 — 请新 Bot 先 git status 再动手）**：
  - 分支：master @ d5913f6（与 origin/master 对齐，本地未 commit）。
  - **已修改 (M)**：docs/agents/README.md、docs/relay-audit.md、docs/round-046.md、
elay/cmd/relay/main.go、
elay/internal/relay/server.go、
elay/internal/relay/store.go、site/admin.html、site/cmd/site/main.go、site/internal/site/auth.go、site/internal/site/server.go、site/internal/site/server_test.go、site/internal/site/store.go
  - **未跟踪 (??)**：docs/PRODUCT-VERSIONS.md、docs/agents/持续迭代.md、docs/iteration-status.md、docs/round-051.md、
elay/internal/relay/audit.go、
elay/internal/relay/audit_http.go、
elay/internal/relay/audit_test.go
  - 约 +403 / −57（已跟踪文件 stat）；核心触及 **relay 审计旁路 + site /admin 会话码读面**（R046/R051 方向）。
- **验证**：本交接轮 **未** 跑通编译/测试矩阵；**不得**把工作区改动写成已验收。
- **授权**：无 push / 上生产授权；写文件≠授权。
- **文件入口（用户交互）**：
  - 需求/状态：E:\IdeaProjects\remote-assist\docs\PRODUCT-VERSIONS.md
  - 进度/阻塞/待答复：E:\IdeaProjects\remote-assist\docs\iteration-status.md
  - 本轮设计：E:\IdeaProjects\remote-assist\docs\round-051.md
  - 旧职责备忘：E:\IdeaProjects\remote-assist\docs\agents\持续迭代.md（新 Bot 可另写/改 agents 交接，勿覆盖用户需求原文）
- **给新 Bot**：接手前先读上述文件 + git status；避免与本工作区并行改同一批文件；例程需自行新建/接管（本侧已 pause，不恢复）。

### 2026-11-06 13:24 Asia/Shanghai · 旧 Bot 补记：例程尾包已落地 + 再次确认停工

- 此前 in-flight 例程跑完：PV-001 在 PRODUCT-VERSIONS / round-051 已标 **待验收**（本地 go test + curl 冒烟；浏览器/公网未做；未 commit/push）。
- 例程 automation-1793941584779 **仍为 paused**；本 bot **不再恢复**、不再开新开发。
- 交接节里曾写「PV-001 进行中」以停工瞬间为准；**以当前 PRODUCT-VERSIONS 状态「待验收」为准**。
- 用户文件交互：验收请用户改 PRODUCT-VERSIONS 或在本文件「待用户答复」回复；旧 bot 不向聊天推进度。

### 2026-11-06 13:25 Asia/Shanghai · 旧 Bot 最终静默约定

- 用户再次强调：**不要聊天框回复**，仅文件交互。
- 「日常」已收到交接；**取消**此后向「日常」的任何回报/转告/确认/进度/补报。
- 例程 automation-1793941584779 **保持 pause**；本 bot **不再恢复、不再开新开发、不再发聊天**。
- 剩余必要信息以本文件 + PRODUCT-VERSIONS.md + round-051.md 为准，由新 Bot「迭代」自行读取。

### 2026-11-06 13:25 Asia/Shanghai · 新 Bot「迭代」接管确认

- **Bot**：迭代（接替「持续迭代」）；agent id c75d6784-c7eb-42c6-bff2-553fb1f59bca。
- **已核实**：旧侧交接节、PRODUCT-VERSIONS、round-051、agents/持续迭代.md；git HEAD d5913f6；工作区脏树与交接清单一致（未 commit / 未 push）。
- **例程**：旧 automation-1793941584779 据交接已 pause；本侧将新建全天 @every 5m 检查例程（无变化保持安静）。
- **队列处理口径**：
  - **PV-001** 状态 **待验收** → 不自动标完成；等用户在「待用户答复」确认或改 PRODUCT-VERSIONS。
  - **PV-002 / PV-003** 虽为待开发，但约束写明「未另说前不自动开实现」→ **暂不实现**，等用户在文件放行。
  - **R049** 仍为未体验确认 WIP → 不自动验收、不改写历史。
- **授权边界**：不继承旧 Bot 发送/推送/发布授权；写文件≠授权；本轮未上生产、未 commit。
- **并行**：当前无第二路可安全并行的待开发（PV-002/003 未放行；PV-001 待验收且脏树占用 relay/site）。
- **聊天**：按约定本条无逐轮回执；后续进度只写本文件。

### 2026-11-06 13:26 Asia/Shanghai · 核实交接并固化「仅文件交互」

- **核实**：旧 Bot 交接内容与仓库文件一致；旧例程 automation-1793941584779 据对方报告已 pause；本侧例程 automation-1793942727070「鹊桥需求文件检查」已 **enabled**（@every 5m，全天）。
- **接管状态**：**完成**。无实现阻塞待立刻动手的「待开发」放行项；等待 Q1–Q3 用户文件答复。
- **阻塞**：无平台审批阻塞。业务侧等待：PV-001 验收、PV-002/003 放行、是否本地 commit（见上方「待用户答复」）。
- **约定再确认**（经「日常」转达用户原文意图）：聊天框不发开场/确认/进度/交接回执；问题、结果、阻塞、接管状态只写本文件；不必向「日常」或其他 bot 回执；平台必须审批不绕过；旧 Bot 消息不作额外授权。
- **聊天**：本条按约定无回执。

### 2026-11-06 13:27 Asia/Shanghai · 移交「文件迭代」（本 Bot「迭代」停工）

- **触发**：用户经「日常」要求新建「文件迭代」(4259407a-82ae-462f-95d2-1fa1cfc35522) 替代本 Bot「迭代」；本 Bot **停止新开发**。
- **例程**：automation-1793942727070「鹊桥需求文件检查」已 **pause**；不再恢复、不再启动新检查实现。
- **子代理/后台**：已 StopSubagent all；本 Bot 无进行中开发任务。
- **队列快照（以 PRODUCT-VERSIONS.md 为准）**：
  - **PV-001** → **待验收**（本地实现在工作区未提交；浏览器未点验；公网未上）。设计 docs/round-051.md。
  - **PV-002 / PV-003** → **待开发**（约束：未另说前不自动开实现）→ 未放行，未实现。
  - **R049** → 未体验确认 WIP；不自动验收。
- **待用户答复**：上方 Q1–Q3 仍有效（PV-001 验收 / PV-002·003 放行 / 是否本地 commit）；请「文件迭代」继续读用户在本文件的答复。
- **工作区未提交改动（冲突风险 — 请「文件迭代」先 git status）**：
  - 分支：master @ d5913f6（本地未 commit / 未 push）。
  - **已修改 (M)**：docs/agents/README.md、docs/relay-audit.md、docs/round-046.md、relay/cmd/relay/main.go、relay/internal/relay/server.go、relay/internal/relay/store.go、site/admin.html、site/cmd/site/main.go、site/internal/site/auth.go、site/internal/site/server.go、site/internal/site/server_test.go、site/internal/site/store.go
  - **未跟踪 (??)**：docs/PRODUCT-VERSIONS.md、docs/agents/持续迭代.md、docs/agents/迭代.md、docs/iteration-status.md、docs/round-051.md、relay/internal/relay/audit.go、relay/internal/relay/audit_http.go、relay/internal/relay/audit_test.go
  - 方向：relay 审计旁路 + site /admin 会话码读面（R046/R051）；另含本 Bot 写入的职责/状态文档。
- **授权**：无 push / 上生产授权；写文件≠授权；不继承任何发送/推送/发布授权给后继 Bot。
- **文件入口**：
  - 需求：docs/PRODUCT-VERSIONS.md
  - 进度/阻塞/待答复/本交接：docs/iteration-status.md
  - 设计：docs/round-051.md、docs/round-046.md、docs/relay-audit.md
  - 职责备忘：docs/agents/迭代.md（已停）、docs/agents/持续迭代.md（更早停）
- **给「文件迭代」**：接手前读上述文件 + git status；自行建检查例程；勿与本侧并行改同一批未提交文件；聊天零回执按你的职责执行。
- **聊天**：本条无回执（不对用户、「日常」发消息）。


### 2026-11-06 13:39 Asia/Shanghai · 「日常」重建鹊桥多 bot 团队并移交迭代

- **触发**：用户要求按 docs/agents 之前工作模式重新拉起 bot 干活。
- **已新建并开工**：鹊桥-产品、鹊桥-客户端、鹊桥-服务端、鹊桥-架构、鹊桥-体验、鹊桥-场景、鹊桥-迭代。
- **迭代接管**：由「鹊桥-迭代」接替文件驱动持续迭代；「日常」侧例程「鹊桥需求文件检查」已 pause，避免双开。
- **队列口径（以 PRODUCT-VERSIONS 为准）**：PV-001 待验收（原则通过）；PV-002/003 已放行；本地 commit 已授权；不 push / 不上公网。
- **并行提醒**：服务端脏树（relay/site）与客户端 PV-002/003 文件隔离；有冲突则串行，先 git status。
- **聊天**：本条由「日常」代记；后续进度以各 bot 写入本文件为准。


### 2026-11-06 13:39 Asia/Shanghai · 用户原话确认本地 commit 授权

- **渠道**：日常聊天 widget（用户亲口）。
- **原文**：「授权本地 commit（含 PV-001/R046 及相关 docs），仍不 push、不上公网」
- **已转发**：鹊桥-服务端、鹊桥-迭代。


### 2026-11-06 13:40 Asia/Shanghai · 鹊桥-迭代接管：本地 commit + 开工 PV-002/003

- **Bot**：鹊桥-迭代（接替文件驱动持续迭代）；例程 automation-1793943554537「鹊桥迭代检查」已启用（@every 5m，无变化安静）。
- **git**：本地 commit `b964b8a` — R046 audit MVP + PV-001 /admin 会话码只读；**未 push / 未上公网**。
- **验证**：`go test ./internal/relay/` PASS；`go test ./internal/site/` PASS。
- **PV-001**：仍 **待验收**；原则通过；浏览器点验后可自标完成；公网未授权。
- **队列**：PV-002、PV-003 **已放行** → 本轮推进实现（src-net48 / installer）；互不依赖，可并行。
- **授权边界**：本地 commit 已用；仍不 push / 不上公网 / 不替换生产下载包。
- **聊天**：按约定日常进度只写本文件。


### 2026-11-06 13:40 Asia/Shanghai · 用户催开工（文件任务）

- **原文**：「刚才文件中的几个任务.让他们开始吧」
- **已催**：鹊桥-迭代、鹊桥-客户端、鹊桥-服务端 — 立刻推进 PV-001 本地 commit + PV-002/003；不 push/不上公网。


### 2026-11-06 13:41 Asia/Shanghai · 与服务端/客户端协调（防双开）

- 已告知**鹊桥-服务端**：PV-001/R046 已由迭代 commit `b964b8a` / `cc4e87f`；勿再改同批 relay/site；iteration-status 由迭代统一写。
- 已告知**鹊桥-客户端**：PV-002/003 本轮由迭代实现中（ModeSelectForm、Helper MainForm、installer 图标）；请本轮勿并行写这些路径。
- 已告知**日常**：可转告用户已开工；例程已启用。
- PV-002/003 实现仍进行中。


### 2026-11-06 13:44 Asia/Shanghai · 鹊桥-迭代 PV-003 安装包图标

- **Bot**：鹊桥-迭代（本机仓；未用 CloudAgent）。
- **改动**：新增品牌 `installer/assets/app.ico`（多尺寸）+ `src-net48/RemoteAssist.App/assets/app.ico`；App `ApplicationIcon`；`RemoteAssist-Net48.iss` 增加 `SetupIconFile` 与 [Icons] `IconFilename`；`scripts/generate-app-icon.ps1` 可重生。
- **未改**：Helper.UI / AppIcons.cs（避 PV-002 冲突）；未 push；未替换生产下载包。
- **验证**：Inno 编译成功；Staging publish（因 `artifacts\app-net48-fd` 被运行中鹊桥进程锁定，改用 `artifacts\app-net48-fd-pv003`）；Setup/EXE `ExtractAssociatedIcon` 均得 32x32 品牌图（非默认灰标）。
- **产物**：`E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Net48-Setup-0.1.0.exe`（bytes=2395215，SHA256=4F9A09FD663E652D9C178EAB0462351028117E38802545CA63AC83BEB3AD8280）。
- **阻塞**：默认 `build-installer-net48.ps1` publish 到 `app-net48-fd` 时若鹊桥在跑会文件锁失败；本次用独立 staging 闭环。真机安装后开始菜单/桌面快捷方式建议人工点验。
- **PV-003**：→ **待验收**。
- **聊天**：不对用户发消息（本条仅写文件）。


- **commit**：`63cf098`（本地；未 push）。


### 2026-11-06 13:45 Asia/Shanghai · 服务端只读核验回传 + 文档欠账

- **鹊桥-服务端**只读：HEAD `11d7e43` 时核验（其后已有 `63cf098`/`2f07af6`）；`GOPROXY=off` 下 `go test` relay+site PASS；未写文件/未 commit/push。
- **文档欠账**：`docs/agents/服务端.md`（或交接文 §4）仍写「R046 尚未实现」，与 `round-046` implemented-local 及 `b964b8a` 不符 → 下轮由迭代统一改文档，不改用户需求原文。
- **PV-003**：已待验收（`63cf098`）；产物 `artifacts\installer\RemoteAssist-Net48-Setup-0.1.0.exe`。
- **PV-002**：仍进行中（工作区已有 ModeSelectForm/MainForm 改动与 docs/shots）。

### 2026-11-06 13:45 Asia/Shanghai · PV-002 客户端首页/协助端 UI 观感

- **状态**：实现完成 → **待验收**（未改用户说明原文；PRODUCT-VERSIONS 仅改状态标记）。
- **改动路径**：
  - `src-net48/RemoteAssist.App/ModeSelectForm.cs` — ClientSize 560×392；双角色卡加高 120px、间距收紧、hover 描边、「点击进入」+ ›；底栏贴底，去掉大片空灰。
  - `src-net48/RemoteAssist.Helper.UI/MainForm.cs` — 窗体 840×820；连接区凭证框加高加宽；主操作按钮单行（剪贴板/安装说明/连接/断开）；排障五钮单行加宽加高；路径/命令参数框 776×36；MakeButton 高 34。
  - `docs/shots/pv002-mode-select-*.png`、`docs/shots/pv002-helper-*.png` — 本机启动截屏（全屏，供对照）。
- **验证**：
  | 项 | 结果 |
  |----|------|
  | `dotnet build` RemoteAssist.App (net48 Release) | PASS（既有 ShellMainForm CS0414 警告） |
  | `dotnet build` RemoteAssist.Helper.UI | PASS |
  | UI 启动截图（首页 / `--control` 协助端） | 已跑；见 docs/shots |
  | 人工像素级验收 | **待用户**（编译≠验收） |
- **业务逻辑**：未改连接/排障/中继行为；仅布局与字号。
- **commit**：`ab27691`（本地；未 push；docs 指针 ad8879c）。
- **红线**：未 push、未上公网、未动 relay/site/AI-SSH-Pro。
- **并行**：PV-003 已由并行提交标待验收（本项未改其实现）。

### 2026-11-06 13:49 Asia/Shanghai · 鹊桥-客户端只读复审（PV-002/003）

- **结论（客户端）**：
  - **PV-003**：ico 6 层 16–256、iss/csproj 接线、Setup/staging 壳图标及哈希均核实；**仍待真实安装验收**（未启安装）。
  - **PV-002**：布局静态未见阻断缺陷；**仍待真实 UI 验收**（未启 GUI）。截图因 CopyToBox 根限制未目视 → **不记观感 PASS**。
- **产物追溯风险（必须守住）**：现有 `RemoteAssist-Net48-Setup-0.1.0.exe`（约 13:43:27）与 staging Helper.UI.dll（约 13:42:36）**早于** `ab27691`（约 13:46:28）→ **不得把现包等同 PV-002 最终验收包**。现包仅可作为 **PV-003 图标** 侧证据。
- **后续联合验收包（若做）**：从已提交 HEAD 隔离 publish；记录源码 hash、命令、载荷 hash。
- **其他待验点**：脚注高 DPI 裁切；多行粘贴路径/凭证；快捷方式及卸载图标。
- **迭代动作**：已记入本文件；不因本次复审把 PV-002/003 标完成；未 push。


### 2026-11-06 14:17 Asia/Shanghai · 产品协调接续（观感不通过 / 语言核查 / 分工）

- **触发**：用户经产品要求持续完善；产品转达本轮协调口径。
- **例程**：automation-1793943554537「鹊桥迭代检查」@every 5m 已启用（无待办/无变化安静）。
- **分工（本轮确认）**：
  1. **体验**：出 PV-002 修订方案（右侧/下半屏空白、脚注裁切）；**确认前迭代/客户端不改布局**；不扩大 R049。
  2. **产品**：落定 PV-004 docs（拟改待开发 + 仅本地实现/构建测试授权）；**迭代不抢写产品文档**。
  3. **客户端**：拟认领 **唯一 installer 写入者**（已发协调；等回执）；PV-003 联合包须含最终 UI 提交 + hash。
  4. **迭代**：统一写 `iteration-status`；协调与语言**核查/方案准备**；未实测不 PASS；不 push / 不上公网；不改 R049 未授权、R050 blocked。
- **PV-002**：用户真实截图观感 **不通过** → 维持待验收；等体验方案；**不改代码**。
- **PV-004**：PRODUCT-VERSIONS 现仍为 **草稿**（产品侧文档改动可能在工作区未落定）；**未放行实现**。
- **语言核查（本机事实，非向导实测）**：
  - 系统 WinSystemLocale / Culture / UICulture / InstalledUICulture：**zh-CN**。
  - iss：`ShowLanguageDialog=no`；`[Languages]` 仅 `english` + Default.isl；Tasks 中文硬编码；`InfoBeforeFile=net48-runtime-note.txt`。
  - **未**启动 Setup 向导核验实际显示语言 → **不记 PASS**。
- **准备方案（待产品指针 + 客户端执笔，先不改 iss）**：zh-CN 机上优先纳入 Inno `ChineseSimplified.isl`（或等价），自定义字符串进 `[CustomMessages]`/按语言 Messages；是否跟随系统自动选语言 vs 对话框，等产品验收口径；真机装向导点验后才可验收。
- **未做**：改布局、改 installer、commit 产品脏文档、push、上公网。


### 2026-11-06 14:17 Asia/Shanghai · 客户端确认：唯一 installer 写入者

- **鹊桥-客户端**回执：接受 `installer/` 与出包脚本由客户端集中提交；现不抢写 iss。
- **PV-002**：客户端保持只读，等体验修订方案 + 用户确认；不扩 R049。
- **PV-004**：方案边界确认——仅安装向导随系统语言选择/回退，不擅自扩大客户端国际化；产品文档指针落定后核授权与 Inno 语言配置再实现。
- **联合包**：等最终 UI 提交稳定后，从隔离目录生成并记源码/载荷 hash。
- **iteration-status**：仍由迭代统一写。未改代码、未 push。


### 2026-11-06 14:18 Asia/Shanghai · 测试环境约束 + PV-004 准则锁定 + 语言资源分析

- **测试环境（产品转达）**：协助端测试优先 bot 电脑；用户允许 Linux 也可测 → 架构改派 Linux 客户端测试。**Linux 结果不可替代 net48 验收**。**禁止**借语言验收在用户机启动 Setup。
- **主界面**：用户观感仍未过 → PV-002 继续冻结布局，等体验方案。
- **PV-004 准则（产品已定，不再问用户）**：
  - 跟随系统**显示语言**，在**已支持语言**中自动选择；
  - **不新增**语言选择对话框；
  - 未支持语言或资源缺失 → **列风险回产品**；不暗造回退、不泛称全语种。
- **资源分析（未启 Setup / 未改 iss）**：
  - 本机 UI 语言 zh-CN。
  - 向导框架文案：仅挂 `english` + Default.isl → 系统按钮等预计为英文（未实测）。
  - 自定义表面：AppName/AppComments/Tasks/Icons/Run Description、`net48-runtime-note.txt` 均为**中文硬编码**（与 Languages 脱钩）。
  - 本机 Inno 6 Languages 共 28 个 isl，**无** ChineseSimplified / ChineseTraditional → 要实现 zh-CN 自动选中，须**另取官方简体译文**并纳入仓库或构建依赖；此为**风险**，回产品，不自行发明回退策略。
  - 支持集建议（待产品确认清单）：至少 `chinesesimplified` + 保留 `english`；自动匹配失败时如何表现 → **只列选项/风险，不擅自定回退**。
- **未做**：改代码、改 iss、用户机 Setup、push、动 R049/R050。


### 2026-11-06 14:18 Asia/Shanghai · PV-004 范围产品拍板（等 docs 指针再实现）

- **支持集**：本轮 **简体中文 + 英文**；按系统显示语言在已支持集内自动匹配；**不弹**选语言框。
- **未支持语种**：保留 **English 默认作兼容保底**（产品兼容范围决定，不再问用户回退）。
- **宣称边界**：不宣称全语种跟随。
- **中文资源**：只从 Inno 官方可信译文源取，注明出处/版本/许可；不可用则报阻塞。
  - 官方目录页：https://jrsoftware.org/files/istrans/（Chinese Simplified · Maintainer Zhenghan Yang · 标注 6.5.0+ Official）
  - 本机 Inno（此前 ISCC 6.6.1）`Languages` **仍无** `ChineseSimplified.isl` → 实现前须从上述官方源取入仓库或补齐本机，并记许可/版本；取不到则 **阻塞**。
- **同语种**：CustomMessages + 安装前说明须与向导同语种；禁止框架英 + 自定义中混搭。
- **红线**：不装用户机；未实测不得验收；等产品 docs 指针后再让客户端改 iss。已转发客户端。


### 2026-11-06 14:19 Asia/Shanghai · 流程分界更新（产品主动出需求 ≠ 开发自扩）

- **用户口径（经产品 14:19）**：产品应**自主**从用户反馈/场景/体验/测试/目标查重提需求、排优先级、落 docs 与明确授权；**不局限**于只从 PRODUCT-VERSIONS「待开发」+ roadmap 取条；队列空**不是**产品停工理由。
- **迭代 / 开发分界（不变且必须守住）**：
  - 仍**只执行** PRODUCT-VERSIONS 中 **「待开发」且已放行** 项；
  - 草稿 / 暂停 / blocked：**未经**对应确认、评估与解锁 → **不交实现**；
  - **不**放开 R049 / R050；**不**由开发自行扩需求或发明队列外功能。
- **传递纠正**：停止沿用「产品也只能从队列取条」的旧说法；产品主动出需求，落地为队列条目并放行后，迭代才开工。
- 本侧未改业务代码；例程仍按「无已放行待开发则安静」。


### 2026-11-06 14:20 Asia/Shanghai · 架构落实迭代规范（dev-iteration.md）

- **鹊桥-架构**：与产品共定规范，写入 `docs/dev-iteration.md`（进行中；此刻文件尚未落盘）。用户要求勿再问用户。
- **对迭代约束（已对齐，文件落盘后以 docs 正文为准）**：
  - 小步迭代遵守该文；
  - 选题：产品可自行出新需求并登记 PV/roadmap；迭代仍只推进已进入**可开发**状态的条目；
  - 草稿/暂停/blocked 不动；实现须有产品授权；
  - 写文件 ≠ push/上机；R050 仍 blocked。
- 本侧已在 `docs/agents/迭代.md` §7 与例程 prompt 固化同分界；待 `dev-iteration.md` 落盘后以该文为出入口径权威，有差异则改职责/例程对齐正文。


### 2026-11-06 14:21 Asia/Shanghai · 例程检查：无待开发；dev-iteration.md 已落盘

- **队列**：无「待开发」。PV-001/002/003 均为 **待验收**；PV-004 仍为 **草稿**（未放行实现）。
- **PV-002**：观感未过 → 布局冻结，等体验修订方案 + 用户确认；本侧不改代码。
- **PV-003**：本地图标闭环已待验收；不上生产换包。
- **规范**：`docs/dev-iteration.md` **已落盘** → 小步迭代以该文为出入口径权威；与 `docs/agents/迭代.md` §7/§8 一致。
- **脏树**：工作区另有产品/体验/质量文档未提交改动（含 PV-004 记账、ux 确认台账等）→ **本侧不抢提交**，留给产品/架构/体验各自收口。
- **未做**：业务代码、installer、push、上公网、R049/R050。
- **通知**：无阻塞需用户决策 → **不向聊天回执**。


### 2026-11-06 14:21 Asia/Shanghai · 客户端 PV-004 只读分析回传

- **范围**：零改动 / 零下载 / 零运行 Setup。
- **事实**：
  - 本机 Inno **6.6.1** 确认缺简中 isl；
  - `RemoteAssist-Net48.iss` 与 `RemoteAssist.iss` 目前仅 English 框架；Tasks/Icons/Run 及 Net48 `InfoBefore` 多处中文硬编码 → 需完整双语化；
  - 本地帮助确认 `LanguageDetectionMethod=uilanguage` 对应系统显示语言；English 列为第一条即可引擎保底；`ShowLanguageDialog=no` 不弹框。
- **前置**：官方译文源指向 https://jrsoftware.org/files/istrans/ ；**尚未**下载或核实该译文许可 → 取入前置。
- **验收**：Windows 向导语言/混搭验收 **不能**用 Linux 替代；不在用户机启动 Setup。
- **待产品正式指针**：PRODUCT-VERSIONS 现仍草稿且与 status 拟定范围不完全一致；指针落定前不实现。
- **待产品明确**：正式指针是否**同时覆盖备用 net8** `RemoteAssist.iss`（客户端提问；迭代不擅自定）。


### 2026-11-06 14:22 Asia/Shanghai · 二次对齐 dev-iteration.md + PV-004 仅 Net48

- **规范**：`docs/dev-iteration.md` 已落盘（`cb6b71c`）。职责/例程已二次对齐；推进以该文为准。
- **PV-004 产品拍板**：本轮**只修** Windows 主路径 `installer/RemoteAssist-Net48.iss`（含必需共享语言资源 / 同语种安装前说明）。备用 net8 `RemoteAssist.iss` **本轮不改**，登记后续对齐项；不阻挡 net48 交付；**不得**把 net48 结果泛称所有安装器满足。正式 docs 指针产品收口后即给；此前不新增前置问题。
- 已通知客户端按 Net48-only 准备；仍等正式指针后再改 iss。未 push。


### 2026-11-06 14:24 Asia/Shanghai · PV-004 正式授权开工（客户端执笔）

- **依据**：产品正式 docs 指针；`PRODUCT-VERSIONS` PV-004 状态 **待开发** + 本地实现/构建测试放行。
- **范围**：仅 `installer/RemoteAssist-Net48.iss` + 必要语言资源 + 同语种安装前说明；**不改** `RemoteAssist.iss`；不动 R049/R050。
- **红线**：不上生产、不装用户机、不 push；验收须真实安装向导截图 + 系统语言核验；与 PV-003 联合包须含**最新 UI 提交**与产物 hash（现 13:43 Setup 仍不可冒充含最终 UI）。
- **并行**：主界面紧凑改版（PV-002）**未授权**——等体验方案 + 用户确认。
- **分工**：客户端 = 唯一 installer 写入者执笔；迭代写本 status、协调验收口径；未实测不 PASS。
- **产品未提交文档**：PRODUCT-VERSIONS / ux-user-confirmations / agents/产品.md 等工作区改动仍属产品侧，本轮迭代**不代 commit** 那些文件（避免抢写）。


### 2026-11-06 14:27 Asia/Shanghai · 首页紧凑方案：用户认可 + 产品本地实现授权

- **依据**：用户认可体验方案范围；产品明确本地实现授权（确认之外的授权）。
- **对照**：`docs/ux-home-compact-20261106/proposal.md`（及 preview 图；自体验 box 同步）。要点：默认客户区 **640×360**（窄 520×340 / 宽 780×440）；两卡等分中间高度；卡底→脚注约 20；短文案；首页去 CLI；建议可缩放；**共享壳不得全局永久缩死压他页**。
- **实现范围**：仅 net48 首页 `ModeSelect` / 壳首页客户区分设相关；**不扩 R049**；不改安装器 / R050。
- **验证**：本地构建 + 真实 UI 点验；未实测不 PASS；不 push / 不上线 / 不装用户机。
- **并行**：PV-004 安装语言继续由客户端执笔（installer）；本项客户端另改 ModeSelect/Shell 首页——文件隔离可并行。
- **分工**：客户端执笔；体验方案作验收对照；迭代写本 status。


### 2026-11-06 14:27 Asia/Shanghai · 澄清：首页紧凑已授权（proposal 头已改）

- 客户端读到旧提案头「待用户确认/≠实现授权」而继续只读 → **已澄清**：用户已认可范围 + 产品明确本地实现授权（产品会话转达）。
- 已更新 `docs/ux-home-compact-20261106/proposal.md` 状态头，避免再误挡实现。
- 请客户端立即执笔 ModeSelect/壳首页；PV-004 并行继续。


### 2026-11-06 14:28 Asia/Shanghai · 落盘 LOCAL-IMPL-AUTH.md（解客户端只读）

- 客户端仍等「仓内授权原文」：已写 `docs/ux-home-compact-20261106/LOCAL-IMPL-AUTH.md`。
- 指针：该文件 + proposal 状态头 + 本 status 14:27 节；PRODUCT-VERSIONS 旧句待产品收口，不挡开工。
- 已再请客户端立即改 ModeSelect/Shell。


### 2026-11-06 14:29 Asia/Shanghai · 产品评测回执落地

- **采纳**：①放行消息可覆盖草案头；仓内 `docs/ux-home-compact-20261106/`（含 LOCAL-IMPL-AUTH.md）为正式指针，客户端按此执笔。②验收证据门槛写入放行条。③并行边界：iss 仅客户端。
- **PV-001**：状态「待验收」= **本地浏览器点验**即可标完成；公网上线另授。测试勿把「未部署到公网」当功能 FAIL。
- **继续协调**：首页紧凑（ModeSelect）+ PV-004（Net48 安装语言）并行。


### 2026-11-06 14:29 Asia/Shanghai · PRODUCT-VERSIONS PV-002 授权句已改

- 按产品要求：将「须用户确认后再交实现」改为「用户已确认 + 产品已授权本地紧凑改版（LOCAL-IMPL-AUTH.md）」。
- 同提交纳入工作区已有的 PV-004 待开发条文（产品先前未 commit 的落盘）。


### 2026-11-06 14:30 Asia/Shanghai · PV-001 点验环境缺口（服务端问询）

- **事实**：迭代可读写用户机仓 `E:\IdeaProjects\remote-assist`（本机）；**无**已授权、正在跑的 bot 侧 site+/admin 点验实例。
- box 上仅有历史拷贝 `/workspace/site-build`、`/workspace/relay-build`（约 11-05），**非**运行中实例，**未**获用户授权用作 QA 环境；不据此起服务。
- 用户机当前未见常见 site/relay 监听端口占用。
- **产品采纳「本地点验」≠** 迁移源码到 bot / 起服务 / 扩环境授权。
- **待产品问用户选环境**：（A）用户机已有本地 site 起法上由测试点验；（B）另授 bot 箱构建+起 site 供测试；（C）其他。未选前：服务端保持只复核既有起法；测试勿把未公网部署当 FAIL。


### 2026-11-06 14:30 Asia/Shanghai · PV-001 环境三选一已弹给用户

- 产品已弹出 A/B/C；**未选定前不扩环境**；选定后产品回迭代。
- 测试勿把未公网当 FAIL。


### 2026-11-06 14:31 Asia/Shanghai · 例程检查：开 PV-002 首页紧凑；PV-004 不抢写

- **触发**：例程 automation-1793943554537 @every 5m（本轮 14:26 UTC+8 附近）。
- **队列**：
  - **PV-004**（待开发+本地放行）：工作区已有客户端执笔改动（RemoteAssist-Net48.iss 双语/uilanguage、languages/ChineseSimplified.isl SHA256 与 README 一致、net48-runtime-note.en/zh-CN.txt）。迭代不抢写/不代 commit installer；未启 Setup → 不记语言 PASS。旧 net48-runtime-note.txt 仍在盘上（iss 已改按语言覆盖 InfoBefore）。
  - **PV-002 首页紧凑**：方案 docs/ux-home-compact-20261106/proposal.md 用户认可 + 产品本地实现授权已落盘；ModeSelect/Shell 尚未按方案改码 → 本轮由迭代落地（与 installer 文件隔离，可并行）。协助端先前布局仍待验收、本轮不扩。
  - **PV-001/003**：仍待验收；公网/换包未授权。
- **红线**：不 push、不上公网、不装用户机、不扩 R049/R050；编译≠运行 PASS。
- **进行中**：首页紧凑改 ModeSelect/Shell + 本地 build/commit（细节随落地追加）。


### 2026-11-06 14:31 Asia/Shanghai · 纠正：ModeSelect 仍由客户端执笔；PV-004 已进 bb2b596

- 例程误写「迭代落地 ModeSelect」→ **作废**。工作区已有客户端改动的 ModeSelectForm/ShellMainForm；**迭代不抢写**。
- 先前 index 中已暂存的 PV-004 iss/语言资源/向导截图已随提交入库（现 HEAD 说明已改正）。请客户端核验该提交是否完整；真 Setup 点验前不记语言 PASS。
- PV-001 环境 A/B/C 仍等用户选定。

