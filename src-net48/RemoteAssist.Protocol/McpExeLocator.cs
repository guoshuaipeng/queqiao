using System;
using System.Collections.Generic;
using System.IO;

namespace RemoteAssist.Protocol;

/// <summary>
/// 猜测本机 RemoteAssist.Mcp.exe 与仓库 scripts/publish-mcp.ps1 路径。
/// </summary>
public static class McpExeLocator
{
    private static readonly string[] RelativeCandidates =
    [
        Path.Combine("artifacts", "mcp-win-x64-sc", "RemoteAssist.Mcp.exe"),
        Path.Combine("artifacts", "mcp-win-x64-fd", "RemoteAssist.Mcp.exe"),
    ];

    public static string Resolve()
    {
        if (TryResolveExisting(out var existing))
            return existing;

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

    /// <summary>若找到真实存在的 MCP exe 则返回 true。</summary>
    public static bool TryResolveExisting(out string fullPath)
    {
        var fromEnv = Environment.GetEnvironmentVariable("REMOTE_ASSIST_MCP_EXE");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            try
            {
                var full = Path.GetFullPath(fromEnv.Trim());
                if (File.Exists(full))
                {
                    fullPath = full;
                    return true;
                }
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
                    {
                        fullPath = path;
                        return true;
                    }
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
                {
                    fullPath = siblingSc;
                    return true;
                }
                var siblingFd = Path.GetFullPath(Path.Combine(start, "..", "mcp-win-x64-fd", "RemoteAssist.Mcp.exe"));
                if (File.Exists(siblingFd))
                {
                    fullPath = siblingFd;
                    return true;
                }
            }
            catch
            {
                /* ignore */
            }
        }

        fullPath = "";
        return false;
    }

    /// <summary>向上查找含 scripts/publish-mcp.ps1 的仓库根。</summary>
    public static string? TryFindRepoRoot()
    {
        foreach (var start in CandidateRoots())
        {
            try
            {
                var dir = new DirectoryInfo(start);
                for (var i = 0; i < 10 && dir is not null; i++, dir = dir.Parent)
                {
                    var script = Path.Combine(dir.FullName, "scripts", "publish-mcp.ps1");
                    if (File.Exists(script))
                        return dir.FullName;
                }
            }
            catch
            {
                /* ignore */
            }
        }

        return null;
    }

    public static string? TryFindPublishScript()
    {
        var root = TryFindRepoRoot();
        if (root is null) return null;
        var script = Path.Combine(root, "scripts", "publish-mcp.ps1");
        return File.Exists(script) ? script : null;
    }

    /// <summary>可粘贴到 PowerShell 的发布命令（绝对路径优先）。</summary>
    public static string GetPublishCommandHint()
    {
        var script = TryFindPublishScript();
        if (!string.IsNullOrEmpty(script))
            return $"powershell -NoProfile -ExecutionPolicy Bypass -File \"{script}\"";

        return "powershell -NoProfile -ExecutionPolicy Bypass -File .\\scripts\\publish-mcp.ps1";
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
