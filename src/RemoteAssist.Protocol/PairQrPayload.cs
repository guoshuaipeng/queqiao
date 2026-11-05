namespace RemoteAssist.Protocol;

/// <summary>
/// 会话载荷：remoteassist:pair?v=1&amp;...
/// v2 起默认经中继：via=relay&amp;relay=host:port&amp;code=&amp;pwd=&amp;tls=&amp;fp=
/// 局域网高级：via=lan&amp;host=&amp;port=...
/// </summary>
public static class PairQrPayload
{
    public const string SchemePrefix = "remoteassist:pair";

    public static string Build(
        string host,
        int port,
        string code,
        string? fingerprint = null,
        bool useTls = true) =>
        BuildLan(host, port, code, fingerprint, useTls);

    public static string BuildLan(
        string host,
        int port,
        string code,
        string? fingerprint = null,
        bool useTls = true)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("host is required", nameof(host));
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        var normalized = SessionCodeGenerator.Normalize(code);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("code is required", nameof(code));

        var sb = new System.Text.StringBuilder();
        sb.Append(SchemePrefix)
            .Append("?v=1&via=lan&host=").Append(Uri.EscapeDataString(host.Trim()))
            .Append("&port=").Append(port)
            .Append("&code=").Append(Uri.EscapeDataString(normalized));

        if (useTls)
        {
            var fp = TlsCertUtil.NormalizeFingerprint(fingerprint);
            if (string.IsNullOrEmpty(fp))
                throw new ArgumentException("TLS 载荷需要证书指纹 fingerprint", nameof(fingerprint));
            sb.Append("&tls=1&fp=").Append(Uri.EscapeDataString(fp));
        }
        else
        {
            sb.Append("&tls=0");
        }

        return sb.ToString();
    }

    public static string BuildRelay(
        string relayEndpoint,
        string code,
        string password,
        string? fingerprint = null,
        bool useTls = true)
    {
        if (!RelayEndpoint.TryParse(relayEndpoint, out _, out _, out var err))
            throw new ArgumentException(err, nameof(relayEndpoint));

        var normalized = SessionCodeGenerator.Normalize(code);
        var pwd = SessionPasswordGenerator.Normalize(password);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("code is required", nameof(code));
        if (string.IsNullOrWhiteSpace(pwd))
            throw new ArgumentException("password is required", nameof(password));

        var sb = new System.Text.StringBuilder();
        sb.Append(SchemePrefix)
            .Append("?v=2&via=relay&relay=").Append(Uri.EscapeDataString(relayEndpoint.Trim()))
            .Append("&code=").Append(Uri.EscapeDataString(normalized))
            .Append("&pwd=").Append(Uri.EscapeDataString(pwd));

        if (useTls)
        {
            var fp = TlsCertUtil.NormalizeFingerprint(fingerprint);
            if (string.IsNullOrEmpty(fp))
                throw new ArgumentException("TLS 载荷需要证书指纹 fingerprint", nameof(fingerprint));
            sb.Append("&tls=1&fp=").Append(Uri.EscapeDataString(fp));
        }
        else
        {
            sb.Append("&tls=0");
        }

        return sb.ToString();
    }

    public static bool TryParse(string payload, out string host, out int port, out string code) =>
        TryParse(payload, out host, out port, out code, out _, out _);

    public static bool TryParse(
        string payload,
        out string host,
        out int port,
        out string code,
        out string? fingerprint,
        out bool useTls) =>
        TryParseFull(payload, out host, out port, out code, out fingerprint, out useTls, out _, out _, out _);

    public static bool TryParseFull(
        string payload,
        out string host,
        out int port,
        out string code,
        out string? fingerprint,
        out bool useTls,
        out bool viaRelay,
        out string? relayEndpoint,
        out string? password)
    {
        host = "";
        port = 0;
        code = "";
        fingerprint = null;
        useTls = true;
        viaRelay = false;
        relayEndpoint = null;
        password = null;

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

        if (!map.TryGetValue("code", out var c) || string.IsNullOrWhiteSpace(c))
            return false;
        code = SessionCodeGenerator.Normalize(c);
        if (code.Length == 0)
            return false;

        if (map.TryGetValue("pwd", out var pwdRaw) && !string.IsNullOrWhiteSpace(pwdRaw))
            password = SessionPasswordGenerator.Normalize(pwdRaw);

        if (map.TryGetValue("tls", out var tlsStr) &&
            (tlsStr == "0" || tlsStr.Equals("false", StringComparison.OrdinalIgnoreCase) ||
             tlsStr.Equals("off", StringComparison.OrdinalIgnoreCase)))
            useTls = false;
        else
            useTls = true;

        if (map.TryGetValue("fp", out var fpRaw) && !string.IsNullOrWhiteSpace(fpRaw))
            fingerprint = TlsCertUtil.NormalizeFingerprint(fpRaw);

        var via = map.TryGetValue("via", out var viaRaw) ? viaRaw.Trim() : "";
        if (via.Equals("relay", StringComparison.OrdinalIgnoreCase) ||
            map.ContainsKey("relay"))
        {
            if (!map.TryGetValue("relay", out var rel) || string.IsNullOrWhiteSpace(rel))
                return false;
            if (!RelayEndpoint.TryParse(rel, out var rh, out var rp, out _))
                return false;
            viaRelay = true;
            relayEndpoint = $"{rh}:{rp}";
            host = rh;
            port = rp;
            return true;
        }

        // LAN (default for old payloads without via=)
        if (!map.TryGetValue("host", out var h) || string.IsNullOrWhiteSpace(h))
            return false;
        if (!map.TryGetValue("port", out var pStr) || !int.TryParse(pStr, out var p) || p is < 1 or > 65535)
            return false;
        host = h.Trim();
        port = p;
        viaRelay = false;
        return true;
    }
}
