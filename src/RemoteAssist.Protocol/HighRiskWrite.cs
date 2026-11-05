namespace RemoteAssist.Protocol;

/// <summary>
/// 高风险写操作判定（宽松档仍须询问）。对齐 docs/trust-levels.md：
/// 删除文件或文件夹、格式化、关机/重启、改用户或权限、改注册表；
/// 「执行命令」若明显属于这类意图也仍要问。
/// </summary>
public static class HighRiskWrite
{
    public static bool IsHighRisk(ToolKind tool, IReadOnlyDictionary<string, string?>? args = null)
    {
        if (ToolCapabilities.IsReadOnly(tool))
            return false;

        // 当前写工具主要是 RunCommand；参数意图决定是否高风险
        if (tool == ToolKind.RunCommand)
            return LooksHighRiskCommand(GetArg(args, "command"));

        // 将来若新增删文件等专用工具，默认视为高风险
        return true;
    }

    public static bool LooksHighRiskCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        var c = command.Trim().ToLowerInvariant();

        // 删除文件或文件夹
        if (c.Contains("del ") || c.StartsWith("del ") ||
            c.Contains("rmdir") || c.Contains("rd ") ||
            c.Contains("remove-item") || c.Contains("rm -") ||
            c.Contains(" erase ") || c.StartsWith("erase ") ||
            c.Contains("unlink") || c.Contains("shred "))
            return true;

        // 格式化
        if (c.Contains("format ") || c.StartsWith("format ") ||
            c.Contains("format.com") || c.Contains("diskpart") ||
            c.Contains("mkfs"))
            return true;

        // 关机 / 重启
        if (c.Contains("shutdown") || c.Contains("restart-computer") ||
            c.Contains("stop-computer") || c.Contains("reboot") ||
            c.Contains("init 0") || c.Contains("init 6") ||
            (c.Contains("poweroff")) ||
            c.Contains("wmic os where") && c.Contains("shutdown"))
            return true;

        // 改用户或权限
        if (c.Contains("net user") || c.Contains("net localgroup") ||
            c.Contains("adduser") || c.Contains("usermod") ||
            c.Contains("icacls") || c.Contains("takeown") ||
            c.Contains("cacls") || c.Contains("setacl") ||
            c.Contains("runas ") || c.Contains("chmod ") ||
            c.Contains("chown ") || c.Contains("passwd"))
            return true;

        // 改注册表
        if (c.Contains("reg add") || c.Contains("reg delete") ||
            c.Contains("reg import") || c.Contains("regedit") ||
            c.Contains("set-itemproperty") && c.Contains("hk") ||
            c.Contains("new-itemproperty") ||
            c.Contains(@"\hkey_") || c.Contains("hklm") || c.Contains("hkcu\\"))
            return true;

        return false;
    }

    private static string? GetArg(IReadOnlyDictionary<string, string?>? args, string key) =>
        args is not null && args.TryGetValue(key, out var v) ? v : null;
}
