from pathlib import Path
p = Path(r"src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
t = p.read_text(encoding="utf-8")
# Fix Windows path watermarks: use verbatim or doubled backslash
bad = '? "例如 C:\\Users\\你的文件夹"'
# After read, the file has single backslashes: C:\Users\
# Let's replace the broken pattern with verbatim strings
import re
t2 = t.replace(
    '? "例如 C:\\Users\\你的文件夹"',
    '? @"例如 C:\\Users\\你的文件夹"',
)
# If file actually has single backslash from how it was written:
t2 = t2.replace(
    '? "例如 C:\Users\你的文件夹"',
    '? @"例如 C:\Users\你的文件夹"',
)
# Also without the ? prefix variants
t2 = t2.replace(
    '"例如 C:\\Users\\你的文件夹"',
    '@"例如 C:\\Users\\你的文件夹"',
)
t2 = t2.replace(
    '"例如 C:\Users\你的文件夹"',
    '@"例如 C:\Users\你的文件夹"',
)

# Fix broken denied output string
bad_out = '''        _output.Text = "[提议一项改动] FAIL: " + WriteConfirmCopy.DeniedMessage + "
";'''
good_out = '        _output.Text = "[提议一项改动] FAIL: " + WriteConfirmCopy.DeniedMessage + "\\n";'
# actual file may have real newline
import re
t2 = re.sub(
    r'_output\.Text = "\[提议一项改动\] FAIL: " \+ WriteConfirmCopy\.DeniedMessage \+ "\n";',
    '_output.Text = "[提议一项改动] FAIL: " + WriteConfirmCopy.DeniedMessage + "\\n";',
    t2,
)
# also the split-line form
t2 = re.sub(
    r'_output\.Text = "\[提议一项改动\] FAIL: " \+ WriteConfirmCopy\.DeniedMessage \+ "\r?\n";',
    '_output.Text = "[提议一项改动] FAIL: " + WriteConfirmCopy.DeniedMessage + "\\n";',
    t2,
)
# split across lines as shown
pat = '_output.Text = "[提议一项改动] FAIL: " + WriteConfirmCopy.DeniedMessage + "\n";'
# read raw lines and fix manually
lines = t2.splitlines(keepends=True)
out = []
i = 0
while i < len(lines):
    if '_output.Text = "[提议一项改动] FAIL:' in lines[i] and 'DeniedMessage' in lines[i]:
        out.append('        _output.Text = "[提议一项改动] FAIL: " + WriteConfirmCopy.DeniedMessage + "\\n";\n')
        # skip continuation lines that are just '";' 
        if lines[i].rstrip().endswith('+ "') or not lines[i].rstrip().endswith(';'):
            i += 1
            while i < len(lines) and lines[i].strip() in ('";', '"'):
                i += 1
            continue
        i += 1
        continue
    # fix watermark lines containing C:\Users with invalid escapes
    if '例如 C:' in lines[i] and 'Users' in lines[i]:
        indent = lines[i][:len(lines[i]) - len(lines[i].lstrip())]
        if lines[i].lstrip().startswith('?'):
            out.append(indent + '? @"例如 C:\\Users\\你的文件夹"\n' if not lines[i].rstrip().endswith(';') and not lines[i].rstrip().endswith(',') else indent + '? @"例如 C:\\Users\\你的文件夹"' + (',' if lines[i].rstrip().endswith(',') else ';') + '\n')
            # simpler:
            if lines[i].rstrip().endswith(','):
                out[-1] = indent + '? @"例如 C:\\Users\\你的文件夹",\n'
            elif lines[i].rstrip().endswith(';'):
                out[-1] = indent + '? @"例如 C:\\Users\\你的文件夹";\n'
            else:
                out[-1] = indent + '? @"例如 C:\\Users\\你的文件夹"\n'
        else:
            if lines[i].rstrip().endswith(','):
                out.append(indent + '@"例如 C:\\Users\\你的文件夹",\n')
            elif lines[i].rstrip().endswith(';'):
                out.append(indent + '@"例如 C:\\Users\\你的文件夹";\n')
            else:
                out.append(indent + '@"例如 C:\\Users\\你的文件夹"\n')
        i += 1
        continue
    out.append(lines[i])
    i += 1

p.write_text(''.join(out), encoding='utf-8')
# verify
lines = p.read_text(encoding='utf-8').splitlines()
for n in [381, 891, 996, 1069]:
    print(f'{n}: {lines[n-1]!r}')
