from pathlib import Path
t=Path('src/RemoteAssist.Helper.UI/MainForm.cs').read_text(encoding='utf-8')
# Get TryBuildOptions / RefreshPreview / ConnectAsync / ToggleAdvanced
for name in ['TryBuildOptions', 'RefreshPreview', 'ConnectAsync', 'ToggleAdvanced', 'SetFailHint', 'PasteClipboard', 'OnInputChanged']:
    i=t.find('    private ')
# find each
import re
for m in re.finditer(r'    private (?:async )?[\w<>?]+\s+(TryBuildOptions|RefreshPreview|ConnectAsync|ToggleAdvanced|SetFailHint|ClearFailHint|PasteClipboard|OnInputChanged|SetStatus|SetConnectedUi)\b', t):
    print(m.group(1), 'at', m.start(), 'line', t[:m.start()].count('\n')+1)
