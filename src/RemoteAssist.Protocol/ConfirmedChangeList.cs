namespace RemoteAssist.Protocol;

/// <summary>
/// 从审计行筛「本次协助已确认的改动」：已确认/自动允许的写操作。
/// 未确认、已拒绝不得出现。
/// </summary>
public static class ConfirmedChangeList
{
    public static readonly HashSet<string> IncludedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "tool_confirmed",
        "tool_auto_relaxed",
        "tool_auto_fulltrust",
        "tool_admin_auto"
    };

    public static readonly HashSet<string> ExcludedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "tool_denied_user",
        "tool_denied_perm"
    };

    public static IReadOnlyList<(string EventType, string Detail)> Filter(
        IEnumerable<(string EventType, string Detail)> entries)
    {
        var list = new List<(string, string)>();
        foreach (var e in entries)
        {
            if (ExcludedEventTypes.Contains(e.EventType))
                continue;
            if (!IncludedEventTypes.Contains(e.EventType))
                continue;
            list.Add(e);
        }
        return list;
    }

    public static string DisplayLine(string eventType, string detail, string? timestamp = null)
    {
        var label = eventType switch
        {
            "tool_confirmed" => "已确认",
            "tool_auto_relaxed" => "宽松自动允许",
            "tool_auto_fulltrust" => "完全信任自动允许",
            "tool_admin_auto" => "已自动执行",
            _ => eventType
        };
        var d = detail.Length > 100 ? detail[..97] + "…" : detail;
        var ts = string.IsNullOrEmpty(timestamp) ? "" : timestamp + "  ";
        return string.IsNullOrEmpty(d) ? $"{ts}{label}" : $"{ts}{label}  ·  {d}";
    }
}
