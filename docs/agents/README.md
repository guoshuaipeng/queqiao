# docs/agents — bot 职责与站立决策（换 bot 可接续）

> **规则：决策落在仓库 docs，不要只放在 bot 记忆里。**  
> 换人 / 换 bot 先读本目录对应角色文 + `docs/PRODUCT.md` / `docs/roadmap.md`，再干活。

## 角色索引

| 角色 | 文件 | 主责 |
|------|------|------|
| 鹊桥-产品 | [产品.md](./产品.md) | 定位、**主动发现与提出需求**、优先级、取舍、排轮与实现授权、验收收口（版本表/roadmap 非唯一来源） |
| 鹊桥-客户端 | [客户端.md](./客户端.md) | Tauri 桌面客户端（`apps/queqiao-tauri`） |
| 鹊桥-服务端 | [服务端.md](./服务端.md) | Go 中继、审计、部署相关实现；生产改动须用户原文授权 |
| 鹊桥-架构 | [架构.md](./架构.md) | 协议、信任模型、PAKE/短凭证边界；不写业务功能码 |
| 鹊桥-体验 | [体验.md](./体验.md) | 交互方案；**须用户确认后**产品才能交 UI 开发 |
| 鹊桥-场景 | [场景.md](./场景.md) | 亲友/客户/运维场景闭环文档；不写代码、不开实现轮 |
| 鹊桥-持续迭代 | [持续迭代.md](./持续迭代.md) | 读 PRODUCT-VERSIONS 小步迭代（仅待开发）；**已交接** |
| 鹊桥-迭代 | [迭代.md](./迭代.md) | 接替持续迭代：只执行已放行「待开发」（草稿/暂停/blocked 不实现） |

## 怎么用

1. 新 bot / 换会话：读本 README → 本角色 `*.md` → `PRODUCT.md` → 当前 `roadmap` / 相关 `round-NNN`。
2. 拍板、默认值、暂停/授权轮次：**写入**本目录或 PRODUCT / roadmap / scene / quality，再 commit（按授权 push）。
3. 聊天里的口头约定若未入库，视为未生效；接续者不应只靠记忆。
4. 桌面客户端只有 Tauri。不要恢复 .NET 工程。

## 交叉正文（勿在 agents 里另立冲突规格）

- 产品总册：`docs/PRODUCT.md`
- 路线图：`docs/roadmap.md`
- 架构：`docs/architecture.md` 及 `tls-trust.md` / `short-credential-trust.md` / `pake-unique-credential-eval.md`
- 体验草案（待确认）：`docs/quality-single-window-codes-draft.md`
- 无人值守场景：`docs/scene-s006-unattended.md`
- 规范化开发迭代步骤：`docs/dev-iteration.md`（架构与产品共同站立；架构独占写入）

## 当前助手重建（2026-11-06 13:39 Asia/Shanghai）

经「日常」按上表角色重建：鹊桥-产品 · 鹊桥-客户端 · 鹊桥-服务端 · 鹊桥-架构 · 鹊桥-体验 · 鹊桥-场景 · 鹊桥-迭代。  
持续迭代以 **鹊桥-迭代** 为准（读 PRODUCT-VERSIONS，进度写 iteration-status）。旧「持续迭代 / 迭代 / 文件迭代」例程应保持 pause。

