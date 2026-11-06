from pathlib import Path

p = Path("src/RemoteAssist.Protocol/WriteConfirmCopy.cs")
t = p.read_text(encoding="utf-8")
old = '        "如果电脑问是否允许连网，点「允许」。";'
new = '        "如果系统问起是否允许联网，选择允许。";'
if old not in t:
    raise SystemExit("old networking line not found")
t = t.replace(old, new)
t = t.replace("R021：带官网下载地址", "R021/R022：带官网下载地址；连网话术中性")
p.write_text(t, encoding="utf-8")
print("WriteConfirmCopy patched")

tp = Path("tests/RemoteAssist.Protocol.Tests/WriteConfirmCopyTests.cs")
tt = tp.read_text(encoding="utf-8")
# Add/ensure assertions for new phrase
if "如果系统问起是否允许联网，选择允许" not in tt:
    needle = '        Assert.Contains("如果你手头有安装包，也可以一起发出去", clip);'
    insert = needle + '\n        Assert.Contains("如果系统问起是否允许联网，选择允许", clip);\n        Assert.DoesNotContain("如果电脑问是否允许连网", clip);'
    if needle not in tt:
        raise SystemExit("test needle not found")
    tt = tt.replace(needle, insert)
    tp.write_text(tt, encoding="utf-8")
    print("tests patched")
else:
    print("tests already have new phrase")

# UiShotRunner -> r022 names; keep same three shots
ur = Path("src/RemoteAssist.Agent.Avalonia/UiShotRunner.cs")
ut = ur.read_text(encoding="utf-8")
ut = ut.replace("R021：去协助端「复制安装说明」带官网下载地址 + 复制反馈（只一次）。",
                "R022：安装说明连网话术中性 + 剪贴板仍含官网下载地址。")
ut = ut.replace("r021-install-entry.png", "r022-install-entry.png")
ut = ut.replace("r021-install-copied.png", "r022-install-copied.png")
ut = ut.replace("r021-clipboard-preview.png", "r022-clipboard-preview.png")
ur.write_text(ut, encoding="utf-8")
print("UiShotRunner updated")

# Verify
print("---")
for line in p.read_text(encoding="utf-8").splitlines():
    if "InstallCopiedHint" in line or "121.196" in line or "macOS" in line or "联网" in line or "连网" in line or "手头有安装包" in line:
        print(line.strip())
