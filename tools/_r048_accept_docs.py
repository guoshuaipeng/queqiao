# -*- coding: utf-8 -*-
from pathlib import Path

docs = Path("docs")
EVIDENCE = "2d427d4c6eaa8ce0b5b699cc3cf43bc41b3700ac"
SHORT = "2d427d4"
SHA = "53974730E878F07C031212CC20338945E5C4B898FDE6C7B201A35D0AA5F4A20D"
VERIFY_SHA = "A4409597AC3892F2AD40FCCB63E0024D600B61272641A8E65BDB672BF1460DFB"

# ---- round-048.md ----
r048 = docs / "round-048.md"
text = r048.read_text(encoding="utf-8")

old_status = "> 状态：**compiled（net48 Helper MCP/安装说明已接线；编译通过；运行未验收；默认产物目录曾被本机已运行进程占用，publish 至 app-net48-fd-r048）** — 代码已接线（见交付）"
new_status = "> 状态：**编译/文档核对 = PASS_WITH_NOTES（`" + SHORT + "`）；运行验收仍 blocked（未验收）**"
if old_status not in text:
    raise SystemExit("round-048 status line not found: " + repr(text[text.find("> 状态"):text.find("> 状态")+200]))
text = text.replace(old_status, new_status, 1)

accept = """

## 产品验收 · 编译/文档核对（2026-11-06 Asia/Shanghai）

**结论：编译/文档核对 = PASS_WITH_NOTES；真实连接 / 运行验收 = 仍 blocked（未验收）。**  
**不是**可公网使用的已验证客户端。

| 项 | 结果 |
|----|------|
| 证据 commit | `{ev}`（短 `{sh}`） |
| 范围 | 只读源码 + 文档 + 产物元数据核对；**未**在用户本机启动 UI / 联调 / 截图 / 剪贴板实贴 Cursor |
| 改动范围 | Helper.UI + Protocol（`McpInstallGuide` / `WriteConfirmCopy` / `McpExeLocator`）；**未**改 Agent / 接受协助逻辑；无 MCP 监听/启动 |
| net48 无 UI 编译 | 客户端交付产物存在；产品另目录 `artifacts/app-net48-fd-r048-verify` publish **0 错误**（默认 `app-net48-fd` 仍可能被已运行进程占锁，**未杀进程**） |
| 交付产物 | `artifacts/app-net48-fd-r048/RemoteAssist.Net48.exe`，**19968** 字节，**2026-11-06 10:25:51 Asia/Shanghai**，SHA256 `{sha}`（与客户端声明一致） |
| 核对重编产物 | `artifacts/app-net48-fd-r048-verify/RemoteAssist.Net48.exe`，**19968** 字节，**2026-11-06 10:29:01 Asia/Shanghai**，SHA256 `{vsha}`（同尺寸；PE 时间戳导致哈希与交付物不同，属预期） |
| 界面入口（源码） | 主屏「复制安装说明」+ MCP 卡三步 +「复制 MCP 配置」+「复制 Skill 说明」；「连接」仍为主按钮 |
| 复制 MCP 配置 | `TryBuildOptions` → 要求合法 fp 且 **非** Insecure → `PairQrPayload.BuildRelay/Lan(..., useTls:true)` → `McpConfigSnippet.BuildClipboardBundle`；缺凭证用人话 `NeedCredentialHint` |
| 复制安装说明 | `WriteConfirmCopy.InstallInstructions` 含「复制连接凭证」口径；成功 `InstallCopiedHint` |
| 凭证格式 / 信任模型 / guest_* | **未改**；禁缺指纹降明文 MCP |
| Avalonia / CLI / R046 / R047 | **本轮未改** |
| 真实连接 / UI 运行 | **blocked / 未验收** |
| 包装部署 | **未做**（不在范围） |

### NOTES

1. 默认 `artifacts/app-net48-fd` 曾被本机已运行 `RemoteAssist.Net48` 占锁，交付改发 `app-net48-fd-r048`；产品核对未杀进程。
2. **未**做 UI/剪贴板/Cursor 连通/真连验收；本结论仅为编译/文档核对。
3. 包装部署不在本轮范围（见 R047）。
4. R046 / R047 仍并行 open，互不阻塞。

## 参考
""".format(ev=EVIDENCE, sh=SHORT, sha=SHA, vsha=VERIFY_SHA)

# Insert acceptance before final ## 参考 if present at end
marker = "\n## 参考\n"
idx = text.rfind(marker)
if idx < 0:
    raise SystemExit("round-048 trailing ## 参考 not found")
# Avoid double-insert
if "产品验收 · 编译/文档核对" in text:
    print("round-048 already has accept section; rewriting from delivery end")
    # strip from first accept if any — but we just replaced status; check
    pass
