from pathlib import Path
import re
import hashlib

p = Path("docs/round-044.md")
t = p.read_text(encoding="utf-8")
t = re.sub(
    r"> 状态：\*\*.*?\*\*",
    "> 状态：**compiled（net48 亲友逐字文案已接线；编译通过；运行未验收；Avalonia/CLI 本轮未改）**",
    t,
    count=1,
)
sec = """

## 交付（net48 文案 · 编译）

> 时间：2026-11-06 Asia/Shanghai · 范围仅 `src-net48` 亲友可见文案；**不改**凭证格式 / 信任模型 / 阻断条件。

### 改动文件

- `RemoteAssist.Protocol/FriendConnectGuide.cs` — R044 逐字常量；Need*Hint 对齐 MissingSecurityInfoHint
- `RemoteAssist.Protocol/ConnectDraftParser.cs` — 非加密通道人话；阻断条件未改
- `RemoteAssist.Protocol/AssistClient.cs` — 缺安全验证信息抛错用逐字句
- `RemoteAssist.Helper.UI/MainForm.cs` — 标签/旁注/预览/连接/失败提示
- `RemoteAssist.Agent/MainForm.cs` — 引导/短码旁注/复制成功失败；失败时展示完整串供手选
- `RemoteAssist.App/ModeSelectForm.cs` — 模式副文去技术词

### 逐字落点

| 规格句 | 源码 |
|--------|------|
| 粘贴完整连接凭证 | PasteCredentialLabel → Helper 输入区标签 |
| 旁注整段粘贴 | PasteCredentialHint → Helper 头/预览；ModeSelect 协助侧 |
| 连接 | ConnectButtonText → Helper 连接按钮 |
| 复制连接凭证 | CopyCredentialButtonText → Agent |
| 连接凭证已复制… | CopyCredentialSucceededHint |
| 未能复制… | CopyCredentialFailedHint + 手动选取对话框 |
| 缺少安全验证信息… | MissingSecurityInfoHint（Need*Hint）→ Parser/Helper/AssistClient |
| 引导 / 短码旁注 | AcceptGuideHint / OralShortCodeHint → Agent |

### 明确未做

- Avalonia / CLI / net8
- 用户本机运行 / 联调 / 截图 / 真连验收（R043 真连 blocked 不变）
- 包装部署；历史改写

### 编译

- `scripts/build-net48.ps1` publish-only（不自动启动）

"""
if "## 交付（net48 文案" not in t:
    if "## 参考" in t:
        t = t.replace("## 参考", sec + "\n## 参考", 1)
    else:
        t += sec
p.write_text(t, encoding="utf-8", newline="\n")
print("round-044 OK")

rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
a = "| R044 | 高频：完整连接凭证**逐字文案**扫尾（亲友 UI 无指纹/TLS 技术词）+ Avalonia/CLI 凭证 UI；有条件时本机外运行验收；备忘 net8 Insecure 债 | 鹊桥-体验（规格已定）/ 鹊桥-客户端（实现） | **todo** |"
b = "| R044 | 高频：完整连接凭证**逐字文案**扫尾（亲友 UI 无指纹/TLS 技术词）+ Avalonia/CLI 凭证 UI；有条件时本机外运行验收；备忘 net8 Insecure 债 | 鹊桥-体验（规格已定）/ 鹊桥-客户端（实现） | **compiled**（net48 文案+编译；运行未验收；Avalonia/CLI 未改） |"
if a in rt:
    rt = rt.replace(a, b, 1)
    print("roadmap table OK")
else:
    print("roadmap table miss")
c = "R044 **todo**——完整连接凭证逐字文案 + Avalonia/CLI UI 扫尾"
d = "R044 **compiled**（net48 逐字文案+编译；运行未验收；Avalonia/CLI 未改）——运行与跨端仍 open"
if c in rt:
    rt = rt.replace(c, d, 1)
    print("roadmap current OK")
else:
    print("roadmap current miss")
rp.write_text(rt, encoding="utf-8", newline="\n")

exe = Path("artifacts/app-net48-fd/RemoteAssist.Net48.exe")
print("BYTES", exe.stat().st_size)
print("SHA256", hashlib.sha256(exe.read_bytes()).hexdigest())