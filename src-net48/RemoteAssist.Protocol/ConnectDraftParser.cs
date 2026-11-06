using System;

namespace RemoteAssist.Protocol;

/// <summary>协助端粘贴草稿：分享两行 / remoteassist:pair 载荷 /（高级）主机+码。</summary>
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

        // 载荷
        if (raw.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!PairQrPayload.TryParseFull(raw, out var host, out var port, out var code, out var fp, out var useTls,
                    out var viaRelay, out var relay, out var pwd))
            {
                error = "粘贴的会话载荷不完整，请让对方重新复制码和密码。";
                return false;
            }
            options = new AssistConnectOptions
            {
                Host = host,
                Port = port,
                Code = code,
                Fingerprint = fp ?? fingerprint,
                Insecure = insecure || !useTls,
                ViaRelay = viaRelay || preferRelay,
                RelayEndpoint = viaRelay ? relay : (relayEndpoint ?? RelayEndpoint.DefaultEndpoint),
                Password = pwd ?? SessionPasswordGenerator.Normalize(passwordBox)
            };
            if (options.ViaRelay && string.IsNullOrWhiteSpace(options.Password))
            {
                error = "连接凭证不完整，请复制完整的「会话码-8位验证码」";
                return false;
            }
            preview = options.ViaRelay
                ? FriendConnectGuide.FriendConnectionPreview(options.Code)
                : ("将连接：" + host + ":" + port);
            return true;
        }

        // 会话码：… / 验证密码：…
        if (FriendConnectGuide.TryParseShareText(raw, out var shareCode, out var sharePwd))
        {
            var pwd = sharePwd ?? SessionPasswordGenerator.Normalize(passwordBox);
            if (string.IsNullOrWhiteSpace(pwd))
            {
                error = "连接凭证不完整，请复制完整的「会话码-8位验证码」";
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
                Fingerprint = fingerprint,
                Insecure = insecure
            };
            preview = FriendConnectGuide.FriendConnectionPreview(shareCode);
            return true;
        }

        // 纯会话码 + 密码框
        var codeOnly = SessionCodeGenerator.Normalize(raw);
        var pwdOnly = SessionPasswordGenerator.Normalize(passwordBox);
        if (codeOnly.Length >= 4 && !string.IsNullOrWhiteSpace(pwdOnly) && preferRelay)
        {
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
                Fingerprint = fingerprint,
                Insecure = insecure
            };
            preview = FriendConnectGuide.FriendConnectionPreview(codeOnly);
            return true;
        }

        // 高级局域网：主机+端口+码
        if (!preferRelay || (!string.IsNullOrWhiteSpace(lanHost) && !string.IsNullOrWhiteSpace(lanPortText)))
        {
            if (string.IsNullOrWhiteSpace(lanHost) || string.IsNullOrWhiteSpace(lanPortText) || codeOnly.Length < 4)
            {
                error = "连接凭证不完整，请复制完整的「会话码-8位验证码」";
                return false;
            }
            if (!int.TryParse(lanPortText.Trim(), out var lp) || lp < 1 || lp > 65535)
            {
                error = "端口无效";
                return false;
            }
            options = new AssistConnectOptions
            {
                Host = lanHost!.Trim(),
                Port = lp,
                Code = codeOnly,
                Fingerprint = fingerprint,
                Insecure = insecure,
                ViaRelay = false
            };
            preview = "将连接：" + options.Host + ":" + options.Port;
            return true;
        }

        error = "连接凭证不完整，请复制完整的「会话码-8位验证码」";
        return false;
    }
}
