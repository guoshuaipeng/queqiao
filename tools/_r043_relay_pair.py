from pathlib import Path
t=Path('src/RemoteAssist.Protocol/AssistClient.cs').read_text(encoding='utf-8')
i=t.find('private static async Task<AssistClient> PairViaRelayGuestAsync')
print(t[i:i+2200])
# QrPayload for relay - does agent share fp in credential?
t2=Path('src/RemoteAssist.Agent/MainForm.cs').read_text(encoding='utf-8')
# How copy works with relay
i=t2.find('FormatShareClipboard')
print('--- copy ---')
print(t2[i-200:i+400])
# BuildRelay payload?
i=t2.find('BuildRelay')
print('BuildRelay', i)
i=t2.find('QrPayload')
# AgentHost QrPayload when preferRelay
t3=Path('src/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
i=t3.find('public string QrPayload')
print(t3[i:i+900])
