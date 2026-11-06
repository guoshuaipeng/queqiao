# -*- coding: utf-8 -*-
from pathlib import Path
p = Path('src-net48/RemoteAssist.Agent/MainForm.cs')
t = p.read_text(encoding='utf-8')

if 'using RemoteAssist.Protocol;' not in t:
    t = t.replace('using QRCoder;', 'using QRCoder;\nusing RemoteAssist.Protocol;')

if 'private readonly Label _credentialLabel;' not in t:
    t = t.replace(
        '    private readonly Label _codeLabel;\n',
        '    private readonly Label _codeLabel;\n    private readonly Label _credentialLabel;\n    private readonly Label _relayStatusLabel;\n')

t = t.replace(
    '被协助端 · 局域网会话码配对 · 危险操作需本机确认',
    '接受协助 · 把连接凭证发给对方 · 危险操作需本机确认')
t = t.replace(
    '把会话码告诉协助方，或让对方扫二维码',
    '把连接凭证发给协助方（对方粘贴后点连接）')

# After _codeLabel creation, shrink it and add credential label
old_code = '''        _codeLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 100),
            Size = new Size(328, 52),
            Font = new Font("Consolas", 32F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            BackColor = Color.FromArgb(0xEE, 0xF3, 0xFF),
            Text = ""
        };
        sessionCard.Controls.Add(_codeLabel);'''

new_code = '''        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 78),
            Size = new Size(328, 20),
            Text = "连接凭证",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

        _credentialLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 100),
            Size = new Size(328, 40),
            Font = new Font("Consolas", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0x20, 0x24, 0x27),
            BackColor = Color.FromArgb(0xEE, 0xF3, 0xFF),
            Text = ""
        };
        sessionCard.Controls.Add(_credentialLabel);

        _codeLabel = new Label
        {
            AutoSize = false,
            Location = new Point(16, 142),
            Size = new Size(160, 22),
            Font = new Font("Consolas", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = ""
        };
        sessionCard.Controls.Add(_codeLabel);

        _relayStatusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(180, 142),
            Size = new Size(164, 22),
            Font = new Font(Font.FontFamily, 8.5F),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68),
            Text = ""
        };
        sessionCard.Controls.Add(_relayStatusLabel);

        var btnCopyCredential = MakeButton("复制连接凭证", 16, 168, 160);
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

# The session card already has a "会话码" label at y=78 - need to remove duplicate
# First remove the old 会话码 label block before _codeLabel
old_sess_lbl = '''        sessionCard.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(16, 78),
            Size = new Size(328, 20),
            Text = "会话码",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        });

'''
if old_sess_lbl in t:
    t = t.replace(old_sess_lbl, '', 1)
    print('removed 会话码 label')

if old_code not in t:
    raise SystemExit('code label block missing')
t = t.replace(old_code, new_code, 1)
print('credential UI inserted')

# Move QR down a bit - change Location 164 -> 200
t = t.replace(
    '''        _qrBox = new PictureBox
        {
            Location = new Point(70, 164),
            Size = new Size(220, 220),''',
    '''        _qrBox = new PictureBox
        {
            Location = new Point(70, 200),
            Size = new Size(200, 180),''')

# TLS fingerprint label y 392 -> 388 still ok if card tall enough - session card height 420
# action card: change primary copy
t = t.replace(
    '''        var btnCopyCode = MakeButton("复制会话码", 252, 48, 84);
        btnCopyCode.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(_host.SessionCode);
                SetStatus("已复制会话码。");
            }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        };
        actionCard.Controls.Add(btnCopyPayload);
        actionCard.Controls.Add(btnCopyMcp);
        actionCard.Controls.Add(btnCopyCode);''',
    '''        var btnCopyCode = MakeButton("复制连接凭证", 252, 48, 100);
        btnCopyCode.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(FriendConnectGuide.FormatShareClipboard(_host.SessionCode, _host.SessionPassword));
                SetStatus("已复制连接凭证。");
            }
            catch (Exception ex) { SetStatus("复制失败：" + ex.Message); }
        };
        actionCard.Controls.Add(btnCopyPayload);
        actionCard.Controls.Add(btnCopyMcp);
        actionCard.Controls.Add(btnCopyCode);''')

# RefreshUi for code
t = t.replace(
    '        _codeLabel.Text = _host.SessionCode;',
    '''        _codeLabel.Text = "码 " + _host.SessionCode;
        if (_credentialLabel is not null)
            _credentialLabel.Text = FriendConnectGuide.FormatShareClipboard(_host.SessionCode, _host.SessionPassword);
        if (_relayStatusLabel is not null && _host.PreferRelay)
        {
            var phase = _host.RelayPhase;
            _relayStatusLabel.Text = phase switch
            {
                "dialing" => "正在连接中继…",
                "ready" or "waiting" => "等待对方连接",
                "paired" => "对方已连上",
                "error" => "中继异常",
                _ => "中继准备中"
            };
        }
        if (_host.PreferRelay)
            _listenValue.Text = "中继 " + _host.RelayEndpointAddress + " · " + _host.RelayPhase;
''')

# Subscribe RelayStatusChanged in ctor - find SessionChanged +=
if 'RelayStatusChanged' not in t:
    t = t.replace(
        '_host.SessionChanged +=',
        '_host.RelayStatusChanged += () => BeginInvokeSafe(RefreshSessionUi);\n        _host.SessionChanged +=')
    # might be different pattern
    if 'RelayStatusChanged' not in t:
        # find where SessionChanged is hooked
        for i,l in enumerate(t.splitlines()):
            if 'SessionChanged' in l:
                print('SessionChanged line', i+1, l)

p.write_text(t, encoding='utf-8', newline='\n')
print('Agent MainForm patched size', len(t))
