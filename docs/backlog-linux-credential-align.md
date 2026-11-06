# Backlog：Linux / Avalonia 连接凭证 UX 对齐（待查重）

> **状态：待查重**（2026-11-06 Asia/Shanghai）  
> **来源**：测试 bot 对 **历史产物**（文档中的 Linux Avalonia 轮次，常称 **r011** 一带）的观察，**不是**对照当前源码 / 新构建产物后的结论。  
> **非 R050**；**不授实现**。历史安装包或旧 artifacts **不等于**现行缺口。

## 观察（仅作查重线索）

测试侧在旧 Linux 产物上曾看到类似现象（摘要）：

- 复制路径可能**缺少**完整连接凭证所需的 `fp=`（或未提供「复制完整 remoteassist:pair」入口）。
- 协助端可能呈现「两行短码」而按现行信任模型，短码**不能单独**建立安全业务通道。
- 本机写确认「三档」入口在旧包上可能缺失或与现行 net48 / 文档不一致。

以上均可能是**过时产物**、未跟上 R043+ 信任纠错，或文档轮次号与二进制漂移——**须查重后再论**。

## 查重要求（通过后才能讨论是否开实现轮）

1. 对照**当前** `src/`（Avalonia / Linux）源码：完整载荷 / `fp=`、短码复制、写确认三档是否已接线。
2. 用**新构建产物**（本轮干净 publish，记录 commit / 哈希）在 Linux 环境复测；结果标 `Linux`，**勿**写成 Net48 PASS。
3. 若现行源码已具备而仅旧包缺失 → 记「文档/发版滞后」或出包轮，**不要**当新功能缺口开码。
4. 若现行源码确缺 → 产品查重登记需求（价值、范围、非目标、验收），再走 `dev-iteration.md` 授权；**仍非** R050 PAKE 实现。

## 明确排除

- 不把本文件当作 R050 / PAKE / 短码可连的实现依据。
- 不把历史 r011（或同阶段）验收通过写成「今日 Linux 已与严格唯一凭证对齐」。
- 架构本条只登记待查重；**不写业务码、不上机**。

## 相关文档

- `docs/tls-trust.md`、`docs/quality-connection-credential.md`
- `docs/short-credential-trust.md`、`docs/pake-unique-credential-eval.md` §11
- `docs/architecture.md` §15–§16、`docs/dev-iteration.md`
