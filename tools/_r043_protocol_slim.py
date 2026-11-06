from pathlib import Path

# --- SessionPasswordGenerator (no Span) ---
Path("src-net48/RemoteAssist.Protocol/SessionPasswordGenerator.cs").write_text("""using System;
using System.Security.Cryptography;
using System.Text;

namespace RemoteAssist.Protocol;

public static class SessionPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 4)
    {
        if (length < 4) length = 4;
        var bytes = new byte[length];
        Net48Crypto.FillRandom(bytes);
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append(Alphabet[bytes[i] % Alphabet.Length]);
        return sb.ToString();
    }

    public static string Normalize(string? password) =>
        string.IsNullOrWhiteSpace(password) ? "" : password.Trim();
}
""", encoding="utf-8", newline="\n")

# --- RelayEndpoint ---
Path("src-net48/RemoteAssist.Protocol/RelayEndpoint.cs").write_text("""using System;

namespace RemoteAssist.Protocol;

public static class RelayEndpoint
{
    public const string DefaultLocal = "127.0.0.1:17891";
    public const string DefaultPublic = "121.196.45.81:17891";
    public const string DefaultEndpoint = DefaultPublic;

    public static bool TryParse(string? endpoint, out string host, out int port, out string? error)
    {
        host = "";
        port = 0;
        error = null;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            error = "empty endpoint";
            return false;
        }

        var s = endpoint.Trim();
        if (s.IndexOf("://", StringComparison.Ordinal) >= 0)
        {
            error = "endpoint must be host:port (no scheme)";
            return false;
        }

        if (s.StartsWith("[") && s.IndexOf(']') > 0)
        {
            var close = s.IndexOf(']');
            if (close <= 1 || close + 1 >= s.Length || s[close + 1] != ':')
            {
                error = "bad [ipv6]:port";
                return false;
            }
            host = s.Substring(1, close - 1);
            if (!int.TryParse(s.Substring(close + 2), out port) || port < 1 || port > 65535)
            {
                error = "bad port";
                return false;
            }
            return true;
        }

        var idx = s.LastIndexOf(':');
        if (idx <= 0 || idx == s.Length - 1)
        {
            error = "expect host:port";
            return false;
        }
        host = s.Substring(0, idx).Trim();
        if (host.Length == 0 || !int.TryParse(s.Substring(idx + 1), out port) || port < 1 || port > 65535)
        {
            error = "bad host or port";
            return false;
        }
        return true;
    }

    public static string Format(string host, int port) => host + ":" + port;
}
""", encoding="utf-8", newline="\n")

# --- Fix FriendConnectGuide HttpRequestException ---
p = Path("src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs")
t = p.read_text(encoding="utf-8")
t = t.replace("if (cur is System.Net.Http.HttpRequestException)", "if (cur.GetType().FullName == \"System.Net.Http.HttpRequestException\")")
p.write_text(t, encoding="utf-8", newline="\n")

# --- Fix RelayClient IO for net48 ---
p = Path("src-net48/RemoteAssist.Protocol/RelayClient.cs")
t = p.read_text(encoding="utf-8")
t = t.replace("await stream.WriteAsync(bytes, ct).ConfigureAwait(false);",
              "await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);")
t = t.replace("await stream.FlushAsync(ct).ConfigureAwait(false);",
              "await stream.FlushAsync().ConfigureAwait(false);")
t = t.replace("var n = await stream.ReadAsync(one.AsMemory(0, 1), ct).ConfigureAwait(false);",
              "var n = await stream.ReadAsync(one, 0, 1).ConfigureAwait(false);")
# remaining ConnectAsync already fixed?
if "tcp.ConnectAsync(host, port, ct)" in t:
    t = t.replace("await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);",
                  "await Net48Sockets.ConnectAsync(tcp, host, port, ct).ConfigureAwait(false);")
p.write_text(t, encoding="utf-8", newline="\n")
print("RelayClient IO fixed")

