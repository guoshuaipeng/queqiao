from pathlib import Path

# PairQrPayload.BuildRelay: require well-formed fp when useTls
p = Path('src-net48/RemoteAssist.Protocol/PairQrPayload.cs')
t = p.read_text(encoding='utf-8')
old = '''        var fp = TlsCertUtil.NormalizeFingerprint(fingerprint);
        if (!string.IsNullOrEmpty(fp))
            sb.Append("&fp=").Append(Uri.EscapeDataString(fp));
        return sb.ToString();
    }

    public static bool TryParse(string payload, out string host, out int port, out string code) =>'''
# There are two similar blocks (BuildLan and BuildRelay). Fix BuildRelay only by uniqueness with pwd=
old_relay = '''            .Append("&pwd=").Append(Uri.EscapeDataString(pwd))
            .Append("&tls=").Append(useTls ? "1" : "0");
        var fp = TlsCertUtil.NormalizeFingerprint(fingerprint);
        if (!string.IsNullOrEmpty(fp))
            sb.Append("&fp=").Append(Uri.EscapeDataString(fp));
        return sb.ToString();
    }'''
new_relay = '''            .Append("&pwd=").Append(Uri.EscapeDataString(pwd))
            .Append("&tls=").Append(useTls ? "1" : "0");
        if (useTls)
        {
            if (!TlsCertUtil.IsWellFormedFingerprint(fingerprint))
                throw new ArgumentException("TLS relay payload requires well-formed certificate fingerprint", nameof(fingerprint));
            sb.Append("&fp=").Append(Uri.EscapeDataString(TlsCertUtil.NormalizeFingerprint(fingerprint)));
        }
        else
        {
            var fpOpt = TlsCertUtil.NormalizeFingerprint(fingerprint);
            if (!string.IsNullOrEmpty(fpOpt))
                sb.Append("&fp=").Append(Uri.EscapeDataString(fpOpt));
        }
        return sb.ToString();
    }'''
if old_relay not in t:
    raise SystemExit('BuildRelay fp block missing')
t = t.replace(old_relay, new_relay, 1)
p.write_text(t, encoding='utf-8', newline='\n')
print('PairQrPayload BuildRelay OK')

