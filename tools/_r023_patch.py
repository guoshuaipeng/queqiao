from pathlib import Path

p = Path("src/RemoteAssist.Protocol/WriteConfirmCopy.cs")
t = p.read_text(encoding="utf-8")
old = '''    /// <summary>协助端「复制安装说明」剪贴板正文（R021/R022：带官网下载地址；连网话术中性；不含端口/放行入站）。</summary>
    public const string InstallInstructions =
        "请打开官网下载「鹊桥远程协助」：http://121.196.45.81/\\n" +
        "Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe\\n" +
        "Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）\\n" +
        "macOS 尚未提供正式下载。\\n" +
        "如果你手头有安装包，也可以一起发出去。\\n" +
        "装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。\\n" +
        "如果系统问起是否允许联网，选择允许。";'''

new = '''    /// <summary>协助端「复制安装说明」剪贴板正文（R021–R023：官网下载 + 中性联网 + .NET 8 运行时短指引；不含端口/放行入站）。</summary>
    public const string InstallInstructions =
        "请打开官网下载「鹊桥远程协助」：http://121.196.45.81/\\n" +
        "Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe\\n" +
        "Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）\\n" +
        "macOS 尚未提供正式下载。\\n" +
        "如果你手头有安装包，也可以一起发出去。\\n" +
        "装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。\\n" +
        "如果系统问起是否允许联网，选择允许。\\n" +
        "若打开时提示缺少 .NET，先安装「.NET 8 运行时」（用来跑这个程序的，不是开发工具）。\\n" +
        "打开 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 ，在「运行时」那一栏按系统下载，不要选 SDK。";'''

if old not in t:
    raise SystemExit("old InstallInstructions block not found")
p.write_text(t.replace(old, new), encoding="utf-8")
print("WriteConfirmCopy updated")

tp = Path("tests/RemoteAssist.Protocol.Tests/WriteConfirmCopyTests.cs")
tt = tp.read_text(encoding="utf-8")
needle = '        Assert.Contains("如果系统问起是否允许联网，选择允许", clip);'
insert = needle + '''
        Assert.Contains(".NET 8 运行时", clip);
        Assert.Contains("https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0", clip);
        Assert.Contains("不要选 SDK", clip);
        Assert.Contains("不是开发工具", clip);
        Assert.DoesNotContain("自动下载", clip);'''
if "dotnet.microsoft.com/zh-cn/download/dotnet/8.0" not in tt:
    if needle not in tt:
        raise SystemExit("test needle missing")
    tt = tt.replace(needle, insert)
    tp.write_text(tt, encoding="utf-8")
    print("tests updated")
else:
    print("tests already have dotnet url")

# UiShotRunner -> r023
ur = Path("src/RemoteAssist.Agent.Avalonia/UiShotRunner.cs")
ut = ur.read_text(encoding="utf-8")
ut = ut.replace("R022：安装说明连网话术中性 + 剪贴板仍含官网下载地址。",
                "R023：安装说明补 .NET 8 运行时短指引 + 仍含官网下载地址。")
ut = ut.replace("r022-install-entry.png", "r023-install-entry.png")
ut = ut.replace("r022-install-copied.png", "r023-install-copied.png")
ut = ut.replace("r022-clipboard-preview.png", "r023-clipboard-preview.png")
ur.write_text(ut, encoding="utf-8")
print("UiShotRunner updated")

# show final
for line in p.read_text(encoding="utf-8").splitlines():
    if "InstallCopiedHint" in line or line.strip().startswith('"'):
        if "InstallInstructionsClipboard" in line:
            continue
        print(line.rstrip())
