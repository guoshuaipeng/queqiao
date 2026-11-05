using System.Net.Sockets;
using System.Text;

namespace RemoteAssist.Protocol;

/// <summary>解析 host:port / [ipv6]:port 中继地址。</summary>
public static class RelayEndpoint
{
    public const int DefaultPort = 17891;
    public const string DefaultEndpoint = "127.0.0.1:17891";

    public static bool TryParse(string? text, out string host, out int port, out string error)
    {
        host = "";
        port = 0;
        error = "";
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "中继地址为空";
            return false;
        }

        var s = text.Trim();
        if (s.StartsWith('['))
        {
            var close = s.IndexOf(']');
            if (close < 1 || close + 2 >= s.Length || s[close + 1] != ':')
            {
                error = "IPv6 中继格式应为 [地址]:端口";
                return false;
            }
            host = s[1..close];
            if (!int.TryParse(s[(close + 2)..], out port) || port is < 1 or > 65535)
            {
                error = "端口需要 1–65535";
                return false;
            }
            return true;
        }

        var idx = s.LastIndexOf(':');
        if (idx <= 0 || idx == s.Length - 1)
        {
            error = "中继格式应为 IP:端口（例如 1.2.3.4:17891）";
            return false;
        }

        host = s[..idx].Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            error = "主机不能为空";
            return false;
        }

        if (!int.TryParse(s[(idx + 1)..], out port) || port is < 1 or > 65535)
        {
            error = "端口需要 1–65535";
            return false;
        }

        return true;
    }
}

