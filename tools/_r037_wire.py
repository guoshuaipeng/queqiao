from pathlib import Path

# Harden Win RefreshPreview
p = Path("src/RemoteAssist.Helper.UI/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = """        // R037：已连上不再把预览刷回「粘贴后点「连接」」
        if (_client is not null)
        {
            if (_previewLabel.Text == HelperSessionCopy.PasteThenConnectHint)
                _previewLabel.Visible = false;
            return;
        }
"""
new = """        // R037：已连上（真连或 ui-shot）不再把预览刷回「粘贴后点「连接」」
        if (_client is not null ||
            string.Equals(_btnConnect.Text, HelperSessionCopy.ConnectedButtonText, StringComparison.Ordinal))
        {
            if (_previewLabel.Text == HelperSessionCopy.PasteThenConnectHint)
                _previewLabel.Visible = false;
            return;
        }
"""
if "真连或 ui-shot" not in t:
    if old not in t:
        raise SystemExit("Win RefreshPreview guard missing")
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("Win RefreshPreview hardened")
else:
    print("Win RefreshPreview already hardened")

# Wire Program.cs
p = Path("tools/UiShot/Program.cs")
t = p.read_text(encoding="utf-8")
old = """        if (args.Any(a => a is \"--r036\"))
            return R036Shots.Run(outDir);
"""
# Use chr to avoid escape issues - actually write with normal quotes in file
old = "        if (args.Any(a => a is \"--r036\"))\n            return R036Shots.Run(outDir);\n"
# Better:
needle = '        if (args.Any(a => a is "--r036"))\n            return R036Shots.Run(outDir);\n'
insert = needle + '\n        if (args.Any(a => a is "--r037"))\n            return R037Shots.Run(outDir);\n'
if "--r037" not in t:
    if needle not in t:
        raise SystemExit("r036 hook missing: " + repr(t[t.find("r036")-40:t.find("r036")+80] if "r036" in t else "no r036"))
    t = t.replace(needle, insert, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("Program wired")
else:
    print("Program already wired")