# --- AssistConnectOptions: rewrite slim with ViaRelay ---
# Keep TryParse from old but add properties - easier to write full slim file for UI use
Path("src-net48/RemoteAssist.Protocol/AssistConnectOptions.cs").write_text(r'''using System;
using System.Collections.Generic;

namespace RemoteAssist.Protocol;

/// <summary>协助端连接参数（R043：默认经公网中继）。</summary>
public sealed class AssistConnectOptions
{
    public const int DefaultPort = 17890;

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Code { get; init; }
    public PermissionLevel Permission { get; init; } = PermissionLevel.Assist;
    public string? Fingerprint { get; init; }
    public bool Insecure { get; init; }

    public bool ViaRelay { get; init; }
    public string? RelayEndpoint { get; init; }
    public string? Password { get; init; }

    public bool OwnerLink { get; init; }
    public string? AccountToken { get; init; }
    public string? DeviceId { get; init; }
    public string? AccountUser { get; init; }
    public string? AccountPassword { get; init; }
}
''', encoding="utf-8", newline="\n")

# --- PairQrPayload slim with relay ---
Path("src-net48/RemoteAssist.Protocol/PairQrPayload.cs").write_text(r'''using System;
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
''', encoding="utf-8", newline="\n")

# --- ConnectDraftParser for paste box ---
Path("src-net48/RemoteAssist.Protocol/ConnectDraftParser.cs").write_text(r'''using System;

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
                error = "还需要验证密码";
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
                error = "还需要验证密码";
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
                error = "请粘贴对方发来的会话码和验证密码";
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

        error = "请粘贴对方发来的会话码和验证密码";
        return false;
    }
}
''', encoding="utf-8", newline="\n")

# Check FriendConnectionPreview exists
fc = Path("src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs").read_text(encoding="utf-8")
if "FriendConnectionPreview" not in fc:
    # add simple helper at end before last brace
    fc = fc.rstrip()
    if fc.endswith("}"):
        fc = fc[:-1] + '''
    public static string FriendConnectionPreview(string code) =>
        "将经默认中继连接会话 " + SessionCodeGenerator.Normalize(code);

}
'''
        Path("src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs").write_text(fc, encoding="utf-8", newline="\n")
        print("added FriendConnectionPreview")
else:
    print("FriendConnectionPreview exists")

