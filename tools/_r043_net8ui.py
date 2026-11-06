from pathlib import Path
t=Path('src/RemoteAssist.Helper.UI/MainForm.cs').read_text(encoding='utf-8')
print('lines', t.count('\n')+1)
for n in ['_credentialBox','_draftBox','_pwdBox','高级','折叠','_advanced', 'FormatCredential', '复制连接凭证', '连接凭证']:
    print(n, t.find(n), t.count(n))
# Agent MainForm net8 credential
a=Path('src/RemoteAssist.Agent/MainForm.cs').read_text(encoding='utf-8')
for n in ['复制连接凭证','FormatShareClipboard','SessionPassword','_pwdLabel','_credential']:
    print('agent', n, a.find(n), a.count(n))
print('--- FormatCredential in Protocol net8 ---')
f=Path('src/RemoteAssist.Protocol/FriendConnectGuide.cs').read_text(encoding='utf-8')
for n in ['FormatShareClipboard','FormatConnectionCredential','TryParseCredential','ConnectionCredential']:
    i=f.find(n)
    print(n,i)
    if i>=0: print(f[i:i+400])
