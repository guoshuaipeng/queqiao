# -*- coding: utf-8 -*-
from pathlib import Path

docs = Path("docs")

# ---- round-045.md ----
r045 = docs / "round-045.md"
text = r045.read_text(encoding="utf-8")

old_status = "> 状态：**compiled（net48 失败文案+Helper catch 收窄已接线；编译通过；运行未验收）**"
new_status = "> 状态：**编译/文档核对 = PASS_WITH_NOTES（`39d6dc1`）；运行验收仍 blocked（未验收）**"
if old_status not in text:
    raise SystemExit("round-045 status line not found")
text = text.replace(old_status, new_status, 1)

# enrich compile section if thin
old_compile = """### 编译

- `scripts/build-net48.ps1` publish-only（不自动启动）


## 参考
"""
new_compile = """### 编译

- `scripts/build-net48.ps1` publish-only（不自动启动）
- 产物：`E:\\IdeaProjects\\remote-assist\\artifacts\\app-net48-fd\\RemoteAssist.Net48.exe`
- **19968** 字节 · **2026-11-06 09:57:04 Asia/Shanghai**
- SHA256 `57F9ADB65B4FEC7A647E9966EAA105A1F4B8B24F23B320FFF3A75265706C60BF`

## 产品验收 · 编译/文档核对（2026-11-06 Asia/Shanghai）

**结论：编译/文档核对 = PASS_WITH_NOTES；真实连接 / 运行验收 = 仍 blocked（未验收）。**  
**不是**可公网使用的已验证客户端。

| 项 | 结果 |
|----|------|
| 证据 commit | `39d6dc1ee96a1748cffb59ead4493e6171a8d3e8`（短 `39d6dc1`） |
| 范围 | 只读源码 + 文档 + 产物元数据核对；**未**在用户本机启动 UI / 联调 / 截图 |
| net48 无 UI 编译 | 客户端交付后无 UI publish 成功 |
| 产物 | `RemoteAssist.Net48.exe`，**19968** 字节，**2026-11-06 09:57:04 Asia/Shanghai**，SHA256 `57F9ADB65B4FEC7A647E9966EAA105A1F4B8B24F23B320FFF3A75265706C60BF` |
| 失败文案 | `FailWrongPassword` / `FailPeerNotAccepting` / `FailStaleCode` 已改为完整连接凭证口径 |
| Helper catch | 通用 catch **仅** `SetFailHint(RedactSecureMaterial(friendly))`，**不再**一律追加 `MissingSecurityInfoHint` |
| 缺 fp 预检 | 保留；`ConnectDraftParser` / `AssistClient` 缺安全验证阻断保留 |
| 凭证格式 / 信任模型 / 脱敏 / 8 位规则 | **未改** |
| Avalonia / CLI | **本轮未改** |
| 真实连接 | **blocked / 未验收** |

### NOTES（转入 R046 / 备忘）

1. 各失败场景仅源码核对，**未**实机验证文案弹出路径。
2. Avalonia/CLI 完整安全载荷主路径、本机外运行验收、net8 Insecure 债 → **另轮**。
3. 产品新需求（会话码生成/使用审计 + 管理台只读查看）→ **R046**（服务端轮；见 `docs/round-046.md`）。

## 参考
"""
if old_compile not in text:
    raise SystemExit("round-045 compile block not found")
text = text.replace(old_compile, new_compile, 1)

# append round-046 to references if missing
if "`docs/round-046.md`" not in text:
    text = text.rstrip() + "\n- `docs/round-046.md`\n"
r045.write_text(text, encoding="utf-8", newline="\n")
print("updated round-045.md")