# --- AssistClient slim ---
Path("src-net48/RemoteAssist.Protocol/AssistClient.cs").write_text(r'''using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Protocol;

public sealed class AssistClient : IDisposable
{
    private readonly IDisposable _transport;
    private readonly Stream _stream;
    private string? _token;
    private PermissionLevel _permission;

    private AssistClient(IDisposable transport, Stream stream, string token, PermissionLevel permission)
    {
        _transport = transport;
        _stream = stream;
        _token = token;
        _permission = permission;
    }

    public string? Token => _token;
    public PermissionLevel Permission => _permission;

    public static Task<AssistClient> PairAsync(
        AssistConnectOptions opts,
        string clientName = "helper",
        CancellationToken ct = default)
    {
        Net48Guard.ThrowIfNull(opts);
        if (opts.ViaRelay)
            return PairViaRelayGuestAsync(opts, clientName, ct);
        return PairLanAsync(opts, clientName, ct);
    }

    private static async Task<AssistClient> PairLanAsync(AssistConnectOptions opts, string clientName, CancellationToken ct)
    {
        var useTls = !opts.Insecure;
        if (useTls && string.IsNullOrWhiteSpace(TlsCertUtil.NormalizeFingerprint(opts.Fingerprint)))
        {
            throw new InvalidOperationException(
                "局域网 TLS 需要证书指纹：请展开「高级」填写，或勾选明文调试。");
        }

        var tcp = new TcpClient();
        await Net48Sockets.ConnectAsync(tcp, opts.Host, opts.Port, ct).ConfigureAwait(false);
        Stream stream = tcp.GetStream();
        try
        {
            if (useTls)
                stream = await TlsTransport.AuthenticateAsClientAsync(stream, opts.Fingerprint!, ct).ConfigureAwait(false);
            return await FinishPairAsync(tcp, stream, opts.Code, opts.Permission, clientName, ct).ConfigureAwait(false);
        }
        catch
        {
            try { stream.Dispose(); } catch { }
            try { tcp.Dispose(); } catch { }
            throw;
        }
    }

    private static async Task<AssistClient> PairViaRelayGuestAsync(AssistConnectOptions opts, string clientName, CancellationToken ct)
    {
        var endpoint = opts.RelayEndpoint ?? RelayEndpoint.DefaultEndpoint;
        if (!RelayEndpoint.TryParse(endpoint, out var rh, out var rp, out var err))
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(err) ? "中继地址无效" : err);

        var password = SessionPasswordGenerator.Normalize(opts.Password ?? "");
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("中继连接需要验证密码");

        var useTls = !opts.Insecure;
        // 经中继时业务 TLS 指纹可能尚未拿到：允许 insecure，或有 fp 时钉扎
        var relay = await RelayClient.GuestJoinAsync(rh, rp, opts.Code, password, ct).ConfigureAwait(false);
        Stream stream = relay.Stream;
        try
        {
            if (useTls)
            {
                var fp = TlsCertUtil.NormalizeFingerprint(opts.Fingerprint);
                if (string.IsNullOrWhiteSpace(fp))
                {
                    // 无指纹时跳过业务 TLS 钉扎（中继已配对）；与亲友主路径一致优先连通
                    // 若对方强制 TLS，FinishPair 仍发 pair 明文帧可能失败——被控端 preferRelay 路径会在配对前完成 TLS。
                    // 保持与 net8 行为：无 fp 时抛人话，引导明文或稍后保存指纹。
                    throw new InvalidOperationException(
                        "连接需要安全校验信息。请让对方重新复制「码+密码」后再试；若仍失败，可在高级选项勾选明文调试（仅内网）。");
                }
                stream = await TlsTransport.AuthenticateAsClientAsync(stream, fp!, ct).ConfigureAwait(false);
            }
            return await FinishPairAsync(relay, stream, opts.Code, opts.Permission, clientName, ct).ConfigureAwait(false);
        }
        catch
        {
            try { stream.Dispose(); } catch { }
            try { relay.Dispose(); } catch { }
            throw;
        }
    }

    private static async Task<AssistClient> FinishPairAsync(
        IDisposable transport,
        Stream stream,
        string sessionCode,
        PermissionLevel requested,
        string clientName,
        CancellationToken ct)
    {
        await FrameIo.WriteAsync(stream, "pair", new PairRequest
        {
            SessionCode = SessionCodeGenerator.Normalize(sessionCode),
            RequestedPermission = requested.ToString(),
            ClientName = clientName
        }, ct: ct).ConfigureAwait(false);

        var response = await FrameIo.ReadAsync(stream, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Connection closed during pair.");
        if (!string.Equals(response.Type, "pair_result", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unexpected response type: " + response.Type);

        var payload = FrameIo.DeserializePayload<PairResponse>(response)
            ?? throw new InvalidOperationException("Empty pair response.");
        if (!payload.Ok || string.IsNullOrWhiteSpace(payload.Token))
            throw new InvalidOperationException(payload.Error ?? "Pairing failed.");

        var level = Enum.TryParse<PermissionLevel>(payload.Permission, true, out var p)
            ? p
            : PermissionLevel.Guest;

        return new AssistClient(transport, stream, payload.Token!, level);
    }

    public async Task<ToolResponse> InvokeToolAsync(ToolKind tool, Dictionary<string, string?>? args = null, CancellationToken ct = default)
    {
        if (_token is null)
            throw new InvalidOperationException("Not paired.");

        await FrameIo.WriteAsync(_stream, "tool", new ToolRequest
        {
            Tool = tool.ToString(),
            Arguments = args ?? new Dictionary<string, string?>()
        }, token: _token, ct: ct).ConfigureAwait(false);

        var response = await FrameIo.ReadAsync(_stream, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Connection closed during tool call.");

        if (string.Equals(response.Type, "error", StringComparison.OrdinalIgnoreCase))
        {
            var err = FrameIo.DeserializePayload<ErrorPayload>(response);
            return new ToolResponse { Ok = false, Error = err?.Message ?? "Unknown error" };
        }

        if (!string.Equals(response.Type, "tool_result", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unexpected response type: " + response.Type);

        return FrameIo.DeserializePayload<ToolResponse>(response)
            ?? new ToolResponse { Ok = false, Error = "Empty tool response." };
    }

    public void Dispose()
    {
        try { _stream.Dispose(); } catch { }
        try { _transport.Dispose(); } catch { }
    }
}
''', encoding="utf-8", newline="\n")

# Relax ViaRelay TLS: net8 requires fp for relay too which is painful for friends.
# For R043 friend path, after guest_join the pipe is to agent which uses TLS with ephemeral cert.
# Looking at net8 again - it DOES require fingerprint for ViaRelay guest. How do friends get fp?
# Share text is only code+password. So how does net8 Helper connect?
# Check net8 HelperWindow connect - maybe insecure false but fp from somewhere, or agent sends cert differently.

print("wrote slim protocol files")
