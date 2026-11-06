# -*- coding: utf-8 -*-
from pathlib import Path

def write_utf8(path, text):
    if not text.endswith("\n"):
        text += "\n"
    Path(path).write_text(text, encoding="utf-8", newline="\n")

# 1. round-032.md
r32 = Path("docs/round-032.md").read_text(encoding="utf-8")
r32 = r32.replace("> 状态：**delivered**", "> 状态：**done（产品已验收）**", 1)
accept = """
## 产品验收（鹊桥-产品 · 2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r032-*`、`r032-balloon-copy-proof.txt` 与源码独立验收通过（`b78fc1e`）。

独立核对：

- `TrayStayCopy.BalloonDefault` / `BalloonWithReturnHome` 沿用原有两句，无第三种说法；`HiddenToTrayFeedback` =「已隐藏到托盘。双击托盘图标可重新打开。」
- Windows 接受协助端底栏默认空白：`docs/screenshots/r032-accept-statusbar-empty.png` 底栏无「就绪。关闭窗口后会继续在托盘运行。」；「复制码+密码」仍是唯一实心主按钮；R031 主状态「已连上网络，可以发给对方了」仍在。
- `docs/screenshots/r032-accept-hidden-to-tray-feedback.png`：底栏一次性「已隐藏到托盘。双击托盘图标可重新打开。」（ui-shot 注入同款文案，窗仍可见，规格允许）。
- 启动气泡未能真拍 NotifyIcon；以 `TrayStayCopy` 常量 + `AgentTrayContext` 接线 + `r032-balloon-copy-proof.txt` 证明未改气泡口径、未另造第三种说法。
- 未改 Avalonia 关窗提示、`InstallInstructions`、Skill/MCP、`docs/trust-levels.md`「才问你」。未覆盖 `r022-*`～`r031-*`。

缺口：未实拍启动气泡本体；关窗反馈图为 ui-shot 注入。公网安装包仍为 R030（交 R033）。

已开 R033（官网 Windows / Linux 安装包跟上含 R031–R032 的客户端；替换后首页旁注与导语不回退；不改中继/AI-SSH-Pro）。
"""
if not r32.endswith("\n"):
    r32 += "\n"
r32 = r32 + accept
write_utf8("docs/round-032.md", r32)
print("round-032 ok")

# 2. round-033.md
r33 = """# 第 33 轮 — 官网安装包跟上含 R031–R032 的客户端

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai
> 实现：鹊桥-客户端（出包）/ 鹊桥-服务端（替换上架）
> 状态：**ready**
> 依据：R032 产品已验收。R031 接受协助端「对方已连上」主状态与 R032 托盘说明去重已进仓库（HEAD 含 `b78fc1e`）；公网安装包仍是 R030（Windows 75,582,164 / Linux 8,001,712），从官网下载的人还拿不到这两轮改动。

## 本轮目标

1. **高频**：用包含 R031–R032 的客户端重新打 Windows / Linux 包，替换官网上的安装包。
2. **打磨**：替换后首页 Windows / Linux 旁注与导语不回退。

## A. 打出含 R031–R032 的新包（高频 · 鹊桥-客户端）

从包含 `b78fc1e`（R032）的 HEAD 打出：

- Windows：`RemoteAssist-Setup-0.1.0.exe`（`scripts/build-installer.ps1`）
- Linux：内部可保留 `queqiao-linux-x64-fd.tar.gz`，公网文件名必须是 `queqiao-linux-x64.tar.gz`（去掉 `-fd`）；`scripts/publish-agent-avalonia.ps1 -Runtimes linux-x64`

交付时写明：字节数、SHA256、本地路径、mtime（Asia/Shanghai）、出包所用 HEAD。新包字节或哈希应与当前公网 R030 包不同（Windows 现为 75,582,164；Linux 现为 8,001,712）。

不要改 `InstallInstructions`、不要改 Skill 文案常量、不要改 R031/R032 已验收文案、不要改 `docs/trust-levels.md` 的「才问你」。

## B. 替换公网上架（高频 · 鹊桥-服务端）

等客户端交付新包后，替换：

- http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe
- http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz

打磨：首页 Windows 旁注仍是「若浏览器提示通常不会下载，选择保留即可。」Linux 旁注仍指向微软 .NET 8 下载页并写「选运行时、不要选 SDK」。导语仍是「Windows 和 Linux 都可下载使用」；macOS 仍「即将提供」。不要改 HTML 其它部分，不要把未上真机的 macOS 写成已经可下，不要占用 17891，能不重启中继就不要重启。

截图放到 `docs/screenshots/r033-*`（至少首页下载区、Windows 下载、Linux 下载）。**勿覆盖** `r022-*`～`r032-*`。

## 本轮不做

- 改中继 JSONL 行为、改 AI-SSH-Pro、完整远程桌面
- 改安装说明 / Skill 文案 / 信任等级「才问你」
- 改 R031 主状态或 R032 托盘文案
- 把 macOS 写成已经可下
- 购买代码签名证书 / 给官网加 HTTPS

## 完成时

- 更新本文交付说明与截图
- 回告产品验收；未开下一轮
"""
write_utf8("docs/round-033.md", r33)
print("round-033 ok")

