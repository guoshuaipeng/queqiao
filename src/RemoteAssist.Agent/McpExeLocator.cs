namespace RemoteAssist.Agent;

/// <summary>
/// 猜测本机 RemoteAssist.Mcp.exe 路径，供托盘「复制 MCP 配置」使用。
/// </summary>
internal static class McpExeLocator
{
    private static readonly string[] RelativeCandidates =
    [
        Path.Combine("artifacts", "mcp-win-x64-sc", "RemoteAssist.Mcp.exe"),
        Path.Combine("artifacts", "mcp-win-x64-fd", "RemoteAssist.Mcp.exe"),
    ];

    public static string Resolve()
    {
        var fromEnv = Environment.GetEnvironmentVariable("REMOTE_ASSIST_MCP_EXE");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            try
            {
                var full = Path.GetFullPath(fromEnv.Trim());
                if (File.Exists(full))
                    return full;
            }
            catch
            {
                /* ignore bad env */
            }
        }

        foreach (var start in CandidateRoots())
        {
            foreach (var rel in RelativeCandidates)
            {
                try
                {
                    var path = Path.GetFullPath(Path.Combine(start, rel));
                    if (File.Exists(path))
                        return path;
                }
                catch
                {
                    /* ignore */
                }
            }

            // Agent 已 publish 到 artifacts/agent-*-sc 时，MCP 常在兄弟目录
            try
            {
                var siblingSc = Path.GetFullPath(Path.Combine(start, "..", "mcp-win-x64-sc", "RemoteAssist.Mcp.exe"));
                if (File.Exists(siblingSc))
                    return siblingSc;
                var siblingFd = Path.GetFullPath(Path.Combine(start, "..", "mcp-win-x64-fd", "RemoteAssist.Mcp.exe"));
                if (File.Exists(siblingFd))
                    return siblingFd;
            }
            catch
            {
                /* ignore */
            }
        }

        // 找不到已 publish 的 exe：返回仓库相对提示路径（粘贴后用户可改）
        foreach (var start in CandidateRoots())
        {
            var hint = Path.Combine(start, RelativeCandidates[0]);
            try
            {
                return Path.GetFullPath(hint);
            }
            catch
            {
                /* ignore */
            }
        }

        return Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "IdeaProjects", "remote-assist", RelativeCandidates[0]));
    }

    private static List<string> CandidateRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();

        void Add(string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            try
            {
                var full = Path.GetFullPath(dir);
                if (seen.Add(full))
                    list.Add(full);
            }
            catch
            {
                /* ignore */
            }
        }

        Add(AppContext.BaseDirectory);
        Add(Environment.CurrentDirectory);

        try
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
                Add(dir.FullName);
        }
        catch
        {
            /* ignore */
        }

        return list;
    }
}
