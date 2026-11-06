from pathlib import Path

# Extract net8 RelayGuestLoop + related fields from AgentHost
t = Path('src/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
# fields around session password
for needle in ['_sessionPassword', 'SessionPassword', 'PreferRelay', '_preferRelay', '_relayEndpoint', 'RelayGuestLoopAsync', 'GuestHostAsync', 'RefreshSessionIdentity', 'public string SessionCode']:
    i = t.find(needle)
    print('---', needle, i)
    if i>=0:
        print(t[max(0,i-200):i+800])
        print()
