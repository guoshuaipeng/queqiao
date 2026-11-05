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

    /// <summary>true = 先出连中继再跑业务 TLS/配对。</summary>
    public bool ViaRelay { get; init; }

    /// <summary>中继 host:port；ViaRelay 时必填。</summary>
    public string? RelayEndpoint { get; init; }

    /// <summary>会话验证密码（中继 guest 路径）。</summary>
    public string? Password { get; init; }

    /// <summary>账号免密控制自己的设备。</summary>
    public bool OwnerLink { get; init; }

    public string? AccountToken { get; init; }
    public string? DeviceId { get; init; }

    public string? AccountUser { get; init; }
    public string? AccountPassword { get; init; }

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
        string? argRelay = null;
        string? argPwd = null;
        string? argToken = null;
        string? argDeviceId = null;
        string? argUser = null;
        string? argAccountPass = null;

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
            else if (a is "--relay")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--relay 需要 host:port";
                    return false;
                }
                argRelay = args[++i];
            }
            else if (a is "--pwd" or "--password")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--pwd/--password 需要参数";
                    return false;
                }
                argPwd = args[++i];
            }
            else if (a is "--token")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--token 需要账号令牌";
                    return false;
                }
                argToken = args[++i];
            }
            else if (a is "--device-id" or "--device")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--device-id 需要设备 ID";
                    return false;
                }
                argDeviceId = args[++i];
            }
            else if (a is "--user" or "--username")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--user 需要用户名";
                    return false;
                }
                argUser = args[++i];
            }
            else if (a is "--account-password")
            {
                if (i + 1 >= args.Count)
                {
                    error = "--account-password 需要参数";
                    return false;
                }
                argAccountPass = args[++i];
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
        var envRelay = NullIfEmpty(getEnv("REMOTE_ASSIST_RELAY"));
        var envPwd = NullIfEmpty(getEnv("REMOTE_ASSIST_PWD")) ?? NullIfEmpty(getEnv("REMOTE_ASSIST_PASSWORD"));
        var envToken = NullIfEmpty(getEnv("REMOTE_ASSIST_TOKEN"));
        var envDeviceId = NullIfEmpty(getEnv("REMOTE_ASSIST_DEVICE_ID"));
        var envUser = NullIfEmpty(getEnv("REMOTE_ASSIST_USER"));
        var envAccountPass = NullIfEmpty(getEnv("REMOTE_ASSIST_ACCOUNT_PASSWORD"));

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

        var relayEp = NullIfEmpty(argRelay) ?? envRelay;
        var token = NullIfEmpty(argToken) ?? envToken;
        var deviceId = NullIfEmpty(argDeviceId) ?? envDeviceId;
        var accountUser = NullIfEmpty(argUser) ?? envUser;
        var accountPassword = NullIfEmpty(argAccountPass) ?? envAccountPass;
        var guestPassword = NullIfEmpty(argPwd) ?? envPwd;

        var ownerLink = !string.IsNullOrWhiteSpace(deviceId) &&
                        (!string.IsNullOrWhiteSpace(token) ||
                         (!string.IsNullOrWhiteSpace(accountUser) &&
                          !string.IsNullOrWhiteSpace(accountPassword ?? guestPassword)));

        if (ownerLink && string.IsNullOrWhiteSpace(accountPassword) && !string.IsNullOrWhiteSpace(guestPassword) &&
            string.IsNullOrWhiteSpace(token))
        {
            accountPassword = guestPassword;
            guestPassword = null;
        }

        if (ownerLink)
        {
            if (string.IsNullOrWhiteSpace(relayEp))
                relayEp = Protocol.RelayEndpoint.DefaultEndpoint;
            if (!Protocol.RelayEndpoint.TryParse(relayEp, out var rh, out var rp, out var rerr))
            {
                error = "中继地址无效: " + rerr;
                return false;
            }

            var insecureOwner = argInsecure == true || envInsecure;
            var fpOwner = NullIfEmpty(argFp) ?? envFp;
            if (!insecureOwner && string.IsNullOrEmpty(TlsCertUtil.NormalizeFingerprint(fpOwner)))
            {
                error = "默认 TLS 需要证书指纹：--fp / REMOTE_ASSIST_FP；明文调试用 --insecure";
                return false;
            }

            var permOwner = PermissionLevel.Assist;
            if (argPerm is PermissionLevel parsedArgPerm) permOwner = parsedArgPerm;
            else if (envPermStr is not null)
            {
                if (!Enum.TryParse(envPermStr, true, out PermissionLevel ep))
                {
                    error = "REMOTE_ASSIST_PERM 必须是 Assist|Guest|Admin";
                    return false;
                }
                permOwner = ep;
            }

            options = new AssistConnectOptions
            {
                Host = rh,
                Port = rp,
                Code = "OWNER",
                Permission = permOwner,
                Fingerprint = insecureOwner ? null : TlsCertUtil.NormalizeFingerprint(fpOwner),
                Insecure = insecureOwner,
                ViaRelay = true,
                RelayEndpoint = $"{rh}:{rp}",
                OwnerLink = true,
                AccountToken = token,
                DeviceId = deviceId,
                AccountUser = accountUser,
                AccountPassword = accountPassword
            };
            return true;
        }

        // guest relay short-circuit: --code + --pwd（--relay 可省略，默认公网中继）
        var codeArg = NullIfEmpty(argCode) ?? envCode;
        if (!string.IsNullOrWhiteSpace(codeArg) &&
            !string.IsNullOrWhiteSpace(NullIfEmpty(guestPassword)) &&
            string.IsNullOrWhiteSpace(explicitPayload) && positional.Count == 0 &&
            string.IsNullOrWhiteSpace(NullIfEmpty(argHost) ?? envHost))
        {
            if (string.IsNullOrWhiteSpace(relayEp))
                relayEp = Protocol.RelayEndpoint.DefaultEndpoint;
            if (!Protocol.RelayEndpoint.TryParse(relayEp, out var rhG, out var rpG, out var rerrG))
            {
                error = "中继地址无效: " + rerrG;
                return false;
            }
            var insecureG = argInsecure == true || envInsecure;
            var fpG = NullIfEmpty(argFp) ?? envFp;
            if (!insecureG && string.IsNullOrEmpty(TlsCertUtil.NormalizeFingerprint(fpG)))
            {
                error = "默认 TLS 需要证书指纹：--fp / REMOTE_ASSIST_FP；明文调试用 --insecure";
                return false;
            }
            var permG = PermissionLevel.Assist;
            if (argPerm is PermissionLevel pg) permG = pg;
            else if (envPermStr is not null)
            {
                if (!Enum.TryParse(envPermStr, true, out PermissionLevel epg))
                {
                    error = "REMOTE_ASSIST_PERM 必须是 Assist|Guest|Admin";
                    return false;
                }
                permG = epg;
            }
            options = new AssistConnectOptions
            {
                Host = rhG,
                Port = rpG,
                Code = SessionCodeGenerator.Normalize(codeArg!),
                Permission = permG,
                Fingerprint = insecureG ? null : TlsCertUtil.NormalizeFingerprint(fpG),
                Insecure = insecureG,
                ViaRelay = true,
                RelayEndpoint = $"{rhG}:{rpG}",
                Password = SessionPasswordGenerator.Normalize(guestPassword!)
            };
            return true;
        }

        var resolvePositional = BuildPositional(positional, explicitPayload, argHost ?? envHost, argCode ?? envCode);

        string host;
        int port;
        string code;
        string? fpPayload;
        bool useTlsFromPayload;
        var viaRelay = false;
        string? relayFromPayload = null;
        string? pwdFromPayload = null;

        if (!string.IsNullOrWhiteSpace(explicitPayload) ||
            (positional.Count >= 1 &&
             positional[0].Trim().StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var raw = explicitPayload ?? positional[0];
            if (!PairQrPayload.TryParseFull(raw!, out host, out port, out code, out fpPayload, out useTlsFromPayload,
                    out viaRelay, out relayFromPayload, out pwdFromPayload))
            {
                error = "无法解析 remoteassist:pair 载荷";
                return false;
            }
        }
        else
        {
            if (!PairTargetResolver.TryResolve(resolvePositional, explicitPayload, defaultPort, out var target, out var resolveErr))
            {
                error = FormatResolveError(resolveErr);
                return false;
            }
            host = target.Host;
            port = target.Port;
            code = target.Code;
            fpPayload = target.Fingerprint;
            useTlsFromPayload = target.UseTls;
        }

        if (argHost is not null) host = argHost.Trim();
        if (argPort is int pOverride) port = pOverride;
        if (argCode is not null) code = SessionCodeGenerator.Normalize(argCode);

        var fp = NullIfEmpty(argFp) ?? (string.IsNullOrEmpty(fpPayload) ? envFp : fpPayload);
        if (fp is not null) fp = TlsCertUtil.NormalizeFingerprint(fp);

        if (!string.IsNullOrWhiteSpace(relayEp))
        {
            viaRelay = true;
            if (!Protocol.RelayEndpoint.TryParse(relayEp, out var rh2, out var rp2, out var rerr2))
            {
                error = "中继地址无效: " + rerr2;
                return false;
            }
            relayFromPayload = $"{rh2}:{rp2}";
            host = rh2;
            port = rp2;
        }
        else if (viaRelay && !string.IsNullOrWhiteSpace(relayFromPayload))
        {
            relayEp = relayFromPayload;
        }

        var password = NullIfEmpty(guestPassword) ?? pwdFromPayload;

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

        if (viaRelay)
        {
            if (string.IsNullOrWhiteSpace(relayEp) && !string.IsNullOrWhiteSpace(relayFromPayload))
                relayEp = relayFromPayload;
            if (string.IsNullOrWhiteSpace(relayEp))
            {
                error = "中继连接需要 relay=host:port（载荷或 --relay / REMOTE_ASSIST_RELAY）";
                return false;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                error = "中继访客连接需要密码（载荷 pwd= 或 --pwd / REMOTE_ASSIST_PWD）";
                return false;
            }
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
        if (argPerm is PermissionLevel parsedPerm)
            perm = parsedPerm;
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
            Insecure = insecure,
            ViaRelay = viaRelay,
            RelayEndpoint = viaRelay ? relayEp : null,
            Password = viaRelay ? SessionPasswordGenerator.Normalize(password!) : password
        };
        return true;
    }

    public static string UsageText => """
        连接参数（环境变量或启动参数；同名字段命令行优先）:

        【临时远程 ID + 密码 · 经中继】
          --payload <remoteassist:pair?v=2&via=relay&relay=121.196.45.81:17891&code=...&pwd=...&tls=1&fp=...>
          或 --code <远程ID> --pwd <密码> --fp <指纹>  [--relay host:port，默认 121.196.45.81:17891]
          env: REMOTE_ASSIST_RELAY（可省略） / REMOTE_ASSIST_CODE / REMOTE_ASSIST_PWD / REMOTE_ASSIST_FP

        【登录后连自己的机器 · 免密】
          --token <令牌> --device-id <设备ID> --fp <指纹>  [--relay，默认同上]
          或 --user <名> --account-password <密> --device-id <ID> --fp <指纹>
          env: REMOTE_ASSIST_TOKEN / REMOTE_ASSIST_USER / REMOTE_ASSIST_ACCOUNT_PASSWORD / REMOTE_ASSIST_DEVICE_ID

        【局域网高级/调试】
          --host <ip> --code <会话码> --fp <指纹> [--port 17890]
          或 REMOTE_ASSIST_PAYLOAD 旧版 via=lan 载荷

        可选: --perm Assist|Guest|Admin；明文调试 --insecure / REMOTE_ASSIST_INSECURE=1
        注意: run_command 等写操作仍须被控本机确认，MCP 无法绕过。
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