if "产品验收 · 编译/文档核对" not in text:
    text = text[:idx] + accept + text[idx+len(marker):]
    # accept already ends with ## 参考\n so we need to keep the rest of references
    # Actually accept ends with "## 参考\n" then we append the old reference body
    # text[idx+len(marker):] is the body after ## 参考
else:
    raise SystemExit("unexpected: accept section already present after status replace")

# enrich compile product metadata if thin
old_meta = "### 编译产物\n\n- 路径：`artifacts/app-net48-fd-r048/RemoteAssist.Net48.exe`（因 `artifacts/app-net48-fd` 被已运行的 RemoteAssist.Net48 占用未能覆盖；**本轮未启动/未结束用户进程**）\n- 元数据见 commit 说明\n"
new_meta = (
    "### 编译产物\n\n"
    "- 路径：`artifacts/app-net48-fd-r048/RemoteAssist.Net48.exe`（因 `artifacts/app-net48-fd` 被已运行的 RemoteAssist.Net48 占用未能覆盖；**本轮未启动/未结束用户进程**）\n"
    "- **19968** 字节 · **2026-11-06 10:25:51 Asia/Shanghai**\n"
    "- SHA256 `" + SHA + "`\n"
)
if old_meta not in text:
    raise SystemExit("compile product block not found")
text = text.replace(old_meta, new_meta, 1)

r048.write_text(text, encoding="utf-8", newline="\n")
print("updated round-048.md")

# ---- roadmap.md ----
rm = docs / "roadmap.md"
rt = rm.read_text(encoding="utf-8")

old_row = "| R048 | 高频：net48 **去协助 / Helper** 补「复制安装说明」+ MCP「复制 MCP 配置」JSON（对齐 net8/Avalonia）；打磨：缺完整凭证阻断、禁明文降级 | 鹊桥-客户端（主） | **compiled**（运行未验收） | `docs/round-048.md` |"
new_row = "| R048 | 高频：net48 **去协助 / Helper** 补「复制安装说明」+ MCP「复制 MCP 配置」JSON（对齐 net8/Avalonia）；打磨：缺完整凭证阻断、禁明文降级 | 鹊桥-客户端（主） | **PASS_WITH_NOTES**（编译/文档 `" + SHORT + "`；运行 blocked） | `docs/round-048.md` |"
if old_row not in rt:
    raise SystemExit("roadmap R048 row not found")
rt = rt.replace(old_row, new_row, 1)

old_cur = "> **当前**：并行三轮——**R046** **open（规格已拍板）**（服务端：会话码审计 + 管理台只读；见 `docs/round-046.md`、`docs/relay-audit.md`）；**R047** **open（规格已开）**（客户端：net48 单文件安装包；见 `docs/round-047.md`）；**R048** **compiled**（net48 Helper MCP/安装说明；运行未验收；见 `docs/round-048.md`）。R045 编译/文档 **PASS_WITH_NOTES**（`39d6dc1`），运行仍 blocked。R044 编译/文档 **PASS_WITH_NOTES**（`7cad0ab`），运行仍 blocked；Avalonia/CLI 完整载荷另轮。R042 已产品验收（出包 `8d61398`，上架 `f90bd8f`，2026-11-06）。"
new_cur = "> **当前**：并行三轮——**R046** **open（规格已拍板）**（服务端：会话码审计 + 管理台只读；见 `docs/round-046.md`、`docs/relay-audit.md`）；**R047** **open（规格已开）**（客户端：net48 单文件安装包；见 `docs/round-047.md`）；**R048** 编译/文档 **PASS_WITH_NOTES**（`" + SHORT + "`；运行 blocked；见 `docs/round-048.md`）。R045 编译/文档 **PASS_WITH_NOTES**（`39d6dc1`），运行仍 blocked。R044 编译/文档 **PASS_WITH_NOTES**（`7cad0ab`），运行仍 blocked；Avalonia/CLI 完整载荷另轮。R042 已产品验收（出包 `8d61398`，上架 `f90bd8f`，2026-11-06）。"
if old_cur not in rt:
    raise SystemExit("roadmap current blurb not found")
rt = rt.replace(old_cur, new_cur, 1)

old_long = "R048 打开（net48 协助端 MCP 复制安装说明/JSON；见 `docs/round-048.md`）。"
new_long = "R048 编译/文档 PASS_WITH_NOTES（`" + SHORT + "`；运行 blocked；见 `docs/round-048.md`）。"
if old_long not in rt:
    raise SystemExit("roadmap long R048 打开 not found")
rt = rt.replace(old_long, new_long, 1)

rm.write_text(rt, encoding="utf-8", newline="\n")
print("updated roadmap.md")

