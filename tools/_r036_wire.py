from pathlib import Path
p = Path("tools/UiShot/Program.cs")
t = p.read_text(encoding="utf-8")
old = '''        if (args.Any(a => a is "--r035"))
            return R035Shots.Run(outDir);
'''
new = '''        if (args.Any(a => a is "--r035"))
            return R035Shots.Run(outDir);

        if (args.Any(a => a is "--r036"))
            return R036Shots.Run(outDir);
'''
if "--r036" not in t:
    if old not in t:
        raise SystemExit("r035 hook missing")
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("wired")
