using System;

namespace RemoteAssist.Protocol;

public static class RelayEndpoint
{
    public const string DefaultLocal = "127.0.0.1:17891";
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
        if (s.IndexOf("://", StringComparison.Ordinal) >= 0)
        {
            error = "endpoint must be host:port (no scheme)";
            return false;
        }

        if (s.StartsWith("[") && s.IndexOf(']') > 0)
        {
            var close = s.IndexOf(']');
            if (close <= 1 || close + 1 >= s.Length || s[close + 1] != ':')
            {
                error = "bad [ipv6]:port";
                return false;
            }
            host = s.Substring(1, close - 1);
            if (!int.TryParse(s.Substring(close + 2), out port) || port < 1 || port > 65535)
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
        host = s.Substring(0, idx).Trim();
        if (host.Length == 0 || !int.TryParse(s.Substring(idx + 1), out port) || port < 1 || port > 65535)
        {
            error = "bad host or port";
            return false;
        }
        return true;
    }

    public static string Format(string host, int port) => host + ":" + port;
}
