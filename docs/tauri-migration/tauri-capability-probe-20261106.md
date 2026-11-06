# Tauri 远程协助能力探针（架构）

- 日期：2026-11-06（Asia/Shanghai）
- 范围：**能力可行性研究**；不迁栈、不改 remote-assist 主仓、不编码实现。
- 对象：鹊桥类 Windows-first 远程协助客户端（截屏、键鼠注入、系统命令、剪贴板、常驻 Agent）。
- 现状对照：主路径 net48 + Go 中继；pair 凭证含 `fp=`；R050 PAKE 仍 blocked。

> **探针 ≠ 迁栈授权。** 本文只回答「Tauri 能不能触达这些系统能力」；即使结论为「技术上可达」，也不构成开始切换技术栈的许可。整栈迁移须产品另立项、范围/工期/凭证与中继兼容性单独收口。

---

## 结论摘要

| 能力 | 结论 | 路径 |
| --- | --- | --- |
| 执行系统级命令 | **能** | 官方 `shell` 插件（capabilities 白名单）或 Rust `Command` / sidecar |
| 截取当前桌面/显示器 | **能（非 WebView 内置）** | Rust 插件 / 原生 API（DXGI 等）或社区截图插件；非前端 DOM 截图 |
| 键鼠注入 | **能（非官方内置）** | 自研 Rust 插件或社区 user-input 类插件（Win `SendInput` 等） |
| 剪贴板读写 | **能** | 官方 `clipboard-manager`（须显式 permission） |
| 跨 Win/Mac/Linux 安装包 | **壳层能** | Tauri 官方三端；**远程协助能力仍按平台各写一套原生** |
| 与现网 nest48 对等的被控/协助主路径 | **短期不适合整栈替换** | 沉没成本 + UAC/安全桌面/服务生命周期/签名/中继与 pair 协议重做 |

一句话：**用户担心的「系统命令 / 截屏」Tauri 做得到；真正难的是远程协助对等（注入质量、UAC、无头常驻、协议与凭证迁徙），不是「Tauri 编不过」。**

---

## 能做什么

### 1. 系统命令 / 子进程

- Tauri v2 官方 **Shell 插件**可 `execute` / `spawn` 子进程；默认危险，须在 `src-tauri/capabilities` 里用 **capabilities + 命令作用域白名单**显式放开。
- 也可在 Rust 侧直接跑进程，或把现有 Agent 打成 **sidecar（`externalBin`）** 由壳启动/监督。
- 文档：
  - https://v2.tauri.app/plugin/shell/
  - https://v2.tauri.app/develop/sidecar/
  - https://v2.tauri.app/security/capabilities/

### 2. 桌面/显示器截图（非 WebView）

- **WebView / 前端无法**稳定拿到「整桌 + 其它应用窗口」像素；须走 **Rust 原生捕获**。
- 可行路径：
  - 社区插件如 `tauri-plugin-screenshots`（窗口/显示器截图 API）：https://github.com/ayangweb/tauri-plugin-screenshots/
  - 自研 crate：Windows DXGI Desktop Duplication、macOS ScreenCaptureKit、Linux PipeWire/X11 等（开源远程桌面项目常见拆法：Tauri 只做壳，capture 在独立 crate）。
- 静态「存一张图」≠ 远程协助所需的 **连续低延迟帧流 + 编码**；后者仍要硬件编码与传输栈。

### 3. 键鼠注入

- **无官方「远程桌面注入」一等插件**；通过 Rust 调平台 API 即可：
  - Windows：`SendInput` 等
  - macOS：`CGEventPost`（需辅助功能权限）
  - Linux：`uinput` / X11（Wayland 受限）
- 社区例：`tauri-plugin-user-input`（模拟键鼠）：https://github.com/kunkunsh/tauri-plugin-user-input/
- 成熟远程桌面开源多采用 **「Tauri = UI/托盘；host 引擎 = 独立原生进程」**（例如 Tether 的 shell 监督 host/client）：https://github.com/nchapman/tether

### 4. 剪贴板

- 官方 `clipboard-manager`：读写文本/图片等；**默认全关**，须 `allow-read-*` / `allow-write-*`。
- 文档：https://v2.tauri.app/plugin/clipboard/
- 注意：鹊桥「完整凭证粘贴 / 防截断」是产品与协议约束，与「能不能写剪贴板」是两层问题。

### 5. 开机自启 / 托盘壳

- `autostart` 插件可注册登录启动：https://v2.tauri.app/plugin/autostart/
- 托盘 + 监督 sidecar 适合「有用户会话的助手」；**不等于 Windows Service 级无交互 Agent**。

### 6. 安全模型（与远程协助相关的点）

- Capabilities 约束的是 **前端 WebView → 后端命令** 的暴露面；**不约束**你在 Rust 里写的任意原生代码。
- 官方明确：capabilities **防不住**恶意/疏忽的 Rust、过宽 scope、故意 bypass。
- 文档：https://v2.tauri.app/security/capabilities/

---

## 不能 / 很难（对标远程协助）

