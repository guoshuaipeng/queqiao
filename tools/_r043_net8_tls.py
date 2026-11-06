from pathlib import Path

# net8 AgentHost relay HandleStream
t=Path('src/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
i=t.find('RelayGuestLoopAsync')
print('=== net8 RelayGuestLoop call HandleStream ===')
print(t[i:i+2200])
print('=== net8 QrPayload preferRelay ===')
i=t.find('public string QrPayload')
print(t[i:i+900])
print('=== net8 Helper force insecure ===')
h=Path('src/RemoteAssist.Helper.UI/MainForm.cs').read_text(encoding='utf-8')
i=h.find('ViaRelay && !opts.Insecure')
print(h[i-200:i+700] if i>=0 else 'NOT FOUND')
print('=== net8 AssistClient PairViaRelay ===')
a=Path('src/RemoteAssist.Protocol/AssistClient.cs').read_text(encoding='utf-8')
i=a.find('PairViaRelayGuestAsync')
print(a[i:i+1200])
