namespace RemoteAssist.Protocol;

/// <summary>
/// R005：协助端界面上的 MCP 安装三步指引（不进被协助端亲友主路径）。
/// </summary>
public static class McpInstallGuide
{
    public const string SectionTitle = "如何安装 MCP";

    public const string CopyConfigButton = "复制 MCP 配置";

    public const string Step1 = "1. 点「复制 MCP 配置」";

    public const string Step2 = "2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存";

    public const string Step3 =
        "3. 刷新或重启后再用智能体协助；对方电脑上要改动的操作仍会弹确认，智能体不能绕过";

    public static string StepsBlock =>
        Step1 + Environment.NewLine + Step2 + Environment.NewLine + Step3;

    public const string NeedCodePasswordHint = "请先粘贴会话码和验证密码，再复制 MCP 配置";

    public const string CopiedOkHint = "已复制 MCP 配置。请粘贴到 Cursor 的 MCP 设置并保存。";

    public const string CopiedPlaceholderHint =
        "已复制 MCP 配置（路径可能需改成实际 RemoteAssist.Mcp.exe）。请粘贴到 MCP 设置并保存。";

    public const string CopySkillButton = "复制 Skill 说明";

    /// <summary>R027：复制给智能体保存为技能的说明（只讲如何安装 MCP，非控制面）。</summary>
    public const string SkillInstructions =
        "把下面这段保存成智能体的技能说明（只用来告诉它怎么安装鹊桥的 MCP，不能代替对方本机确认）。\n" +
        "\n" +
        "1. 在鹊桥里点「复制 MCP 配置」。\n" +
        "2. 打开 Cursor（或同类工具）的 MCP 设置，粘贴进去并保存。\n" +
        "3. 刷新或重启后再用智能体协助。\n" +
        "\n" +
        "对方电脑上要改动的操作仍会弹确认，智能体不能绕过。不要用这段说明去远程控制电脑。";

    public static string SkillInstructionsClipboard =>
        SkillInstructions.Replace("\n", Environment.NewLine);

    public const string SkillCopiedHint =
        "已复制 Skill 说明。请交给智能体保存为技能，它只会用来安装 MCP。";
}
