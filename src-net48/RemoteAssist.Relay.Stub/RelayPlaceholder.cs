namespace RemoteAssist.Relay.Stub;

/// <summary>
/// 中继地址格式校验（R043：真连接由 Protocol.RelayEndpoint / RelayClient 完成）。
/// </summary>
public static class RelayPlaceholder
{
    public static bool TryParseEndpoint(string? text, out string host, out int port, out string error)
    {
        if (RemoteAssist.Protocol.RelayEndpoint.TryParse(text, out host, out port, out var err))
        {
            error = "";
            return true;
        }
        host = "";
        port = 0;
        error = string.IsNullOrWhiteSpace(err) ? "中继格式应为 IP:端口" : err!;
        // 人话：不要露出协议英文
        if (error.Contains("empty") || error.Contains("expect"))
            error = "中继格式应为 IP:端口（例如 121.196.45.81:17891）";
        return false;
    }
}
