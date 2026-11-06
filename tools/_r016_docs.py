from pathlib import Path

md5 = {
"r016-tools-hidden.png": "48A0FADF15CF4706A89B9987A831040F",
"r016-tools-five.png": "F9690581B530E302F62FB436C386D864",
"r016-connected-button-light.png": "ADC23371A4465C17C76BF5CA9191ACF8",
"r016-propose-denied.png": "D235B25461B6B28AA3746C9A594B8D06",
}

delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- **常用操作**：未连接（含仅登录未免密）整块不展示。访客会话码或账号免密连上后展开五键，顺序与文案：查看正在运行的程序 / 查看文件夹 / 查看日志 / 查看文件 / 提议一项改动。标题下轻引导「建议先看正在运行的程序，再决定要不要改」。
- **提议一项改动**：走既有 `RunCommand` + `WriteConfirmCopy.DeniedMessage`（「已拒绝，未做任何改动」）。只读四项不走写确认弹窗（协议侧既有行为）。
- **路径输入**：与 Windows 一致的「路径 / 命令参数」+ 人话占位「例如 /home/你的文件夹」（Windows 为 `C:\\Users\\…`），无英文命令行窗。
- **已连接按钮权重**：账号区改为禁用轻态「已连接」（灰底深字，非实心绿）；会话码区「连接」已连接时灰禁用并去掉绿色填充。未连接选中在线时 R015 实心绿「连接：{{name}}」不变。
- **未改** R013/R014/R015 已验收原文；未改 `docs/trust-levels.md`。
- **osx**：与 Linux 同树可出包；本轮未重跑 publish、**未上真机**，不写成 Mac 已验收。

### 截图（`docs/screenshots/r016-*`，已复制 `%USERPROFILE%\\Downloads\\r016-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r016-tools-hidden.png` | 仅登录未免密；无「常用操作」整块；ui-shot | {md5["r016-tools-hidden.png"]} |
| `r016-tools-five.png` | 免密已连；五键+引导；账号区「已连接」轻态；ui-shot | {md5["r016-tools-five.png"]} |
| `r016-connected-button-light.png` | 已连接后账号「已连接」非实心绿；会话码「连接」灰禁；ui-shot | {md5["r016-connected-button-light.png"]} |
| `r016-propose-denied.png` | 拒绝后「已拒绝，未做任何改动」；ui-shot | {md5["r016-propose-denied.png"]} |

截图为 **ui-shot**（示例设备客厅电脑/书房电脑），不是现场公网双机。亲友界面不出现 Assist、Admin、ownerLink、TLS、端口号、deviceId、offline。

### 测试

`dotnet test`：169 通过 / 0 失败（RemoteAssist.sln Release）。

### 明确未做

- 未开 R017 / 下一轮
- 未改 `relay/`、官网、AI-SSH-Pro
- 未把 Windows 迁到 Avalonia；未重做 R006–R015
- 未改 R013–R015 已验收文案；未改 `docs/trust-levels.md`
- 现场公网双机非本轮必须，未做

'''

p = Path("docs/round-016.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**todo**", "> 状态：**ready-for-product**")
if "## 交付说明" in text:
    text = text[:text.index("## 交付说明")] + delivery.lstrip()
else:
    text = text.rstrip() + "\n" + delivery
p.write_text(text, encoding="utf-8")
print("round-016 ok")

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt = rt.replace(
    "| R016 | 高频：Avalonia 协助端连上后五键白话 + 引导 + 已连接按钮权重；打磨：文件夹/文件路径轻量输入 | 鹊桥-客户端 | **todo** | `docs/round-016.md` |",
    "| R016 | 高频：Avalonia 协助端连上后五键白话 + 引导 + 已连接按钮权重；打磨：文件夹/文件路径轻量输入 | 鹊桥-客户端 | **ready-for-product** | `docs/round-016.md` |",
)
old = "已开 R016（Avalonia 协助端连上后操作对齐 Windows）；见 `docs/round-016.md`。"
new = "R016 Avalonia 协助端连上后五键白话与已连接按钮权重已交付待产品验收（见 `docs/round-016.md`）；未开下一轮。"
if old in rt:
    rt = rt.replace(old, new)
rm.write_text(rt, encoding="utf-8")
print("roadmap ok")