# ---- round-046.md ----
r046 = """# 第 46 轮 — 会话码生成/使用审计 + 管理台只读查看

> 产品负责人：鹊桥-产品 · 2026-11-06 Asia/Shanghai  
> 实现：鹊桥-服务端（主）；架构只读评估对齐后落地  
> 状态：**open**（产品拍板已定；服务端/架构只读评估细节待回；**未**实现）  
> 依据：用户要求「服务端记录生成的 code，以及使用记录；管理台可以查看」。R045 编译/文档 **PASS_WITH_NOTES**（`39d6dc1`）；运行仍 blocked。  
> **本轮主题**：中继侧会话码**审计可查**；**不**改客户端亲友 UI、**不**在用户本机跑应用。  
> **范围默认仅服务端 / 管理台**；客户端凭证格式与信任模型**不动**。

## 本轮目标（高频 · 产品拍板）

1. **中继持久化/审计会话码生命周期**（至少）：
   - **生成**（何时创建会话码）
   - **使用**：join **成功** / **失败**
   - **结束**（会话结束）
2. **管理台只读列表**：可查看上述记录；列至少含：
   - `created_at`（生成时间）
   - `code`（会话码）
   - `status`（如 used / failed / ended；具体枚举以实现评估后为准，须覆盖生成、使用成败、结束）
3. **严禁存储或展示**：
   - 验证码 / `pwd`
   - 完整 `PairQrPayload` / 完整连接凭证
   - 业务证书指纹 `fp`
   - 业务管道明文或其它业务载荷
4. 参与者标识仅用协议已有、经确认允许的字段；**不**把 IP 当实名。
5. 文件日志 vs SQLite、是否改协议 JSONL 字段 → **待服务端/架构读到现网实现后再定**；本轮文档先锁产品边界，实现细节以评估回执为准。
6. 同步清理 roadmap 等文档里过时的「R045 打开」状态句（本验收落仓一并改）。

## 验收标准（产品）

1. 管理台能只读列出会话码生成与使用记录；字段含生成时间、code、状态（使用成败/结束可区分）。
2. 管理台与服务端日志/库中**看不到** pwd、完整凭证、fp、业务载荷。
3. 不改 `remoteassist:pair` 客户端分享形态、不改业务 TLS+Agent 指纹钉扎、不引入 TOFU/自动降明文。
4. **不**在用户本机启动客户端 UI / 联调打断工作；本轮**无**客户端功能交付要求。
5. 未完成服务端/架构评估前，不得把「open」写成已实现；评估结论须写入本轮文档再开发。

## 本轮不做

- 客户端亲友 UI / Avalonia / CLI 改动
- 在用户本机弹窗实测客户端
- 存储或展示验证码、完整连接凭证、指纹、业务载荷
- 改写 git 历史 / force-push
- 官网换包、S005 大功能（除非评估证明必须极小协议补丁且产品另批）

## 完成时

- 服务端：改动文件；存储选型；事件点对照（生成/join 成败/结束）；管理台只读 API 或页面说明；明确未记字段清单。
- 架构：与 `relay-protocol.md` / 现网 `121.196.45.81:17891` 是否矛盾的书面结论。
- 产品：对照本验收标准；不把编译客户端当本轮验收。

## 参考

- `docs/round-045.md`
- `docs/relay-protocol.md`
- `docs/relay-deploy.md`
- `docs/tls-trust.md`
- `docs/quality-connection-credential.md`
"""
(docs / "round-046.md").write_text(r046, encoding="utf-8", newline="\n")
print("wrote round-046.md")

# ---- roadmap.md ----
rm = docs / "roadmap.md"
rt = rm.read_text(encoding="utf-8")

old_r045_row = "| R045 | 高频：亲友失败文案残留（码+密码→完整连接凭证）+ Helper 缺安全验证提示收窄（仅缺 fp/短凭证）+ 文档状态行清理；稳定性/UX，不扩功能 | 鹊桥-客户端 | **compiled**（net48；运行未验收） | `docs/round-045.md` |"
new_r045_r046_rows = """| R045 | 高频：亲友失败文案残留（码+密码→完整连接凭证）+ Helper 缺安全验证提示收窄（仅缺 fp/短凭证）+ 文档状态行清理；稳定性/UX，不扩功能 | 鹊桥-客户端 | **PASS_WITH_NOTES**（编译/文档 `39d6dc1`；运行 blocked） | `docs/round-045.md` |
| R046 | 高频：中继会话码**生成/使用审计** + 管理台**只读**列表（created_at / code / status）；严禁存展示 pwd/完整凭证/fp/业务载荷 | 鹊桥-服务端（主）/ 鹊桥-架构（评估） | **open** | `docs/round-046.md` |"""
if old_r045_row not in rt:
    raise SystemExit("roadmap R045 row not found")
rt = rt.replace(old_r045_row, new_r045_r046_rows, 1)

old_cur = "> **当前**：R045 **compiled**（net48 失败文案+catch 收窄；运行未验收；仅 net48；不扩功能）。R044 编译/文档 **PASS_WITH_NOTES**（`7cad0ab`；产物 19968 B / 2026-11-06 09:46:59 Asia/Shanghai / SHA256 `a78c0f24…d81e`），运行仍 blocked；Avalonia/CLI 完整载荷另轮。R043 编译/文档 **PASS_WITH_NOTES**（`0122127`），真连仍 blocked。见 `docs/round-045.md`、`docs/round-044.md`。R042 已产品验收（出包 `8d61398`，上架 `f90bd8f`，2026-11-06）。"
new_cur = "> **当前**：R046 **open**（会话码生成/使用审计 + 管理台只读查看；服务端轮；禁记 pwd/完整凭证/fp/业务载荷；细节待服务端/架构评估）。R045 编译/文档 **PASS_WITH_NOTES**（`39d6dc1`；产物 19968 B / 2026-11-06 09:57:04 Asia/Shanghai / SHA256 `57F9ADB6…60BF`），运行仍 blocked。R044 编译/文档 **PASS_WITH_NOTES**（`7cad0ab`），运行仍 blocked；Avalonia/CLI 完整载荷另轮。见 `docs/round-046.md`、`docs/round-045.md`。R042 已产品验收（出包 `8d61398`，上架 `f90bd8f`，2026-11-06）。"
if old_cur not in rt:
    raise SystemExit("roadmap current blurb not found")
