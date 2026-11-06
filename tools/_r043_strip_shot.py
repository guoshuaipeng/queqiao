from pathlib import Path
p = Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs')
t = p.read_text(encoding='utf-8')
# Remove R043 shot helper + debug entirely
import re
# Remove debug write near shot var
t2 = re.sub(
    r'\n\s*// R043 截图辅助：.*?\n\s*private void ApplyR043Shot\(string shot\)\s*\{.*?\n\s*\}\n',
    '\n',
    t,
    count=1,
    flags=re.S,
)
# Also remove if debug File.WriteAllText left in ctor
t2 = re.sub(
    r'\n\s*var shot = Environment\.GetEnvironmentVariable\("REMOTE_ASSIST_R043_SHOT"\);.*?(?=\n\s*\}\n\n\s*private )',
    '\n',
    t2,
    count=1,
    flags=re.S,
)
# Simpler: find markers
if 'REMOTE_ASSIST_R043_SHOT' in t2 or 'ApplyR043Shot' in t2:
    # manual cut from comment to end of ApplyR043Shot
    start = t2.find('        // R043 截图辅助')
    if start < 0:
        start = t2.find('REMOTE_ASSIST_R043_SHOT')
        start = t2.rfind('\n', 0, start)
    end = t2.find('    private void ApplyR043Shot')
    if end < 0:
        raise SystemExit('no ApplyR043Shot')
    # find closing brace of method
    i = t2.find('{', end)
    depth = 0
    j = i
    while j < len(t2):
        if t2[j] == '{': depth += 1
        elif t2[j] == '}':
            depth -= 1
            if depth == 0:
                j += 1
                break
        j += 1
    # also remove blank line after
    while j < len(t2) and t2[j] in '\r\n':
        j += 1
    t2 = t2[:start] + t2[j:]
    print('removed shot helper block')
else:
    print('shot helper already gone')

# Ensure ctor still ends properly — LayoutPanels RefreshPreview SetConnectedUi
if 'LayoutPanels();' not in t2 or 'SetConnectedUi(false);' not in t2:
    raise SystemExit('ctor broken')
# Fix ctor end if we ate SetConnectedUi close
# Check for double closing
p.write_text(t2, encoding='utf-8', newline='\n')
# verify compile-critical snippet at ctor end
idx = t2.find('LayoutPanels();')
print(repr(t2[idx:idx+400]))
