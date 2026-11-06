from pathlib import Path
p=Path('src-net48/RemoteAssist.Agent/MainForm.cs')
t=p.read_text(encoding='utf-8')
old='''    private void UpdateListenLabel()
    {
        var tls = _host.UseTls ? "TLS 开" : "明文";
        var n = _host.ListPairedClients().Count;
        _listenValue.Text = $"监听 {_host.Port} · {tls} · 已配对 {n} 个";
        _headerSubtitle.Text = n == 0
            ? "接受协助 · 把连接凭证发给对方 · 危险操作需本机确认"
            : $"被协助端 · 已有 {n} 个协助方连接 · 可随时断开";
    }'''
new='''    private void UpdateListenLabel()
    {
        var n = _host.ListPairedClients().Count;
        if (_host.PreferRelay)
        {
            var phase = _host.RelayPhase switch
            {
                "dialing" => "正在连接中继",
                "ready" or "waiting" => "等待对方连接",
                "paired" => "对方已连上",
                "error" => "中继异常（将自动重试）",
                _ => "中继准备中"
            };
            _listenValue.Text = phase + " · 已配对 " + n + " 个";
        }
        else
        {
            var tls = _host.UseTls ? "TLS 开" : "明文";
            _listenValue.Text = $"监听 {_host.Port} · {tls} · 已配对 {n} 个";
        }
        _headerSubtitle.Text = n == 0
            ? "接受协助 · 把连接凭证发给对方 · 危险操作需本机确认"
            : $"被协助端 · 已有 {n} 个协助方连接 · 可随时断开";
    }'''
if old not in t:
    raise SystemExit('UpdateListenLabel missing')
t=t.replace(old,new,1)
t=t.replace(
    '_host.SessionChanged -= OnSessionChanged;',
    '_host.RelayStatusChanged -= OnSessionChanged;\n            _host.SessionChanged -= OnSessionChanged;')
# TLS label when preferRelay plaintext business - show 中继通道
t=t.replace(
'''        if (_host.UseTls && _host.CertificateFingerprint is { } fp)
        {
            var shortFp = fp.Length > 23 ? fp[..10] + "…" + fp[^8..] : fp;
            _tlsValue.Text = shortFp;
            _tlsValue.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
            Tip().SetToolTip(_tlsValue, fp);
        }
        else
        {
            _tlsValue.Text = "明文模式（--insecure）";
            _tlsValue.ForeColor = Color.DarkOrange;
        }''',
'''        if (_host.PreferRelay)
        {
            _tlsValue.Text = "公网中继（码+密码）";
            _tlsValue.ForeColor = Color.FromArgb(0x0F, 0x9D, 0x58);
            Tip().SetToolTip(_tlsValue, _host.RelayEndpointAddress);
        }
        else if (_host.UseTls && _host.CertificateFingerprint is { } fp)
        {
            var shortFp = fp.Length > 23 ? fp[..10] + "…" + fp[^8..] : fp;
            _tlsValue.Text = shortFp;
            _tlsValue.ForeColor = Color.FromArgb(0x3C, 0x40, 0x43);
            Tip().SetToolTip(_tlsValue, fp);
        }
        else
        {
            _tlsValue.Text = "明文模式（--insecure）";
            _tlsValue.ForeColor = Color.DarkOrange;
        }''')
p.write_text(t,encoding='utf-8',newline='\n')
print('UpdateListenLabel OK')
