from pathlib import Path
t=Path('src/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
# find GuestHostAsync through HandleStreamAsync
i=t.find('GuestHostAsync')
print(t[i:i+1800])
print('==== HandleStreamAsync signature net8 ====')
i=t.find('private async Task HandleStreamAsync')
print(t[i:i+900])
# git blame-ish: when was skipTls introduced in 5996705
print('==== DIFF AgentHost skipTls / BuildRelay useTls ====')
