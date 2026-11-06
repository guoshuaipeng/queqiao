# -*- coding: utf-8 -*-
from pathlib import Path
p = Path('src-net48/RemoteAssist.Agent/MainForm.cs')
t = p.read_text(encoding='utf-8')

# Add using Protocol if missing
if 'using RemoteAssist.Protocol;' not in t:
    t = t.replace('using QRCoder;', 'using QRCoder;\nusing RemoteAssist.Protocol;')

# Add _pwdLabel field after _codeLabel
if 'private readonly Label _pwdLabel;' not in t:
    t = t.replace(
        'private readonly Label _codeLabel;',
        'private readonly Label _codeLabel;\n    private readonly Label _pwdLabel;\n    private readonly Label _credentialLabel;\n    private readonly Label _relayStatusLabel;')

# Subtitle
t = t.replace(
    '被协助端 · 局域网会话码配对 · 危险操作需本机确认',
    '接受协助 · 把连接凭证发给对方 · 危险操作需本机确认')

# Session card intro
t = t.replace(
    '把会话码告诉协助方，或让对方扫二维码',
    '把连接凭证发给协助方（对方粘贴后点连接）')

# Find copy button area - look for 复制会话码 or similar
import re
# Show what copy buttons exist
for i,l in enumerate(t.splitlines()):
    if '复制' in l or '_codeLabel.Text' in l or 'RefreshUi' in l or 'btnCopy' in l:
        print(f'{i+1}:{l[:120]}')
