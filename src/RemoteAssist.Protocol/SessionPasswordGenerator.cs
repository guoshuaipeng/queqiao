using System.Security.Cryptography;

namespace RemoteAssist.Protocol;

public static class SessionPasswordGenerator
{
    // Short numeric-ish password for human sharing (avoid 0/1 ambiguity lightly)
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    public static string Generate(int length = 4)
    {
        if (length < 4 || length > 12)
            throw new ArgumentOutOfRangeException(nameof(length));
        Span<char> chars = stackalloc char[length];
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        return new string(chars);
    }

    public static string Normalize(string password) =>
        (password ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
}
