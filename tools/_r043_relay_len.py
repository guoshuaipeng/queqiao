from pathlib import Path
roots = [Path('relay'), Path('server'), Path('src'), Path('docs')]
for root in Path('.').iterdir():
    if not root.is_dir(): continue
    if root.name.startswith('.') or root.name in ('node_modules','artifacts','bin','obj'): continue
    for p in root.rglob('*'):
        if p.suffix.lower() not in {'.go','.rs','.py','.js','.ts','.md','.cs'} : continue
        try: t=p.read_text(encoding='utf-8', errors='ignore')
        except: continue
        if 'password' in t.lower() and ('guest_host' in t or 'guest_join' in t or 'len(password)' in t or 'password.Length' in t or 'min 4' in t):
            if 'password' in t and any(k in t for k in ['Length','len(','min ','max ','4','8']):
                if any(k in str(p).lower() for k in ['relay','server','protocol']):
                    print(p)
