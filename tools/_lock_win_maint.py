# -*- coding: utf-8 -*-
from pathlib import Path

# --- PRODUCT.md ---
p = Path("docs/PRODUCT.md")
text = p.read_text(encoding="utf-8")

repls = [
(
"""- 客户端：**Windows 主路径 = .NET Framework 4.8（`src-net48/` + `scripts/build-net48.ps1`）**，WinForms；演示与验收优先开 net48 产物。**.NET 8 自包含安装包仅作兼容/出包备用**，不是 Windows 亲友主路径。macOS / Linux 仍用 .NET 8 + Avalonia（见 architecture.md 第 13 节）；src-net48 不上 Mac/Linux。""",
"""- 客户端：**Windows 主维护路径 = .NET Framework 4.8（`src-net48/` + `scripts/build-net48.ps1`）**，WinForms；官网/亲友下载与演示验收均优先 **net48 框架依赖小包**（约 1.4 MB 档，非几十兆自包含）。
- **冻结 `src/` net8 WinForms 功能迭代**：不再为功能对等向 net8 WinForms 补齐；仅接受安全/协议关键回补。功能缺口（如信任三档等）**迁入 net48**，不为对等去回填 net8。
- **Avalonia** = **Mac/Linux** 轨道，**不是** Windows 亲友主路径。
- **.NET 8 自包含 Windows 安装包**仅历史/兼容备用（约 72 MB），不作为主维护与官网主推。macOS / Linux 仍用 .NET 8 + Avalonia（见 architecture.md 第 13 节）；src-net48 不上 Mac/Linux。"""
),
(
"""- **同一安装包、双模式**：接受协助 / 去协助别人；可返回首页。视觉质感以首页和配对确认为标杆，协助端向它们看齐；原则见 docs/quality.md（Windows 主路径为 net48 WinForms，不换框架；net8 自包含安装包为兼容备用）。""",
"""- **同一安装包、双模式**：接受协助 / 去协助别人；可返回首页。视觉质感以首页和配对确认为标杆，协助端向它们看齐；原则见 docs/quality.md（**Windows 主维护 = net48 WinForms**，不换框架；**冻结 net8 WinForms 功能**；net8 自包含仅为兼容备用；Avalonia 只管 Mac/Linux）。"""
),
(
"""**net48 安装包（R047）**：把 `app-net48-fd` 打成单个 Setup 发给别人安装；须写明 .NET Framework 4.8 前提；与 R046 并行、互不阻塞。见 docs/round-047.md。""",
"""**net48 安装包（R047）**：把 `app-net48-fd` 打成单个 **Framework 4.8 依赖**小 Setup 发给别人安装（几兆档，非几十兆自包含）；安装说明/旁注须写 **.NET Framework 4.8**，**不得**再写缺「.NET 8 运行时」。**跟进（未改代码）**：`src-net48`/`src` 的 `WriteConfirmCopy` 协助端「复制安装说明」仍含「先安装 .NET 8 运行时」——须在 R047 实现时改成 Framework 4.8（Windows 行）并保留 Linux 的 .NET 8 表述；改代码前须客户端授权。与 R046 并行、互不阻塞。见 docs/round-047.md。"""
),
(
"""- **分发**：Windows **主路径**走 **R047 net48** Setup（框架依赖；Inno 优先）；现有 net8 自包含 Inno 为兼容备用；macOS 为 .app + .dmg；Linux 先便携 .tar.gz，随后可选 .deb。""",
"""- **分发**：Windows **主维护**走 **R047 net48** Setup（Framework 4.8 依赖小包；Inno 优先）；现有 net8 自包含 Inno 冻结功能、仅兼容备用；macOS 为 .app + .dmg；Linux 先便携 .tar.gz，随后可选 .deb。"""
),
(
"""- **.NET 8 自包含包保留为兼容备用**（不愿或不能先装 Framework 4.8 / 过渡官网大包），体积上限不差于当前约 72 MB；手段为 PublishTrimmed + 单文件，Native AOT 作后续试验。官网若仍挂 net8 大包，视为过渡/备用，产品叙事以 net48 小包为准。""",
"""- **.NET 8 自包含包保留为兼容备用**（不愿或不能先装 Framework 4.8 / 过渡官网大包），体积上限不差于当前约 72 MB；**不再为功能对等维护**。官网若仍挂 net8 大包，视为过渡/备用，产品叙事与主维护以 net48 小包为准。"""
),
(
"""- 安装包：现有 `installer/RemoteAssist.iss` → `RemoteAssist-Setup-*.exe` 面向 **.NET 8 自包含**（兼容备用）。**Windows 亲友主路径分发**开 **R047**：打 **net48** 单文件 Setup（Inno 优先，或 MSI），内容来自 `artifacts/app-net48-fd`，依赖系统 **.NET Framework 4.8**；默认仍装到 `%LOCALAPPDATA%\\Programs\\RemoteAssist`（与 net8 并存策略见 `docs/round-047.md`）。官网上传可选另轮。""",
"""- 安装包：现有 `installer/RemoteAssist.iss` → `RemoteAssist-Setup-*.exe` 面向 **.NET 8 自包含**（兼容备用，**功能冻结**）。**Windows 亲友主维护分发**开 **R047**：打 **net48** Framework 4.8 依赖小 Setup（Inno 优先，或 MSI），内容来自 `artifacts/app-net48-fd`；默认仍装到 `%LOCALAPPDATA%\\Programs\\RemoteAssist`（与 net8 并存策略见 `docs/round-047.md`）。官网上传可选另轮。"""
),
]

