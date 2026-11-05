namespace RemoteAssist.Protocol;

/// <summary>
/// 中继地址 host:port 解析（无 scheme）。
/// </summary>
public static class RelayEndpoint
{
    /// <summary>本机开发 / smoke 用。</summary>
    public const string DefaultLocal = "127.0.0.1:17891";

    /// <summary>正式默认公网中继（见 docs/relay-deploy.md）；用户设置与 CLI 可覆盖。</summary>
    public const string DefaultPublic = "121.196.45.81:17891";

    public const string DefaultEndpoint = DefaultPublic;

    public static bool TryParse(string? endpoint, out string host, out int port, out string? error)
    {
        host = "";
        port = 0;
        error = null;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            error = "empty endpoint";
            return false;
        }

        var s = endpoint.Trim();
        if (s.Contains("://", StringComparison.Ordinal))
        {
            error = "endpoint must be host:port (no scheme)";
            return false;
        }

        // [ipv6]:port
        if (s.StartsWith('[') && s.Contains(']', StringComparison.Ordinal))
        {
            var close = s.IndexOf(']');
            if (close <= 1 || close + 1 >= s.Length || s[close + 1] != ':')
            {
                error = "bad [ipv6]:port";
                return false;
            }
            host = s[1..close];
            if (!int.TryParse(s[(close + 2)..], out port) || port is < 1 or > 65535)
            {
                error = "bad port";
                return false;
            }
            return true;
        }

        var idx = s.LastIndexOf(':');
        if (idx <= 0 || idx == s.Length - 1)
        {
            error = "expect host:port";
            return false;
        }
        host = s[..idx].Trim();
        if (host.Length == 0 || !int.TryParse(s[(idx + 1)..], out port) || port is < 1 or > 65535)
        {
            error = "bad host or port";
            return false;
        }
        return true;
    }

    public static string Format(string host, int port) => $"{host}:{port}";
}
