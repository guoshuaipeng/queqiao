from pathlib import Path
import re
p = Path("docs/round-048.md")
t = p.read_text(encoding="utf-8")
t = re.sub(
    r"> 状态：\*\*.*?\*\*",
    "> 状态：**compiled（net48 Helper MCP/安装说明已接线；编译通过；运行未验收；默认产物目录曾被本机已运行进程占用，publish 至 app-net48-fd-r048）**",
    t,
    count=1,
)
sec = """

## 交付（net48 Helper · 编译）

> 时间：2026-11-06 Asia/Shanghai · 仅 src-net48 Helper + Protocol 常量/辅助；**不**改 Avalonia/连接格式/信任模型；**不**自动对外监听 MCP。

### 改动文件

- `RemoteAssist.Protocol/McpInstallGuide.cs`（新）
- `RemoteAssist.Protocol/WriteConfirmCopy.cs`（新，安装说明；「复制连接凭证」口径）
- `RemoteAssist.Protocol/McpExeLocator.cs`（自 net8 Protocol 迁入，供 Helper）
- `RemoteAssist.Helper.UI/MainForm.cs`：主屏「复制安装说明」+ MCP 卡三步 +「复制 MCP 配置」+「复制 Skill 说明」

### 接线要点

- 「复制 MCP 配置」：`TryBuildOptions` → 要求合法 fp 且非 Insecure → `PairQrPayload.BuildRelay/Lan(..., useTls:true)` → `McpConfigSnippet.BuildClipboardBundle`；**禁止**缺指纹降明文
- 「复制安装说明」：`WriteConfirmCopy.InstallInstructionsClipboard`；成功 `InstallCopiedHint`
- 反馈经 `RedactSecureMaterial`；不把完整载荷写进状态/失败原文
- 无 MCP 监听/服务启动代码

### 编译产物

- 路径：`artifacts/app-net48-fd-r048/RemoteAssist.Net48.exe`（因 `artifacts/app-net48-fd` 被已运行的 RemoteAssist.Net48 占用未能覆盖；**本轮未启动/未结束用户进程**）
- 元数据见 commit 说明

### 未做 / 未验证

- 用户本机 UI 运行、联调、截图、剪贴板实贴 Cursor
- Avalonia/CLI；R046/R047；包装部署；真连 PASS

"""
if "## 交付（net48 Helper" not in t:
    if "## 参考" in t:
        t = t.replace("## 参考", sec + "\n## 参考", 1)
    else:
        t += sec
# strike "尚未实现代码" in body if still in first lines beyond status
t = t.replace("— **尚未实现代码**", "— 代码已接线（见交付）", 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("round-048 OK")

rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
a = "| R048 | 高频：net48 **去协助 / Helper** 补「复制安装说明」+ MCP「复制 MCP 配置」JSON（对齐 net8/Avalonia）；打磨：缺完整凭证阻断、禁明文降级 | 鹊桥-客户端（主） | **open（规格已开）** | `docs/round-048.md` |"
b = "| R048 | 高频：net48 **去协助 / Helper** 补「复制安装说明」+ MCP「复制 MCP 配置」JSON（对齐 net8/Avalonia）；打磨：缺完整凭证阻断、禁明文降级 | 鹊桥-客户端（主） | **compiled**（运行未验收） | `docs/round-048.md` |"
if a in rt:
    rt = rt.replace(a, b, 1)
    print("roadmap table OK")
else:
    print("roadmap table miss")
c = "**R048** **open（规格已开）**（客户端：net48 协助端补 MCP 复制安装说明/JSON；见 `docs/round-048.md`）"
d = "**R048** **compiled**（net48 Helper MCP/安装说明；运行未验收；见 `docs/round-048.md`）"
if c in rt:
    rt = rt.replace(c, d, 1)
    print("roadmap current OK")
else:
    print("roadmap current miss")
rp.write_text(rt, encoding="utf-8", newline="\n")