# -*- coding: utf-8 -*-
from pathlib import Path

# PRODUCT: strengthen freeze bullet with full-parity migration backlog
p = Path("docs/PRODUCT.md")
t = p.read_text(encoding="utf-8")
old = """- **冻结 `src/` net8 WinForms 功能迭代**：不再为功能对等向 net8 WinForms 补齐；仅接受安全/协议关键回补。功能缺口（如信任三档等）**迁入 net48**，不为对等去回填 net8。"""
new = """- **冻结 `src/` net8 WinForms 功能迭代**：不再向 net8 WinForms 加新功能；仅接受安全/协议关键回补。**凡此前在 net8（含 Avalonia/WinForms）已迭代、且仍属产品意图的能力，须迁到 net48 直至能力对等**——不为「两边都做一遍」去回填 net8。
- **net48 迁移 backlog（已知缺口，非穷尽）**：信任三档；「结束这次协助」；本次协助已确认改动回看；写确认/安装说明等 `WriteConfirmCopy` 亲友文案与 Windows 安装前提（Framework 4.8）。**不迁**：账号免密 / `connect_device` / `ownerLink` / 设备密钥快捷连（产品意图已废止）。"""
if old not in t:
    raise SystemExit("PRODUCT freeze bullet not found")
t = t.replace(old, new, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("PRODUCT OK")

# roadmap current blurb
r = Path("docs/roadmap.md")
rt = r.read_text(encoding="utf-8")
old_lock = """> **Windows 主维护锁定（2026-11-06）**：亲友主路径 = **net48 Framework 4.8 依赖小包**；**冻结 `src/` net8 WinForms 功能**（仅安全/协议关键回补）；缺口迁 net48，**不为对等回填 net8**；**Avalonia = Mac/Linux**，非 Windows 亲友主路径。"""
new_lock = """> **Windows 主维护锁定（2026-11-06）**：亲友主路径 = **net48 Framework 4.8 依赖小包**；**冻结 `src/` net8 WinForms 功能**（仅安全/协议关键回补）。**net8 上已迭代且仍属产品意图的能力须全量迁 net48（能力对等）**；已知 backlog：信任三档、「结束这次协助」、已确认改动回看、`WriteConfirmCopy` 文案/安装前提；**不迁账号免密**（已废止）。**不为对等回填 net8**；**Avalonia = Mac/Linux**，非 Windows 亲友主路径。"""
if old_lock not in rt:
    raise SystemExit("roadmap lock blurb not found")
rt = rt.replace(old_lock, new_lock, 1)

# Optionally add a small backlog row or note under platform - keep brief
old_stack = """| 技术栈与体积方案 | 鹊桥-架构 | **done（2026-11-06 主维护锁定）** | Windows **主维护=net48 WinForms + Framework 4.8 小包**；**冻结 net8 WinForms 功能**；Avalonia=Mac/Linux；自包含 ~72 MB 仅兼容备用；见 PRODUCT |"""
new_stack = """| 技术栈与体积方案 | 鹊桥-架构 | **done（2026-11-06 主维护锁定）** | Windows **主维护=net48 + Framework 4.8 小包**；冻结 net8 WinForms；**net8 已迭代能力迁 net48 对等**（信任三档/结束协助/已确认改动/WriteConfirmCopy；不迁免密）；Avalonia=Mac/Linux；见 PRODUCT |"""
if old_stack not in rt:
    raise SystemExit("roadmap stack row not found")
rt = rt.replace(old_stack, new_stack, 1)
r.write_text(rt, encoding="utf-8", newline="\n")
print("roadmap OK")

# retro amend
retro = Path("docs/retro.md")
rv = retro.read_text(encoding="utf-8")
old_retro = """- **拍板**：Windows 亲友主路径 = **net48 Framework 4.8 依赖小包**；**冻结 `src/` net8 WinForms 功能迭代**（仅安全/协议关键回补）；功能缺口迁入 net48，**不为对等回填 net8**；**Avalonia = Mac/Linux**，不是 Windows 亲友主路径。
- **R047**：出 net48 小 Setup；安装文案须写 Framework 4.8。**跟进**：`WriteConfirmCopy` 仍写「.NET 8 运行时」——实现时改文案，本轮未改 C#（须客户端授权）。
- **预防**：官网/复制安装说明勿再把几十兆 .NET 8 自包含当 Windows 主推；发错包视为产品事故。"""
new_retro = """- **拍板**：Windows 亲友主路径 = **net48 Framework 4.8 依赖小包**；**冻结 `src/` net8 WinForms 功能迭代**（仅安全/协议关键回补）。**凡 net8 已迭代且仍属产品意图的能力须迁 net48 至能力对等**，不为对等回填 net8；**Avalonia = Mac/Linux**，不是 Windows 亲友主路径。
- **迁移 backlog（已知）**：信任三档；「结束这次协助」；本次协助已确认改动回看；`WriteConfirmCopy` 亲友文案与 Windows 安装前提（Framework 4.8）。**不迁**：账号免密 / ownerLink / 设备密钥快捷连（已废止）。
- **R047**：出 net48 小 Setup；安装文案须写 Framework 4.8。**跟进**：`WriteConfirmCopy` 仍写「.NET 8 运行时」——实现时改文案，本轮未改 C#（须客户端授权）。
- **预防**：官网/复制安装说明勿再把几十兆 .NET 8 自包含当 Windows 主推；发错包视为产品事故。"""
if old_retro not in rv:
    raise SystemExit("retro section not found")
rv = rv.replace(old_retro, new_retro, 1)
retro.write_text(rv, encoding="utf-8", newline="\n")
print("retro OK")
