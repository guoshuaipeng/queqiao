using System;
using System.Collections.Generic;

namespace RemoteAssist.Protocol;

public static class PairQrPayload
{
    public const string SchemePrefix = "remoteassist:pair";

    public static string Build(string host, int port, string code, string? fingerprint, bool useTls) =>
        BuildLan(host, port, code, fingerprint, useTls);

    public static string BuildLan(string host, int port, string code, string? fingerprint, bool useTls)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(SchemePrefix)
            .Append("?v=1&via=lan&host=").Append(Uri.EscapeDataString(host.Trim()))
            .Append("&port=").Append(port)
            .Append("&code=").Append(Uri.EscapeDataString(SessionCodeGenerator.Normalize(code)))
            .Append("&tls=").Append(useTls ? "1" : "0");
        var fp = TlsCertUtil.NormalizeFingerprint(fingerprint);
        if (!string.IsNullOrEmpty(fp))
            sb.Append("&fp=").Append(Uri.EscapeDataString(fp));
        return sb.ToString();
    }

    public static string BuildRelay(string relayEndpoint, string code, string password, string? fingerprint, bool useTls)
    {
        if (!RelayEndpoint.TryParse(relayEndpoint, out _, out _, out var err))
            throw new ArgumentException(err ?? "bad relay", nameof(relayEndpoint));
        var pwd = SessionPasswordGenerator.Normalize(password);
        if (string.IsNullOrWhiteSpace(pwd))
            throw new ArgumentException("password is required", nameof(password));
        var sb = new System.Text.StringBuilder();
        sb.Append(SchemePrefix)
            .Append("?v=2&via=relay&relay=").Append(Uri.EscapeDataString(relayEndpoint.Trim()))
            .Append("&code=").Append(Uri.EscapeDataString(SessionCodeGenerator.Normalize(code)))
            .Append("&pwd=").Append(Uri.EscapeDataString(pwd))
            .Append("&tls=").Append(useTls ? "1" : "0");
        var fp = TlsCertUtil.NormalizeFingerprint(fingerprint);
        if (!string.IsNullOrEmpty(fp))
            sb.Append("&fp=").Append(Uri.EscapeDataString(fp));
        return sb.ToString();
    }

    public static bool TryParse(string payload, out string host, out int port, out string code) =>
        TryParse(payload, out host, out port, out code, out _, out _);

    public static bool TryParse(
        string payload, out string host, out int port, out string code, out string? fingerprint, out bool useTls) =>
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
        if (string.IsNullOrWhiteSpace(payload)) return false;
        var s = payload.Trim();
        if (!s.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase)) return false;
        var q = s.IndexOf('?');
        if (q < 0 || q == s.Length - 1) return false;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in s.Substring(q + 1).Split('&'))
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            var k = Uri.UnescapeDataString(part.Substring(0, eq).Trim());
            var v = Uri.UnescapeDataString(part.Substring(eq + 1).Trim());
            map[k] = v;
        }
        if (!map.TryGetValue("code", out var c) || string.IsNullOrWhiteSpace(c)) return false;
        code = SessionCodeGenerator.Normalize(c);
        if (map.TryGetValue("pwd", out var pwdRaw) && !string.IsNullOrWhiteSpace(pwdRaw))
            password = SessionPasswordGenerator.Normalize(pwdRaw);
        if (map.TryGetValue("tls", out var tlsRaw))
            useTls = tlsRaw == "1" || tlsRaw.Equals("true", StringComparison.OrdinalIgnoreCase);
        if (map.TryGetValue("fp", out var fp) && !string.IsNullOrWhiteSpace(fp))
            fingerprint = TlsCertUtil.NormalizeFingerprint(fp);

        var via = map.TryGetValue("via", out var viaRaw) ? viaRaw.Trim() : "";
        if (via.Equals("relay", StringComparison.OrdinalIgnoreCase) || map.ContainsKey("relay"))
        {
            if (!map.TryGetValue("relay", out var rel) || string.IsNullOrWhiteSpace(rel)) return false;
            if (!RelayEndpoint.TryParse(rel, out var rh, out var rp, out _)) return false;
            viaRelay = true;
            relayEndpoint = rh + ":" + rp;
            host = rh;
            port = rp;
            return true;
        }

        if (!map.TryGetValue("host", out var h) || string.IsNullOrWhiteSpace(h)) return false;
        if (!map.TryGetValue("port", out var pStr) || !int.TryParse(pStr, out var p) || p < 1 || p > 65535) return false;
        host = h.Trim();
        port = p;
        viaRelay = false;
        return true;
    }
}
