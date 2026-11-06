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
        var one = new byte[1];
        var mod = Alphabet.Length;
        // 拒绝采样：仅用 < (256 - 256%mod) 的字节，避免 mod 偏
        var limit = 256 - (256 % mod);
        for (var i = 0; i < length; i++)
        {
            byte b;
            do
            {
                Net48Crypto.FillRandom(one);
                b = one[0];
            } while (b >= limit);
            chars[i] = Alphabet[b % mod];
        }
        return new string(chars);
    }

    public static string Normalize(string code) =>
        (code ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
}
