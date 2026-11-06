from pathlib import Path
p=Path('src-net48/RemoteAssist.Agent/MainForm.cs')
t=p.read_text(encoding='utf-8')
t=t.replace(
    '_host.RelayStatusChanged += () => BeginInvokeSafe(RefreshSessionUi);',
    '_host.RelayStatusChanged += OnSessionChanged;')
# Fix switch or pattern for older - use if chain if needed; LangVersion latest should be fine for C# 9+
# Check RefreshFromHost sets code
i=t.find('private void RefreshFromHost')
print(t[i:i+800] if i>=0 else 'no RefreshFromHost')
p.write_text(t,encoding='utf-8',newline='\n')
