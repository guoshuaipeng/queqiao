using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class TrustLevelCopyTests
{
    [Fact]
    public void Names_And_Blurbs_Match_TrustLevelsDoc()
    {
        Assert.Equal("对方要改这台电脑时", TrustLevelCopy.GroupTitle);
        Assert.Equal("每次询问", TrustLevelCopy.AskEveryTimeName);
        Assert.Equal("宽松", TrustLevelCopy.RelaxedName);
        Assert.Equal("完全信任", TrustLevelCopy.FullTrustName);
        Assert.Equal("对方改动前都会问你（推荐）", TrustLevelCopy.AskEveryTimeBlurb);
        Assert.Equal("只有高风险改动才问你，其余会记下但不再打扰", TrustLevelCopy.RelaxedBlurb);
        Assert.Equal("仅这次不再询问，但会记下", TrustLevelCopy.FullTrustBlurb);
        Assert.Equal("仅这次", TrustLevelCopy.FullTrustOnceHint);
        Assert.Equal("结束完全信任", TrustLevelCopy.EndFullTrust);
        Assert.Equal("结束完全信任", TrustLevelCopy.TipActionOf(TrustLevelKind.FullTrust));
        Assert.Equal("改回每次询问", TrustLevelCopy.TipActionOf(TrustLevelKind.Relaxed));
        Assert.Equal("高风险包括删除文件、关机、改权限等。", TrustLevelCopy.HighRiskSupplement);
        Assert.Equal("如果对方只能查看，不会因为这个设置就能改你的电脑。", TrustLevelCopy.GuestCeilingNote);
        Assert.Equal("本次协助已确认的改动", TrustLevelCopy.ConfirmedChangesTitle);
        Assert.Equal("改回每次询问", TrustLevelCopy.RevertToAskEveryTime);
    }

    [Fact]
    public void FullTrustConfirm_Copy_Is_Verbatim()
    {
        Assert.Equal("这次协助不再询问了吗？", TrustLevelCopy.FullTrustConfirmTitle);
        Assert.Equal(
            "打开后，只对现在这位协助者、这一次有效。对方做的改动都不会再弹窗问你，但仍会记在这台电脑的记录里。你可以随时改回。这次结束后，或换一个人再连，不会继续完全信任，会回到你平时的选择。",
            TrustLevelCopy.FullTrustConfirmBody);
        Assert.Equal("确定，这次不再询问", TrustLevelCopy.FullTrustConfirmOk);
        Assert.Equal("取消", TrustLevelCopy.FullTrustConfirmCancel);
        // 验收语义（trust-levels.md 2026-11-05）
        Assert.Contains("只对现在这位协助者、这一次有效", TrustLevelCopy.FullTrustConfirmBody);
        Assert.Contains("不会再弹窗", TrustLevelCopy.FullTrustConfirmBody);
        Assert.Contains("记在这台电脑的记录里", TrustLevelCopy.FullTrustConfirmBody);
        Assert.Contains("你可以随时改回", TrustLevelCopy.FullTrustConfirmBody);
        Assert.Contains("换一个人再连，不会继续完全信任", TrustLevelCopy.FullTrustConfirmBody);
        Assert.Contains("会回到你平时的选择", TrustLevelCopy.FullTrustConfirmBody);
    }

    [Fact]
    public void Banners_Match_Doc()
    {
        Assert.Equal("当前是「宽松」：只有高风险改动才会问你。", TrustLevelCopy.RelaxedBanner);
        Assert.Equal("当前是「完全信任」：这次协助的改动不会再问你。", TrustLevelCopy.FullTrustBanner);
        Assert.Equal("", TrustLevelCopy.BannerOf(TrustLevelKind.AskEveryTime));
    }

    [Fact]
    public void No_Banned_Internal_Terms_In_Ui_Copy()
    {
        var samples = new[]
        {
            TrustLevelCopy.GroupTitle,
            TrustLevelCopy.AskEveryTimeName,
            TrustLevelCopy.RelaxedName,
            TrustLevelCopy.FullTrustName,
            TrustLevelCopy.AskEveryTimeBlurb,
            TrustLevelCopy.RelaxedBlurb,
            TrustLevelCopy.FullTrustBlurb,
            TrustLevelCopy.FullTrustConfirmTitle,
            TrustLevelCopy.FullTrustConfirmBody,
            TrustLevelCopy.RelaxedBanner,
            TrustLevelCopy.FullTrustBanner,
            TrustLevelCopy.ConfirmedChangesTitle,
            TrustLevelCopy.GuestCeilingNote,
            TrustLevelCopy.EndFullTrust,
            TrustLevelCopy.FullTrustOnceHint
        };
        foreach (var s in samples)
        {
            Assert.DoesNotContain("Assist", s, StringComparison.Ordinal);
            Assert.DoesNotContain("Admin", s, StringComparison.Ordinal);
            Assert.DoesNotContain("ownerLink", s, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public class HighRiskWriteTests
{
    [Theory]
    [InlineData("del C:\\temp\\a.txt", true)]
    [InlineData("Remove-Item foo", true)]
    [InlineData("rmdir /s old", true)]
    [InlineData("format D:", true)]
    [InlineData("shutdown /s /t 0", true)]
    [InlineData("Restart-Computer", true)]
    [InlineData("net user bob /add", true)]
    [InlineData("icacls C:\\x /grant Everyone:F", true)]
    [InlineData("reg add HKLM\\Software\\X /v A /d 1", true)]
    [InlineData("echo hello", false)]
    [InlineData("dir C:\\", false)]
    [InlineData("Get-Process", false)]
    public void LooksHighRiskCommand(string cmd, bool expected)
        => Assert.Equal(expected, HighRiskWrite.LooksHighRiskCommand(cmd));

    [Fact]
    public void RunCommand_Uses_Args()
    {
        var args = new Dictionary<string, string?> { ["command"] = "echo hi" };
        Assert.False(HighRiskWrite.IsHighRisk(ToolKind.RunCommand, args));
        args["command"] = "del x";
        Assert.True(HighRiskWrite.IsHighRisk(ToolKind.RunCommand, args));
    }
}

public class TrustLevelPolicyTests
{
    [Fact]
    public void AskEveryTime_NeedsConfirm_For_Writes()
    {
        Assert.True(TrustLevelPolicy.NeedsLocalConfirm(TrustLevelKind.AskEveryTime, ToolKind.RunCommand,
            new Dictionary<string, string?> { ["command"] = "echo x" }));
    }

    [Fact]
    public void Relaxed_Skips_LowRisk_But_Asks_HighRisk()
    {
        Assert.False(TrustLevelPolicy.NeedsLocalConfirm(TrustLevelKind.Relaxed, ToolKind.RunCommand,
            new Dictionary<string, string?> { ["command"] = "echo x" }));
        Assert.True(TrustLevelPolicy.NeedsLocalConfirm(TrustLevelKind.Relaxed, ToolKind.RunCommand,
            new Dictionary<string, string?> { ["command"] = "shutdown /s" }));
    }

    [Fact]
    public void FullTrust_Never_NeedsConfirm()
    {
        Assert.False(TrustLevelPolicy.NeedsLocalConfirm(TrustLevelKind.FullTrust, ToolKind.RunCommand,
            new Dictionary<string, string?> { ["command"] = "del x" }));
    }

    [Fact]
    public void ReadOnly_Never_NeedsConfirm()
    {
        Assert.False(TrustLevelPolicy.NeedsLocalConfirm(TrustLevelKind.AskEveryTime, ToolKind.ListProcesses));
    }

    [Fact]
    public void FullTrust_Not_Persistable()
    {
        Assert.False(TrustLevelPolicy.IsPersistable(TrustLevelKind.FullTrust));
        Assert.Equal(TrustLevelKind.AskEveryTime, TrustLevelPolicy.Persistable(TrustLevelKind.FullTrust));
        Assert.True(TrustLevelPolicy.IsPersistable(TrustLevelKind.Relaxed));
    }
}

public class ConfirmedChangeListTests
{
    [Fact]
    public void Filter_Includes_Confirmed_And_Auto_Excludes_Denied()
    {
        var rows = new (string, string)[]
        {
            ("tool_confirmed", "tool=RunCommand; echo"),
            ("tool_denied_user", "tool=RunCommand; del"),
            ("tool_denied_perm", "guest"),
            ("tool_auto_relaxed", "tool=RunCommand; echo2"),
            ("tool_auto_fulltrust", "tool=RunCommand; echo3"),
            ("tool_ok", "should skip"),
            ("pair_ok", "skip")
        };
        var filtered = ConfirmedChangeList.Filter(rows);
        Assert.Equal(3, filtered.Count);
        Assert.All(filtered, x => Assert.DoesNotContain("denied", x.EventType, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(filtered, x => x.EventType == "tool_confirmed");
        Assert.Contains(filtered, x => x.EventType == "tool_auto_relaxed");
        Assert.Contains(filtered, x => x.EventType == "tool_auto_fulltrust");
    }

    [Fact]
    public void DisplayLine_Labels()
    {
        var line = ConfirmedChangeList.DisplayLine("tool_confirmed", "cmd=echo", "2026-11-05 12:00:00");
        Assert.Contains("已确认", line);
        Assert.Contains("echo", line);
        Assert.Contains("2026-11-05", line);
    }

    [Fact]
    public void HumanizeDetail_Strips_Internal_Fields_Keeps_Headline()
    {
        var raw = "tool=RunCommand; source=协助者; via=ui; result=approved; at=2026-11-05T12:00:00Z; 运行一条命令 · 命令：echo hello";
        var h = ConfirmedChangeList.HumanizeDetail(raw);
        Assert.Equal("运行一条命令 · 命令：echo hello", h);
        Assert.DoesNotContain("RunCommand", h);
        Assert.DoesNotContain("tool=", h);

        var line = ConfirmedChangeList.DisplayLine("tool_confirmed", raw, "2026-11-05 12:00:00");
        Assert.Contains("已确认", line);
        Assert.Contains("运行一条命令", line);
        Assert.DoesNotContain("tool=RunCommand", line);
    }
}

public class TrustLevelSessionTests
{
    [Fact]
    public void Default_Is_AskEveryTime()
    {
        // 不依赖本机已保存偏好：直接测 EffectiveForClient 与 Enable/Revert
        var s = new TrustLevelSession();
        // 可能读到本机已保存宽松；强制设回
        s.SetPreference(TrustLevelKind.AskEveryTime);
        Assert.Equal(TrustLevelKind.AskEveryTime, s.Effective);
        Assert.False(s.IsFullTrustActive);
    }

    [Fact]
    public void FullTrust_Requires_Bound_Token_And_Clears_On_New_Peer()
    {
        var s = new TrustLevelSession();
        s.SetPreference(TrustLevelKind.AskEveryTime);
        s.EnableFullTrust(Array.Empty<string>());
        Assert.True(s.IsFullTrustActive);
        Assert.Equal(TrustLevelKind.AskEveryTime, s.EffectiveForClient("t1")); // 未绑定不放行
        s.OnClientPaired("t1");
        Assert.Equal(TrustLevelKind.FullTrust, s.EffectiveForClient("t1"));
        s.OnClientPaired("t2"); // 换人
        Assert.False(s.IsFullTrustActive);
        Assert.Equal(TrustLevelKind.AskEveryTime, s.EffectiveForClient("t2"));
    }

    [Fact]
    public void SessionRefresh_Clears_FullTrust_Keeps_Relaxed_Preference()
    {
        var s = new TrustLevelSession();
        s.SetPreference(TrustLevelKind.Relaxed);
        s.EnableFullTrust(new[] { "tok" });
        Assert.Equal(TrustLevelKind.FullTrust, s.Effective);
        s.OnSessionRefreshed();
        Assert.False(s.IsFullTrustActive);
        Assert.Equal(TrustLevelKind.Relaxed, s.Effective);
        Assert.Equal(TrustLevelKind.Relaxed, s.Preference);
    }

    [Fact]
    public void RevertToAskEveryTime_Immediate()
    {
        var s = new TrustLevelSession();
        s.SetPreference(TrustLevelKind.Relaxed);
        s.EnableFullTrust(new[] { "a" });
        s.RevertToAskEveryTime();
        Assert.Equal(TrustLevelKind.AskEveryTime, s.Effective);
        Assert.False(s.IsFullTrustActive);
    }

    [Fact]
    public void EndFullTrust_Restores_Saved_Preference_Not_Forced_Ask()
    {
        var s = new TrustLevelSession();
        s.SetPreference(TrustLevelKind.Relaxed);
        s.EnableFullTrust(new[] { "a" });
        Assert.Equal(TrustLevelKind.FullTrust, s.Effective);
        s.EndFullTrust();
        Assert.False(s.IsFullTrustActive);
        Assert.Equal(TrustLevelKind.Relaxed, s.Effective);
        Assert.Equal(TrustLevelKind.Relaxed, s.Preference);
    }

    [Fact]
    public void SetPreference_FullTrust_Throws()
    {
        var s = new TrustLevelSession();
        Assert.Throws<ArgumentException>(() => s.SetPreference(TrustLevelKind.FullTrust));
    }
}
