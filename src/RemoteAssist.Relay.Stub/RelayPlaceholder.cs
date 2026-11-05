namespace RemoteAssist.Relay.Stub;

/// <summary>
/// TODO(v2+): 公网/跨 NAT 中继隧道。
/// v1 仅局域网直连（Agent TCP 监听 + Helper/MCP 配对）。
/// 后续可选方案：自建 relay、或临时接入 FRP 类现成隧道；中继应只转发会话令牌流量，不做账号体系。
/// </summary>
public static class RelayPlaceholder
{
    public const string Status = "NotImplemented_v1_LAN_only";

    public static void EnsureNotUsedInV1()
    {
        throw new NotSupportedException(
            "公网中继未实现。v1 请使用局域网：Helper/MCP 直连 Agent 的 host:port + 会话码。");
    }
}
