from pathlib import Path
t=Path('relay/internal/relay/server.go').read_text(encoding='utf-8')
# find password validation
for needle in ['password', 'Password', 'len(', 'guest_host', 'guest_join', 'MinPassword', 'maxPass']:
    pass
import re
for m in re.finditer(r'.{0,80}password.{0,120}', t, re.I):
    s=m.group(0).replace('\n',' ')
    if any(k in s.lower() for k in ['len','min','max','valid','short','long','4','8','==']):
        print(s)
        print('---')
print('==== store ====')
s=Path('relay/internal/relay/store.go').read_text(encoding='utf-8')
for m in re.finditer(r'.{0,60}[Pp]assword.{0,100}', s):
    print(m.group(0).replace('\n',' '))
    print('---')
print('==== PRODUCT password ====')
prod=Path('docs/PRODUCT.md').read_text(encoding='utf-8')
for i,l in enumerate(prod.splitlines()):
    if '密码' in l or 'password' in l.lower():
        if any(k in l for k in ['4','8','位','长度','最少']):
            print(i+1, l)
