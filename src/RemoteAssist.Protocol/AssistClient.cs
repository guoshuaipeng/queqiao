using System.Net.Sockets;
using System.Text.Json;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Protocol;

public sealed class AssistClient : IDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private string? _token;
    private PermissionLevel _permission;

    private AssistClient(TcpClient tcp, NetworkStream stream, string token, PermissionLevel permission)
    {
        _tcp = tcp;
        _stream = stream;
        _token = token;
        _permission = permission;
    }

    public string? Token => _token;
    public PermissionLevel Permission => _permission;

    public static async Task<AssistClient> PairAsync(
        string host,
        int port,
        string sessionCode,
        PermissionLevel requested = PermissionLevel.Assist,
        string clientName = "helper",
        CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);
        var stream = tcp.GetStream();

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
