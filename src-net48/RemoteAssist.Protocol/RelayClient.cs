using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RemoteAssist.Protocol;

/// <summary>
/// 公网中继客户端（JSON Lines 控制协议，见 docs/relay-protocol.md）。
/// 配对成功后 Stream 为透明字节管道。
/// </summary>
public sealed class RelayClient : IDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;

    private RelayClient(TcpClient tcp, NetworkStream stream)
    {
        _tcp = tcp;
        _stream = stream;
    }

    public Stream Stream => _stream;

    /// <summary>底层 TCP，供协助端探测对端是否已关掉会话（R035）。</summary>
    public System.Net.Sockets.Socket? TcpSocket
    {
        get
        {
            try { return _tcp.Client; }
            catch { return null; }
        }
    }


    public static async Task<(RelayClient Client, string Code)> GuestHostAsync(
        string host,
        int port,
        string password,
        string? preferredCode = null,
        Action<string>? onCodeAssigned = null,
        CancellationToken ct = default)
    {
        password = SessionPasswordGenerator.Normalize(password);
        if (password.Length < 4)
            throw new ArgumentException("password min 4 chars", nameof(password));

        var tcp = new TcpClient();
        await Net48Sockets.ConnectAsync(tcp, host, port, ct).ConfigureAwait(false);
        var stream = tcp.GetStream();
        try
        {
            var req = new Dictionary<string, object?>
            {
                ["op"] = "guest_host",
                ["password"] = password,
                ["code"] = string.IsNullOrWhiteSpace(preferredCode)
                    ? ""
                    : SessionCodeGenerator.Normalize(preferredCode)
            };
            var hostReply = await RoundTripAsync(stream, req, ct).ConfigureAwait(false);
            EnsureOk(hostReply, "guest_host");
            var code = GetString(hostReply, "code")
                ?? throw new InvalidOperationException("guest_host missing code");
            var normalized = SessionCodeGenerator.Normalize(code);
            try { onCodeAssigned?.Invoke(normalized); } catch { /* UI callback must not break wait */ }

            // Wait until helper joins
            var paired = await ReadJsonLineAsync(stream, ct).ConfigureAwait(false);
            EnsureOk(paired, "paired");
            if (!string.Equals(GetString(paired, "op"), "paired", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("expected paired, got " + GetString(paired, "op"));

            var client = new RelayClient(tcp, stream);
            return (client, normalized);
        }
        catch
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    public static async Task<RelayClient> GuestJoinAsync(
        string host,
        int port,
        string code,
        string password,
        CancellationToken ct = default)
    {
        code = SessionCodeGenerator.Normalize(code);
        password = SessionPasswordGenerator.Normalize(password);
        var tcp = new TcpClient();
        await Net48Sockets.ConnectAsync(tcp, host, port, ct).ConfigureAwait(false);
        var stream = tcp.GetStream();
        try
        {
            var reply = await RoundTripAsync(stream, new Dictionary<string, object?>
            {
                ["op"] = "guest_join",
                ["code"] = code,
                ["password"] = password
            }, ct).ConfigureAwait(false);
            EnsureOk(reply, "paired");
            return new RelayClient(tcp, stream);
        }
        catch
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    public static async Task<string> SignupOrLoginAsync(
        string host,
        int port,
        bool signup,
        string user,
        string password,
        CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await Net48Sockets.ConnectAsync(tcp, host, port, ct).ConfigureAwait(false);
        try
        {
            var stream = tcp.GetStream();
            var op = signup ? "signup" : "login";
            var reply = await RoundTripAsync(stream, new Dictionary<string, object?>
            {
                ["op"] = op,
                ["username"] = user.Trim(),
                ["password"] = password
            }, ct).ConfigureAwait(false);
            EnsureOk(reply, op);
            if (signup)
            {
                // signup does not return token; login next on same connection
                reply = await RoundTripAsync(stream, new Dictionary<string, object?>
                {
                    ["op"] = "login",
                    ["username"] = user.Trim(),
                    ["password"] = password
                }, ct).ConfigureAwait(false);
                EnsureOk(reply, "login");
            }
            return GetString(reply, "token")
                ?? throw new InvalidOperationException("login missing token");
        }
        finally
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
        }
    }

    /// <summary>兼容旧名。</summary>
    public static Task<string> AuthAsync(
        string host, int port, bool register, string user, string password, CancellationToken ct = default) =>
        SignupOrLoginAsync(host, port, register, user, password, ct);

    public static async Task<IReadOnlyList<(string Id, string Name, bool Online)>> ListDevicesAsync(
        string host,
        int port,
        string token,
        CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await Net48Sockets.ConnectAsync(tcp, host, port, ct).ConfigureAwait(false);
        try
        {
            var stream = tcp.GetStream();
            await AuthTokenAsync(stream, token, ct).ConfigureAwait(false);
            var reply = await RoundTripAsync(stream, new Dictionary<string, object?> { ["op"] = "list_devices" }, ct)
                .ConfigureAwait(false);
            EnsureOk(reply, "list_devices");
            var list = new List<(string, string, bool)>();
            if (reply.TryGetValue("devices", out var raw) && raw is JsonElement el && el.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in el.EnumerateArray())
                {
                    var id = d.TryGetProperty("device_id", out var idp) ? idp.GetString() ?? "" : "";
                    var name = d.TryGetProperty("display_name", out var np) ? np.GetString() ?? id : id;
                    var online = d.TryGetProperty("online", out var op) && op.ValueKind == JsonValueKind.True;
                    if (id.Length > 0)
                        list.Add((id, name, online));
                }
            }
            return list;
        }
        finally
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
        }
    }

    public static async Task UpsertDeviceAsync(
        string host, int port, string token, string deviceId, string name, CancellationToken ct = default)
    {
        // JSONL relay upserts on device_online; no separate UPSERT op required.
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public static async Task<RelayClient> DeviceOnlineAsync(
        string host,
        int port,
        string accountToken,
        string deviceId,
        string displayName,
        CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await Net48Sockets.ConnectAsync(tcp, host, port, ct).ConfigureAwait(false);
        var stream = tcp.GetStream();
        try
        {
            await AuthTokenAsync(stream, accountToken, ct).ConfigureAwait(false);
            var reply = await RoundTripAsync(stream, new Dictionary<string, object?>
            {
                ["op"] = "device_online",
                ["device_id"] = deviceId.Trim(),
                ["display_name"] = string.IsNullOrWhiteSpace(displayName) ? deviceId : displayName.Trim()
            }, ct).ConfigureAwait(false);
            EnsureOk(reply, "device_online");

            var paired = await ReadJsonLineAsync(stream, ct).ConfigureAwait(false);
            EnsureOk(paired, "paired");
            return new RelayClient(tcp, stream);
        }
        catch
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    public static async Task<RelayClient> DeviceConnectAsync(
        string host,
        int port,
        string accountToken,
        string deviceId,
        CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await Net48Sockets.ConnectAsync(tcp, host, port, ct).ConfigureAwait(false);
        var stream = tcp.GetStream();
        try
        {
            await AuthTokenAsync(stream, accountToken, ct).ConfigureAwait(false);
            var reply = await RoundTripAsync(stream, new Dictionary<string, object?>
            {
                ["op"] = "connect_device",
                ["device_id"] = deviceId.Trim()
            }, ct).ConfigureAwait(false);
            EnsureOk(reply, "paired");
            return new RelayClient(tcp, stream);
        }
        catch
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    // ---- legacy names used by AssistClient ----
    public static Task<RelayClient> RegisterGuestAsync(
        string host, int port, string sessionCode, string role, string password, CancellationToken ct = default)
    {
        role = role.Trim().ToLowerInvariant();
        if (role is "helper" or "b")
            return GuestJoinAsync(host, port, sessionCode, password, ct);
        throw new InvalidOperationException("agent guest path should use GuestHostAsync");
    }

    private static async Task AuthTokenAsync(Stream stream, string token, CancellationToken ct)
    {
        var reply = await RoundTripAsync(stream, new Dictionary<string, object?>
        {
            ["op"] = "auth_token",
            ["token"] = token
        }, ct).ConfigureAwait(false);
        EnsureOk(reply, "auth_token");
    }

    private static async Task<Dictionary<string, JsonElement>> RoundTripAsync(
        Stream stream, Dictionary<string, object?> req, CancellationToken ct)
    {
        await WriteJsonLineAsync(stream, req, ct).ConfigureAwait(false);
        return await ReadJsonLineAsync(stream, ct).ConfigureAwait(false);
    }

    private static async Task WriteJsonLineAsync(Stream stream, object req, CancellationToken ct)
    {
        string json;
        if (req is Dictionary<string, object?> map)
        {
            var node = new JsonObject();
            foreach (var kv in map)
            {
                node[kv.Key] = kv.Value switch
                {
                    null => null,
                    string s => s,
                    bool b => b,
                    int i => i,
                    long l => l,
                    JsonNode jn => jn,
                    _ => JsonValue.Create(kv.Value?.ToString())
                };
            }
            json = node.ToJsonString();
        }
        else
        {
            json = JsonSerializer.Serialize(req);
        }
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }

    private static async Task<Dictionary<string, JsonElement>> ReadJsonLineAsync(Stream stream, CancellationToken ct)
    {
        var buf = new MemoryStream();
        var one = new byte[1];
        while (true)
        {
            var n = await stream.ReadAsync(one, 0, 1).ConfigureAwait(false);
            if (n == 0)
                throw new EndOfStreamException("relay closed during JSONL control");
            if (one[0] == (byte)'\n')
                break;
            if (one[0] != (byte)'\r')
                buf.WriteByte(one[0]);
            if (buf.Length > 16 * 1024)
                throw new InvalidDataException("relay JSONL line too long");
        }
        var text = Encoding.UTF8.GetString(buf.ToArray()).Trim();
        if (text.Length == 0)
            throw new InvalidDataException("empty relay JSONL line");
        using var doc = JsonDocument.Parse(text);
        var map = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in doc.RootElement.EnumerateObject())
            map[p.Name] = p.Value.Clone();
        return map;
    }

    private static void EnsureOk(Dictionary<string, JsonElement> reply, string expectHint)
    {
        if (reply.TryGetValue("ok", out var okEl) && okEl.ValueKind == JsonValueKind.True)
            return;
        var err = reply.TryGetValue("error", out var e) ? e.GetString() : null;
        throw new InvalidOperationException(err ?? ("relay error near " + expectHint));
    }

    private static string? GetString(Dictionary<string, JsonElement> map, string key)
    {
        if (!map.TryGetValue(key, out var el)) return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => el.ToString()
        };
    }

    public void Dispose()
    {
        try { _stream.Dispose(); } catch { /* ignore */ }
        try { _tcp.Dispose(); } catch { /* ignore */ }
    }
}


