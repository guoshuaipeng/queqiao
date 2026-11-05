namespace RemoteAssist.Protocol;

/// <summary>
/// 协助端 GUI 连接草稿解析：中继 code+密码 / 载荷，或局域网高级手填。
/// </summary>
public static class ConnectDraftParser
{
    public const int DefaultPort = AssistConnectOptions.DefaultPort;

    public static bool TryParse(
        string? input,
        string? hostField,
        string? portField,
        string? fingerprintField,
        PermissionLevel permission,
        bool insecure,
        out AssistConnectOptions options,
        out string preview,
        out string error) =>
        TryParse(input, hostField, portField, fingerprintField, permission, insecure,
            passwordField: null, relayField: null, preferRelay: true,
            out options, out preview, out error);

    public static bool TryParse(
        string? input,
        string? hostField,
        string? portField,
        string? fingerprintField,
        PermissionLevel permission,
        bool insecure,
        string? passwordField,
        string? relayField,
        bool preferRelay,
        out AssistConnectOptions options,
        out string preview,
        out string error)
    {
        options = null!;
        preview = "";
        error = "";

        var text = (input ?? "").Trim();
        var useTls = !insecure;
        string? fp = null;
        string code;
        string? password = string.IsNullOrWhiteSpace(passwordField)
            ? null
            : SessionPasswordGenerator.Normalize(passwordField);
        string? relayEp = string.IsNullOrWhiteSpace(relayField) ? null : relayField.Trim();
        var viaRelay = preferRelay;

        if (text.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!PairQrPayload.TryParseFull(text, out var host, out var port, out code, out fp, out var payloadTls,
                    out var payloadRelay, out var payloadRelayEp, out var payloadPwd))
            {
                error = "无法解析会话载荷";
                return false;
            }

            if (!payloadTls)
                useTls = false;
            // 载荷写明 via=lan/relay 时以载荷为准；勿被 PreferRelay 强行改写
            viaRelay = payloadRelay;
            if (payloadRelayEp is not null)
                relayEp = payloadRelayEp;
            if (!string.IsNullOrWhiteSpace(payloadPwd))
                password = payloadPwd;

            if (!string.IsNullOrWhiteSpace(hostField) && !viaRelay)
                host = hostField.Trim();
            if (TryParsePort(portField, out var portOverride, out var portErr))
            {
                if (portErr is not null) { error = portErr; return false; }
                if (portOverride is int p && !viaRelay)
                    port = p;
            }
            else { error = "端口无效"; return false; }

            if (!string.IsNullOrWhiteSpace(fingerprintField))
                fp = TlsCertUtil.NormalizeFingerprint(fingerprintField);

            if (insecure) useTls = false;
            if (useTls && string.IsNullOrEmpty(TlsCertUtil.NormalizeFingerprint(fp)))
            {
                error = "默认 TLS 需要证书指纹：请粘贴含 fp= 的载荷，或填写指纹；明文调试请勾选「明文」";
                return false;
            }

            if (viaRelay)
            {
                relayEp ??= $"{host}:{port}";
                if (!RelayEndpoint.TryParse(relayEp, out _, out _, out var rerr))
                {
                    error = rerr;
                    return false;
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    error = "中继连接需要验证密码（载荷 pwd= 或密码框）";
                    return false;
                }

                options = new AssistConnectOptions
                {
                    Host = host,
                    Port = port,
                    Code = code,
                    Permission = permission,
                    Fingerprint = useTls ? TlsCertUtil.NormalizeFingerprint(fp) : null,
                    Insecure = !useTls,
                    ViaRelay = true,
                    RelayEndpoint = relayEp,
                    Password = password
                };
            }
            else
            {
                options = new AssistConnectOptions
                {
                    Host = host,
                    Port = port,
                    Code = code,
                    Permission = permission,
                    Fingerprint = useTls ? TlsCertUtil.NormalizeFingerprint(fp) : null,
                    Insecure = !useTls,
                    ViaRelay = false,
                    Password = password
                };
            }

            preview = FormatPreview(options);
            return true;
        }

