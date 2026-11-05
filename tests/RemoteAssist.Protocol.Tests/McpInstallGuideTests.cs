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
    public void SkillInstructions_IsVerbatim_NoBannedTerms_HintSeparate()
    {
        var clip = McpInstallGuide.SkillInstructionsClipboard;
        Assert.Contains("把下面这段保存成智能体的技能说明", clip);
        Assert.Contains("只用来告诉它怎么安装鹊桥的 MCP", clip);
        Assert.Contains("不能代替对方本机确认", clip);
        Assert.Contains("1. 在鹊桥里点「复制 MCP 配置」。", clip);
        Assert.Contains("2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。", clip);
        Assert.Contains("3. 刷新或重启后再用智能体协助。", clip);
        Assert.Contains("对方电脑上要改动的操作仍会弹确认，智能体不能绕过。", clip);
        Assert.Contains("不要用这段说明去远程控制电脑。", clip);
        Assert.Equal(
            "已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。",
            McpInstallGuide.SkillCopiedHint);
        Assert.Equal("复制 Skill 说明", McpInstallGuide.CopySkillButton);
        Assert.DoesNotContain("17890", clip);
        Assert.DoesNotContain("17891", clip);
        Assert.DoesNotContain("Assist", clip);
        Assert.DoesNotContain("ownerLink", clip);
        Assert.DoesNotContain("TLS", clip);
        Assert.DoesNotContain("SmartScreen", clip);
        Assert.DoesNotContain(McpInstallGuide.SkillCopiedHint, clip);
        Assert.DoesNotContain(McpInstallGuide.CopiedOkHint, clip);
        // 三步原文未改
        Assert.Equal("1. 点「复制 MCP 配置」", McpInstallGuide.Step1);
        Assert.Equal("已复制 MCP 配置。请粘贴到 Cursor 的 MCP 设置并保存。", McpInstallGuide.CopiedOkHint);
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
