# 验收复验取证 · 2026-11-07 Asia/Shanghai

> 角色：鹊桥-测试（专职验证）  
> 时间：2026-11-07 08:55:08 Asia/Shanghai  
> 机器：DESKTOP-J9F0EKT  
> Tauri wt：`E:\IdeaProjects\remote-assist-wt-tauri` · `feat/tauri-client-migration`  
> 主仓产品收口：`docs/acceptance/pv-002-004-product-closeout-20261107.md`（e794827）、`pv-001-product-closeout-20261107.md`（da9f763）  
> **未改业务码；未 push；未上公网；中继仅 127.0.0.1:17991，测完已停。**  
> 不放行剪贴板成功写入 /「重试开启→等待」编码（产品收口维持）。

## Soft QA

| 命令 | 目录 | EXIT | 摘要 |
|------|------|------|------|
| `node --test test/copy.test.mjs`（package `check`） | `apps/queqiao-tauri` | **0** | 53 pass / 0 fail |
| `cargo test -- --nocapture` | `apps/queqiao-tauri/src-tauri` | **0** | 25 pass / 0 fail（lib）；main/doc 0 |
| 前端 typecheck | — | **N/A** | 无 `tsconfig.json` / 无独立 typecheck 脚本；约定 Soft QA = node test + cargo test |

> Soft QA PASS ≠ 运行 PASS ≠ 安全 PASS。下列真 UI 另取证。

## 复验矩阵

| 条目 | 结果 | 证据路径 | 阻塞/备注 |
|------|------|----------|-----------|
| Soft QA node+cargo | **PASS** | 本文件 Soft QA 节；本机执行记录 | — |
| PV-001 | **不重开** | 主仓 `pv-001-product-closeout-20261107.md` | 未发现证据造假 |
| PV-002 net48 首页双卡留白 | **仍缺口** | `acceptance/pv002-net48-home-20261107-084632.png`；`pv002-net48-uia-20261107-084632.txt` | 真 UI：双角色卡在上半，下方大块留白仍在。产品「不可视为验收完成」仍有效 → **不得伪 PASS** |
| PV-002 Tauri 协助旁证 | **部分（旁证）** | `pv002-tauri-idle-reverify-20261107-085304.png`；`desktop-while-waiting.png`；`desktop-after-stop.png` | 一屏双区 Idle/Waiting 有效；**不替代** net48 协助端真点验 |
| PV-002 旧 docs/shots | **无效** | 主仓 `docs/shots/pv002-mode-select-134431.png`、`pv002-helper-134443.png`（1920×1080） | IDE/聊天误拍，勿当 PASS |
| PV-003 Setup 包图标 | **PASS** | `pv003-setup-icon-artifacts-reverify-20261107-084557.png` | artifacts `RemoteAssist-Net48-Setup-0.1.0.exe` SHA256 前缀 **`71CE3908300A8046`**（全哈希 `71CE3908300A804697502D80361B4FF9476E35F0F0B04A2286E70FA58DF39239`）。Downloads 另份前缀 `460D8758F65064EC`（不同包，不作为本项权威） |
| PV-003 开始菜单 .lnk | **PASS**（本轮补证） | `pv003-startmenu-explorer-20261107-084557.png`；`pv003-startmenu-lnk-icon-20261107-084557.png`；`pv003-lnk-meta-20261107-084557.txt` | `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\鹊桥远程协助\鹊桥远程协助.lnk` 存在，品牌图标可见。先前「未找到」为检索遗漏/口径过时 |
| PV-003 桌面 .lnk | **PASS**（隔离重装补证） | `pv003-desktop-lnk-icon-20261107-084632.png`；`pv003-desktop-explorer-20261107-084632.png`；Inno log | 用 artifacts Setup `/VERYSILENT /TASKS=desktopicon` 重装后出现 `C:\Users\Public\Desktop\鹊桥远程协助.lnk`（品牌图标）。默认 Tasks 桌面项为 unchecked——未勾选则无桌面快捷方式属预期 |
| PV-004 iss 配置旁证 | **配置已备** | `installer/RemoteAssist-Net48.iss` | `ShowLanguageDialog=no`；`LanguageDetectionMethod=uilanguage`；english+chinesesimplified；CustomMessages 双语。**≠ 向导 PASS** |
| PV-004 向导语言实测 | **PASS（UIA）** | `pv004-wizard-uia-20261107-085400.txt`（及同刻 page 文本） | 隔离启动 artifacts Setup；窗标题 `安装 - 鹊桥远程协助 0.1.0 (net48)`；控件含「信息 / 下一步(N) / 取消」及中文 InfoBefore；本机 UICulture=**zh-CN** |
| PV-004 向导可视截图 | **缺口（截屏阻塞）** | 尝试文件 `pv004-wizard-zh-CN-reverify-20261107-085430.png` 等 **不可用** | PrintWindow 失败（黑图）；CopyFromScreen 捕获到 **锁屏 Spotlight 壁纸**而非向导像素。**禁止**把该图当向导 PASS。语言结论以 UIA 为准 |
| PV-004 旧 docs/shots 向导图 | **无效** | `docs/shots/pv004-wizard-zh-CN-uilang.png`、`pv004-wizard-en-LANG.png` | 误拍（聊天界面），保持无效 |
| Tauri Idle→Waiting | **PASS** | `desktop-while-waiting.png`（本轮刷新） | Waiting≠Connected；有「复制连接凭证」「停止接受」 |
| Tauri 停止→Idle | **PASS** | `desktop-after-stop.png`；`_session-reverify-20261107-085304.txt` | `STOP_RESULT=True`：`backToIdle / noCopy / noWait / noSession` |
| Tauri 完整 pair（拒绝+允许+结束） | **PASS** | `desktop-before-reject.png`；`desktop-after-reject-helper.png`；`desktop-both-live.png`；`desktop-both-open.png` | `BOTH_RESULT=True`：拒绝提示、允许后「正在协助对方 · 已连接」、结束回 Idle。凭证经 Edit 手填（绕过剪贴板）含 `relay=127.0.0.1:17991` + fp/code |
| 开启失败文案 +「重试开启」按钮 | **PASS（现状）** | `desktop-open-fail.png` | 文案「没能开启接受协助，请检查网络后重试。」+ 按钮「重试开启」可见 |
| 「重试开启」→等待 | **未实现（现状）** | 同上；`retryDoesNotEnterWaiting=True` | **未授权实现**；只记现状，不要求修码 |
| 复制失败不谎报 | **PASS（失败路径）** | `clipboard-error5-repro.txt`；会话中 UIA「没能复制，请再点一次。」 | `OpenClipboard` **err=5 ACCESS_DENIED**；inJob=True。**不得冒充复制成功已修好**；成功路径=环境阻塞，非本轮编码目标 |

