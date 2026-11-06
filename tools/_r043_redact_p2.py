# -*- coding: utf-8 -*-
from pathlib import Path
ROOT = Path("src-net48")

# --- AgentHost PrintBanner + refresh + start/tls/relay audits ---
p = ROOT / "RemoteAssist.Agent/AgentHost.cs"
t = p.read_text(encoding="utf-8")

repls = [
(
'''_audit.Write("session_refresh", $"code={code}; kicked_all=true");
        Console.WriteLine($"[会话码已刷新] {code}");''',
'''_audit.Write("session_refresh", "kicked_all=true");
        Console.WriteLine("[会话码已刷新]（新凭证仅经界面「复制连接凭证」，日志不输出码/密码/载荷）");'''
),
(
'''        Console.WriteLine($"监听端口 : {Port}");
        Console.WriteLine($"传输加密 : {(_insecure ? "明文 --insecure（仅调试）" : "TLS 1.2/1.3 + 证书指纹钉扎（默认）")}");
        if (!_insecure && _certFingerprint is not null)
            Console.WriteLine($"证书指纹 : {_certFingerprint}");
        Console.WriteLine($"本机地址 : {string.Join(", ", LanIps)}");
        Console.WriteLine($"QR 主机  : {SelectedHost}");
        Console.WriteLine($"会话码   : {SessionCode}");
        Console.WriteLine($"二维码载荷: {QrPayload}");
        Console.WriteLine($"审计日志 : {AuditPath}");
        Console.WriteLine($"确认方式 : {(_autoYes ? "自动(--yes)" : _headless ? "控制台(--headless)" : "本机窗口+托盘(默认)")}");
        Console.WriteLine($"确认超时 : {_confirmTimeoutSeconds}s");
        Console.WriteLine();
        Console.WriteLine("协助端示例:");
        Console.WriteLine($"  dotnet run --project src/RemoteAssist.Helper -- pair {SelectedHost} {SessionCode}");
        Console.WriteLine($"  dotnet run --project src/RemoteAssist.Helper -- pair \\"{QrPayload}\\"");
        Console.WriteLine();''',
'''        Console.WriteLine($"监听端口 : {Port}");
        Console.WriteLine($"传输加密 : {(_insecure ? "明文 --insecure（仅调试）" : "TLS 1.2/1.3 + 证书指纹钉扎（默认）")}");
        Console.WriteLine(_insecure
            ? "证书指纹 : （明文调试模式无）"
            : "证书指纹 : 已就绪（日志不输出；请用界面「复制连接凭证」）");
        Console.WriteLine($"本机地址 : {string.Join(", ", LanIps)}");
        Console.WriteLine($"QR 主机  : {SelectedHost}");
        Console.WriteLine("会话凭证 : 已生成（码/密码/完整载荷不写日志；请用界面复制）");
        Console.WriteLine($"审计日志 : {AuditPath}");
        Console.WriteLine($"确认方式 : {(_autoYes ? "自动(--yes)" : _headless ? "控制台(--headless)" : "本机窗口+托盘(默认)")}");
        Console.WriteLine($"确认超时 : {_confirmTimeoutSeconds}s");
        Console.WriteLine();
        Console.WriteLine("协助端：粘贴被控端「复制连接凭证」得到的完整安全载荷（勿从日志读取凭证）。");
        Console.WriteLine();'''
),
(
'''        if (!_insecure && _serverCert is not null)
            _audit.Write("tls_cert", $"fp={_certFingerprint}");
        else if (_insecure)
            _audit.Write("tls_off", "insecure plaintext");

        _cts = new CancellationTokenSource();
        _audit.Write("agent_start",
            $"relay={_preferRelay}; relayEp={_relayEndpoint}; port={Port}; code={SessionCode}; host={SelectedHost}; tls={(!_insecure).ToString().ToLowerInvariant()}; confirm={(_autoYes ? "auto" : _headless ? "console" : "ui-tray")}; timeout={_confirmTimeoutSeconds}s");''',
'''        if (!_insecure && _serverCert is not null)
            _audit.Write("tls_cert", "ephemeral_cert_ready");
        else if (_insecure)
            _audit.Write("tls_off", "insecure plaintext");

        _cts = new CancellationTokenSource();
        _audit.Write("agent_start",
            $"relay={_preferRelay}; relayEp={_relayEndpoint}; port={Port}; host={SelectedHost}; tls={(!_insecure).ToString().ToLowerInvariant()}; confirm={(_autoYes ? "auto" : _headless ? "console" : "ui-tray")}; timeout={_confirmTimeoutSeconds}s");'''
),
(
'''_audit.Write("relay_guest_code", $"code={c}");''',
'''_audit.Write("relay_guest_code", "assigned");'''
),
(
'''_audit.Write("relay_guest_paired", $"code={code}");''',
'''_audit.Write("relay_guest_paired", "ok");'''
),
(
'''_audit.Write("relay_guest_error", ex.ToString());''',
'''_audit.Write("relay_guest_error", FriendConnectGuide.RedactSecureMaterial(ex.ToString()));'''
),
(
'''_audit.Write("client_error", $"{remote}; {ex.Message}");''',
'''_audit.Write("client_error", $"{remote}; {FriendConnectGuide.RedactSecureMaterial(ex.Message)}");'''
),
(
'''_audit.Write("tool_error", $"tool={tool}; {ex.Message}");''',
'''_audit.Write("tool_error", $"tool={tool}; {FriendConnectGuide.RedactSecureMaterial(ex.Message)}");'''
),
]

