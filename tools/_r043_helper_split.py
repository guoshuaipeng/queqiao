from pathlib import Path
t = Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs').read_text(encoding='utf-8')
# find ConnectAsync
i = t.find('    private async Task ConnectAsync')
print('ConnectAsync at', i, 'line', t[:i].count('\n')+1)
# Save from ConnectAsync to end for reuse
Path('tools/_r043_helper_tail.cs').write_text(t[i:], encoding='utf-8')
print('tail lines', t[i:].count('\n')+1)
# Also save tool card construction start
i2 = t.find('var toolsCard')
print('toolsCard', i2, t[:i2].count('\n')+1)
# Status/relay placeholder section
for n in ['即将支持', 'RelayPlaceholder', '_relayBox', 'statusCard', 'toolsCard']:
    print(n, t.find(n))
