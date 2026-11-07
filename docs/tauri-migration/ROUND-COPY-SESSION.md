# 复制凭证与开启等待

## 当前界面（实机）

等待页有「复制连接凭证」。本机 `OpenClipboard` 长期返回错误 5（空句柄/窗口句柄/ClipSVC 拉起后仍如此；`clip.exe` 也 Access Denied）。失败时应提示「没能复制，请再点一次。」，原文框获焦；不得出现「已复制，请完整发给对方。」。成功只在读回内容与凭证一致时出现。

首页「开启接受协助」在本机中继 `127.0.0.1:17991` 听着时应进入「等待对方连接」，并出现「复制连接凭证」。

## 本轮

1. 高频：写入路径关闭前读回校验；错误 5 时绝不报成功。最小复现：`acceptance/clipboard-error5-repro.txt` + `test/clip-probe.ps1` / `test/round-copy-session.ps1`。
2. 会话回归：真点击「开启接受协助」→「等待对方连接」。全屏与窄窗截图。

## 验收结果

- 开启等待：`waiting=True copyBtn=True noConnected=True`。图：`desktop-copy-session-before.png`、`desktop-waiting-after-open.png`、`desktop-waiting-narrow.png`。
- 复制：`copySuccessHint=False copyFailHint=True manualFocused=True`；连点与切设置后再点仍失败、不报成功。图：`desktop-after-copy-fail.png`（拍前清空原文）。复现日志：`clipboard-error5-repro.txt`（`OpenClipboard=0 GetLastError=5`）。
- `npm run check` 53 项；`cargo test --offline` 25 项。发布构建后点击。
- 不是两台电脑，不是生产中继。深色主题无，未验。

## 下一轮只做这两件

1. 系统允许剪贴板时打通复制成功写入（读回一致才提示成功）。
2. 同一窗口开启失败后，中继起来再点「重试开启」，应进入等待。
