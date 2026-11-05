using System;

namespace RemoteAssist.Protocol;

public static class SessionCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 6)
    {
        if (length < 4 || length > 12)
            throw new ArgumentOutOfRangeException(nameof(length));

        var chars = new char[length];
        var bytes = new byte[length];
        Net48Crypto.FillRandom(bytes);
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        return new string(chars);
    }

    public static string Normalize(string code) =>
        (code ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
}
