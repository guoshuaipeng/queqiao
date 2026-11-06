from pathlib import Path
t=Path('src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs').read_text(encoding='utf-8')
i=t.find('R043：单行')
print(t[i:i+700] if i>=0 else 'credential block missing')
i=t.find('FormatConnectionCredential')
print('fmt', t[i:i+250])