for a,b in repls:
    if a not in t:
        raise SystemExit('AgentHost missing block:\n' + a[:120])
    t = t.replace(a,b)
# client_error appears twice — replace already did both if identical
count_ce = t.count('_audit.Write("client_error"')
print('client_error writes', count_ce)
# verify no remaining QrPayload in Console
if 'QrPayload}' in t or 'QrPayload)"' in t or '{QrPayload}' in t:
    # still used as property elsewhere — check Console lines only
    for i,line in enumerate(t.splitlines(),1):
        if 'Console.' in line and 'QrPayload' in line:
            raise SystemExit(f'still Console QrPayload L{i}: {line}')
        if 'Console.' in line and 'SessionCode' in line:
            raise SystemExit(f'still Console SessionCode L{i}: {line}')
        if 'Console.' in line and '_certFingerprint' in line:
            raise SystemExit(f'still Console fp L{i}: {line}')
p.write_text(t, encoding='utf-8', newline='\n')
print('AgentHost OK')

# --- Agent MainForm status/error redact ---
p = ROOT / "RemoteAssist.Agent/MainForm.cs"
t = p.read_text(encoding="utf-8")
repls2 = [
(
'''catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }''',
'''catch (Exception ex) { SetStatus("复制失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message)); }'''
),
(
'''SetStatus("二维码生成失败：" + ex.Message);''',
'''SetStatus("二维码生成失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));'''
),
(
'''SetStatus(outcome.Message);''',
'''SetStatus(FriendConnectGuide.RedactSecureMaterial(outcome.Message));'''
),
]
for a,b in repls2:
    if a not in t:
        raise SystemExit('Agent MainForm missing: '+a)
    t = t.replace(a,b,1)
p.write_text(t, encoding='utf-8', newline='\n')
print('Agent MainForm OK')

# --- Agent Program.cs ---
p = ROOT / "RemoteAssist.Agent/Program.cs"
t = p.read_text(encoding="utf-8")
old = '''try { Console.WriteLine($"[主界面失败] {ex}"); } catch { /* ignore */ }
                try
                {
                    MessageBox.Show(ex.ToString(), "鹊桥启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }'''
