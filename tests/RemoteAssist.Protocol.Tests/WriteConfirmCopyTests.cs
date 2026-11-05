using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class WriteConfirmCopyTests
{
    [Fact]
    public void DialogCopy_HasNoBannedTerms()
    {
        Assert.Equal("对方想在这台电脑执行操作", WriteConfirmCopy.DialogTitle);
        Assert.Equal("已拒绝，未做任何改动", WriteConfirmCopy.DeniedMessage);
        Assert.Equal("允许", WriteConfirmCopy.AllowButton);
        Assert.Equal("拒绝", WriteConfirmCopy.DenyButton);
        Assert.Equal("说明里已带上官网下载地址，发给对方即可", WriteConfirmCopy.InstallCopiedHint);
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(WriteConfirmCopy.DialogTitle));
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(WriteConfirmCopy.DeniedMessage));
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(WriteConfirmCopy.TimedOutMessage));
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(WriteConfirmCopy.InstallCopiedHint));
        Assert.Contains("超时", WriteConfirmCopy.TimedOutMessage);
        Assert.Contains("未做任何改动", WriteConfirmCopy.TimedOutMessage);
    }

    [Fact]
    public void Describe_RunCommand_HeadlineIsPlain_DetailHasCommand()
    {
        var d = WriteConfirmCopy.Describe(ToolKind.RunCommand, new Dictionary<string, string?>
        {
            ["command"] = "echo hello"
        });
        Assert.Equal("运行一条命令", d.Headline);
        Assert.Contains("echo hello", d.Detail);
        Assert.StartsWith("命令：", d.Detail);
        Assert.DoesNotContain("echo hello", d.Headline);
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(d.Headline));
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(d.Detail));
        Assert.DoesNotContain("RunCommand", d.ForAudit);
    }

    [Fact]
    public void Describe_DeleteStyle_HeadlineSaysDeleteFile()
    {
        var d = WriteConfirmCopy.Describe(ToolKind.RunCommand, new Dictionary<string, string?>
        {
            ["command"] = "del C:\\temp\\a.txt"
        });
        Assert.Equal("删除一个文件或文件夹", d.Headline);
        Assert.Contains("del C:\\temp\\a.txt", d.Detail);
        Assert.DoesNotContain("del ", d.Headline);
    }

    [Fact]
    public void Describe_Tasklist_MentionsListProcessesInHeadline()
    {
        var d = WriteConfirmCopy.Describe(ToolKind.RunCommand, new Dictionary<string, string?>
        {
            ["command"] = "tasklist"
        });
        Assert.Equal("运行一条命令，列出当前进程", d.Headline);
        Assert.Contains("tasklist", d.Detail);
    }

    [Fact]
    public void InstallInstructions_HasOfficialDownloads_NoPorts_HintSeparate()
    {
        var clip = WriteConfirmCopy.InstallInstructionsClipboard;
        var lines = clip.Replace("\r\n", "\n").Split('\n');
        Assert.True(lines.Length >= 6);
        Assert.Contains("http://121.196.45.81/", clip);
        Assert.Contains("http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe", clip);
        Assert.Contains("http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz", clip);
        Assert.Contains(".NET 8 桌面方向运行时", clip);
        Assert.Contains("macOS 尚未提供正式下载", clip);
        Assert.Contains("如果你手头有安装包，也可以一起发出去", clip);
        Assert.Contains("如果系统问起是否允许联网，选择允许", clip);
        Assert.DoesNotContain("如果电脑问是否允许连网", clip);
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
    }

    [Fact]
    public void Countdown_DoesNotAutoAllowWording()
    {
        var t = WriteConfirmCopy.CountdownText(30);
        Assert.Contains("自动拒绝", t);
        Assert.DoesNotContain("自动允许", t);
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(t));
    }
}
