using System;

namespace RemoteAssist.Protocol;

/// <summary>
/// Windows 接受协助端「结束这次协助」（R033）。关窗仍只是藏托盘；要停协助用本入口。
/// </summary>
public static class EndAssistCopy
{
    /// <summary>主界面轻按钮 / 确认窗确认按钮。</summary>
    public const string ButtonText = "结束这次协助";

    /// <summary>结束前确认正文。</summary>
    public const string ConfirmBody = "结束后对方将立刻连不上。";

    /// <summary>确认窗确认按钮（与 ButtonText 同文）。</summary>
    public const string ConfirmOk = ButtonText;

    /// <summary>确认窗取消。</summary>
    public const string ConfirmCancel = "取消";
}
