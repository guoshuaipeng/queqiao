namespace RemoteAssist.Relay.Stub;

/// <summary>
/// TODO(v2+): 公网/跨 NAT 中继隧道。
/// v1 仅局域网直连（Agent TCP 监听 + Helper/MCP 配对）。
/// 后续：协助端/被协助端主动出连中继（可用纯 IP:端口，不强制域名）；
/// 中继做鉴权、会话路由与转发；简单账号可挂在协助方登录拿 token。
/// </summary>
public static class RelayPlaceholder
{
    public const string Status = "NotImplemented_v1_LAN_only";

    public const string UpcomingHint =
        "即将支持：填写中继 IP:端口 后可经公网服务器转发（可用纯 IP，不强制域名）";

    /// <summary>
    /// 校验「IP或主机:端口」格式，供设置页占位；当前不发起连接。
    /// </summary>
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
        // 支持 [ipv6]:port 与 host:port
        if (s.StartsWith("["))
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
            error = "中继格式应为 IP:端口（例如 1.2.3.4:443）";
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
        throw new NotSupportedException(
            "公网中继未实现。v1 请使用局域网：Helper/MCP 直连 Agent 的 host:port + 会话码。" +
            Environment.NewLine + UpcomingHint);
    }
}