/// <summary>
/// 出连公网中继：控制行配对后返回透明 TCP 流（业务帧/TLS 仍在端到端）。
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

    public static async Task<RelayClient> RegisterGuestAsync(
        string host,
        int port,
        string sessionCode,
        string role,
        string password,
        CancellationToken ct = default)
    {
        var code = SessionCodeGenerator.Normalize(sessionCode);
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("session code required", nameof(sessionCode));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("password required", nameof(password));

        role = NormalizeRole(role);
        var line = $"REGISTER {code} {role} {password}\n";
        return await DialControlAsync(host, port, line, ct).ConfigureAwait(false);
    }

    public static async Task<RelayClient> DeviceOnlineAsync(
        string host,
        int port,
        string accountToken,
        string deviceId,
        string displayName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountToken))
            throw new ArgumentException("token required", nameof(accountToken));
        deviceId = deviceId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? deviceId : displayName.Trim();
        // name may contain spaces — relay joins remaining fields
        var line = $"DEVICE ONLINE {accountToken} {deviceId} {displayName}\n";
        return await DialControlAsync(host, port, line, ct).ConfigureAwait(false);
    }

    public static async Task<RelayClient> DeviceConnectAsync(
        string host,
        int port,
        string accountToken,
        string deviceId,
        CancellationToken ct = default)
    {
        var line = $"DEVICE CONNECT {accountToken} {deviceId.Trim()}\n";
        return await DialControlAsync(host, port, line, ct).ConfigureAwait(false);
    }

    public static async Task<string> AuthAsync(
        string host,
        int port,
        bool register,
        string user,
        string password,
        CancellationToken ct = default)
    {
        var op = register ? "REGISTER" : "LOGIN";
        var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);
        try
        {
            var stream = tcp.GetStream();
            await WriteAsciiLineAsync(stream, $"AUTH {op} {user.Trim()} {password}\n", ct).ConfigureAwait(false);
            var reply = await ReadAsciiLineAsync(stream, ct).ConfigureAwait(false);
            if (!reply.StartsWith("OK token ", StringComparison.Ordinal))
                throw new InvalidOperationException(reply.StartsWith("ERR ", StringComparison.Ordinal) ? reply[4..] : reply);
            return reply["OK token ".Length..].Trim();
        }
        finally
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
        }
    }

    public static async Task UpsertDeviceAsync(
        string host,
        int port,
        string token,
        string deviceId,
        string name,
        CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);
        try
        {
            var stream = tcp.GetStream();
            await WriteAsciiLineAsync(stream, $"DEVICE UPSERT {token} {deviceId.Trim()} {name.Trim()}\n", ct)
                .ConfigureAwait(false);
            var reply = await ReadAsciiLineAsync(stream, ct).ConfigureAwait(false);
            if (!string.Equals(reply, "OK", StringComparison.Ordinal))
                throw new InvalidOperationException(reply.StartsWith("ERR ", StringComparison.Ordinal) ? reply[4..] : reply);
        }
        finally
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
        }
    }

    public static async Task<IReadOnlyList<(string Id, string Name, bool Online)>> ListDevicesAsync(
        string host,
        int port,
        string token,
        CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);
        try
        {
            var stream = tcp.GetStream();
            await WriteAsciiLineAsync(stream, $"DEVICE LIST {token}\n", ct).ConfigureAwait(false);
            var hdr = await ReadAsciiLineAsync(stream, ct).ConfigureAwait(false);
            if (!hdr.StartsWith("OK ", StringComparison.Ordinal))
                throw new InvalidOperationException(hdr.StartsWith("ERR ", StringComparison.Ordinal) ? hdr[4..] : hdr);
            if (!int.TryParse(hdr[3..].Trim(), out var n) || n < 0)
                throw new InvalidOperationException("bad DEVICE LIST header: " + hdr);
            var list = new List<(string, string, bool)>(n);
            for (var i = 0; i < n; i++)
            {
                var row = await ReadAsciiLineAsync(stream, ct).ConfigureAwait(false);
                var parts = row.Split('\t');
                if (parts.Length < 3) continue;
                list.Add((parts[0], parts[1], parts[2] == "1"));
            }
            return list;
        }
        finally
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
        }
    }

    private static async Task<RelayClient> DialControlAsync(string host, int port, string line, CancellationToken ct)
    {
        var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);
        try
        {
            var stream = tcp.GetStream();
            await WriteAsciiLineAsync(stream, line, ct).ConfigureAwait(false);
            var reply = await ReadAsciiLineAsync(stream, ct).ConfigureAwait(false);
            if (reply.StartsWith("ERR ", StringComparison.Ordinal))
                throw new InvalidOperationException(reply[4..]);
            if (!reply.StartsWith("OK ", StringComparison.Ordinal))
                throw new InvalidOperationException("unexpected relay reply: " + reply);
            // OK waiting | OK paired — both OK to proceed; bytes queue until peer joins
            return new RelayClient(tcp, stream);
        }
        catch
        {
            try { tcp.Dispose(); } catch { /* ignore */ }
            throw;
        }
    }

    private static string NormalizeRole(string role) =>
        role.Trim().ToLowerInvariant() switch
        {
            "agent" or "a" => "agent",
            "helper" or "b" => "helper",
            _ => throw new ArgumentException("role must be agent|helper", nameof(role))
        };

    private static async Task WriteAsciiLineAsync(Stream stream, string line, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(line);
        await stream.WriteAsync(bytes, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    private static async Task<string> ReadAsciiLineAsync(Stream stream, CancellationToken ct)
    {
        var buf = new MemoryStream();
        var one = new byte[1];
        while (true)
        {
            var n = await stream.ReadAsync(one.AsMemory(0, 1), ct).ConfigureAwait(false);
            if (n == 0)
                throw new EndOfStreamException("relay closed during control reply");
            if (one[0] == (byte)'\n')
                break;
            if (one[0] != (byte)'\r')
                buf.WriteByte(one[0]);
            if (buf.Length > 2048)
                throw new InvalidDataException("relay control line too long");
        }
        return Encoding.UTF8.GetString(buf.ToArray()).Trim();
    }

    public void Dispose()
    {
        try { _stream.Dispose(); } catch { /* ignore */ }
        try { _tcp.Dispose(); } catch { /* ignore */ }
    }
}
