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
    }

    [Fact]
    public void BannerCopy_UsesPlainSetting_NoZeroWidthChars()
    {
        Assert.Equal("打开防火墙设置", FriendConnectGuide.OpenFirewallButtonText);
        Assert.Contains("系统设置", FriendConnectGuide.MomentAAllow);
        Assert.Equal(-1, FriendConnectGuide.MomentAAllow.IndexOf((char)0x2060));
        Assert.Equal(-1, FriendConnectGuide.MomentAAllow.IndexOf((char)0xFEFF));
        Assert.Equal(-1, FriendConnectGuide.OpenFirewallButtonText.IndexOf((char)0x2060));
        Assert.DoesNotContain("设0置", FriendConnectGuide.MomentAAllow);
        Assert.DoesNotContain("设0置", FriendConnectGuide.OpenFirewallButtonText);
    }
}
