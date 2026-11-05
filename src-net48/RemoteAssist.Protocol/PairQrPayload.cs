namespace RemoteAssist.Protocol;

/// <summary>
/// 会话码二维码载荷。
/// 格式：remoteassist:pair?v=1&amp;host=...&amp;port=...&amp;code=...&amp;tls=1&amp;fp=&lt;SHA256&gt;
/// tls=0 表示明文（仅 --insecure 调试）。
/// </summary>
public static class PairQrPayload
{
    public const string SchemePrefix = "remoteassist:pair";

    public static string Build(
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
            .Append("?v=1&host=").Append(Uri.EscapeDataString(host.Trim()))
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

    /// <summary>兼容旧调用：仅解析 host/port/code。</summary>
    public static bool TryParse(string payload, out string host, out int port, out string code) =>
        TryParse(payload, out host, out port, out code, out _, out _);

    public static bool TryParse(
        string payload,
        out string host,
        out int port,
        out string code,
        out string? fingerprint,
        out bool useTls)
    {
        host = "";
        port = 0;
        code = "";
        fingerprint = null;
        useTls = true;
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
        foreach (var part in query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
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
        if (code.Length == 0)
            return false;

        if (map.TryGetValue("tls", out var tlsStr) &&
            (tlsStr == "0" || tlsStr.Equals("false", StringComparison.OrdinalIgnoreCase) ||
             tlsStr.Equals("off", StringComparison.OrdinalIgnoreCase)))
        {
            useTls = false;
        }
        else
        {
            useTls = true;
        }

        if (map.TryGetValue("fp", out var fpRaw) && !string.IsNullOrWhiteSpace(fpRaw))
            fingerprint = TlsCertUtil.NormalizeFingerprint(fpRaw);
        else
            fingerprint = null;

        // 旧载荷无 tls/fp：仍视为「想用 TLS」，但缺指纹需调用方用 --fp 或 --insecure 补齐
        return true;
    }
}
