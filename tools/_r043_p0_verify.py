from pathlib import Path

# Verify AgentHost QrPayload block complete
t=Path('src-net48/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
i=t.find('if (_preferRelay)')
print('=== QrPayload preferRelay ===')
print(t[i:i+750])
print('=== skipTls call ===')
j=t.find('skipTls:')
print(t[j-120:j+80])

# Helper early return
h=Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs').read_text(encoding='utf-8')
print('=== Helper block ===')
k=h.find('无法安全连接')
print(h[k-400:k+550] if k>=0 else 'MISSING')

# AssistClient message
a=Path('src-net48/RemoteAssist.Protocol/AssistClient.cs').read_text(encoding='utf-8')
print('=== AssistClient no-fp ===')
for needle in ['将自动用安全通道', '加密连接需要证书指纹', '短连接凭证']:
    print(needle, a.find(needle))
