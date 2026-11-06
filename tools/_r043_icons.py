from pathlib import Path
for p in Path('src-net48').rglob('*.cs'):
    t=p.read_text(encoding='utf-8')
    if 'class AppIcons' in t or 'static class AppIcons' in t:
        print(p)
