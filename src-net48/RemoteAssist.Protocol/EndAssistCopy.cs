using System;

namespace RemoteAssist.Protocol;

/// <summary>
/// net48 被协助端「结束这次协助」+ 本机改动回看入口文案（迁移③）。
/// 结束只断开、旧码仍可再用；回看只读本机审计、不上传云端。
/// </summary>
public static class EndAssistCopy
{
    public const string ButtonText = "结束这次协助";

    /// <summary>确认正文：断开连接，但不作废未刷新的码。</summary>
    public const string ConfirmBody = "结束后对方将立刻连不上。未刷新则旧码仍可再用。";

    public const string ConfirmOk = ButtonText;

    public const string ConfirmCancel = "取消";

    /// <summary>本机结束后主状态（对齐 quality-after-disconnect）。</summary>
    public const string StatusEndedKeepCode =
        "你已结束这次协助。旧码还可再用；若还要请人帮，把下面的码再发给对方即可（或先点「刷新会话码」换新的）。";

    public const string ReviewButtonText = "本机改动回看";

    public const string ReviewTitle = "本次协助已确认的改动";

    public const string ReviewEmpty = "（暂无已确认的改动）";

    public const string ReviewCloseText = "收起回看";
}