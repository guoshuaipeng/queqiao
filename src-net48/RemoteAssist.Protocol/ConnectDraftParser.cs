namespace RemoteAssist.Protocol;

/// <summary>
/// 协助端 GUI 连接草稿解析：支持整段 remoteassist:pair 载荷，或「主机 + 会话码 + 指纹」手填。
/// </summary>
public static class ConnectDraftParser
{
    public const int DefaultPort = AssistConnectOptions.DefaultPort;

    /// <summary>
    /// 解析 UI 字段。
    /// <paramref name="input"/> 可为整段载荷，或纯会话码（此时必须填 host）。
    /// 手填 host/port/fp/insecure 可覆盖载荷内对应字段。
    /// </summary>
    public static bool TryParse(
        string? input,
        string? hostField,
        string? portField,
        string? fingerprintField,
        PermissionLevel permission,
        bool insecure,
        out AssistConnectOptions options,
        out string preview,
        out string error)
    {
        options = null!;
        preview = "";
        error = "";

        var text = (input ?? "").Trim();
        string host;
        int port;
        string code;
        string? fp;
        var useTls = !insecure;

        if (text.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!PairQrPayload.TryParse(text, out host, out port, out code, out fp, out var payloadTls))
            {
                error = "无法解析会话载荷（需要 remoteassist:pair?v=1&host=...&code=...&fp=...）";
                return false;
            }

            if (!payloadTls)
                useTls = false;

            // 手填可覆盖
            if (!string.IsNullOrWhiteSpace(hostField))
                host = hostField.Trim();
            if (TryParsePort(portField, out var portOverride, out var portErr))
            {
                if (portErr is not null)
                {
                    error = portErr;
                    return false;
                }
                if (portOverride is int p)
                    port = p;
            }
            else
            {
                error = "端口无效";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(fingerprintField))
                fp = TlsCertUtil.NormalizeFingerprint(fingerprintField);
        }
        else
        {
            // 纯会话码（或空，允许只靠字段）
            code = string.IsNullOrWhiteSpace(text)
                ? ""
                : SessionCodeGenerator.Normalize(text);

            host = (hostField ?? "").Trim();
            if (string.IsNullOrWhiteSpace(host))
            {
                error = "请粘贴整段会话载荷，或填写主机 IP + 会话码";
                return false;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                error = "会话码不能为空";
                return false;
            }

            port = DefaultPort;
            if (!TryParsePort(portField, out var portOverride, out var portErr))
            {
                error = "端口无效";
                return false;
            }
            if (portErr is not null)
            {
                error = portErr;
                return false;
            }
            if (portOverride is int p)
                port = p;

            fp = string.IsNullOrWhiteSpace(fingerprintField)
                ? null
                : TlsCertUtil.NormalizeFingerprint(fingerprintField);
        }

        if (insecure)
            useTls = false;

        if (useTls && string.IsNullOrEmpty(TlsCertUtil.NormalizeFingerprint(fp)))
        {
            error = "默认 TLS 需要证书指纹：请粘贴含 fp= 的载荷，或在下方填写指纹；明文调试请勾选「明文」";
            return false;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            error = "主机不能为空";
            return false;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            error = "会话码不能为空";
            return false;
        }

        options = new AssistConnectOptions
        {
            Host = host,
            Port = port,
            Code = code,
            Permission = permission,
            Fingerprint = useTls ? TlsCertUtil.NormalizeFingerprint(fp) : null,
            Insecure = !useTls
        };

        preview = FormatPreview(options);
        return true;
    }

    public static string FormatPreview(AssistConnectOptions options)
    {
        Net48Guard.ThrowIfNull(options);
        if (options.Insecure)
            return $"将连接 {options.Host}:{options.Port} · 明文 TCP · 权限 {options.Permission} · 码 {options.Code}";

        var fp = options.Fingerprint ?? "";
        var shortFp = fp.Length >= 16 ? fp[..8] + "…" + fp[^8..] : fp;
        return $"将连接 {options.Host}:{options.Port} · TLS · fp={shortFp} · 权限 {options.Permission} · 码 {options.Code}";
    }

    /// <summary>
    /// 从剪贴板/输入框自动拆分：若是载荷则填入各字段建议值（供 UI 回填）。
    /// </summary>
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
