from pathlib import Path
from datetime import datetime

r = Path('docs/round-043.md')
t = r.read_text(encoding='utf-8')
t = t.replace('> 状态：**wip**', '> 状态：**client-done（待产品验收；本交付未含用户本机运行截图）**', 1)

delivery = '''

## 客户端交付说明（2026-11-06 Asia/Shanghai）

### 改动摘要（仅 `src-net48/` + 本轮文档）

1. **协助端主路径**：单框粘贴连接凭证 / `remoteassist:pair` 载荷 +「连接」；Host/Port/TLS/中继在「高级」默认折叠；去掉「即将支持·仍走局域网」。
2. **默认公网中继**：`RelayEndpoint.DefaultPublic = 121.196.45.81:17891`；`HelperUiSettings.PreferRelay=true`。
3. **品牌**：窗口 / Product「鹊桥」；去掉「远端协助」产品名。
4. **接受协助启动**：
   - **实测根因**（用户本机 ui-errors.log，约束收紧前）：`ConfirmPrompt.EnsureWinFormsInitialized` 在首页 Form 已创建后再次调用 `Application.SetCompatibleTextRenderingDefault`，抛出 `InvalidOperationException`（「在创建第一个 IWin32Window 之前必须调用…」），`AppShell` 原逻辑 `MessageBox.Show(ex.ToString(), "启动被控模式失败")` 把堆栈甩给用户。
   - **修复**：`EnsureWinFormsInitialized` 捕获该异常（对齐 net8）；`AppShell` 改为人话 + 下一步，堆栈只追加 `%LocalAppData%\\RemoteAssist\\ui-errors.log`。
   - **另**：被控默认 `preferRelay` + `guest_host` 中继环（`AgentHost.RelayGuestLoopAsync`），LAN 监听默认跳过——属主路径接线，**不是**未复现的「端口占用」臆测。
5. **连接凭证（Protocol）**：`SessionPasswordGenerator` 新码固定 8 位数字（安全随机、保留前导零）；`FormatShareClipboard` → `{码}-{验证码}`；`TryParseShareText` 先按最后一个 `-` 拆分再 Normalize；兼容旧两行；残缺文案固定「连接凭证不完整，请复制完整的「会话码-8位验证码」」。被控主按钮「复制连接凭证」。
6. **公网接线**：移植 `RelayClient` / `RelayEndpoint` / `AssistClient.PairViaRelayGuest` / Agent `guest_host` 环；**未改中继 JSONL 字段**。

### 开放问题结论

| # | 结论 |
|---|------|
| 1 生成 | 改前 net48 无独立 SessionPassword；net8 仍为默认 4 位字母数字。本轮 net48 新码 **8 位数字**。 |
| 2 中继限长 | `relay/internal/relay/store.go`：`minPasswordLen=4`，guest 无最大长度/字符集限制；字符串哈希比对。**8 位数字可直接 guest_host/guest_join**（约束收紧前曾对公网 `121.196.45.81:17891` 实测 ok；本交付按新硬约束未再对本机发起连接）。 |
| 3 迁移 | 协助端仍解析旧两行与 ≥4 位旧码；刷新后旧凭证失效（既有语义）。 |
| 4 拆分 | 单行无冒号时按 **最后一个** `-` 拆：左侧会话码、右侧验证码，再分别 Normalize。 |

### 构建产物

- 命令：`powershell -File scripts/build-net48.ps1`（**仅编译/publish，不自动启动**）
- 目录：`artifacts/app-net48-fd/`
- 入口：`artifacts/app-net48-fd/RemoteAssist.Net48.exe`（约 19.5 KB；整夹约 1.48 MB）
- 依赖：本机已装 .NET Framework 4.8

### 验证实际执行 / 未做

**已做（本交付）**

- 用户本机：**仅** `dotnet`/`build-net48.ps1` 无 UI 编译 publish；编译通过。
- 隔离 box：连接凭证格式/解析/旧两行/残缺语义的 Python 对拍；8 位数字生成前导零抽样。
- 源码核对：中继 `minPasswordLen=4`、无 max；根因堆栈证据见上（来自约束收紧前本机 `ui-errors.log`）。

**未做（新硬约束：禁止用户本机启动 exe / 联调 / 弹窗 / ui-shot）**

- 用户本机运行验收与 `docs/screenshots/r043-net48-*` **本交付不附截图**（不冒充旧图）。
- 端到端经公网中继双端配对（本交付未再对本机发起）。
- Avalonia / CLI UI 扫尾 → **R044**。
- S005 中途掉线人话 → **R044**。
- 打安装包 / 部署 → 不做。

### 非用户本机复现步骤（供隔离 Windows / 产品机）

1. checkout 本 commit；`powershell -File scripts/build-net48.ps1`
2. 在**非用户日常机**运行 `artifacts/app-net48-fd/RemoteAssist.Net48.exe`
3. 「去协助」：确认首屏仅凭证框+连接，高级默认收起，状态含默认中继 `121.196.45.81:17891`
4. 「接受协助」：应出 `{码}-{8位}` +「复制连接凭证」；不应再出「启动被控模式失败」+堆栈
5. 双端经默认中继粘贴凭证试连；残缺粘贴应出固定人话

'''

if '## 客户端交付说明' not in t:
    # append before 参考 or at end
    if '## 参考' in t:
        t = t.replace('## 参考', delivery + '\n## 参考', 1)
    else:
        t = t + delivery
r.write_text(t, encoding='utf-8', newline='\n')
print('round-043 updated')

# roadmap brief
rp = Path('docs/roadmap.md')
rt = rp.read_text(encoding='utf-8')
if 'R043' in rt and 'client-done' not in rt:
    # light touch: find R043 line
    lines = rt.splitlines()
    for i,l in enumerate(lines):
        if 'R043' in l or 'round-043' in l:
            if 'wip' in l.lower() or '进行' in l or '开放' in l:
                lines[i] = l.replace('wip', 'client-done').replace('进行中', 'client-done待验收')
            print('roadmap hit', i+1, lines[i][:120])
    rp.write_text('\n'.join(lines) + '\n', encoding='utf-8', newline='\n')
else:
    print('roadmap skip or already marked', 'R043' in rt)
