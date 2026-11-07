# 验收取证 · 2026-11-07 Asia/Shanghai

> 产品收口：不放行剪贴板成功写入 /「重试开启」编码；本轮仅取证。权威：主仓 `docs/round-product-auth-20261107-clipboard-session.md`（82bb337）。
> 未 push；未改业务码；未改 relay/site。中继仅 `127.0.0.1:17991`，测完已停。

## 证据清单

| 条目 | 测的栈 | 结果 | 截图/文件 | 备注 |
|------|--------|------|-----------|------|
| PV-002 首页观感 | net48（`artifacts/app-net48-fd-pv004`） | **缺口** | `acceptance/pv002-net48-home-20261107.png` | 真 UI：双角色卡；下方仍有留白。产品/用户曾判「不可视为验收完成」仍有效。 |
| PV-002 协助端按钮/输入 | Tauri（release exe） | **部分** | `acceptance/pv002-tauri-idle-20261107.png`、`desktop-idle.png`、`desktop-waiting-after-open.png` | 一屏双区；「连接」单行；凭证输入框高度够用。约束原文写 src-net48；Tauri 为迁移主路径旁证，不替代 net48 协助端真点验。 |
| PV-002 既有 docs/shots | — | **无效/缺口** | `docs/shots/pv002-mode-select-134431.png`、`pv002-helper-134443.png` | **误拍**：1920×1080 全屏 IDE/聊天，不是客户端 UI。勿作验收。 |
| PV-002 紧凑预览 | 设计示意 | 非运行证据 | `docs/ux-home-compact-20261106/main-preview.png`、`docs/shots/pv002-home-compact-143130.png` | 示意/旧图可参考；≠ 本轮 PASS。 |
| PV-003 Setup 文件图标 | net48 Setup | **PASS（包图标）** | `acceptance/pv003-setup-icon-artifacts.png`、`pv003-setup-icon-downloads.png` | `RemoteAssist-Net48-Setup-0.1.0.exe` 关联图标为品牌蓝桥标（非空白灰标）。artifacts SHA256 前缀 `71CE3908300A8046`；Downloads 另有一份 hash 不同。 |
| PV-003 快捷方式图标 | 已装 net48 | **缺口** | `acceptance/pv003-installed-exe-icon.png` | 本机 `Local\Programs\RemoteAssist\RemoteAssist.Net48.exe` 有品牌图标；**未找到**开始菜单/桌面 `.lnk`（可能未勾选任务或已清）。未上公网换包。 |
| PV-003 wt 内可验包 | Tauri wt | **未找到 Setup 产物** | — | wt 无 `artifacts/installer`。本地出包：`.\scripts\build-installer-net48.ps1`（需 Inno ISCC；本机已有）。 |
| PV-004 安装界面语言 | net48 iss 静态 | **配置已备** | `installer/RemoteAssist-Net48.iss` | `ShowLanguageDialog=no`；`LanguageDetectionMethod=uilanguage`；`english`+`chinesesimplified`；CustomMessages 双语。本机 UICulture=**zh-CN**。 |
| PV-004 真实向导截图 | — | **缺口** | `docs/shots/pv004-wizard-zh-CN-uilang.png`、`pv004-wizard-en-LANG.png` | **误拍**：实为深色聊天界面，非 Inno 向导。本轮未再跑隔离安装向导。不得伪称全语言已实测 PASS。 |
| Tauri Idle→Waiting | Tauri + 本机 17991 | **PASS** | `desktop-waiting-after-open.png`、`desktop-while-waiting.png`（本轮 `round-stop` 刷新） | Waiting≠Connected；有「复制连接凭证」「停止接受」。 |
| Tauri 停止→Idle | Tauri | **PASS** | `desktop-after-stop.png` | 本轮 `test/round-stop.ps1` → `stop-ok`：`backToIdle=True noCopy=True noWait=True noSession=True`。 |
| Tauri 完整 pair + 结束 | Tauri | **PASS** | `desktop-both-live.png`、`desktop-both-open.png` | 本轮 `test/round-both.ps1` → `both-ok`：配对确认、拒绝/允许记录、结束回 Idle。凭证含 fp=（脚本 `capturedPayload=True`）。 |
| 会话拒绝连接 | Tauri | **PASS（既有）** | `desktop-before-reject.png`、`desktop-after-reject-helper.png`；`ROUND-REJECT-WHEEL.md` | 索引既有；本轮未重跑。 |
| 开启失败（无中继） | Tauri | **PASS（既有·现状）** | `desktop-open-fail.png`；`ROUND-RETRY-COPY.md` | 失败文案+「重试开启」按钮可见；**未实现**「中继起来再点重试进等待」（产品否决本阶段编码）。 |
| 复制失败不谎报 | Tauri | **PASS（失败路径）** | `desktop-after-copy-fail.png`、`clipboard-error5-repro.txt` | OpenClipboard 错误5；不得报复制成功已修好。 |

## 本轮命令摘要

- `apps/queqiao-tauri/test/round-stop.ps1` → EXIT 0 / stop-ok
- `apps/queqiao-tauri/test/round-both.ps1` → EXIT 0 / both-ok
- 中继：`go run .\cmd\relay` @ `RELAY_ADDR=127.0.0.1:17991`；测完已杀进程，端口无监听

## 明确未做

- 未改剪贴板成功路径 / 重试开启实现
- 未改 relay/site；未动 PRODUCT-VERSIONS 状态（仍待验收，留给产品）
- 未 push；未上公网换包