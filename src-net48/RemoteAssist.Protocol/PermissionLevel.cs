namespace RemoteAssist.Protocol;

public enum PermissionLevel
{
    Guest = 0,
    Assist = 1,
    Admin = 2
}

public enum ToolKind
{
    ListProcesses,
    ReadLogTail,
    ListDir,
    ReadFile,
    RunCommand
}

public static class ToolCapabilities
{
    public static bool IsReadOnly(ToolKind tool) =>
        tool is ToolKind.ListProcesses or ToolKind.ReadLogTail or ToolKind.ListDir or ToolKind.ReadFile;

    public static bool RequiresConfirmation(ToolKind tool, PermissionLevel level) =>
        !IsReadOnly(tool) && level == PermissionLevel.Assist;

    public static bool IsAllowed(ToolKind tool, PermissionLevel level)
    {
        if (IsReadOnly(tool))
            return true;
        return level >= PermissionLevel.Assist;
    }
}
