from pathlib import Path
t=Path('src/RemoteAssist.Helper.UI/MainForm.cs').read_text(encoding='utf-8')
# extract advanced toggle and main paste area roughly lines with 高级
lines=t.splitlines()
for i,l in enumerate(lines):
    if '高级' in l or '_advanced' in l or '_pwdBox' in l or '连接凭证' in l or '验证密码' in l or '粘贴' in l and 'Label' in l:
        if i < 350 or '高级' in l or '_advancedPanel' in l:
            print(f'{i+1}:{l}')
