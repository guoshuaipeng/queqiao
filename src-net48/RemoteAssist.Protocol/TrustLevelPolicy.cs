namespace RemoteAssist.Protocol;

/// <summary>信任等级 → 本机是否还要弹确认。</summary>
public static class TrustLevelPolicy
{
    /// <summary>
    /// 写操作是否需要本机确认。只读工具返回 false。
    /// Guest 写权限在调用方用 ToolCapabilities.IsAllowed 拦下，此处不处理。
    /// </summary>
    public static bool NeedsLocalConfirm(TrustLevelKind level, ToolKind tool, IReadOnlyDictionary<string, string?>? args = null)
    {
        if (ToolCapabilities.IsReadOnly(tool))
            return false;

        return level switch
        {
            TrustLevelKind.FullTrust => false,
            TrustLevelKind.Relaxed => HighRiskWrite.IsHighRisk(tool, args),
            _ => true // AskEveryTime 默认
        };
    }

    /// <summary>可持久化的偏好：完全信任不能保存。</summary>
    public static TrustLevelKind Persistable(TrustLevelKind level) =>
        level == TrustLevelKind.FullTrust ? TrustLevelKind.AskEveryTime : level;

    public static bool IsPersistable(TrustLevelKind level) =>
        level is TrustLevelKind.AskEveryTime or TrustLevelKind.Relaxed;
}
