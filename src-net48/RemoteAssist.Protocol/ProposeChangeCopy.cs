namespace RemoteAssist.Protocol;

/// <summary>
/// R041：去协助端「提议一项改动」四态人话 + 刚连上结果区提示。
/// 拒绝/超时原文仍锁定在 <see cref="WriteConfirmCopy"/>。
/// </summary>
public static class ProposeChangeCopy
{
    /// <summary>发出提议后、对方尚未允许/拒绝/超时。</summary>
    public const string WaitingStatus = "正在等对方在本机确认…";

    /// <summary>对方允许且执行成功。</summary>
    public const string AllowedStatus = "对方已允许，改动已执行";

    /// <summary>配对刚成功、尚未点操作时的结果区人话（代替 PAIR_OK）。</summary>
    public const string ConnectedResultHint = "已连上。建议先点「查看正在运行的程序」。";

    /// <summary>
    /// DeniedByUser 时优先展示非空 Error（超时句来自被控端）；否则拒绝句。
    /// 非用户拒绝失败则展示 Error 或「失败」。
    /// </summary>
    public static string FailureStatus(bool deniedByUser, string? error)
    {
        if (deniedByUser)
        {
            if (!string.IsNullOrWhiteSpace(error))
                return error.Trim();
            return WriteConfirmCopy.DeniedMessage;
        }

        return string.IsNullOrWhiteSpace(error) ? "失败" : error.Trim();
    }

    /// <summary>提议改动成功主状态；只读操作请调用方自行处理。</summary>
    public static string AllowedStatusFor(ToolKind tool) =>
        tool == ToolKind.RunCommand ? AllowedStatus : "完成";

    /// <summary>是否应对该工具显示「正在等对方确认」等待句（仅写操作提议）。</summary>
    public static bool UsesWaitingStatus(ToolKind tool) => tool == ToolKind.RunCommand;
}
