using System.Net.Sockets;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Protocol;

public sealed class AssistClient : IDisposable
{
    private readonly TcpClient _tcp;
    private readonly Stream _stream;
    private string? _token;
    private PermissionLevel _permission;

    private AssistClient(TcpClient tcp, Stream stream, string token, PermissionLevel permission)
    {
        _tcp = tcp;
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
        if (useTls && string.IsNullOrWhiteSpace(TlsCertUtil.NormalizeFingerprint(expectedFingerprint)))
        {
            throw new InvalidOperationException(
                "默认启用 TLS，需要证书指纹：请使用含 fp= 的会话载荷，或传 --fp / REMOTE_ASSIST_FP；调试明文请用 --insecure。");
        }

        var tcp = new TcpClient();
        await Net48Sockets.ConnectAsync(tcp, host, port, ct).ConfigureAwait(false);
        Stream stream = tcp.GetStream();
        try
        {
            if (useTls)
            {
                stream = await TlsTransport.AuthenticateAsClientAsync(stream, expectedFingerprint!, ct)
                    .ConfigureAwait(false);
            }

            await FrameIo.WriteAsync(stream, "pair", new PairRequest
            {
                SessionCode = SessionCodeGenerator.Normalize(sessionCode),
                RequestedPermission = requested.ToString(),
                ClientName = clientName
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

            return new AssistClient(tcp, stream, payload.Token, level);
        }
        catch
        {
            try { stream.Dispose(); } catch { /* ignore */ }
            try { tcp.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    /// <summary>从 AssistConnectOptions 配对。</summary>
    public static Task<AssistClient> PairAsync(
        AssistConnectOptions opts,
        string clientName = "helper",
        CancellationToken ct = default)
    {
        Net48Guard.ThrowIfNull(opts);
        return PairAsync(
            opts.Host,
            opts.Port,
            opts.Code,
            opts.Permission,
            clientName,
            opts.Fingerprint,
            useTls: !opts.Insecure,
            ct);
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
        _stream.Dispose();
        _tcp.Dispose();
    }
}
