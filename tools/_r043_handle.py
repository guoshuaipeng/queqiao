from pathlib import Path
t=Path('src-net48/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
i=t.find('private async Task HandleClientAsync')
print(t[i:i+4500])
print('===== NET8 HandleStreamAsync sig =====')
t2=Path('src/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
i=t2.find('private async Task HandleStreamAsync')
print(t2[i:i+2000])
# Does net48 Agent have TrustLevelSession?
print('TrustLevel', 'TrustLevel' in t)
print('ownerLink', 'ownerLink' in t2[i:i+500])
