from pathlib import Path
md5 = {
"r015-device-list-human.png": "570A7FA4ABE02332F6123E662C50A733",
"r015-connect-unselected.png": "29F01207F14AC3700B28A669C66F6AE0",
"r015-connect-online.png": "48A0FADF15CF4706A89B9987A831040F",
"r015-connect-offline.png": "9423BA39F2C2311FA24E7D707CE511D3",
"r015-owner-tools-enabled.png": "C8A5857BBD31EEFDFD93ED774FEB95FE",
}
delivery = f'''
## 交付说明（鹊桥-客户端 · 2026-11-05 Asia/Shanghai）

### 实现摘要

- **`HelperWindow` 设备列表**：引入 `DeviceListItem`，`ToString` 只输出「人话名称 + 制表符 + 在线/离线」；内部 `Id` 不进入列表展示。刷新设备、免密连接均按该项取值。
- **免密连接按钮态**：`ApplyOwnConnectStyle`——可点时实心绿 `#0F9D58` + 白字加粗（与上方会话码「连接」同色语义）；禁用时灰底 `#E8EAED` + 灰字。未选中：「连接这台电脑」禁用 +「请先点选一台自己的电脑」。选中在线：「连接：{{name}}」可点 + R014 旁注「将免密连接「…」」。选中离线：按钮禁用 +「这台电脑现在不在线」。
- **打磨**：`SetConnectedUi(true)` 后「查看正在运行的程序 / 查看日志 / 运行一条命令」`IsEnabled=true`，并用浅蓝可点样式（`StyleToolButtonEnabled`）。
- **未改** R014 账号/密码可见标签、免密成功文案「已用你的账号连接，无需再确认配对（写操作仍须对方本机确认）」、审计人话；未改 R013「本次协助已确认的改动」与刷新确认原文。
- **osx**：与 Linux 同树可出包；本轮未重跑 publish、**未上真机**，不写成 Mac 已验收。

### 截图（`docs/screenshots/r015-*`，已复制 `%USERPROFILE%\\Downloads\\r015-shots\\`）

| 文件 | 要点 | MD5 |
|------|------|-----|
| `r015-device-list-human.png` | 列表「客厅电脑 / 书房电脑」+ 在线/离线；无内部 id；ui-shot | {md5["r015-device-list-human.png"]} |
| `r015-connect-unselected.png` | 未选中；灰色禁用「连接这台电脑」+「请先点选一台自己的电脑」；ui-shot | {md5["r015-connect-unselected.png"]} |
| `r015-connect-online.png` | 选中在线；实心绿「连接：客厅电脑」+「将免密连接「客厅电脑」」；ui-shot | {md5["r015-connect-online.png"]} |
| `r015-connect-offline.png` | 选中离线「书房电脑」；连接禁用 +「这台电脑现在不在线」；ui-shot | {md5["r015-connect-offline.png"]} |
| `r015-owner-tools-enabled.png` | 免密已连文案；常用操作三项可点；ui-shot | {md5["r015-owner-tools-enabled.png"]} |

设备与连接态截图为 **ui-shot**（示例设备「客厅电脑 / 书房电脑」），不是现场公网双机结对。列表展示不出现 Assist、Admin、ownerLink、TLS、端口号、deviceId、offline、dev-living、dev-office。

### 测试

`dotnet test`：见提交时实测。

### 明确未做

- 未开 R016 / 下一轮
- 未改 `relay/`、官网下载、AI-SSH-Pro
- 未把 Windows 迁到 Avalonia；未重做 R006–R014
- 未改 R013/R014 已验收文案；未改 `docs/trust-levels.md`「才问你」
- 现场公网双机非本轮必须，未做

'''

p = Path("docs/round-015.md")
text = p.read_text(encoding="utf-8")
text = text.replace("> 状态：**wip**", "> 状态：**ready-for-product**")
if "## 交付说明" not in text:
    text = text.rstrip() + "\n" + delivery
else:
    # replace from 交付说明 to end if re-run
    idx = text.index("## 交付说明")
    text = text[:idx] + delivery.lstrip()
p.write_text(text, encoding="utf-8")
print("round-015 updated", len(text.splitlines()))

rm = Path("docs/roadmap.md")
rt = rm.read_text(encoding="utf-8")
rt = rt.replace(
    "| R015 | 高频：Avalonia 设备列表人话（隐藏内部 id）+ 免密连接可点击态；打磨：免密连上后常用操作可用 | 鹊桥-客户端 | **wip** | `docs/round-015.md` |",
    "| R015 | 高频：Avalonia 设备列表人话（隐藏内部 id）+ 免密连接可点击态；打磨：免密连上后常用操作可用 | 鹊桥-客户端 | **ready-for-product** | `docs/round-015.md` |",
)
old = "下一步 R015 Avalonia 设备列表人话与免密连接可点击态。"
new = "R015 Avalonia 设备列表人话与免密连接可点击态已交付待产品验收（见 `docs/round-015.md`）；未开下一轮。"
if old in rt:
    rt = rt.replace(old, new)
rm.write_text(rt, encoding="utf-8")
print("roadmap updated")
