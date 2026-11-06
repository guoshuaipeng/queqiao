namespace RemoteAssist.Protocol;

/// <summary>被协助端信任等级（docs/trust-levels.md）。完全信任仅会话内；② 无 UI 前生效档强制每次询问。</summary>
public enum TrustLevelKind
{
    /// <summary>每次询问（默认）</summary>
    AskEveryTime = 0,
    /// <summary>宽松</summary>
    Relaxed = 1,
    /// <summary>完全信任（仅当前会话 + 当前协助者）</summary>
    FullTrust = 2
}