## 本轮关键命令摘要

- Soft QA：`node --test test/copy.test.mjs`；`cargo test -- --nocapture`
- PV-003：artifacts Setup `/VERYSILENT /NORESTART /TASKS=desktopicon`（EXIT 0）
- PV-004：交互启动 Setup；UIA 读窗；CopyFromScreen/PrintWindow 截屏失败（见上）
- 会话：自定义对抗脚本（读 Edit 凭证、拒绝/允许/结束、无中继开启失败）；既有 `round-stop.ps1`/`round-both.ps1` 因剪贴板 err5 导致 `capturedPayload` 辅助断言失败，但核心状态机已由对抗脚本复验 PASS
- 中继：`go run .\cmd\relay` @ `RELAY_ADDR=127.0.0.1:17991`；**测完已停，端口无监听**

## 明确未做 / 禁区遵守

- 未改业务码；未改 relay/site；未 push；未上公网换包
- 未要求/未暗示实现剪贴板成功路径或「重试开启→等待」
- 未动 R050；PV-001 未重开
- 未把污染向导截图、旧 docs/shots 误拍当作 PASS

## 拦回建议（给客户端/协调）

1. **PV-002**：net48 首页留白仍在 → 维持产品「待验收」；**不**借验收开 net48 编码（产品未授）。
2. **PV-004 可视截图**：本机对 Inno Modern 向导 PrintWindow/CopyFromScreen 不可靠（锁屏污染）。若产品坚持「必须有可辨认向导像素图」才收口，需另定取证手段（人工跟屏 / 其它捕获 API）；**当前语言项已用 UIA PASS，配置旁证已备**。
3. **剪贴板 err5**：环境阻塞（进程 inJob）；失败文案已正确。**不要**派成功路径编码。
4. **重试开启→等待**：未实现；**不要**派编码。
5. **无必须因功能回归拦回客户端的新缺陷**：Tauri 会话主路径对抗复验 PASS。

## 给产品的收口建议（测试视角）

| 编号 | 建议状态 |
|------|----------|
| PV-001 | 保持本地收口完成（公网未授） |
| PV-002 | **维持待验收（留白缺口）** |
| PV-003 | **可升为完成**（包图标 + 开始菜单 .lnk + 勾选任务后桌面 .lnk 均有证据）；注明桌面快捷方式默认不勾选 |
| PV-004 | **语言实测 UIA PASS + 配置已备**；可视截图像素仍缺口（技术阻塞）。是否收口由产品定 |
| Tauri 会话主路径 | 功能旁证 **PASS**（本轮对抗复验） |