new = '''try { Console.WriteLine("[主界面失败] " + FriendConnectGuide.RedactSecureMaterial(ex.ToString())); } catch { /* ignore */ }
                try
                {
                    MessageBox.Show(FriendConnectGuide.RedactSecureMaterial(ex.ToString()), "鹊桥启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }'''
if old not in t:
    # show nearby
    idx = t.find('主界面失败')
    print(repr(t[idx-50:idx+200]))
    raise SystemExit('Agent Program fail block not found')
# need using?
if 'using RemoteAssist.Protocol;' not in t:
    t = 'using RemoteAssist.Protocol;\n' + t
t = t.replace(old, new)
p.write_text(t, encoding='utf-8', newline='\n')
print('Agent Program OK')

# --- ConfirmPrompt ---
p = ROOT / "RemoteAssist.Agent/ConfirmPrompt.cs"
t = p.read_text(encoding="utf-8")
t2 = t.replace(
    'Console.WriteLine($"[确认窗口失败，回退控制台] {ex.Message}");',
    'Console.WriteLine("[确认窗口失败，回退控制台] " + FriendConnectGuide.RedactSecureMaterial(ex.Message));'
).replace(
    'Console.WriteLine($"[配对窗口失败，回退控制台] {ex.Message}");',
    'Console.WriteLine("[配对窗口失败，回退控制台] " + FriendConnectGuide.RedactSecureMaterial(ex.Message));'
)
if t2 == t:
    raise SystemExit('ConfirmPrompt no change')
if 'using RemoteAssist.Protocol;' not in t2:
    t2 = t2.replace('namespace RemoteAssist.Agent;', 'using RemoteAssist.Protocol;\n\nnamespace RemoteAssist.Agent;')
p.write_text(t2, encoding='utf-8', newline='\n')
print('ConfirmPrompt OK')

# --- McpConfigCopyHelper ---
p = ROOT / "RemoteAssist.Agent/McpConfigCopyHelper.cs"
t = p.read_text(encoding="utf-8")
old = 'return new Outcome(OutcomeKind.Failed, false, "复制失败：" + ex.Message, "", payload);'
new = 'return new Outcome(OutcomeKind.Failed, false, "复制失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message), "", payload);'
if old not in t:
    raise SystemExit('McpConfigCopyHelper fail msg not found')
p.write_text(t.replace(old, new), encoding='utf-8', newline='\n')
print('McpConfigCopyHelper OK')

# --- Helper.UI MainForm ---
p = ROOT / "RemoteAssist.Helper.UI/MainForm.cs"
t = p.read_text(encoding="utf-8")
repls3 = [
(
'''SetStatus("读取剪贴板失败：" + ex.Message);''',
'''SetStatus("读取剪贴板失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));'''
),
(
'''AppendOutput("[" + tool + "] ERR: " + ex.Message + "\\r\\n");''',
'''AppendOutput("[" + tool + "] ERR: " + FriendConnectGuide.RedactSecureMaterial(ex.Message) + "\\r\\n");'''
),
(
'''SetStatus("调用失败：" + FriendConnectGuide.FriendlyOrRaw(ex));''',
'''SetStatus("调用失败：" + FriendConnectGuide.FriendlyOrRaw(ex));'''  # already friendly; keep
),
]
changed = 0
for a,b in repls3:
    if a == b:
        continue
    if a not in t:
        raise SystemExit('Helper missing: '+repr(a))
    t = t.replace(a,b)
    changed += 1
p.write_text(t, encoding='utf-8', newline='\n')
print('Helper MainForm OK', changed)

# --- App Program if any ex dump ---
p = ROOT / "RemoteAssist.App/Program.cs"
t = p.read_text(encoding="utf-8")
# check for Console with ex
if 'Console.WriteLine' in t and 'ex' in t:
    print('App Program has Console - inspect')
    for i,l in enumerate(t.splitlines(),1):
        if 'Console' in l:
            print(i, l)
else:
    print('App Program no ex Console')

print('phase2 done')
