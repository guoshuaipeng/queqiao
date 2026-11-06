from pathlib import Path
p=Path('src-net48/RemoteAssist.Agent/MainForm.cs')
t=p.read_text(encoding='utf-8')
# Fix broken RelayStatusChanged line (may have newline)
import re
t2=re.sub(
    r'_host\.RelayStatusChanged \+= \(\) => BeginInvokeSafe\(RefreshSessionUi\s*\);?',
    '_host.RelayStatusChanged += OnSessionChanged;',
    t)
if t2==t:
    # try exact multiline
    t2=t.replace('_host.RelayStatusChanged += () => BeginInvokeSafe(RefreshSessionUi\n);',
                 '_host.RelayStatusChanged += OnSessionChanged;')
    t2=t2.replace('_host.RelayStatusChanged += () => BeginInvokeSafe(RefreshSessionUi);',
                 '_host.RelayStatusChanged += OnSessionChanged;')
print('relay hook fixed', 'OnSessionChanged' in t2[t2.find('RelayStatusChanged'):t2.find('RelayStatusChanged')+80] if 'RelayStatusChanged' in t2 else False)
print(repr(t2[t2.find('RelayStatusChanged')-20:t2.find('RelayStatusChanged')+90]))

# Fix RefreshFromHost listenValue overwrite - read section
i=t2.find('private void RefreshFromHost')
print('--- RefreshFromHost ---')
print(t2[i:i+1200])
p.write_text(t2,encoding='utf-8',newline='\n')
