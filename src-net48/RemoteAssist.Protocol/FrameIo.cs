using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Protocol;

/// <summary>Length-prefixed (4-byte LE) UTF-8 JSON framing over a duplex Stream (TCP / SslStream).</summary>
public static class FrameIo
{
    public static async Task WriteAsync(Stream stream, WireEnvelope envelope, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(envelope, ProtocolJson.Options);
        var payload = Encoding.UTF8.GetBytes(json);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await stream.WriteAsync(header, 0, header.Length, ct).ConfigureAwait(false);
        await stream.WriteAsync(payload, 0, payload.Length, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    public static async Task WriteAsync(Stream stream, string type, object? payload, string? token = null, string? id = null, CancellationToken ct = default)
    {
        var envelope = new WireEnvelope
        {
            Type = type,
            Id = id ?? Guid.NewGuid().ToString("N"),
            Token = token,
            Payload = payload
        };
        await WriteAsync(stream, envelope, ct).ConfigureAwait(false);
    }

    public static async Task<WireEnvelope?> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        var header = new byte[4];
        if (!await ReadExactAsync(stream, header, ct).ConfigureAwait(false))
            return null;

        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length <= 0 || length > 8 * 1024 * 1024)
            throw new InvalidDataException($"Invalid frame length: {length}");

        var payload = new byte[length];
        if (!await ReadExactAsync(stream, payload, ct).ConfigureAwait(false))
            return null;

        var json = Encoding.UTF8.GetString(payload);
        return JsonSerializer.Deserialize<WireEnvelope>(json, ProtocolJson.Options);
    }

    public static T? DeserializePayload<T>(WireEnvelope envelope)
    {
        if (envelope.Payload is null)
            return default;
        if (envelope.Payload is JsonElement element)
            return element.Deserialize<T>(ProtocolJson.Options);
        var json = JsonSerializer.Serialize(envelope.Payload, ProtocolJson.Options);
        return JsonSerializer.Deserialize<T>(json, ProtocolJson.Options);
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, ct).ConfigureAwait(false);
            if (read == 0)
                return false;
            offset += read;
        }
        return true;
    }
}
