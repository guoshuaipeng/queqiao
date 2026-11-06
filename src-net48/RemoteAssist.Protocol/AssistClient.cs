using System;
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
        var relay = await RelayClient.GuestJoinAsync(rh, rp, opts.Code, password, ct).ConfigureAwait(false);
        Stream stream = relay.Stream;
        try
        {
            if (useTls)
            {
                var fp = TlsCertUtil.NormalizeFingerprint(opts.Fingerprint);
                if (string.IsNullOrWhiteSpace(fp))
                    throw new InvalidOperationException(
                        "加密连接需要证书指纹：请粘贴含 fp= 的完整载荷，或展开「高级」填写 TLS 指纹（短连接凭证不含指纹）。");
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
