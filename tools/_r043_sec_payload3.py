from pathlib import Path

# Agent MainForm — copy + display
p = Path('src-net48/RemoteAssist.Agent/MainForm.cs')
t = p.read_text(encoding='utf-8')

# Label "连接凭证" → oral short + note
t = t.replace(
'''            Text = "连接凭证",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        _credentialLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 100),
            Size = new Size(328, 40),
            Font = new Font("Consolas", 18F, FontStyle.Bold),
''',
'''            Text = "口述短码（不可单独加密连接）",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        _credentialLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 100),
            Size = new Size(328, 40),
            Font = new Font("Consolas", 16F, FontStyle.Bold),
''')

# Primary copy button handler
old_btn = '''        var btnCopyCredential = MakeButton("复制连接凭证", 16, 168, 160);
        btnCopyCredential.BackColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopyCredential.ForeColor = Color.White;
        btnCopyCredential.FlatStyle = FlatStyle.Flat;
        btnCopyCredential.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(FriendConnectGuide.FormatShareClipboard(_host.SessionCode, _host.SessionPassword));
                SetStatus("已复制连接凭证。");
            }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        };
        sessionCard.Controls.Add(btnCopyCredential);'''

new_btn = '''        var btnCopyCredential = MakeButton("复制连接凭证", 16, 168, 160);
        btnCopyCredential.BackColor = Color.FromArgb(0x1A, 0x73, 0xE8);
        btnCopyCredential.ForeColor = Color.White;
        btnCopyCredential.FlatStyle = FlatStyle.Flat;
        btnCopyCredential.Click += (_, _) => CopySecureCredential();
        sessionCard.Controls.Add(btnCopyCredential);'''
if old_btn not in t:
    raise SystemExit('btnCopyCredential block missing')
t = t.replace(old_btn, new_btn, 1)

# Second copy button in action card
old2 = '''        var btnCopyCode = MakeButton("复制连接凭证", 252, 48, 100);
        btnCopyCode.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(FriendConnectGuide.FormatShareClipboard(_host.SessionCode, _host.SessionPassword));
                SetStatus("已复制连接凭证。");
            }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        };'''
new2 = '''        var btnCopyCode = MakeButton("复制连接凭证", 252, 48, 100);
        btnCopyCode.Click += (_, _) => CopySecureCredential();'''
if old2 not in t:
    raise SystemExit('btnCopyCode missing')
t = t.replace(old2, new2, 1)

# RefreshFromHost credential label = oral short only
t = t.replace(
    '_credentialLabel.Text = FriendConnectGuide.FormatShareClipboard(_host.SessionCode, _host.SessionPassword);',
    '_credentialLabel.Text = FriendConnectGuide.FormatConnectionCredential(_host.SessionCode, _host.SessionPassword);',
)

# Subtitle
t = t.replace(
    '接受协助 · 把连接凭证发给对方 · 危险操作需本机确认',
    '接受协助 · 点「复制连接凭证」发完整安全载荷 · 危险操作需本机确认',
)
t = t.replace(
    '把连接凭证发给协助方（对方粘贴后点连接）',
    '点「复制连接凭证」把完整安全载荷发给对方（含业务指纹）；上方短码仅供口述',
)

# Add CopySecureCredential method before UpdateListenLabel or after SetStatus
if 'void CopySecureCredential()' not in t:
    method = '''
    private void CopySecureCredential()
    {
        try
        {
            if (_host.PreferRelay)
            {
                if (!_host.UseTls || string.IsNullOrWhiteSpace(_host.CertificateFingerprint))
                {
                    SetStatus("当前无法生成加密载荷（明文调试或证书未就绪）。");
                    return;
                }
                if (!TlsCertUtil.IsWellFormedFingerprint(_host.CertificateFingerprint))
                {
                    SetStatus("证书指纹异常，请刷新会话后重试。");
                    return;
                }
                // 与 QrPayload / PairQrPayload.BuildRelay 同一形态（含 fp、pwd、relay）
                Clipboard.SetText(_host.QrPayload);
                SetStatus("已复制完整安全连接载荷（请发给协助方一次粘贴）。短码不能单独加密连接。");
                return;
            }

            Clipboard.SetText(_host.QrPayload);
            SetStatus("已复制完整安全连接载荷。");
        }
        catch (Exception ex)
        {
            SetStatus("复制失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
        }
    }

'''
    anchor = '    private void UpdateListenLabel()'
    if anchor not in t:
        raise SystemExit('UpdateListenLabel missing')
    t = t.replace(anchor, method + anchor, 1)