for i, (a, b) in enumerate(repls):
    if a not in text:
        raise SystemExit(f"PRODUCT repl {i} not found: {a[:60]!r}")
    text = text.replace(a, b, 1)

p.write_text(text, encoding="utf-8", newline="\n")
print("PRODUCT.md OK")

# --- roadmap.md ---
r = Path("docs/roadmap.md")
rt = r.read_text(encoding="utf-8")

old_r047_row = """| R047 | 高频：Windows **net48 安装包**（Inno 或 MSI 单文件 Setup，依赖 .NET Framework 4.8）；打磨：安装说明写清运行时前提；官网上传可选另轮 | 鹊桥-客户端（主） | **open（规格已开）** | `docs/round-047.md` |"""
new_r047_row = """| R047 | 高频：Windows **net48 Framework 4.8 依赖小 Setup**（Inno/MSI；非几十兆自包含）；打磨：安装说明须写 **Framework 4.8**（改掉仍写「.NET 8 运行时」的 `WriteConfirmCopy`，须客户端授权）；官网上传可选另轮 | 鹊桥-客户端（主） | **open（规格已开）** | `docs/round-047.md` |"""
if old_r047_row not in rt:
    raise SystemExit("roadmap R047 row not found")
rt = rt.replace(old_r047_row, new_r047_row, 1)

old_cur = """> **当前**：并行——**R046** open（规格已拍板）；**R047** open（规格已开）；**R048** compiled（运行未验收）。**R049** draft / **待用户确认**（交互见 `quality-single-window-codes-draft.md`；确认前不开 UI）。**R050** **blocked**（**严格唯一凭证已拍板** 2026-11-06 10:35；取消免密/设备密钥旁路；仍等架构 PAKE 正式评估收口后授权实现；**不得**宣称现网已安全满足；见 `round-050.md`）。R045/R044 运行仍 blocked；R042 已产品验收。"""
new_cur = """> **当前**：并行——**R046** open（规格已拍板）；**R047** open（规格已开；主维护小包）；**R048** compiled（运行未验收）。**R049** draft / **待用户确认**（交互见 `quality-single-window-codes-draft.md`；确认前不开 UI）。**R050** **blocked**（**严格唯一凭证已拍板** 2026-11-06 10:35；取消免密/设备密钥旁路；仍等架构 PAKE 正式评估收口后授权实现；**不得**宣称现网已安全满足；见 `round-050.md`）。R045/R044 运行仍 blocked；R042 已产品验收。
>
> **Windows 主维护锁定（2026-11-06）**：亲友主路径 = **net48 Framework 4.8 依赖小包**；**冻结 `src/` net8 WinForms 功能**（仅安全/协议关键回补）；缺口迁 net48，**不为对等回填 net8**；**Avalonia = Mac/Linux**，非 Windows 亲友主路径。"""
if old_cur not in rt:
    raise SystemExit("roadmap current blurb not found")
rt = rt.replace(old_cur, new_cur, 1)

# platform stack row if present
old_stack = """| 技术栈与体积方案 | 鹊桥-架构 | **done** | `architecture.md` §13：Mac/Linux=Avalonia；Windows 暂留 WinForms；默认小包依赖运行时（目标约 1.4 MB 档）；自包含备选上限约 72 MB；macOS `.dmg`，Linux `.tar.gz` |"""
new_stack = """| 技术栈与体积方案 | 鹊桥-架构 | **done（2026-11-06 主维护锁定）** | Windows **主维护=net48 WinForms + Framework 4.8 小包**；**冻结 net8 WinForms 功能**；Avalonia=Mac/Linux；自包含 ~72 MB 仅兼容备用；见 PRODUCT |"""
if old_stack in rt:
    rt = rt.replace(old_stack, new_stack, 1)
else:
    print("WARN: stack row not exact; skipped")

r.write_text(rt, encoding="utf-8", newline="\n")
print("roadmap.md OK")

# --- retro.md ---
retro = Path("docs/retro.md")
rv = retro.read_text(encoding="utf-8")
if "Windows 主维护锁定" in rv:
    print("retro already has section; skip append")
else:
    rv = rv.rstrip() + """

## Windows 主维护锁定（2026-11-06 Asia/Shanghai）

- **触发**：用户比对 net48 / net8 后明确——主做依赖 Framework 4.8 的小体积包；net8 不再当 Windows 功能主维护。
- **拍板**：Windows 亲友主路径 = **net48 Framework 4.8 依赖小包**；**冻结 `src/` net8 WinForms 功能迭代**（仅安全/协议关键回补）；功能缺口迁入 net48，**不为对等回填 net8**；**Avalonia = Mac/Linux**，不是 Windows 亲友主路径。
- **R047**：出 net48 小 Setup；安装文案须写 Framework 4.8。**跟进**：`WriteConfirmCopy` 仍写「.NET 8 运行时」——实现时改文案，本轮未改 C#（须客户端授权）。
- **预防**：官网/复制安装说明勿再把几十兆 .NET 8 自包含当 Windows 主推；发错包视为产品事故。
"""
    retro.write_text(rv + "\n", encoding="utf-8", newline="\n")
    print("retro.md OK")
