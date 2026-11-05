namespace RemoteAssist.Protocol;

/// <summary>
/// 生成可粘贴到 Cursor mcp.json 的配置片段（含当前会话载荷）。
/// </summary>
public static class McpConfigSnippet
{
    public const string DefaultServerName = "remote-assist";
    public const string DefaultRelativeExeHint = "artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe";

    /// <summary>
    /// 推荐：exe + env.REMOTE_ASSIST_PAYLOAD（换会话码时只改 env）。
    /// </summary>
    public static string BuildCursorEnvPayload(
        string mcpExePath,
        string payload,
        string serverName = DefaultServerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mcpExePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        if (string.IsNullOrWhiteSpace(serverName))
            serverName = DefaultServerName;

        var cmd = ToJsonPath(mcpExePath.Trim());
        var pl = EscapeJson(payload.Trim());
        var name = EscapeJson(serverName.Trim());

        return
            "{\n" +
            "  \"mcpServers\": {\n" +
            $"    \"{name}\": {{\n" +
            $"      \"command\": \"{cmd}\",\n" +
            "      \"args\": [],\n" +
            "      \"env\": {\n" +
            $"        \"REMOTE_ASSIST_PAYLOAD\": \"{pl}\"\n" +
            "      }\n" +
            "    }\n" +
            "  }\n" +
            "}";
    }

    /// <summary>
    /// 备选：exe + args --payload（载荷写在 args 里）。
    /// </summary>
    public static string BuildCursorArgsPayload(
        string mcpExePath,
        string payload,
        string serverName = DefaultServerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mcpExePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        if (string.IsNullOrWhiteSpace(serverName))
            serverName = DefaultServerName;

        var cmd = ToJsonPath(mcpExePath.Trim());
        var pl = EscapeJson(payload.Trim());
        var name = EscapeJson(serverName.Trim());

        return
            "{\n" +
            "  \"mcpServers\": {\n" +
            $"    \"{name}\": {{\n" +
            $"      \"command\": \"{cmd}\",\n" +
            "      \"args\": [\n" +
            "        \"--payload\",\n" +
            $"        \"{pl}\"\n" +
            "      ]\n" +
            "    }\n" +
            "  }\n" +
            "}";
    }

    /// <summary>
    /// 剪贴板友好：env 写法在前，args 写法附后（纯文本，方便对照）。
    /// </summary>
    public static string BuildClipboardBundle(string mcpExePath, string payload, string serverName = DefaultServerName)
    {
        var env = BuildCursorEnvPayload(mcpExePath, payload, serverName);
        var args = BuildCursorArgsPayload(mcpExePath, payload, serverName);
        return
            "// Cursor mcp.json — env 写法（推荐；换会话码只改 REMOTE_ASSIST_PAYLOAD）\n" +
            env +
            "\n\n" +
            "// 备选：args --payload\n" +
            args +
            "\n";
    }

    /// <summary>Windows 路径转正斜杠，便于 JSON / 跨工具粘贴。</summary>
    public static string ToJsonPath(string path) =>
        EscapeJson(path.Replace('\\', '/'));

    public static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
}
