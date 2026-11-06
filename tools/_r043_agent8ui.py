from pathlib import Path
t=Path('src/RemoteAssist.Agent/MainForm.cs').read_text(encoding='utf-8')
lines=t.splitlines()
for i,l in enumerate(lines):
    if any(k in l for k in ['复制','_pwdLabel','SessionPassword','FormatShare','验证','连接凭证','_codeLabel']):
        print(f'{i+1}:{l[:140]}')
