from pathlib import Path

# Agent MainForm TLS label for preferRelay — show fp again, not "码+密码" plaintext claim
p = Path('src-net48/RemoteAssist.Agent/MainForm.cs')
t = p.read_text(encoding='utf-8')
old = '''        if (_host.PreferRelay)
        {
            _tlsValue.Text = "公网中继（码+密码）";
            _tlsValue.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            Tip().SetToolTip(_tlsValue, _host.RelayEndpointAddress);
        }
        else if (_host.UseTls && _host.CertificateFingerprint is { } fp)'''
new = '''        if (_host.PreferRelay && _host.UseTls && _host.CertificateFingerprint is { } fpRelay)
        {
            var shortFp = fpRelay.Length > 23 ? fpRelay[..10] + "…" + fpRelay[^8..] : fpRelay;
            _tlsValue.Text = "中继 TLS · " + shortFp;
            _tlsValue.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            Tip().SetToolTip(_tlsValue, "业务 TLS 指纹（完整载荷含 fp=）：" + fpRelay + " · 中继 " + _host.RelayEndpointAddress);
        }
        else if (_host.PreferRelay && !_host.UseTls)
        {
            _tlsValue.Text = "中继明文（--insecure 调试）";
            _tlsValue.ForeColor = Color.DarkOrange;
            Tip().SetToolTip(_tlsValue, _host.RelayEndpointAddress);
        }
        else if (_host.UseTls && _host.CertificateFingerprint is { } fp)'''
if old not in t:
    raise SystemExit('tls label block missing: ' + repr(t[t.find('PreferRelay'):t.find('PreferRelay')+400] if 'PreferRelay' in t else 'none'))
t = t.replace(old, new, 1)
p.write_text(t, encoding='utf-8', newline='\n')
print('Agent MainForm TLS label OK')

# AssistClient: clarify relay no-fp error (not "将自动用安全通道")
ac = Path('src-net48/RemoteAssist.Protocol/AssistClient.cs')
at = ac.read_text(encoding='utf-8')
old_msg = '''                if (string.IsNullOrWhiteSpace(fp))
                    throw new InvalidOperationException(
                        "局域网 TLS 需要证书指纹：请展开「高级」填写，或使用默认中继连接（将自动用安全通道）。");'''
new_msg = '''                if (string.IsNullOrWhiteSpace(fp))
                    throw new InvalidOperationException(
                        "加密连接需要证书指纹：请粘贴含 fp= 的完整载荷，或展开「高级」填写 TLS 指纹（短连接凭证不含指纹）。");'''
if old_msg not in at:
    # maybe already different
    if '将自动用安全通道' in at:
        raise SystemExit('msg variant')
    print('AssistClient msg already updated or absent')
else:
    at = at.replace(old_msg, new_msg, 1)
    ac.write_text(at, encoding='utf-8', newline='\n')
    print('AssistClient msg OK')
