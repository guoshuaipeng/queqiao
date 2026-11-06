from pathlib import Path
import re, hashlib

# round-045
p = Path("docs/round-045.md")
t = p.read_text(encoding="utf-8")
t = re.sub(
    r"> 状态：\*\*.*?\*\*",
    "> 状态：**compiled（net48 失败文案+Helper catch 收窄已接线；编译通过；运行未验收）**",
    t,
    count=1,
)
sec = """

## 交付（net48 · 编译）

> 时间：2026-11-06 Asia/Shanghai · **不改**凭证格式 / 信任模型 / 阻断条件 / 脱敏逻辑。

### 改动

- `FriendConnectGuide.cs`：`FailWrongPassword` / `FailPeerNotAccepting` / `FailStaleCode` → 完整连接凭证口径
- `Helper.UI/MainForm.cs`：`ConnectAsync` 通用 catch **不再**追加 `MissingSecurityInfoHint`；仍 `RedactSecureMaterial(FriendlyOrRaw)`
- 保留：连接前缺 fp 预检、`ConnectDraftParser`/`Need*`、`AssistClient` 缺安全验证阻断

### 明确未做

- 用户本机运行 / 联调 / 截图；Avalonia/CLI；包装部署；真连验收；历史改写

### 编译

- `scripts/build-net48.ps1` publish-only（不自动启动）

"""
if "## 交付（net48" not in t:
    if "## 参考" in t:
        t = t.replace("## 参考", sec + "\n## 参考", 1)
    else:
        t += sec
p.write_text(t, encoding="utf-8", newline="\n")
print("round-045 OK")

# roadmap — only current R045 cell / 当前 pointer
rp = Path("docs/roadmap.md")
rt = rp.read_text(encoding="utf-8")
a = "| R045 | 高频：亲友失败文案残留（码+密码→完整连接凭证）+ Helper 缺安全验证提示收窄（仅缺 fp/短凭证）+ 文档状态行清理；稳定性/UX，不扩功能 | 鹊桥-客户端 | **open** | `docs/round-045.md` |"
b = "| R045 | 高频：亲友失败文案残留（码+密码→完整连接凭证）+ Helper 缺安全验证提示收窄（仅缺 fp/短凭证）+ 文档状态行清理；稳定性/UX，不扩功能 | 鹊桥-客户端 | **compiled**（net48；运行未验收） | `docs/round-045.md` |"
if a in rt:
    rt = rt.replace(a, b, 1)
    print("roadmap table OK")
else:
    print("roadmap table miss")
c = "R045 **open**（亲友失败文案残留 + Helper 缺安全验证提示收窄 + 文档状态清理；仅 net48；不扩功能）"
d = "R045 **compiled**（net48 失败文案+catch 收窄；运行未验收；仅 net48；不扩功能）"
if c in rt:
    rt = rt.replace(c, d, 1)
    print("roadmap current OK")
else:
    print("roadmap current miss")
rp.write_text(rt, encoding="utf-8", newline="\n")
print("docs done")