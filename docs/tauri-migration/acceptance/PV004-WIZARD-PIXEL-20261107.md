# PV-004 安装向导可视像素截图补证（2026-11-07）

- **时区**：Asia/Shanghai（UTC+8）
- **记录时间**：2026-11-07 21:35:07 Asia/Shanghai
- **结果**：**缺口（会话锁屏阻塞）** — 未伪 PASS
- **范围**：仅补向导可视像素图；不改码、不 push、不改 iss、未启动 Setup

## Setup 包复核

| 项 | 值 |
|----|----|
| 路径 | `E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Net48-Setup-0.1.0.exe` |
| SHA256 | `71CE3908300A804697502D80361B4FF9476E35F0F0B04A2286E70FA58DF39239` |
| 前缀匹配 | **是**（`71CE3908300A8046`） |
| 大小 | 2399692 bytes |

## 锁屏检测（本轮必做，已触发停止）

| 检查项 | 结果 |
|--------|------|
| LogonUI | 运行中（count=1，PID=27576） |
| LockApp | 运行中（count=1，PID=41932） |
| `query user` | 见下 |
| OpenInputDesktop | True（可打开输入桌面句柄，但 LogonUI/LockApp 并存 → 按产品口径仍判锁屏，禁止取证） |

```
USERNAME              SESSIONNAME        ID  STATE   IDLE TIME  LOGON TIME
>user                  console             1  Active      none   2026/11/6 15:21
```

**判定**：LogonUI + LockApp 同时存在 → **会话锁屏/未解锁跟屏**。上一轮 CopyFromScreen 已误捕 Spotlight 锁屏图并禁止冒充；本轮遵守「若锁屏立刻停止，不要交出锁屏图」。

## 捕获动作

- **未启动** `RemoteAssist-Net48-Setup-0.1.0.exe`
- **未截图**（避免再交黑图/锁屏 Spotlight）
- UIA 语言项此前已 PASS，本文件不重开语言验收

## 历史无效证据（勿当 PASS）

同目录既有 `pv004-wizard-*-085*.png` 等：PrintWindow 约 1247B 黑图；CopyFromScreen ~808KB 为锁屏 Spotlight。均**无效**。

## 阻塞与需要主人做的事

1. 在本机 **DESKTOP-J9F0EKT** 解锁屏幕（结束 LogonUI/LockApp 锁屏态），保持桌面可交互跟屏。
2. 解锁后通知测试/产品再跑一轮：启动 Setup → 向导第一页（标题含「安装 - 鹊桥」）→ 多种 HWND 捕获至非黑非锁屏 PNG → 取消向导 → 更新本 md 为 PASS。

## 自检结论

| 项 | 状态 |
|----|------|
| 可辨认向导 PNG | **无**（未采集） |
| 黑图/锁屏排除 | 本轮未产出图；主动停止 |
| 伪 PASS | **否** |

**总评：缺口 + 阻塞原因 = 本机会话锁屏，需用户解锁跟屏后再取证。**
