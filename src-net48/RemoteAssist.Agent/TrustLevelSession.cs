using System;
using System.Collections.Generic;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 信任等级会话态（对齐 net8 / docs/trust-levels.md）。
/// ②：内核与 AgentHost 已接线；<see cref="ElevatedTrustUiEnabled"/> 为 false 时，
/// 升档（宽松/完全信任）不得生效——Effective* 恒为每次询问，直至另轮本机三档 UI。
/// </summary>
internal sealed class TrustLevelSession
{
    /// <summary>三档 UI 未开放前禁止升档生效。</summary>
    public const bool ElevatedTrustUiEnabled = false;

    private readonly object _gate = new();
    private TrustLevelKind _preference = TrustLevelKind.AskEveryTime;
    private bool _fullTrustActive;
    private readonly HashSet<string> _fullTrustTokens = new(StringComparer.Ordinal);

    public event Action? Changed;

    public TrustLevelSession()
    {
        // ② 无 UI：不从磁盘加载宽松偏好，避免无入口时静默升档。偏好固定每次询问。
        _preference = TrustLevelKind.AskEveryTime;
    }

    public TrustLevelKind Preference
    {
        get { lock (_gate) return _preference; }
    }

    public TrustLevelKind Effective
    {
        get
        {
            lock (_gate)
            {
                if (!ElevatedTrustUiEnabled)
                    return TrustLevelKind.AskEveryTime;
                return _fullTrustActive ? TrustLevelKind.FullTrust : _preference;
            }
        }
    }

    public bool IsFullTrustActive
    {
        get
        {
            lock (_gate)
                return ElevatedTrustUiEnabled && _fullTrustActive;
        }
    }

    public void SetPreference(TrustLevelKind level)
    {
        if (!ElevatedTrustUiEnabled)
            throw new InvalidOperationException("信任档位 UI 未开放，不能修改偏好。");
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

    public void EnableFullTrust(IEnumerable<string> currentClientTokens)
    {
        if (!ElevatedTrustUiEnabled)
            throw new InvalidOperationException("信任档位 UI 未开放，不能启用完全信任。");

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

    public void EndFullTrust()
    {
        lock (_gate)
        {
            _fullTrustActive = false;
            _fullTrustTokens.Clear();
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
        if (ElevatedTrustUiEnabled)
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
            // 刷新会话码：不沿用升档；② 亦强制偏好回每次询问
            if (_preference != TrustLevelKind.AskEveryTime)
            {
                _preference = TrustLevelKind.AskEveryTime;
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
                _fullTrustActive = false;
                _fullTrustTokens.Clear();
                changed = true;
            }
        }
        if (changed) RaiseChanged();
    }

    public bool FullTrustCovers(string? token)
    {
        lock (_gate)
        {
            if (!ElevatedTrustUiEnabled || !_fullTrustActive) return false;
            if (string.IsNullOrWhiteSpace(token)) return false;
            if (_fullTrustTokens.Count == 0) return false;
            return _fullTrustTokens.Contains(token);
        }
    }

    public TrustLevelKind EffectiveForClient(string? token)
    {
        lock (_gate)
        {
            // ② 无三档 UI：工具策略侧恒每次询问（默认不放宽）
            if (!ElevatedTrustUiEnabled)
                return TrustLevelKind.AskEveryTime;

            if (_fullTrustActive && !string.IsNullOrWhiteSpace(token) &&
                _fullTrustTokens.Count > 0 && _fullTrustTokens.Contains(token))
                return TrustLevelKind.FullTrust;
            return _preference;
        }
    }

    private void SavePreference()
    {
        // ② UI 未开放前不写盘升档；预留字段仅供后续 UI 轮
        try
        {
            var s = HelperUiSettings.Load();
            s.TrustLevelPreference = TrustLevelKind.AskEveryTime.ToString();
            s.Save();
        }
        catch { /* ignore */ }
    }

    private void RaiseChanged()
    {
        try { Changed?.Invoke(); } catch { /* UI */ }
    }
}