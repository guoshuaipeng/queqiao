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

    /// <summary>
    /// 去掉 tool=/source=/via=/result=/at= 等内部字段，留下 WriteConfirmCopy 人话摘要。
    /// </summary>
    public static string HumanizeDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return "";

        var d = detail.Trim();
        // 反复剥掉前导 technical key=value;
        while (true)
        {
            var cut = -1;
            foreach (var key in new[] { "tool=", "source=", "via=", "result=", "at=" })
            {
                if (d.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    var semi = d.IndexOf(';');
                    if (semi < 0)
                    {
                        d = "";
                        cut = 0;
                    }
                    else
                    {
                        d = d[(semi + 1)..].TrimStart();
                        cut = 0;
                    }
                    break;
                }
            }
            if (cut < 0) break;
        }

        // 若仍以 RunCommand 等内部名开头，尽量丢掉
        if (d.StartsWith("RunCommand", StringComparison.OrdinalIgnoreCase))
        {
            var semi = d.IndexOf(';');
            d = semi >= 0 ? d[(semi + 1)..].TrimStart() : "";
        }

        return d.Trim().TrimStart('·', ' ', '	');
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
        var d = HumanizeDetail(detail);
        if (d.Length > 90) d = d[..87] + "…";
        var ts = string.IsNullOrEmpty(timestamp) ? "" : timestamp + "  ";
        return string.IsNullOrEmpty(d) ? $"{ts}{label}" : $"{ts}{label}  ·  {d}";
    }
}