# ---- PRODUCT.md ----
prod = docs / "PRODUCT.md"
pt = prod.read_text(encoding="utf-8")

old_opt = "**Windows 主路径 net48「去协助」此前缺该入口**——开 **R048** 从 net8/Avalonia 迁入（Agent 侧已有复制配置不代替 Helper）。"
new_opt = "**Windows 主路径 net48「去协助」缺 MCP/安装说明入口**已由 **R048** 从 net8/Avalonia 迁入（编译/文档 PASS_WITH_NOTES `" + SHORT + "`；运行未验收；Agent 侧已有复制配置不代替 Helper）。"
if old_opt not in pt:
    raise SystemExit("PRODUCT optional MCP sentence not found")
pt = pt.replace(old_opt, new_opt, 1)

old_p = "**R048**（规格已开 — net48「去协助」补「复制安装说明」+「复制 MCP 配置」可贴 JSON，对齐 net8/Avalonia；禁缺指纹降明文；`docs/round-048.md`）。"
new_p = "**R048** 编译/文档核对 **PASS_WITH_NOTES**（`" + SHORT + "`；产物 19968 字节 / 2026-11-06 10:25:51 Asia/Shanghai / SHA256 `" + SHA + "`）；**运行仍 blocked**；net48 Helper 已接线复制安装说明 + MCP 配置/Skill；禁缺指纹降明文。见 `docs/round-048.md`。并行仍打开："
# Wait - the original continues with "后续迭代...". Need careful replace.
# Find the exact R048 parenthetical and replace, keeping "后续迭代"
needle = "**R048**（规格已开 — net48「去协助」补「复制安装说明」+「复制 MCP 配置」可贴 JSON，对齐 net8/Avalonia；禁缺指纹降明文；`docs/round-048.md`）。后续迭代"
repl = (
    "**R048** 编译/文档核对 **PASS_WITH_NOTES**（`" + SHORT + "`；产物 19968 字节 / 2026-11-06 10:25:51 Asia/Shanghai / SHA256 `" + SHA
    + "`）；**运行仍 blocked**；net48 Helper 已接线复制安装说明 + MCP 配置/Skill；禁缺指纹降明文。见 `docs/round-048.md`。后续迭代"
)
if needle not in pt:
    raise SystemExit("PRODUCT R048 规格已开 needle not found")
pt = pt.replace(needle, repl, 1)

old_mcp = "**net48 协助端 MCP（R048）**：去协助界面须有「复制安装说明」与「复制 MCP 配置」（可贴进 Cursor 等的 JSON，载荷为完整连接凭证）；对齐 net8 Helper / Avalonia；缺完整凭证阻断，禁止明文降级。见 docs/round-048.md。"
new_mcp = "**net48 协助端 MCP（R048）**：去协助界面须有「复制安装说明」与「复制 MCP 配置」（可贴进 Cursor 等的 JSON，载荷为完整连接凭证）；对齐 net8 Helper / Avalonia；缺完整凭证阻断，禁止明文降级。编译/文档 **PASS_WITH_NOTES**（`" + SHORT + "`）；运行未验收。见 docs/round-048.md。"
if old_mcp not in pt:
    raise SystemExit("PRODUCT net48 MCP blurb not found")
pt = pt.replace(old_mcp, new_mcp, 1)

prod.write_text(pt, encoding="utf-8", newline="\n")
print("updated PRODUCT.md")

# ---- retro.md ----
retro = docs / "retro.md"
rr = retro.read_text(encoding="utf-8")
addon = """

## R048 产品验收复盘（2026-11-06 Asia/Shanghai）

编译/文档核对 **PASS_WITH_NOTES**（证据 `""" + SHORT + """`）；真实连接 / 运行 **仍 blocked**。

- **做对的**：net48 Helper 主屏补齐「复制安装说明」+ MCP 三步 +「复制 MCP 配置」+「复制 Skill 说明」；`TryBuildOptions` 后强制合法 fp 且非 Insecure，再生成 TLS 完整凭证载荷；未改接受协助逻辑、未新增 MCP 监听；交付产物哈希与声明一致；另目录 verify publish 0 错误；不杀用户进程、不启 UI。
- **NOTES**：默认 `app-net48-fd` 被已运行进程占锁；无 UI/剪贴板/Cursor/真连验证；包装不在范围（R047）；verify 重编同尺寸但 SHA 因 PE 时间戳不同。
- **并行**：R046 / R047 仍 open。

"""
if "R048 产品验收复盘" in rr:
    print("retro already has R048 section; skip")
else:
    if not rr.endswith("\n"):
        rr += "\n"
    rr += addon
    retro.write_text(rr, encoding="utf-8", newline="\n")
    print("updated retro.md")

print("ALL OK")
