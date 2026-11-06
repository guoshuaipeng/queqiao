from pathlib import Path
t=Path('src-net48/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
print(t[:3500])
print('===== START METHOD =====')
i=t.find('public void Start')
print(t[i:i+1500] if i>=0 else 'no Start')
print('===== REFRESH =====')
i=t.find('RefreshSession')
print(t[i:i+800] if i>=0 else 'no refresh')
