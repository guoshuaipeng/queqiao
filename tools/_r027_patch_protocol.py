from pathlib import Path

# --- McpInstallGuide.cs ---
p = Path("src/RemoteAssist.Protocol/McpInstallGuide.cs")
t = p.read_text(encoding="utf-8")
if "CopySkillButton" not in t:
    insert = '''
    public const string CopySkillButton = "复制 Skill 说明";

    /// <summary>R027：复制给智能体保存为技能的说明（只讲如何安装 MCP，非控制面）。</summary>
    public const string SkillInstructions =
        "把下面这段保存成智能体的技能说明（只用来告诉它怎么安装鹊桥的 MCP，不能代替对方本机确认）。\\n" +
        "\\n" +
        "1. 在鹊桥里点「复制 MCP 配置」。\\n" +
        "2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。\\n" +
        "3. 刷新或重启后再用智能体协助。\\n" +
        "\\n" +
        "对方电脑上要改动的操作仍会弹确认，智能体不能绕过。不要用这段说明去远程控制电脑。";

    public static string SkillInstructionsClipboard =>
        SkillInstructions.Replace("\\n", Environment.NewLine);

    public const string SkillCopiedHint =
        "已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。";
'''
    # insert before closing brace of class
    needle = '    public const string CopiedPlaceholderHint =\n        "已复制 MCP 配置（路径可能需改成实际 RemoteAssist.Mcp.exe）。请粘贴到 MCP 设置并保存。";\n}'
    if needle not in t:
        raise SystemExit("McpInstallGuide insert point not found")
    t = t.replace(
        needle,
        '    public const string CopiedPlaceholderHint =\n        "已复制 MCP 配置（路径可能需改成实际 RemoteAssist.Mcp.exe）。请粘贴到 MCP 设置并保存。";\n'
        + insert
        + "}",
    )
    p.write_text(t, encoding="utf-8")
    print("McpInstallGuide updated")
else:
    print("McpInstallGuide already has skill")

# --- tests ---
tp = Path("tests/RemoteAssist.Protocol.Tests/McpInstallGuideTests.cs")
tt = tp.read_text(encoding="utf-8")
if "SkillInstructions_IsVerbatim_AndHintOnceFriendly" not in tt:
    extra = '''
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
'''
    # before last closing braces of class
    if "    [Fact]\n    public void BannerCopy_UsesPlainSetting_NoZeroWidthChars()" not in tt:
        raise SystemExit("test anchor missing")
    tt = tt.replace(
        "    [Fact]\n    public void BannerCopy_UsesPlainSetting_NoZeroWidthChars()",
        extra + "\n    [Fact]\n    public void BannerCopy_UsesPlainSetting_NoZeroWidthChars()",
    )
    tp.write_text(tt, encoding="utf-8")
    print("tests updated")
else:
    print("tests already")
