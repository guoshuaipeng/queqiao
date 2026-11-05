namespace RemoteAssist.Protocol;

/// <summary>
/// 协助端 / MCP 启动时的连接参数：支持整段 remoteassist:pair 载荷，或 host+code+port。
/// 环境变量与命令行均可；同名字段命令行优先于环境变量。
/// </summary>
public sealed class AssistConnectOptions
{
    public const int DefaultPort = 17890;

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Code { get; init; }
    public PermissionLevel Permission { get; init; } = PermissionLevel.Assist;

    /// <summary>期望的 Agent 证书 SHA-256 指纹（TLS 钉扎）。</summary>
    public string? Fingerprint { get; init; }

    /// <summary>true = 明文 TCP（仅调试）。默认 false = TLS。</summary>
    public bool Insecure { get; init; }

    /// <summary>
    /// 从 argv + 环境变量解析。
    /// env: REMOTE_ASSIST_PAYLOAD / HOST / PORT / CODE / PERM / FP / INSECURE
    /// args: --payload|-P, --host, --port|-p, --code, --perm, --fp, --insecure；或 positional 载荷 / host+code
    /// </summary>
    public static bool TryParse(
        IReadOnlyList<string> args,
        Func<string, string?> getEnv,
        out AssistConnectOptions options,
        out string error)
    {
        options = null!;
        error = "";

        var positional = new List<string>();
        string? argPayload = null;
        string? argHost = null;
        int? argPort = null;
        string? argCode = null;
        PermissionLevel? argPerm = null;
        string? argFp = null;
        bool? argInsecure = null;

        for (var i = 0; i < args.Count; i++)
        {
            var a = args[i];
            if (a is "--payload" or "-P")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--payload / -P 需要参数（整段 remoteassist:pair?... 载荷）";
                    return false;
                }
                argPayload = args[++i];
            }
            else if (a is "--host")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--host 需要参数";
                    return false;
                }
                argHost = args[++i];
            }
            else if (a is "--port" or "-p")
            {
                if (i + 1 >= args.Count || !int.TryParse(args[++i], out var p) || p is < 1 or > 65535)
                {
                    error = "--port 需要 1–65535 的整数";
                    return false;
                }
                argPort = p;
            }
            else if (a is "--code")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--code 需要会话码参数";
                    return false;
                }
                argCode = args[++i];
            }
            else if (a is "--perm")
            {
                if (i + 1 >= args.Count || !Enum.TryParse(args[++i], true, out PermissionLevel pl))
                {
                    error = "--perm 需要 Assist|Guest|Admin";
                    return false;
                }
                argPerm = pl;
            }
            else if (a is "--fp" or "--fingerprint")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--fp 需要证书指纹（SHA-256 hex）";
                    return false;
                }
                argFp = args[++i];
            }
            else if (a is "--insecure")
            {
                argInsecure = true;
            }
            else if (a is "-h" or "--help" or "help")
            {
                error = "help";
                return false;
            }
            else if (a.StartsWith('-'))
            {
                error = $"未知选项: {a}（支持 --payload/--host/--port/--code/--perm/--fp/--insecure）";
                return false;
            }
            else
            {
                positional.Add(a);
            }
        }

        var envPayload = NullIfEmpty(getEnv("REMOTE_ASSIST_PAYLOAD"));
        var envHost = NullIfEmpty(getEnv("REMOTE_ASSIST_HOST"));
        var envPortStr = NullIfEmpty(getEnv("REMOTE_ASSIST_PORT"));
        var envCode = NullIfEmpty(getEnv("REMOTE_ASSIST_CODE"));
        var envPermStr = NullIfEmpty(getEnv("REMOTE_ASSIST_PERM"));
        var envFp = NullIfEmpty(getEnv("REMOTE_ASSIST_FP"));
        var envInsecure = ParseTruthy(getEnv("REMOTE_ASSIST_INSECURE"));

        var explicitPayload = NullIfEmpty(argPayload) ?? envPayload;

        var defaultPort = DefaultPort;
        if (argPort is int ap)
            defaultPort = ap;
        else if (envPortStr is not null)
        {
            if (!int.TryParse(envPortStr, out var ep) || ep is < 1 or > 65535)
            {
                error = "REMOTE_ASSIST_PORT 必须是 1–65535 的整数";
                return false;
            }
            defaultPort = ep;
        }

        var resolvePositional = BuildPositional(positional, explicitPayload, argHost ?? envHost, argCode ?? envCode);

        if (!PairTargetResolver.TryResolve(resolvePositional, explicitPayload, defaultPort, out var target, out var resolveErr))
        {
            error = FormatResolveError(resolveErr);
            return false;
        }

        var host = target.Host;
        var port = target.Port;
        var code = target.Code;
        var fp = target.Fingerprint;
        var useTlsFromPayload = target.UseTls;

        // 显式命令行可覆盖载荷内字段（本机调试 / 端口转发）
        if (argHost is not null) host = argHost.Trim();
        if (argPort is int pOverride) port = pOverride;
        if (argCode is not null) code = SessionCodeGenerator.Normalize(argCode);
        if (argFp is not null) fp = TlsCertUtil.NormalizeFingerprint(argFp);
        else if (envFp is not null && string.IsNullOrEmpty(fp))
            fp = TlsCertUtil.NormalizeFingerprint(envFp);

        if (string.IsNullOrWhiteSpace(host))
        {
            error = "host 不能为空";
            return false;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            error = "会话码不能为空（REMOTE_ASSIST_CODE / --code / 载荷内 code）";
            return false;
        }

        var insecure = false;
        if (argInsecure == true)
            insecure = true;
        else if (envInsecure)
            insecure = true;
        else if (!useTlsFromPayload)
            insecure = true;

        if (!insecure && string.IsNullOrEmpty(TlsCertUtil.NormalizeFingerprint(fp)))
        {
            error = "默认 TLS 需要证书指纹：请使用含 fp= 的会话载荷，或设 --fp / REMOTE_ASSIST_FP；明文调试用 --insecure / REMOTE_ASSIST_INSECURE=1";
            return false;
        }

        var perm = PermissionLevel.Assist;
        if (argPerm is PermissionLevel parsedArgPerm)
            perm = parsedArgPerm;
        else if (envPermStr is not null)
        {
            if (!Enum.TryParse(envPermStr, true, out PermissionLevel ep))
            {
                error = "REMOTE_ASSIST_PERM 必须是 Assist|Guest|Admin";
                return false;
            }
            perm = ep;
        }

        options = new AssistConnectOptions
        {
            Host = host,
            Port = port,
            Code = code,
            Permission = perm,
            Fingerprint = insecure ? null : TlsCertUtil.NormalizeFingerprint(fp),
            Insecure = insecure
        };
        return true;
    }

    public static string UsageText => """
        连接参数（环境变量或启动参数；同名字段命令行优先）:
          REMOTE_ASSIST_PAYLOAD / --payload <remoteassist:pair?v=1&host=...&port=...&code=...&tls=1&fp=...>
          或 REMOTE_ASSIST_HOST + REMOTE_ASSIST_CODE + REMOTE_ASSIST_FP（可选 PORT，默认 17890）
          或 --host <ip> --code <会话码> --fp <指纹> [--port 17890]
          可选 REMOTE_ASSIST_PERM / --perm  Assist|Guest|Admin（默认 Assist）
          调试明文: --insecure 或 REMOTE_ASSIST_INSECURE=1（Agent 也需 --insecure）
        """;

    private static List<string> BuildPositional(
        List<string> positional,
        string? explicitPayload,
        string? host,
        string? code)
    {
        if (explicitPayload is not null)
            return positional;

        if (positional.Count >= 1 &&
            positional[0].Trim().StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
            return positional;

        if (positional.Count >= 2)
        {
            var list = new List<string>(positional);
            if (!string.IsNullOrWhiteSpace(host)) list[0] = host!;
            if (!string.IsNullOrWhiteSpace(code)) list[1] = code!;
            return list;
        }

        if (positional.Count == 1 && !string.IsNullOrWhiteSpace(code))
            return new List<string> { positional[0], code! };

        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(code))
            return new List<string> { host!, code! };

        return positional;
    }

    private static string? NullIfEmpty(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static bool ParseTruthy(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return false;
        v = v.Trim();
        return v is "1" or "true" or "TRUE" or "yes" or "YES" or "on" or "ON";
    }

    private static string FormatResolveError(string resolveErr)
    {
        if (resolveErr.Contains("用法", StringComparison.Ordinal) ||
            resolveErr.Contains("无法解析", StringComparison.Ordinal))
        {
            var hint = resolveErr.Contains("无法解析", StringComparison.Ordinal)
                ? resolveErr + Environment.NewLine
                : "缺少配对目标。";
            return hint + Environment.NewLine + UsageText.Trim();
        }

        return resolveErr;
    }
}
