using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 被协助端信任等级会话态：偏好可持久化（每次询问/宽松）；完全信任仅当前会话+协助者。
/// </summary>
internal sealed class TrustLevelSession
{
    private readonly object _gate = new();
    private TrustLevelKind _preference;
    private bool _fullTrustActive;
    private readonly HashSet<string> _fullTrustTokens = new(StringComparer.Ordinal);

    public event Action? Changed;

    public TrustLevelSession()
    {
        var settings = HelperUiSettings.Load();
        _preference = ParsePreference(settings.TrustLevelPreference);
    }

    public TrustLevelKind Preference
    {
        get { lock (_gate) return _preference; }
    }

    /// <summary>UI 与确认逻辑看到的当前生效档。</summary>
    public TrustLevelKind Effective
    {
        get
        {
            lock (_gate)
                return _fullTrustActive ? TrustLevelKind.FullTrust : _preference;
        }
    }

    public bool IsFullTrustActive
    {
        get { lock (_gate) return _fullTrustActive; }
    }

    public void SetPreference(TrustLevelKind level)
    {
        if (level == TrustLevelKind.FullTrust)
            throw new ArgumentException("完全信任不能作为可持久化偏好写入。");

        lock (_gate)
        {
            _preference = level;
            _fullTrustActive = false;
            _fullTrustTokens.Clear();
        }
        SavePreference();
        RaiseChanged();
    }

    /// <summary>二次确认通过后启用完全信任；绑定当前已连接令牌（可为空，等下一位配对）。</summary>
    public void EnableFullTrust(IEnumerable<string> currentClientTokens)
    {
        lock (_gate)
        {
            _fullTrustActive = true;
            _fullTrustTokens.Clear();
            foreach (var t in currentClientTokens)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    _fullTrustTokens.Add(t);
            }
        }
        RaiseChanged();
    }

    public void RevertToAskEveryTime()
    {
        lock (_gate)
        {
            _preference = TrustLevelKind.AskEveryTime;
            _fullTrustActive = false;
            _fullTrustTokens.Clear();
        }
        SavePreference();
        RaiseChanged();
    }

    public void OnSessionRefreshed()
    {
        var cleared = false;
        lock (_gate)
        {
            if (_fullTrustActive || _fullTrustTokens.Count > 0)
            {
                _fullTrustActive = false;
                _fullTrustTokens.Clear();
                cleared = true;
            }
        }
        if (cleared) RaiseChanged();
    }

    public void OnClientDisconnected(string token)
    {
        var cleared = false;
        lock (_gate)
        {
            if (!_fullTrustActive) return;
            if (_fullTrustTokens.Remove(token) && _fullTrustTokens.Count == 0)
            {
                _fullTrustActive = false;
                cleared = true;
            }
        }
        if (cleared) RaiseChanged();
    }

    public void OnClientPaired(string token)
    {
        var changed = false;
        lock (_gate)
        {
            if (!_fullTrustActive) return;
            if (_fullTrustTokens.Count == 0)
            {
                _fullTrustTokens.Add(token);
                changed = true;
            }
            else if (!_fullTrustTokens.Contains(token))
            {
                // 新访客 / 换人：完全信任失效
                _fullTrustActive = false;
                _fullTrustTokens.Clear();
                changed = true;
            }
        }
        if (changed) RaiseChanged();
    }

    /// <summary>该会话令牌下写操作是否仍享受完全信任自动放行。</summary>
    public bool FullTrustCovers(string? token)
    {
        lock (_gate)
        {
            if (!_fullTrustActive) return false;
            if (string.IsNullOrWhiteSpace(token)) return false;
            // 尚未绑定任何协助者时，暂不放行（等 OnClientPaired 绑定）
            if (_fullTrustTokens.Count == 0) return false;
            return _fullTrustTokens.Contains(token);
        }
    }

    public TrustLevelKind EffectiveForClient(string? token)
    {
        lock (_gate)
        {
            if (_fullTrustActive && !string.IsNullOrWhiteSpace(token) &&
                _fullTrustTokens.Count > 0 && _fullTrustTokens.Contains(token))
                return TrustLevelKind.FullTrust;
            return _preference;
        }
    }

    private void SavePreference()
    {
        try
        {
            var s = HelperUiSettings.Load();
            s.TrustLevelPreference = _preference.ToString();
            s.Save();
        }
        catch { /* ignore */ }
    }

    private void RaiseChanged()
    {
        try { Changed?.Invoke(); } catch { /* UI */ }
    }

    private static TrustLevelKind ParsePreference(string? raw)
    {
        if (string.Equals(raw, nameof(TrustLevelKind.Relaxed), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "Relaxed", StringComparison.OrdinalIgnoreCase) ||
            raw == "宽松")
            return TrustLevelKind.Relaxed;
        return TrustLevelKind.AskEveryTime;
    }
}
