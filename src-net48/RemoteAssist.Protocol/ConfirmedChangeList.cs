using System;
using System.Collections.Generic;

namespace RemoteAssist.Protocol;

/// <summary>
/// 从审计行筛「本次协助已确认的改动」：已确认/自动允许的写操作。
/// 未确认、已拒绝不得出现。展示「已确认」≠ 执行成功（执行另见 tool_ok/tool_error）。
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

    public static string HumanizeDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return "";

        var d = detail.Trim();
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
                        d = d.Substring(semi + 1).TrimStart();
                        cut = 0;
                    }
                    break;
                }
            }
            if (cut < 0) break;
        }

        if (d.StartsWith("RunCommand", StringComparison.OrdinalIgnoreCase))
        {
            var semi = d.IndexOf(';');
            d = semi >= 0 ? d.Substring(semi + 1).TrimStart() : "";
        }

        return d.Trim().TrimStart('·', ' ', '\t');
    }

    public static string DisplayLine(string eventType, string detail, string? timestamp = null)
    {
        // 批准≠执行成功：此处标签为确认/自动允许，不写「执行成功」
        var label = eventType switch
        {
            "tool_confirmed" => "已确认",
            "tool_auto_relaxed" => "宽松自动允许",
            "tool_auto_fulltrust" => "完全信任自动允许",
            "tool_admin_auto" => "已自动执行",
            _ => eventType
        };
        var d = HumanizeDetail(detail);
        if (d.Length > 90) d = d.Substring(0, 87) + "…";
        var ts = string.IsNullOrEmpty(timestamp) ? "" : timestamp + "  ";
        return string.IsNullOrEmpty(d) ? ts + label : ts + label + "  ·  " + d;
    }
}