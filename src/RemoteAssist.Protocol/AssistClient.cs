using System.Net.Sockets;
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
        string host,
        int port,
        string sessionCode,
        PermissionLevel requested = PermissionLevel.Assist,
        string clientName = "helper",
        CancellationToken ct = default) =>
        PairAsync(host, port, sessionCode, requested, clientName, expectedFingerprint: null, useTls: true, ct);

    public static async Task<AssistClient> PairAsync(
        string host,
        int port,
        string sessionCode,
        PermissionLevel requested,
        string clientName,
        string? expectedFingerprint,
        bool useTls,
        CancellationToken ct = default)
    {
        var opts = new AssistConnectOptions
        {
            Host = host,
            Port = port,
            Code = sessionCode,
            Permission = requested,
            Fingerprint = expectedFingerprint,
            Insecure = !useTls,
            ViaRelay = false
        };
        return await PairAsync(opts, clientName, ct).ConfigureAwait(false);
    }

    public static async Task<AssistClient> PairAsync(
        AssistConnectOptions opts,
        string clientName = "helper",
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(opts);

        // OwnerLink + 中继设备通道才走免密；LAN 直连时仅把 ownerLink 标志传给被控（被控仍需 owner 通道才自动批）。
        if (opts.OwnerLink && opts.ViaRelay)
            return await PairOwnerDeviceAsync(opts, clientName, ct).ConfigureAwait(false);

        if (opts.ViaRelay)
            return await PairViaRelayGuestAsync(opts, clientName, ct).ConfigureAwait(false);

        return await PairLanAsync(opts, clientName, ct).ConfigureAwait(false);
    }

    private static async Task<AssistClient> PairLanAsync(AssistConnectOptions opts, string clientName, CancellationToken ct)
    {
        var useTls = !opts.Insecure;
        if (useTls && string.IsNullOrWhiteSpace(TlsCertUtil.NormalizeFingerprint(opts.Fingerprint)))
        {
            throw new InvalidOperationException(
                "默认启用 TLS，需要证书指纹：请使用含 fp= 的会话载荷，或传 --fp / REMOTE_ASSIST_FP；调试明文请用 --insecure。");
        }

        var tcp = new TcpClient();
        await tcp.ConnectAsync(opts.Host, opts.Port, ct).ConfigureAwait(false);
        Stream stream = tcp.GetStream();
        try
        {
            if (useTls)
            {
                stream = await TlsTransport.AuthenticateAsClientAsync(stream, opts.Fingerprint!, ct)
                    .ConfigureAwait(false);
            }

            return await FinishPairAsync(tcp, stream, opts.Code, opts.Permission, clientName, ownerLink: opts.OwnerLink, ct)
                .ConfigureAwait(false);
        }
        catch
        {
            try { stream.Dispose(); } catch { /* ignore */ }
            try { tcp.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    private static async Task<AssistClient> PairViaRelayGuestAsync(AssistConnectOptions opts, string clientName, CancellationToken ct)
    {
        var endpoint = opts.RelayEndpoint ?? $"{opts.Host}:{opts.Port}";
        if (!RelayEndpoint.TryParse(endpoint, out var rh, out var rp, out var err))
            throw new InvalidOperationException(err);

        var password = SessionPasswordGenerator.Normalize(opts.Password ?? "");
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("中继连接需要验证密码（载荷 pwd= 或密码框）");

        var useTls = !opts.Insecure;
        if (useTls && string.IsNullOrWhiteSpace(TlsCertUtil.NormalizeFingerprint(opts.Fingerprint)))
        {
            throw new InvalidOperationException(
                "默认启用 TLS，需要证书指纹：请使用含 fp= 的会话载荷，或填写指纹；调试明文请用 --insecure。");
        }

        var relay = await RelayClient.GuestJoinAsync(rh, rp, opts.Code, password, ct)
            .ConfigureAwait(false);
        Stream stream = relay.Stream;
        try
        {
            if (useTls)
            {
                stream = await TlsTransport.AuthenticateAsClientAsync(stream, opts.Fingerprint!, ct)
                    .ConfigureAwait(false);
            }

            return await FinishPairAsync(relay, stream, opts.Code, opts.Permission, clientName, ownerLink: false, ct)
                .ConfigureAwait(false);
        }
        catch
        {
            try { stream.Dispose(); } catch { /* ignore */ }
            try { relay.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    private static async Task<AssistClient> PairOwnerDeviceAsync(AssistConnectOptions opts, string clientName, CancellationToken ct)
    {
        var endpoint = opts.RelayEndpoint ?? throw new InvalidOperationException("缺少中继地址");
        if (!RelayEndpoint.TryParse(endpoint, out var rh, out var rp, out var err))
            throw new InvalidOperationException(err);
        if (string.IsNullOrWhiteSpace(opts.DeviceId))
            throw new InvalidOperationException("免密控制需要设备 ID");

        var token = opts.AccountToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            if (string.IsNullOrWhiteSpace(opts.AccountUser) || string.IsNullOrWhiteSpace(opts.AccountPassword))
                throw new InvalidOperationException("免密控制需要账号令牌，或用户名+密码登录");
            token = await RelayClient.SignupOrLoginAsync(rh, rp, signup: false, opts.AccountUser!, opts.AccountPassword!, ct)
                .ConfigureAwait(false);
        }

        var useTls = !opts.Insecure;
        // Owner path: fingerprint may be unknown until first connect — allow insecure or require saved fp
        if (useTls && string.IsNullOrWhiteSpace(TlsCertUtil.NormalizeFingerprint(opts.Fingerprint)))
            throw new InvalidOperationException("免密控制仍建议 TLS：请先用一次 code+密码连接以保存指纹，或勾选明文（仅调试）");

        var relay = await RelayClient.DeviceConnectAsync(rh, rp, token!, opts.DeviceId!, ct)
            .ConfigureAwait(false);
        Stream stream = relay.Stream;
        try
        {
            if (useTls)
            {
                stream = await TlsTransport.AuthenticateAsClientAsync(stream, opts.Fingerprint!, ct)
                    .ConfigureAwait(false);
            }

            return await FinishPairAsync(relay, stream, opts.Code ?? "OWNER", opts.Permission, clientName, ownerLink: true, ct)
                .ConfigureAwait(false);
        }
        catch
        {
            try { stream.Dispose(); } catch { /* ignore */ }
            try { relay.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    private static async Task<AssistClient> FinishPairAsync(
        IDisposable transport,
        Stream stream,
        string sessionCode,
        PermissionLevel requested,
        string clientName,
        bool ownerLink,
        CancellationToken ct)
    {
        await FrameIo.WriteAsync(stream, "pair", new PairRequest
        {
            SessionCode = SessionCodeGenerator.Normalize(sessionCode),
            RequestedPermission = requested.ToString(),
            ClientName = clientName,
            OwnerLink = ownerLink
        }, ct: ct).ConfigureAwait(false);

        var response = await FrameIo.ReadAsync(stream, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Connection closed during pair.");
        if (!string.Equals(response.Type, "pair_result", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unexpected response type: {response.Type}");

        var payload = FrameIo.DeserializePayload<PairResponse>(response)
            ?? throw new InvalidOperationException("Empty pair response.");
        if (!payload.Ok || string.IsNullOrWhiteSpace(payload.Token))
            throw new InvalidOperationException(payload.Error ?? "Pairing failed.");

        var level = Enum.TryParse<PermissionLevel>(payload.Permission, true, out var p)
            ? p
            : PermissionLevel.Guest;

        return new AssistClient(transport, stream, payload.Token, level);
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
            throw new InvalidOperationException($"Unexpected response type: {response.Type}");

        return FrameIo.DeserializePayload<ToolResponse>(response)
            ?? new ToolResponse { Ok = false, Error = "Empty tool response." };
    }

    public void Dispose()
    {
        try { _stream.Dispose(); } catch { /* ignore */ }
        try { _transport.Dispose(); } catch { /* ignore */ }
    }
}

