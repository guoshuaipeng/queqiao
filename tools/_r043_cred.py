from pathlib import Path
t=Path('src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs').read_text(encoding='utf-8')
for n in ['FormatShareClipboard','FormatCredential','TryParseShare','ConnectionCredential','8位','复制连接']:
    i=t.find(n)
    print(n, i)
print('--- last 80 lines ---')
print('\n'.join(t.splitlines()[-80:]))
print('--- ConnectDraftParser TryBuild ---')
t2=Path('src-net48/RemoteAssist.Protocol/ConnectDraftParser.cs').read_text(encoding='utf-8')
print(t2[:2500])
