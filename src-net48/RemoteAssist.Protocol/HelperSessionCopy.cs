using System;

namespace RemoteAssist.Protocol;

/// <summary>
/// 去协助端会话主状态与会话码主按钮文案（R035/R036/R037）；R039 锁定五键文案顺序。
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

    /// <summary>会话码区主按钮：未连上时可点实心绿。</summary>
    public const string ConnectButtonText = "连接";

    /// <summary>会话码区主按钮：已连上后不可点（非实心绿）。账号区免密连上后的文案也可复用此句。</summary>
    public const string ConnectedButtonText = "已连接";

    /// <summary>会话码主按钮文案：连上 →「已连接」；否则 →「连接」。</summary>
    public static string SessionConnectButtonLabel(bool connected) =>
        connected ? ConnectedButtonText : ConnectButtonText;

    /// <summary>未连上时会话码区操作提示（原文锁定；R037 只控制显隐）。</summary>
    public const string PasteThenConnectHint = "粘贴后点「连接」";

    /// <summary>R037：已连上不显示粘贴提示；断开/结束/未连上再显示。</summary>
    public static bool ShowPasteThenConnectHint(bool sessionConnected) => !sessionConnected;

    /// <summary>R016：仅已连接时展示五个常用操作。</summary>
    public static bool ShowCommonTools(bool connected) => connected;

    /// <summary>R016/R039：五个常用操作按钮文案与顺序（锁定）。</summary>
    public static readonly string[] CommonToolButtonLabels =
    {
        "查看正在运行的程序",
        "查看文件夹",
        "查看日志",
        "查看文件",
        "提议一项改动",
    };
}