| 点 | 说明 |
| --- | --- |
| WebView 直接做被控采集/注入 | **不能**当主路径；像素与输入必须在原生侧 |
| Shell 任意命令无限制 | 官方设计是 **白名单**；远程协助若需要「任意命令」应放在受控 Agent，而非把 `shell:allow-execute` 开成无界 |
| macOS 无授权截全桌 | 需 **Screen Recording**；注入需 **Accessibility**（可用权限检查类插件辅助，但用户仍须进系统设置授权） |
| Linux Wayland | 截屏走 portal、注入走 uinput；无头/无 seat 场景更难 |
| Windows UAC / 安全桌面 | 提权、Consent UI、锁屏/UAC 桌面捕获与注入是经典远程协助硬坑；**与是否 Tauri 无关**，Rust 也要单独设计（manifest、辅助进程、uiAccess/签名等） |
| 把整个 Tauri GUI 当 Windows Service | 社区实践倾向 **独立 service exe + IPC**，GUI 不当 LocalSystem 交互式壳；Tauri 无「一键变服务」官方方案 |
| Sidecar 生命周期 | 子进程通常随应用/Job Object 管理；**注销后常驻、崩溃拉起、升级替换**要自建监督，不能假设默认等于现网 Agent |
| 代码签名 / SmartScreen / Gatekeeper | 换栈后安装信任链路重做；未签名会显著抬高「下载即用」摩擦 |
| 与现网协议对等 | pair/`fp=`、中继、单会话互斥、R050 等 **不因换 UI 壳自动继承**；须重做或长期双栈 |

---

## 与鹊桥主路径对比

| 维度 | 当前 net48 主路径 | Tauri 路径（若迁） |
| --- | --- | --- |
| UI | WinForms；AI 画控件易翻车，但已有细则/真 UI 验收 | Web 前端；AI 更熟，**不自动解决**系统能力 |
| 截屏/注入 | 已有（或可继续加深）Win32 路径 | 须 Rust 重写同级能力 |
| 中继 / pair / `fp=` | 已落地 | 须对接或重写；R050 仍 blocked，迁栈不解除 |
| 安装包体积 | 相对大 | Tauri 通常更小（用户动机之一） |
| 三端通用 | Windows 主；Avalonia/Linux 辅 | 壳三端；**协助能力仍分平台** |
| 风险 | 界面迭代慢 | **推倒宿主 + 安全边界 + 协议兼容**；工期与回归远大于「做个小安装包」 |

架构立场（与此前对用户答复一致）：**主路径继续 net48**；Tauri 仅适合「能力探针 / 未来轻量官网下载器或另立项跨端」评估，**不绑入口 A、不绑 R050**。

---

## 若仅做原型：建议验收项

目标：在 **隔离沙箱仓库**（非 remote-assist 主仓）用 Tauri v2 做 Windows-first 探针。通过 ≠ 迁栈。

1. **Shell**：capabilities 白名单下执行固定命令；拒绝未声明命令。
2. **截屏**：多显示器各抓一帧；有前台窗/全屏游戏时抽检（允许质量差，但须有帧）。
3. **连续采集**：≥15–30s 桌面帧流（可先软编），记录 CPU/延迟；证明不是「只存 PNG」。
4. **注入**：绝对/相对鼠标、点击、常用快捷键；焦点在其它进程时仍生效。
5. **剪贴板**：读写文本；长凭证字符串不截断（对齐产品口径）。
6. **权限失败可观测**：故意缺权限时有明确错误，而非空帧/静默失败。
7. **进程模型**：sidecar 或独立 host 在 UI 退出后的行为写清（随退 / 常驻二选一，并测崩溃拉起）。
8. **不做**：不接现网中继、不迁 pair、不改主仓、不宣称三端协助对等。

可选加分（仍属探针）：锁屏或 UAC 提示出现时的行为记录（预期：**多数方案此处失败或降级**——记为已知硬阻塞，而非 PASS）。

---

## 明确边界

1. **探针 ≠ 迁栈授权。** 用户口头「验证完就切换」不能替代产品立项与架构放行。
2. **本文件只写在 `/workspace`**；未修改任何远程协助业务仓库。
3. **「能」指平台可达**，不是「两周替代 nest48 主路径」。
4. 后续若产品要立项：先过上述原型验收 + 中继/凭证兼容方案，再谈双栈或切换；入口 A / 官网 / R050 并行节奏不变。

---

## 主要参考链接（已核对）

- Shell：https://v2.tauri.app/plugin/shell/
- Sidecar：https://v2.tauri.app/develop/sidecar/
- Capabilities：https://v2.tauri.app/security/capabilities/
- Clipboard：https://v2.tauri.app/plugin/clipboard/
- Autostart：https://v2.tauri.app/plugin/autostart/
- 插件目录：https://v2.tauri.app/plugin/
- 截图插件：https://github.com/ayangweb/tauri-plugin-screenshots/
- macOS 权限插件：https://github.com/ayangweb/tauri-plugin-macos-permissions/
- 键鼠插件例：https://github.com/kunkunsh/tauri-plugin-user-input/
- Tauri 作壳 + 原生 host 例：https://github.com/nchapman/tether
- UAC 清单实践（平台层，非 Tauri 特有）：https://learn.microsoft.com/en-us/windows/win32/secbp/running-with-administrator-privileges
