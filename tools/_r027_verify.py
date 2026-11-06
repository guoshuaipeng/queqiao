from pathlib import Path
import re
t=Path('src/RemoteAssist.Protocol/McpInstallGuide.cs').read_text(encoding='utf-8')
body=t.split('SkillInstructions =',1)[1].split(';',1)[0]
parts=re.findall(r'"((?:[^"\\]|\\.)*)"', body)
text=''.join(p.replace('\\n','\n') for p in parts)
print(text)
print('---HINT---')
m=re.search(r'SkillCopiedHint\s*=\s*\n?\s*"([^"]+)"', t)
print(m.group(1) if m else 'MISSING')
ht=Path('src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs').read_text(encoding='utf-8')
print('has_method', 'private async Task CopySkillInstructionsAsync()' in ht)
print('has_shot', 'PrepareSkillClipboardPreviewForShot' in ht)
