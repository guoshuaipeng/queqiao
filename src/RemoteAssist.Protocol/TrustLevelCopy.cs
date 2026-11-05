namespace RemoteAssist.Protocol;

/// <summary>被协助端信任等级（R007 / R012）。完全信任仅会话内有效，不跨会话持久化。</summary>
public enum TrustLevelKind
{
    /// <summary>每次询问（默认）</summary>
    AskEveryTime = 0,
    /// <summary>宽松</summary>
    Relaxed = 1,
    /// <summary>完全信任（仅当前会话 + 当前协助者）</summary>
    FullTrust = 2
}

/// <summary>界面原文，必须与 docs/trust-levels.md 一致，禁止另发明名称或句子。</summary>
public static class TrustLevelCopy
{
    public const string GroupTitle = "对方要改这台电脑时";

    public const string AskEveryTimeName = "每次询问";
    public const string RelaxedName = "宽松";
    public const string FullTrustName = "完全信任";

    public const string AskEveryTimeBlurb = "对方改动前都会问你（推荐）";
    public const string RelaxedBlurb = "只有高风险改动才问你，其余会记下但不再打扰";
    /// <summary>完全信任选项一行说明（trust-levels.md 2026-11-05）。</summary>
    public const string FullTrustBlurb = "仅这次不再询问，但会记下";

    /// <summary>完全信任选项旁小字。</summary>
    public const string FullTrustOnceHint = "仅这次";

    public const string HighRiskSupplement = "高风险包括删除文件、关机、改权限等。";

    public const string GuestCeilingNote = "如果对方只能查看，不会因为这个设置就能改你的电脑。";

    public const string FullTrustConfirmTitle = "这次协助不再询问了吗？";

    /// <summary>二次确认正文（trust-levels.md 原文）。</summary>
    public const string FullTrustConfirmBody =
        "打开后，只对现在这位协助者、这一次有效。对方做的改动都不会再弹窗问你，但仍会记在这台电脑的记录里。你可以随时改回。这次结束后，或换一个人再连，不会继续完全信任，会回到你平时的选择。";

    public const string FullTrustConfirmOk = "确定，这次不再询问";
    public const string FullTrustConfirmCancel = "取消";

    public const string RelaxedBanner =
        "当前是「宽松」：只有高风险改动才会问你。";

    public const string FullTrustBanner =
        "当前是「完全信任」：这次协助的改动不会再问你。";

    public const string RevertToAskEveryTime = "改回每次询问";

    /// <summary>完全信任主界面操作（P2-7）：只结束本次，不改平时偏好。</summary>
    public const string EndFullTrust = "结束完全信任";

    public const string ConfirmedChangesTitle = "本次协助已确认的改动";

    public static string NameOf(TrustLevelKind kind) => kind switch
    {
        TrustLevelKind.Relaxed => RelaxedName,
        TrustLevelKind.FullTrust => FullTrustName,
        _ => AskEveryTimeName
    };

    public static string BlurbOf(TrustLevelKind kind) => kind switch
    {
        TrustLevelKind.Relaxed => RelaxedBlurb,
        TrustLevelKind.FullTrust => FullTrustBlurb,
        _ => AskEveryTimeBlurb
    };

    public static string BannerOf(TrustLevelKind kind) => kind switch
    {
        TrustLevelKind.Relaxed => RelaxedBanner,
        TrustLevelKind.FullTrust => FullTrustBanner,
        _ => ""
    };

    /// <summary>主界面操作按钮：宽松→改回每次询问；完全信任→结束完全信任。</summary>
    public static string TipActionOf(TrustLevelKind kind) => kind switch
    {
        TrustLevelKind.FullTrust => EndFullTrust,
        TrustLevelKind.Relaxed => RevertToAskEveryTime,
        _ => ""
    };

    /// <summary>
    /// 「最近审计」信任等级 detail 人话：禁止 preference=AskEveryTime 等内部名外露。
    /// </summary>
    public static string HumanizeAuditDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return "";

        var d = detail.Trim();
        var reverted = d.Contains("reverted=1", StringComparison.OrdinalIgnoreCase);

        if (d.Contains("fulltrust=enabled", StringComparison.OrdinalIgnoreCase))
            return FullTrustName;
        if (d.Contains("fulltrust=ended", StringComparison.OrdinalIgnoreCase))
            return EndFullTrust;

        string? levelName = null;
        if (d.Contains("preference=AskEveryTime", StringComparison.OrdinalIgnoreCase) ||
            d.Contains("preference=0", StringComparison.OrdinalIgnoreCase))
            levelName = AskEveryTimeName;
        else if (d.Contains("preference=Relaxed", StringComparison.OrdinalIgnoreCase) ||
                 d.Contains("preference=1", StringComparison.OrdinalIgnoreCase))
            levelName = RelaxedName;
        else if (d.Contains("preference=FullTrust", StringComparison.OrdinalIgnoreCase) ||
                 d.Contains("preference=2", StringComparison.OrdinalIgnoreCase))
            levelName = FullTrustName;
        else if (d.StartsWith("preference=", StringComparison.OrdinalIgnoreCase))
        {
            // 未知枚举名：仍不把 preference= 原文给亲友
            var raw = d["preference=".Length..];
            var semi = raw.IndexOf(';');
            if (semi >= 0) raw = raw[..semi];
            levelName = raw.Trim() switch
            {
                nameof(TrustLevelKind.AskEveryTime) => AskEveryTimeName,
                nameof(TrustLevelKind.Relaxed) => RelaxedName,
                nameof(TrustLevelKind.FullTrust) => FullTrustName,
                _ => AskEveryTimeName
            };
        }

        if (levelName is not null)
            return reverted ? RevertToAskEveryTime : levelName;

        // 剥掉 preference=/fulltrust= 残留键，避免泄漏
        while (true)
        {
            var cut = -1;
            foreach (var key in new[] { "preference=", "fulltrust=", "reverted=" })
            {
                if (d.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    var semi = d.IndexOf(';');
                    d = semi < 0 ? "" : d[(semi + 1)..].TrimStart();
                    cut = 0;
                    break;
                }
            }
            if (cut < 0) break;
        }

        if (d.Contains("preference=", StringComparison.OrdinalIgnoreCase) ||
            d.Contains("AskEveryTime", StringComparison.OrdinalIgnoreCase) ||
            d.Contains("FullTrust", StringComparison.OrdinalIgnoreCase))
            return "";

        return d;
    }
}