# ConnectDraftParser rewrite TryBuildOptions security rules
Path('src-net48/RemoteAssist.Protocol/ConnectDraftParser.cs').write_text(r'''using System;

namespace RemoteAssist.Protocol;

/// <summary>协助端粘贴草稿：完整安全载荷为主；短码/旧两行须另有合法指纹或显式明文调试。</summary>
public static class ConnectDraftParser
{
    public const int DefaultPort = AssistConnectOptions.DefaultPort;

    public static bool TryBuildOptions(
        string? paste,
        string? passwordBox,
        string? relayEndpoint,
        bool preferRelay,
        string? lanHost,
        string? lanPortText,
        string? fingerprint,
        bool insecure,
        out AssistConnectOptions options,
        out string preview,
        out string error)
    {
        options = null!;
        preview = "";
        error = "";
        var raw = (paste ?? "").Trim();
        if (raw.Length == 0 && string.IsNullOrWhiteSpace(passwordBox))
        {
            error = "";
            preview = "";
            return false;
        }

        var advancedFp = fingerprint;
        var advancedFpOk = TlsCertUtil.IsWellFormedFingerprint(advancedFp);

        // 完整安全载荷 remoteassist:pair…
        if (raw.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!PairQrPayload.TryParseFull(raw, out var host, out var port, out var code, out var fp, out var useTls,
                    out var viaRelay, out var relay, out var pwd))
            {
                error = FriendConnectGuide.NeedSecurePayloadHint;
                return false;
            }

            var resolvedFp = TlsCertUtil.IsWellFormedFingerprint(fp) ? TlsCertUtil.NormalizeFingerprint(fp)
                : (advancedFpOk ? TlsCertUtil.NormalizeFingerprint(advancedFp) : "");
            var wantTls = !insecure && useTls;

            options = new AssistConnectOptions
            {
                Host = host,
                Port = port,
                Code = code,
                Fingerprint = string.IsNullOrEmpty(resolvedFp) ? null : resolvedFp,
                Insecure = insecure || !useTls,
                ViaRelay = viaRelay || preferRelay,
                RelayEndpoint = viaRelay ? relay : (relayEndpoint ?? RelayEndpoint.DefaultEndpoint),
                Password = pwd ?? SessionPasswordGenerator.Normalize(passwordBox)
            };

            if (options.ViaRelay && string.IsNullOrWhiteSpace(options.Password))
            {
                error = FriendConnectGuide.NeedSecurePayloadHint;
                return false;
            }

            if (wantTls && !TlsCertUtil.IsWellFormedFingerprint(options.Fingerprint))
            {
                error = FriendConnectGuide.NeedFingerprintHint;
                return false;
            }

            // 显式 tls=0 且未勾选明文调试 → 仍按安全默认要求指纹或阻断
            if (!insecure && !useTls)
            {
                error = "载荷标明非加密通道。公网请让对方重新「复制连接凭证」（加密载荷）；本机调试请勾选「明文调试」。";
                return false;
            }

            preview = options.ViaRelay
                ? FriendConnectGuide.FriendConnectionPreview(options.Code)
                : ("将连接：" + host + ":" + port);
            return true;
        }

        // 旧两行 / 短串 {码}-{验证码}：须高级合法指纹或显式明文；否则引导完整载荷
        if (FriendConnectGuide.TryParseShareText(raw, out var shareCode, out var sharePwd))
        {
            var pwd = sharePwd ?? SessionPasswordGenerator.Normalize(passwordBox);
            if (string.IsNullOrWhiteSpace(pwd))
            {
                error = FriendConnectGuide.NeedSecurePayloadHint;
                return false;
            }

            if (!insecure && !advancedFpOk)
            {
                error = FriendConnectGuide.NeedSecurePayloadHint;
                return false;
            }

            var ep = string.IsNullOrWhiteSpace(relayEndpoint) ? RelayEndpoint.DefaultEndpoint : relayEndpoint!.Trim();
            if (!RelayEndpoint.TryParse(ep, out var rh, out var rp, out _))
            {
                error = "中继地址无效";
                return false;
            }

            options = new AssistConnectOptions
            {
                Host = rh,
                Port = rp,
                Code = shareCode,
                Password = pwd,
                ViaRelay = true,
                RelayEndpoint = ep,
                Fingerprint = insecure ? null : TlsCertUtil.NormalizeFingerprint(advancedFp),
                Insecure = insecure
            };
            preview = FriendConnectGuide.FriendConnectionPreview(shareCode);
            return true;
        }

        var codeOnly = SessionCodeGenerator.Normalize(raw);
        var pwdOnly = SessionPasswordGenerator.Normalize(passwordBox);
        if (codeOnly.Length >= 4 && !string.IsNullOrWhiteSpace(pwdOnly) && preferRelay)
        {
            if (!insecure && !advancedFpOk)
            {
                error = FriendConnectGuide.NeedSecurePayloadHint;
                return false;
            }
            var ep = string.IsNullOrWhiteSpace(relayEndpoint) ? RelayEndpoint.DefaultEndpoint : relayEndpoint!.Trim();
            if (!RelayEndpoint.TryParse(ep, out var rh, out var rp, out _))
            {
                error = "中继地址无效";
                return false;
            }
            options = new AssistConnectOptions
            {
                Host = rh,
                Port = rp,
                Code = codeOnly,
                Password = pwdOnly,
                ViaRelay = true,
                RelayEndpoint = ep,
                Fingerprint = insecure ? null : TlsCertUtil.NormalizeFingerprint(advancedFp),
                Insecure = insecure
            };
            preview = FriendConnectGuide.FriendConnectionPreview(codeOnly);
            return true;
        }

        // 高级局域网
        if (!preferRelay || (!string.IsNullOrWhiteSpace(lanHost) && !string.IsNullOrWhiteSpace(lanPortText)))
        {
            if (string.IsNullOrWhiteSpace(lanHost) || string.IsNullOrWhiteSpace(lanPortText) || codeOnly.Length < 4)
            {
                error = FriendConnectGuide.NeedSecurePayloadHint;
                return false;
            }
            if (!int.TryParse(lanPortText.Trim(), out var lp) || lp < 1 || lp > 65535)
            {
                error = "端口无效";
                return false;
            }
            if (!insecure && !advancedFpOk)
            {
                error = FriendConnectGuide.NeedFingerprintHint;
                return false;
            }
            options = new AssistConnectOptions
            {
                Host = lanHost!.Trim(),
                Port = lp,
                Code = codeOnly,
                Fingerprint = insecure ? null : TlsCertUtil.NormalizeFingerprint(advancedFp),
                Insecure = insecure,
                ViaRelay = false
            };
            preview = "将连接：" + options.Host + ":" + options.Port;
            return true;
        }

        error = FriendConnectGuide.NeedSecurePayloadHint;
        return false;
    }
}
''', encoding='utf-8', newline='\n')
print('ConnectDraftParser OK')
