from pathlib import Path
p = Path('docs/round-043.md')
t = p.read_text(encoding='utf-8')

# Status line
import re
t = re.sub(
    r'> 状态：\*\*.*?\*\*',
    '> 状态：**blocked（P0 明文回退已移除；短凭证无业务指纹 → 安全主路径未闭环；产品「内置官方指纹」前提不成立；暂不验收）**',
    t,
    count=1,
)

block = '''

## 信任模型核实（只读，2026-11-06 Asia/Shanghai）

产品设想：默认公网中继可「客户端内置/随版本发布官方 TLS 指纹」，短码+8 位仅会话授权。

**核实结论：该前提对「业务握手钉扎」不成立，禁止按「官方中继指纹」接线。**

| 问题 | 证据 |
|------|------|
| 仓库有无已发布、可内置的官方业务指纹常量？ | **无**（检索 `OfficialFp`/`TrustedFp`/官方指纹等无命中；`relay-deploy.md` 只给地址 `121.196.45.81:17891`，无证书指纹表） |
| 中继控制面是否 TLS？ | **否**。`relay/internal/relay/server.go`：`net.Listen("tcp", ...)` 明文 JSONL；部署文档写明「中继只做 JSONL → 透明 pipe」「本阶段不做中继侧 TLS 终结业务流量」 |
| 载荷 `fp=` / 钉扎对象是谁？ | **被控 Agent 进程内临时自签业务证书**，非中继证书。`AgentHost` 构造：`TlsCertUtil.CreateEphemeralServerCert()` → `Sha256Fingerprint`；`PairQrPayload.BuildRelay(..., fp, useTls:true)`；`TlsTransport.AuthenticateAsClientAsync` 校验对端业务证 |
| 能否用「中继官方证书指纹」钉扎业务 TLS？ | **不能**。中继无业务 TLS 终结；每台/每次启动 Agent 证书不同；钉错身份等于不安全或必失败 |

因此：**短连接凭证（无 fp）不能靠「内置中继指纹」完成 architecture 要求的业务 TLS。** 自定义中继同样需要完整含 `fp=` 的安全载荷或显式高级指纹。无指纹 → **阻断**；无 TOFU、无明文静默降级。

'''

p0 = '''

## P0 安全纠错落地（同轮授权 · 仅 src-net48）

已移除本轮引入的明文回退（未改 net8 Helper 既有 force-Insecure 债）：

1. `AgentHost.RelayGuestLoopAsync`：`skipTls: false`；非 `--insecure` 时业务 `AuthenticateAsServerAsync`。
2. `QrPayload` preferRelay：非 insecure 时 `useTls: true` + **本机 Agent 业务证书指纹**（非中继指纹）。
3. `Helper.UI/MainForm.ConnectAsync`：删除无指纹自动 `Insecure=true`；无指纹 → 「无法安全连接」人话，引导「复制载荷」或高级填指纹；明文仅显式「明文调试」。
4. 主分享仍为短凭证 `{码}-{8位}`（**未**擅自改成另一主格式）；并写明短凭证**不携带**业务指纹 → 默认协助主路径仍 **blocked**（安全默认，非明文放行）。
5. 未发明指纹/共享私钥/新协议字段。

**状态仍 blocked，不提请「真实连接」验收。** 产品可做文档/编译核对；真实联调须隔离环境且须完整安全载荷。

'''

if '## 信任模型核实' not in t:
    if '## 参考' in t:
        t = t.replace('## 参考', block + p0 + '\n## 参考', 1)
    else:
        t = t + block + p0
else:
    if '## P0 安全纠错落地' not in t:
        t = t.replace('## 参考', p0 + '\n## 参考', 1)

# Update table rows if old plaintext claims remain
t = t.replace(
    '| 闸门 | **未通过** — 不得请本轮产品验收 |',
    '| 闸门 | P0 后明文回退已撤；短凭证闭环未完成 → **仍 blocked，不验收** |',
)
t = t.replace(
    '| 公网中继配对后业务流 | **明文**（本轮 net48 主动跳过业务 TLS） |',
    '| 公网中继配对后业务流 | P0：恢复业务 TLS；无 fp 则协助端阻断（非明文） |',
)

p.write_text(t, encoding='utf-8', newline='\n')
print('docs updated, len', len(t))

rp = Path('docs/roadmap.md')
rt = rp.read_text(encoding='utf-8')
for a,b in [
    ('R043 blocked（安全闸门：公网业务明文未过）', 'R043 blocked（P0 明文已撤；短凭证无业务 fp，主路径未闭环）'),
    ('R043 blocked（P0 明文已撤；短凭证无 fp 主路径未闭环，不验收）', 'R043 blocked（P0 明文已撤；短凭证无业务 fp，主路径未闭环）'),
]:
    if a in rt:
        rt = rt.replace(a, b, 1)
        print('roadmap replaced')
        break
else:
    if 'R043 blocked' not in rt:
        print('roadmap may need manual; snippet:')
        for i,l in enumerate(rt.splitlines()):
            if 'R043' in l and '当前' in l:
                print(i+1, l[:140])
rp.write_text(rt, encoding='utf-8', newline='\n')
