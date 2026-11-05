using System.Security.Cryptography;
using System.Text;

namespace RemoteAssist.Protocol;

public static class SessionCodeGenerator
{
    // Avoid ambiguous chars: 0/O, 1/I/L
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 6)
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

    public static string Normalize(string code) =>
        (code ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
}