# 3. roadmap.md
rm = Path("docs/roadmap.md").read_text(encoding="utf-8")
old_r032 = "| R032 | 高频：关闭窗口后留在托盘的说明只留一处；打磨：关窗后底栏一次性反馈 | 鹊桥-客户端 | **delivered** | `docs/round-032.md` |"
new_r032 = "| R032 | 高频：关闭窗口后留在托盘的说明只留一处；打磨：关窗后底栏一次性反馈 | 鹊桥-客户端 | **done（产品已验收）** | `docs/round-032.md` |"
new_r033 = "| R033 | 高频：官网安装包跟上含 R031–R032 的客户端；打磨：替换后首页旁注与导语不回退 | 鹊桥-客户端 / 鹊桥-服务端 | **ready** | `docs/round-033.md` |"
assert old_r032 in rm, "R032 row missing"
rm = rm.replace(old_r032, new_r032 + "\n" + new_r033, 1)

old_para_end = "R032 已交付（关闭窗口后留在托盘的说明只留一处，见 `docs/round-032.md`）；未开下一轮。"
new_para_end = "R032 已产品验收（`b78fc1e`）；下一步 R033：官网安装包跟上含 R031–R032；未开再下一轮。"
assert old_para_end in rm, "para end missing"
rm = rm.replace(old_para_end, new_para_end, 1)

old_status_cell = "**done（R019–R030）**"
count = rm.count(old_status_cell)
assert count == 1, f"expected 1 status cell, got {count}"
rm = rm.replace(old_status_cell, "**done（R019–R030）；R033 ready 跟上 R031–R032**", 1)
write_utf8("docs/roadmap.md", rm)
print("roadmap ok")

# 4. PRODUCT.md
prod = Path("docs/PRODUCT.md").read_text(encoding="utf-8")
old_prod = "下一步 R032：关闭窗口后留在托盘的说明只留一处。"
new_prod = "R032 已于 2026-11-06 产品对照截图验收通过（`b78fc1e`）。下一步 R033：官网安装包跟上含 R031–R032 的客户端。"
assert old_prod in prod, "PRODUCT ending missing"
prod = prod.replace(old_prod, new_prod, 1)
write_utf8("docs/PRODUCT.md", prod)
print("PRODUCT ok")

# 5. retro.md
retro = Path("docs/retro.md").read_text(encoding="utf-8")
retro_append = """
## R032 产品验收（2026-11-06 Asia/Shanghai）

产品对照 `docs/screenshots/r032-*` 与源码验收通过（`b78fc1e`）：

- Windows 接受协助端底栏默认不再写「就绪。关闭窗口后会继续在托盘运行。」；启动气泡口径保留；关窗后底栏可出现一次性「已隐藏到托盘。双击托盘图标可重新打开。」
- 缺口：未实拍 NotifyIcon 气泡；关窗反馈图为 ui-shot；公网包仍为 R030。

已开 R033（官网安装包跟上含 R031–R032 的客户端；替换后首页旁注与导语不回退；客户端出包，服务端替换上架；不改中继/AI-SSH-Pro）。
"""
if not retro.endswith("\n"):
    retro += "\n"
retro = retro + retro_append
write_utf8("docs/retro.md", retro)
print("retro ok")

for f in ["docs/round-032.md","docs/round-033.md","docs/PRODUCT.md","docs/roadmap.md","docs/retro.md"]:
    raw = Path(f).read_bytes()[:4]
    bom = raw.startswith(b"\xef\xbb\xbf")
    print(f, "BOM" if bom else "no-BOM")
