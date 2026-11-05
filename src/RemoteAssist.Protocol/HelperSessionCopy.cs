using System;

namespace RemoteAssist.Protocol;

/// <summary>
/// 去协助端会话主状态（R035）：对方结束 / 自己断开 / 未连上失败 三类分场景。
/// </summary>
public static class HelperSessionCopy
{
    /// <summary>已成功连上之后，对方结束了这次协助（或对等断开会话）。</summary>
    public const string StatusPeerEndedAssist = "对方已结束这次协助。";

    /// <summary>已成功连上时的主状态。</summary>
    public const string StatusConnected = "已连接对方电脑";

    /// <summary>协助方自己点断开后的主状态。</summary>
    public const string StatusDisconnectedSelf = "状态：未连接";

    /// <summary>还没连上就失败时的主状态（细节见 FriendConnectGuide）。</summary>
    public const string StatusConnectFailed = "连接失败";

    /// <summary>
    /// 已连上的会话结束时选主状态：自己点断开 → 未连接句；否则 → 对方已结束。
    /// </summary>
    public static string StatusAfterConnectedSessionEnd(bool selfInitiatedDisconnect) =>
        selfInitiatedDisconnect ? StatusDisconnectedSelf : StatusPeerEndedAssist;

    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;
}
