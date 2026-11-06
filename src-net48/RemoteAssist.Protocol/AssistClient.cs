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
    private readonly CancellationTokenSource _watchCts = new();
    private int _connectionLostRaised;
    private int _disposed;

    private AssistClient(IDisposable transport, Stream stream, string token, PermissionLevel permission)
    {
        _transport = transport;
        _stream = stream;
        _token = token;
        _permission = permission;
        _ = Task.Run(() => WatchTransportAsync(_watchCts.Token));
    }

    public string? Token => _token;
    public PermissionLevel Permission => _permission;

    /// <summary>已配对后底层传输被对端关掉（对方结束协助 / 连接中断）。自己 Dispose 不会触发。</summary>
    public event Action? ConnectionLost;

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
                FriendConnectGuide.MissingSecurityInfoHint);
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
                        FriendConnectGuide.MissingSecurityInfoHint);
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

        WireEnvelope? response;
        try
        {
            response = await FrameIo.ReadAsync(_stream, ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            RaiseConnectionLost();
            throw;
        }
        if (response is null)
        {
            RaiseConnectionLost();
            throw new InvalidOperationException("Connection closed during tool call.");
        }

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
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        try { _watchCts.Cancel(); } catch { /* ignore */ }
        try { _watchCts.Dispose(); } catch { /* ignore */ }
        try { _stream.Dispose(); } catch { }
        try { _transport.Dispose(); } catch { }
    }

    private async Task WatchTransportAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(400, ct).ConfigureAwait(false);
                if (IsTransportClosed())
                {
                    RaiseConnectionLost();
                    return;
                }
            }
        }
        catch (OperationCanceledException) { /* dispose */ }
        catch
        {
            RaiseConnectionLost();
        }
    }

    private bool IsTransportClosed()
    {
        try
        {
            var sock = TryGetSocket();
            if (sock is null)
                return false;
            if (!sock.Connected)
                return true;
            return sock.Poll(0, SelectMode.SelectRead) && sock.Available == 0;
        }
        catch
        {
            return true;
        }
    }

    private Socket? TryGetSocket()
    {
        try
        {
            if (_transport is TcpClient tcp)
                return tcp.Client;
            if (_transport is RelayClient relay)
                return relay.TcpSocket;
            return null;
        }
        catch
        {
            return null;
        }
    }

    private void RaiseConnectionLost()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;
        if (Interlocked.Exchange(ref _connectionLostRaised, 1) != 0)
            return;
        try { ConnectionLost?.Invoke(); } catch { /* UI */ }
    }
}