rt = rt.replace(old_cur, new_cur, 1)

# Fix long platform paragraph: "R045 打开" → PASS_WITH_NOTES + R046 open
old_long = "R043 编译/文档 PASS_WITH_NOTES（`0122127`；真连 blocked）；R044 编译/文档 PASS_WITH_NOTES（`7cad0ab`；运行 blocked；见 `docs/round-044.md`）；R045 打开（失败文案残留 + 缺安全提示收窄；见 `docs/round-045.md`）。"
new_long = "R043 编译/文档 PASS_WITH_NOTES（`0122127`；真连 blocked）；R044 编译/文档 PASS_WITH_NOTES（`7cad0ab`；运行 blocked；见 `docs/round-044.md`）；R045 编译/文档 PASS_WITH_NOTES（`39d6dc1`；运行 blocked；见 `docs/round-045.md`）；R046 打开（会话码审计 + 管理台只读；见 `docs/round-046.md`）。"
if old_long not in rt:
    raise SystemExit("roadmap long platform paragraph R045 打开 not found")
rt = rt.replace(old_long, new_long, 1)

# sanity: no leftover "R045 打开"
if "R045 打开" in rt:
    raise SystemExit("still has R045 打开 somewhere")
rm.write_text(rt, encoding="utf-8", newline="\n")
print("updated roadmap.md")

# ---- PRODUCT.md ----
prod = docs / "PRODUCT.md"
pt = prod.read_text(encoding="utf-8")
old_p = "R044 编译/文档核对 **PASS_WITH_NOTES**（`7cad0ab`；产物 19968 字节 / 2026-11-06 09:46:59 Asia/Shanghai / SHA256 `a78c0f246c4532025cd5ed0407013ac1828e2e3bb379fc692a0a32604080d81e`）；**运行仍 blocked**；net48 七句逐字文案已接线，Avalonia/CLI 未改。见 `docs/round-044.md`。当前打开 **R045**：亲友失败文案残留（码+密码→完整连接凭证）+ Helper 缺安全验证提示收窄 + 文档状态清理（`docs/round-045.md`）。后续迭代优先稳定性和用户体验；本机验收勿打断用户工作，优先本机外路径。"
new_p = "R044 编译/文档核对 **PASS_WITH_NOTES**（`7cad0ab`；产物 19968 字节 / 2026-11-06 09:46:59 Asia/Shanghai / SHA256 `a78c0f246c4532025cd5ed0407013ac1828e2e3bb379fc692a0a32604080d81e`）；**运行仍 blocked**；net48 七句逐字文案已接线，Avalonia/CLI 未改。见 `docs/round-044.md`。R045 编译/文档核对 **PASS_WITH_NOTES**（`39d6dc1`；产物 19968 字节 / 2026-11-06 09:57:04 Asia/Shanghai / SHA256 `57F9ADB65B4FEC7A647E9966EAA105A1F4B8B24F23B320FFF3A75265706C60BF`）；**运行仍 blocked**；失败文案改完整连接凭证口径，Helper catch 不再叠缺安全提示。见 `docs/round-045.md`。当前打开 **R046**：中继会话码生成/使用审计 + 管理台只读列表（禁记/禁展 pwd、完整凭证、fp、业务载荷）（`docs/round-046.md`）。后续迭代优先稳定性和用户体验；本机验收勿打断用户工作，优先本机外路径。"
if old_p not in pt:
    raise SystemExit("PRODUCT platform paragraph snippet not found")
pt = pt.replace(old_p, new_p, 1)
prod.write_text(pt, encoding="utf-8", newline="\n")
print("updated PRODUCT.md")

# ---- retro.md ----
retro = docs / "retro.md"
rr = retro.read_text(encoding="utf-8")
addon = """

## R045 产品验收复盘（2026-11-06 Asia/Shanghai）

编译/文档核对 **PASS_WITH_NOTES**（证据 `39d6dc1`）；真实连接 / 运行 **仍 blocked**。

- **做对的**：三条失败句改完整连接凭证口径；Helper 通用 catch 不再一律追加缺安全验证句；缺 fp 预检与阻断保留；无 UI 编译、不打断用户本机；不改协议/信任模型。
- **NOTES**：失败场景仅源码核对未实机弹出；Avalonia/CLI 完整载荷与真连验收仍另轮。
- **产品新方向**：用户要求服务端记录会话码生成与使用，并在管理台只读可查——**严禁**记/展 pwd、完整凭证、fp、业务载荷。

已开 R046（服务端：会话码审计 + 管理台只读；细节待服务端/架构评估回执）。

"""
if "R045 产品验收复盘" in rr:
    print("retro already has R045 section; skip append")
else:
    if not rr.endswith("\n"):
        rr += "\n"
    rr += addon
    retro.write_text(rr, encoding="utf-8", newline="\n")
    print("updated retro.md")

print("ALL OK")
