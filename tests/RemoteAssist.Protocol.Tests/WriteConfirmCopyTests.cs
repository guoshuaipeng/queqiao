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
        Assert.Equal("请把安装包和这段说明一起发出去", WriteConfirmCopy.InstallCopiedHint);
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
    public void InstallInstructions_ThreeLines_NoPorts_HintSeparate()
    {
        var clip = WriteConfirmCopy.InstallInstructionsClipboard;
        var lines = clip.Replace("\r\n", "\n").Split('\n');
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
