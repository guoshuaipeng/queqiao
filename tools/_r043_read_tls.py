from pathlib import Path
t=Path('src/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
idx=t.find('SetRelayPhase("paired"')
print('idx', idx)
print(t[idx:idx+1500])
idx2=t.find('AuthenticateAsServerAsync')
# find relay related TLS after guest
for needle in ['relay.Stream', 'AuthenticateAsServer', 'HandleClient', 'paired']:
    i = 0
    while True:
        j = t.find(needle, i)
        if j < 0: break
        if 'relay' in t[max(0,j-80):j+80].lower() or needle=='AuthenticateAsServer':
            line = t[:j].count('\n')+1
            if 400 < line < 550 or 'relay' in t[j:j+100].lower():
                print(line, t[j:j+120].replace('\n',' '))
        i = j+1
        if i > len(t): break