# Tip should not be an issue - local fp display OK
p.write_text(t, encoding='utf-8', newline='\n')
print('Agent MainForm OK')

# Helper UI strings + fail hints
h = Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs')
ht = h.read_text(encoding='utf-8')
ht = ht.replace(
    '粘贴对方发来的连接凭证或会话载荷，点「连接」',
    '粘贴对方「复制连接凭证」的完整安全载荷，点「连接」',
)
ht = ht.replace(
    '连接凭证（会话码-8位验证码）或 remoteassist:pair 载荷',
    '完整安全载荷（remoteassist:pair…，含指纹；不要只贴口述短码）',
)
ht = ht.replace(
    '将通过默认中继连接',
    '请粘贴完整安全载荷后连接',
)

# Update ConnectAsync incomplete / no-fp messages to use constants (already have custom text - align)
old_fail = '''            if (opts.ViaRelay && !opts.Insecure && string.IsNullOrWhiteSpace(opts.Fingerprint))
            {
                SetStatus("无法安全连接");
                SetFailHint(
                    "还缺少安全校验（证书指纹）。「连接凭证」只有会话码和验证码，不能单独走加密通道。"
                    + Environment.NewLine
                    + "下一步：请对方用「复制载荷」发来含指纹的完整连接信息并粘贴；或展开「高级」填写 TLS 指纹后再连接。"
                    + Environment.NewLine
                    + "（仅本机调试可勾选「明文调试」，公网请勿使用。）");
                return;
            }'''
new_fail = '''            if (opts.ViaRelay && !opts.Insecure && !TlsCertUtil.IsWellFormedFingerprint(opts.Fingerprint))
            {
                SetStatus("无法安全连接");
                SetFailHint(
                    FriendConnectGuide.NeedFingerprintHint
                    + Environment.NewLine
                    + "（仅本机调试可勾选「明文调试」，公网请勿使用。）");
                return;
            }'''
if old_fail not in ht:
    if 'NeedFingerprintHint' in ht:
        print('Helper fail already updated')
    else:
        i = ht.find('ViaRelay && !opts.Insecure')
        print(repr(ht[i:i+500]))
        raise SystemExit('helper fail block')
else:
    ht = ht.replace(old_fail, new_fail, 1)

# TryBuildOptions fail default message
ht = ht.replace(
'''            SetFailHint(string.IsNullOrEmpty(err)
                ? "连接凭证不完整，请复制完整的「会话码-8位验证码」"
                : err);''',
'''            SetFailHint(string.IsNullOrEmpty(err)
                ? FriendConnectGuide.NeedSecurePayloadHint
                : err);''')

# catch: redact
ht = ht.replace(
    'SetFailHint(friendly + "\\r\\n下一步：确认对方已点「接受协助」且凭证未刷新，然后重试。");',
    'SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly) + "\\r\\n下一步：请对方重新「复制连接凭证」发完整安全载荷后重试。");',
)
# actual string uses \r\n in source as real newlines in C# verbatim? Check
if 'SetFailHint(friendly +' in ht:
    ht = ht.replace(
        'SetFailHint(friendly + "\\r\\n下一步：确认对方已点「接受协助」且凭证未刷新，然后重试。");',
        'SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly) + Environment.NewLine + "下一步：请对方重新「复制连接凭证」发完整安全载荷后重试。");',
    )
    # try without escape
    import re
    ht2, n = re.subn(
        r'SetFailHint\(friendly \+ "[^"]*下一步：确认对方已点「接受协助」[^"]*"\);',
        'SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly) + Environment.NewLine + "下一步：请对方重新「复制连接凭证」发完整安全载荷后重试。");',
        ht,
        count=1,
    )
    if n:
        ht = ht2
        print('catch redact OK', n)
    else:
        print('catch pattern miss')

h.write_text(ht, encoding='utf-8', newline='\n')
print('Helper MainForm OK')
