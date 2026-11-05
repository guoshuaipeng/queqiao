namespace RemoteAssist.Relay.Stub;

/// <summary>
/// 兼容旧引用。公网中继已由 Go <c>relay/</c> + Protocol.RelayClient 接通；
/// 本类型仅保留 endpoint 解析与提示文案。
/// </summary>
public static class RelayPlaceholder
{
    public const string Status = "Implemented_relay_mvp";

    public const string UpcomingHint =
        "默认公网中继 121.196.45.81:17891（可改）；被控出示 code+密码，控制端输入后连接。也可登录账号免密控自己的机器。";

    public static bool TryParseEndpoint(string? text, out string host, out int port, out string error)
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
            error = "中继格式应为 IP:端口（例如 121.196.45.81:17891）";
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

    public static void EnsureNotUsedInV1()
    {
        // retained for binary compat; no-op
    }
}
