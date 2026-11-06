using System;
using System.Security.Cryptography;
using System.Text;

namespace RemoteAssist.Protocol;

/// <summary>验证码生成：新码固定 8 位纯数字（保留前导零）；Normalize 不去掉内部字符。</summary>
public static class SessionPasswordGenerator
{
    /// <summary>新生成默认长度（R043 连接凭证）。</summary>
    public const int DefaultLength = 8;

    public static string Generate(int length = DefaultLength)
    {
        if (length < 4) length = 4;
        var sb = new StringBuilder(length);
        var one = new byte[1];
        for (var i = 0; i < length; i++)
        {
            // 拒绝采样：丢弃 250..255，避免 byte%10 模偏
            byte b;
            do
            {
                Net48Crypto.FillRandom(one);
                b = one[0];
            } while (b >= 250);
            sb.Append((char)('0' + (b % 10)));
        }
        return sb.ToString();
    }

    /// <summary>仅 Trim；保留前导零与大小写（数字码无大小写问题）。</summary>
    public static string Normalize(string? password) =>
        string.IsNullOrWhiteSpace(password) ? "" : password.Trim();
}