        // 非载荷：会话码 + 密码（中继）或 host+code（LAN）
        code = string.IsNullOrWhiteSpace(text) ? "" : SessionCodeGenerator.Normalize(text);
        if (string.IsNullOrWhiteSpace(code))
        {
            error = "请填写会话码，或粘贴整段 remoteassist:pair 载荷";
            return false;
        }

        if (preferRelay || !string.IsNullOrWhiteSpace(relayField))
        {
            relayEp ??= RelayEndpoint.DefaultEndpoint;
            if (!string.IsNullOrWhiteSpace(relayField))
                relayEp = relayField.Trim();
            if (!RelayEndpoint.TryParse(relayEp, out var rh, out var rp, out var rerr))
            {
                error = rerr;
                return false;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                error = "中继连接需要验证密码";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(fingerprintField))
                fp = TlsCertUtil.NormalizeFingerprint(fingerprintField);
            if (insecure) useTls = false;
            if (useTls && string.IsNullOrEmpty(TlsCertUtil.NormalizeFingerprint(fp)))
            {
                error = "默认 TLS 需要证书指纹（或勾选明文仅调试）";
                return false;
            }

            options = new AssistConnectOptions
            {
                Host = rh,
                Port = rp,
                Code = code,
                Permission = permission,
                Fingerprint = useTls ? TlsCertUtil.NormalizeFingerprint(fp) : null,
                Insecure = !useTls,
                ViaRelay = true,
                RelayEndpoint = $"{rh}:{rp}",
                Password = password
            };
            preview = FormatPreview(options);
            return true;
        }

        var host2 = (hostField ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host2))
        {
            error = "局域网模式请填写主机 IP，或开启中继并填密码";
            return false;
        }
        var port2 = DefaultPort;
        if (!TryParsePort(portField, out var po, out var pe))
        {
            error = "端口无效";
            return false;
        }
        if (pe is not null) { error = pe; return false; }
        if (po is int p2) port2 = p2;
        fp = string.IsNullOrWhiteSpace(fingerprintField) ? null : TlsCertUtil.NormalizeFingerprint(fingerprintField);
        if (insecure) useTls = false;
        if (useTls && string.IsNullOrEmpty(TlsCertUtil.NormalizeFingerprint(fp)))
        {
            error = "默认 TLS 需要证书指纹；明文调试请勾选「明文」";
            return false;
        }

        options = new AssistConnectOptions
        {
            Host = host2,
            Port = port2,
            Code = code,
            Permission = permission,
            Fingerprint = useTls ? TlsCertUtil.NormalizeFingerprint(fp) : null,
            Insecure = !useTls,
            ViaRelay = false
        };
        preview = FormatPreview(options);
        return true;
    }

    public static string FormatPreview(AssistConnectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var mode = options.ViaRelay ? $"中继 {options.RelayEndpoint}" : $"{options.Host}:{options.Port}";
        if (options.Insecure)
            return $"将经 {mode} · 明文 · 权限 {options.Permission} · 码 {options.Code}";
        var fp = options.Fingerprint ?? "";
        var shortFp = fp.Length >= 16 ? fp[..8] + "…" + fp[^8..] : fp;
        return $"将经 {mode} · TLS · fp={shortFp} · 权限 {options.Permission} · 码 {options.Code}";
    }

    public static bool TryPeekPayload(
        string? input,
        out string host,
        out int port,
        out string code,
        out string? fingerprint,
        out bool useTls)
    {
        host = "";
        port = DefaultPort;
        code = "";
        fingerprint = null;
        useTls = true;
        var text = (input ?? "").Trim();
        if (!text.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
            return false;
        return PairQrPayload.TryParse(text, out host, out port, out code, out fingerprint, out useTls);
    }

    private static bool TryParsePort(string? portField, out int? port, out string? error)
    {
        port = null;
        error = null;
        if (string.IsNullOrWhiteSpace(portField))
            return true;
        if (!int.TryParse(portField.Trim(), out var p) || p is < 1 or > 65535)
        {
            error = "端口需要 1–65535";
            return false;
        }
        port = p;
        return true;
    }
}
