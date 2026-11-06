from pathlib import Path
# How does net8 AssistClient handle relay TLS?
t=Path('src/RemoteAssist.Protocol/AssistClient.cs').read_text(encoding='utf-8')
i=t.find('PairViaRelayGuest')
print(t[i:i+1800] if i>=0 else 'missing')
print('===== Agent Start insecure with preferRelay =====')
# Program args for agent
for p in ['src/RemoteAssist.Agent/Program.cs','src/RemoteAssist.App/Program.cs','src-net48/RemoteAssist.App/Program.cs']:
    print('---', p)
    text=Path(p).read_text(encoding='utf-8')
    for n in ['insecure','preferRelay','PreferRelay']:
        if n in text:
            for i,l in enumerate(text.splitlines()):
                if n in l: print(f'{i+1}:{l.strip()}')
# Does HandleStream skip TLS for relay somehow?
t=Path('src/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
i=t.find('private async Task HandleStreamAsync')
block=t[i:i+800]
print('HandleStream head:', block)
# Search for relay insecure or skip tls
for needle in ['relay', 'ownerLink', 'skipTls', 'business']:
    pass
# Look at FinishPair
i=t.find('FinishPair') 
print('FinishPair in agent?', i)
# In AssistClient FinishPairAsync
t2=Path('src/RemoteAssist.Protocol/AssistClient.cs').read_text(encoding='utf-8')
i=t2.find('FinishPair')
print(t2[i:i+900])
