from pathlib import Path
p = Path('docs/round-043.md')
t = p.read_text(encoding='utf-8')
t = t.replace(
    '> 状态：**blocked（安全闸门未通过 — 公网中继业务明文；暂不验收）**',
    '> 状态：**blocked（P0 明文回退已移除；短凭证无指纹 → 安全主路径未闭环；暂不验收）**',
    1,
)
fix = '''

## P0 安全纠错（2026-11-06 Asia/Shanghai，同轮授权）

已按安全核查 P0 **仅改 `src-net48/`**（未动 net8 既有 Helper force-Insecure 问题）：

1. `AgentHost.RelayGuestLoopAsync`：`skipTls: false`，非 `--insecure` 时对中继 pipe `AuthenticateAsServerAsync`。
2. `AgentHost.QrPayload`（preferRelay）：恢复 `useTls: true` + 证书指纹；仅显式 `_insecure` 才 `useTls: false`。
3. `Helper.UI/MainForm.ConnectAsync`：**删除**无指纹自动 `Insecure=true`；公网/中继无指纹时**安全阻断**并人话引导「复制载荷」(含 fp) 或高级填指纹；明文仅当用户勾选「明文调试」。
4. **未**改中继 JSONL、未自创 TOFU/证书分发、**未**把主分享「连接凭证」`{码}-{8位}` 改成另一格式。

### 仍 blocked 的原因（产品需后续拍板）

- 短连接凭证**不携带指纹** → 协助端默认无法在无高级/无载荷情况下完成 **architecture 要求的业务 TLS**。
- 因此「只粘贴短凭证即公网加密连上」**尚未闭环**；当前安全默认是**阻断**而非明文连通。
- **不提请验收。**

### 后续（非本提交）

- 产品选：主路径是否改为一次粘贴含 `fp=` 的载荷，或架构书面同意的其它不降明文方案。
- net8 Helper 仍存在「无指纹 force Insecure」既有问题，本轮**未改**，单列跟进。
- S005 / Avalonia·CLI 凭证 UI → R044。

'''
# Insert after security section or before 参考
if '## P0 安全纠错' not in t:
    if '### 本轮验收' in t and '不提请验收' in t:
        # append after 本轮验收 paragraph block
        marker = '**不提请验收。** 实现中的凭证/UI/首启人话可保留为后续修补基线，但 **5996705 含公网业务明文，安全闸门未通过。**'
        if marker in t:
            t = t.replace(marker, marker + fix, 1)
        else:
            t = t.replace('## 参考', fix + '\n## 参考', 1)
    else:
        t = t.replace('## 参考', fix + '\n## 参考', 1)
p.write_text(t, encoding='utf-8', newline='\n')

# Soften old FAIL table header to note P0 applied
if '闸门 | **未通过** — 不得请本轮产品验收' in t:
    t = p.read_text(encoding='utf-8')
    t = t.replace(
        '| 闸门 | **未通过** — 不得请本轮产品验收 |',
        '| 闸门 | **曾未通过（明文）**；P0 已纠错移除明文回退；**短凭证闭环未完成 → 仍 blocked，不验收** |',
        1)
    t = t.replace(
        '| 公网中继配对后业务流 | **明文**（本轮 net48 主动跳过业务 TLS） |',
        '| 公网中继配对后业务流 | P0 后：非 `--insecure` 时恢复业务 TLS；短凭证无 fp 则协助端**安全阻断**（非明文放行） |',
        1)
    p.write_text(t, encoding='utf-8', newline='\n')
print('docs OK')

rp = Path('docs/roadmap.md')
rt = rp.read_text(encoding='utf-8')
rt2 = rt.replace(
    'R043 blocked（安全闸门：公网业务明文未过）',
    'R043 blocked（P0 明文已撤；短凭证无 fp 主路径未闭环，不验收）',
    1)
if rt2 != rt:
    rp.write_text(rt2, encoding='utf-8', newline='\n')
    print('roadmap OK')
