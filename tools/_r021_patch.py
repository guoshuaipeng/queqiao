from pathlib import Path

# --- WriteConfirmCopy.cs: update InstallCopiedHint + InstallInstructions ---
p = Path("src/RemoteAssist.Protocol/WriteConfirmCopy.cs")
text = p.read_text(encoding="utf-8")

old_hint = '    public const string InstallCopiedHint = "请把安装包和这段说明一起发出去";'
new_hint = '    public const string InstallCopiedHint = "说明里已带上官网下载地址，发给对方即可";'
if old_hint not in text:
    raise SystemExit("InstallCopiedHint old text not found")
text = text.replace(old_hint, new_hint)

old_inst = '''    /// <summary>协助端「复制安装说明」剪贴板正文（三行，不含端口/放行入站）。</summary>
    public const string InstallInstructions =
        "我发你一个安装包，打开装上就行，名字叫「鹊桥远程协助」。\\n" +
        "装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。\\n" +
        "如果电脑问是否允许连网，点「允许」。";'''

new_inst = '''    /// <summary>协助端「复制安装说明」剪贴板正文（R021：带官网下载地址；不含端口/放行入站）。</summary>
    public const string InstallInstructions =
        "请打开官网下载「鹊桥远程协助」：http://121.196.45.81/\\n" +
        "Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe\\n" +
        "Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）\\n" +
        "macOS 尚未提供正式下载。\\n" +
        "如果你手头有安装包，也可以一起发出去。\\n" +
        "装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。\\n" +
        "如果电脑问是否允许连网，点「允许」。";'''

if old_inst not in text:
    raise SystemExit("InstallInstructions old block not found")
text = text.replace(old_inst, new_inst)
p.write_text(text, encoding="utf-8")
print("WriteConfirmCopy.cs updated")

# --- Tests ---
tp = Path("tests/RemoteAssist.Protocol.Tests/WriteConfirmCopyTests.cs")
tt = tp.read_text(encoding="utf-8")
tt = tt.replace(
    'Assert.Equal("请把安装包和这段说明一起发出去", WriteConfirmCopy.InstallCopiedHint);',
    'Assert.Equal("说明里已带上官网下载地址，发给对方即可", WriteConfirmCopy.InstallCopiedHint);',
)

old_test = '''    [Fact]
    public void InstallInstructions_ThreeLines_NoPorts_HintSeparate()
    {
        var clip = WriteConfirmCopy.InstallInstructionsClipboard;
        var lines = clip.Replace("\\r\\n", "\\n").Split('\\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("鹊桥远程协助", lines[0]);
        Assert.Contains("接受协助", lines[1]);
        Assert.Contains("复制码+密码", lines[1]);
        Assert.Contains("允许", lines[2]);
        Assert.DoesNotContain("17890", clip);
        Assert.DoesNotContain("17891", clip);
        Assert.DoesNotContain("放行入站", clip);
        Assert.DoesNotContain(WriteConfirmCopy.InstallCopiedHint, clip);
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(clip));
    }'''

new_test = '''    [Fact]
    public void InstallInstructions_HasOfficialDownloads_NoPorts_HintSeparate()
    {
        var clip = WriteConfirmCopy.InstallInstructionsClipboard;
        var lines = clip.Replace("\\r\\n", "\\n").Split('\\n');
        Assert.True(lines.Length >= 6);
        Assert.Contains("http://121.196.45.81/", clip);
        Assert.Contains("http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe", clip);
        Assert.Contains("http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz", clip);
        Assert.Contains(".NET 8 桌面方向运行时", clip);
        Assert.Contains("macOS 尚未提供正式下载", clip);
        Assert.Contains("如果你手头有安装包，也可以一起发出去", clip);
        Assert.Contains("接受协助", clip);
        Assert.Contains("复制码+密码", clip);
        Assert.DoesNotContain("17890", clip);
        Assert.DoesNotContain("17891", clip);
        Assert.DoesNotContain("放行入站", clip);
        Assert.DoesNotContain("TLS", clip);
        Assert.DoesNotContain("stdio", clip);
        Assert.DoesNotContain(WriteConfirmCopy.InstallCopiedHint, clip);
        // 官网文件名含 RemoteAssist；亲友界面禁词检查不套用整段剪贴板
        Assert.DoesNotContain("ownerLink", clip);
        Assert.DoesNotContain("MCP", clip);
    }'''

if old_test not in tt:
    raise SystemExit("old InstallInstructions test not found")
tt = tt.replace(old_test, new_test)
tp.write_text(tt, encoding="utf-8")
print("tests updated")
