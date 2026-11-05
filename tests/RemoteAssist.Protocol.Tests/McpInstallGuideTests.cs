using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class McpInstallGuideTests
{
    [Fact]
    public void Steps_ContainThreeActions_AndConfirmCannotBypass()
    {
        Assert.Contains("复制 MCP 配置", McpInstallGuide.Step1);
        Assert.Contains("MCP 设置", McpInstallGuide.Step2);
        Assert.Contains("粘贴", McpInstallGuide.Step2);
        Assert.Contains("确认", McpInstallGuide.Step3);
        Assert.Contains("不能绕过", McpInstallGuide.Step3);
        Assert.DoesNotContain("17890", McpInstallGuide.StepsBlock);
        Assert.DoesNotContain("17891", McpInstallGuide.StepsBlock);
        Assert.False(WriteConfirmCopy.ContainsBannedUiTerm(McpInstallGuide.StepsBlock.Replace("MCP", "")));
    }

    [Fact]
    public void SettingWord_JoinerKeepsTogether_InMomentA()
    {
        Assert.Contains("\u2060", FriendConnectGuide.MomentAAllow);
        Assert.Contains("\u2060", FriendConnectGuide.OpenFirewallButtonText);
        // 不应再出现可被折成「改设」「置」的旧句式
        Assert.DoesNotContain("不用改设置", FriendConnectGuide.MomentAAllow);
    }
}
