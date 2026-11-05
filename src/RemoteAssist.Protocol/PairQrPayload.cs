namespace RemoteAssist.Protocol;

/// <summary>
/// 会话码二维码载荷。v1 Helper 仍用手输码；载荷便于以后扫码与手机展示。
/// 格式：remoteassist:pair?v=1&amp;host=...&amp;port=...&amp;code=...
/// </summary>
public static class PairQrPayload
{
    public const string SchemePrefix = "remoteassist:pair";

    public static string Build(string host, int port, string code)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("host is required", nameof(host));
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        var normalized = SessionCodeGenerator.Normalize(code);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("code is required", nameof(code));

        return $"{SchemePrefix}?v=1&host={Uri.EscapeDataString(host.Trim())}&port={port}&code={Uri.EscapeDataString(normalized)}";
    }

    public static bool TryParse(string payload, out string host, out int port, out string code)
    {
        host = "";
        port = 0;
        code = "";
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        var text = payload.Trim();
        if (!text.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var q = text.IndexOf('?');
        if (q < 0 || q == text.Length - 1)
            return false;

        var query = text[(q + 1)..];
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            var k = Uri.UnescapeDataString(part[..eq]);
            var v = Uri.UnescapeDataString(part[(eq + 1)..]);
            map[k] = v;
        }

        if (!map.TryGetValue("host", out var h) || string.IsNullOrWhiteSpace(h))
            return false;
        if (!map.TryGetValue("port", out var pStr) || !int.TryParse(pStr, out var p) || p is < 1 or > 65535)
            return false;
        if (!map.TryGetValue("code", out var c) || string.IsNullOrWhiteSpace(c))
            return false;

        host = h.Trim();
        port = p;
        code = SessionCodeGenerator.Normalize(c);
        return code.Length > 0;
    }
}
