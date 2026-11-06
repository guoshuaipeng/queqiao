from pathlib import Path

p = Path("src/RemoteAssist.Protocol/WriteConfirmCopy.cs")
t = p.read_text(encoding="utf-8")
old = '''    /// <summary>协助端「复制安装说明」剪贴板正文（R021–R023：官网下载 + 中性联网 + .NET 8 运行时短指引；不含端口/放行入站）。</summary>
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

new = '''    /// <summary>协助端「复制安装说明」剪贴板正文（R021–R025：官网下载 + 中性联网 + .NET 8 运行时短指引 + 下载/打开不受信任怎么选；不含端口/放行入站）。</summary>
    public const string InstallInstructions =
        "请打开官网下载「鹊桥远程协助」：http://121.196.45.81/\\n" +
        "Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe\\n" +
        "Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）\\n" +
        "macOS 尚未提供正式下载。\\n" +
        "如果你手头有安装包，也可以一起发出去。\\n" +
        "装好后打开，选「接受协助」，再点「复制码+密码」，把复制出来的发给我。\\n" +
        "如果系统问起是否允许联网，选择允许。\\n" +
        "若打开时提示缺少 .NET，先安装「.NET 8 运行时」（用来跑这个程序的，不是开发工具）。\\n" +
        "打开 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 ，在「运行时」那一栏按系统下载，不要选 SDK。\\n" +
        "如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。";'''

if old not in t:
    raise SystemExit("old block not found")
p.write_text(t.replace(old, new), encoding="utf-8")
print("WriteConfirmCopy ok")
# assert hint untouched
assert 'InstallCopiedHint = "说明里已带上官网下载地址，发给对方即可"' in p.read_text(encoding="utf-8")

tp = Path("tests/RemoteAssist.Protocol.Tests/WriteConfirmCopyTests.cs")
tt = tp.read_text(encoding="utf-8")
needle = '        Assert.Contains("不要选 SDK", clip);'
extra = needle + '\n        Assert.Contains("如果浏览器提示通常不会下载，选择保留", clip);\n        Assert.Contains("选择仍要运行", clip);\n        Assert.DoesNotContain("SmartScreen", clip);\n        Assert.DoesNotContain("证书", clip);\n        Assert.DoesNotContain("签名", clip);'
if "如果浏览器提示通常不会下载，选择保留" not in tt:
    if needle not in tt:
        raise SystemExit("test needle missing")
    tp.write_text(tt.replace(needle, extra), encoding="utf-8")
    print("tests ok")
else:
    print("tests already")

ur = Path("src/RemoteAssist.Agent.Avalonia/UiShotRunner.cs")
ut = ur.read_text(encoding="utf-8")
ut = ut.replace("R023：安装说明补 .NET 8 运行时短指引 + 仍含官网下载地址。",
                "R025：安装说明补下载/打开不受信任怎么选。")
ut = ut.replace("r023-install-entry.png", "r025-install-entry.png")
ut = ut.replace("r023-install-copied.png", "r025-install-copied.png")
ut = ut.replace("r023-clipboard-preview.png", "r025-clipboard-preview.png")
ur.write_text(ut, encoding="utf-8")
print("UiShotRunner ok")
