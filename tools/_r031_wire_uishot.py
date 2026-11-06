from pathlib import Path
p = Path("tools/UiShot/Program.cs")
t = p.read_text(encoding="utf-8")
old = """        if (args.Any(a => a is \"--r029\"))
            return R029Shots.Run(outDir);
"""
new = """        if (args.Any(a => a is \"--r029\"))
            return R029Shots.Run(outDir);

        if (args.Any(a => a is \"--r031\"))
            return R031Shots.Run(outDir);
"""
if "--r031" not in t:
    if old not in t:
        raise SystemExit("r029 hook missing")
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("Program.cs wired")
else:
    print("already wired")
