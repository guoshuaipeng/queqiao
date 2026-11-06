from pathlib import Path
p = Path(r"src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
lines = p.read_text(encoding="utf-8").splitlines(keepends=True)
win_wm = '@"例如 C:' + chr(92) + 'Users' + chr(92) + '你的文件夹"'
out = []
i = 0
while i < len(lines):
    line = lines[i]
    if "例如 C:" in line and "Users" in line:
        indent = line[:len(line) - len(line.lstrip())]
        trailing = "," if line.rstrip().endswith(",") else (";" if line.rstrip().endswith(";") else "")
        prefix = "? " if line.lstrip().startswith("?") else ""
        out.append(indent + prefix + win_wm + trailing + "\n")
        i += 1
        continue
    if "_output.Text = " in line and "提议一项改动" in line and "DeniedMessage" in line:
        indent = line[:len(line) - len(line.lstrip())]
        out.append(indent + '_output.Text = "[提议一项改动] FAIL: " + WriteConfirmCopy.DeniedMessage + "\\n";\n')
        # skip broken continuation
        if not line.rstrip().endswith(";"):
            i += 1
            while i < len(lines) and ('";' in lines[i] or lines[i].strip() == '";' or lines[i].strip() == '"'):
                i += 1
            continue
        i += 1
        continue
    out.append(line)
    i += 1
p.write_text("".join(out), encoding="utf-8")
lines = p.read_text(encoding="utf-8").splitlines()
for n in [381, 891, 996, 1069]:
    print(n, repr(lines[n-1]))