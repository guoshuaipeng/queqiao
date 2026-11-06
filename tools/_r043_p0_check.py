from pathlib import Path
t=Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs').read_text(encoding='utf-8')
i=t.find('private async Task ConnectAsync')
print(t[i:i+2200])
