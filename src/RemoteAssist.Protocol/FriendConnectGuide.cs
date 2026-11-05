using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace RemoteAssist.Protocol;

/// <summary>
/// 亲友向「连不上」引导与分享文案（R003）。两端同一套话术；主路径不出现端口号/放行入站。
/// </summary>
public enum FriendConnectFailKind
{
    None = 0,
    WrongPassword,
    StaleCode,
    RelayUnreachable,
    PeerNotAccepting,
    Other
}

public static class FriendConnectGuide
{
    public const string MomentAAllow =
        "请点「允许」，让鹊桥连网。弹窗里也许写着 RemoteAssist.Agent，就是本软件。系统设\u2060置一般不用改。";

    public const string MomentADenied =
        "刚才可能点了取消，对方将连不上。";

    public const string MomentAFirewallHint =
        "找到本软件（可能叫 RemoteAssist.Agent），在列表里勾选允许";

    public const string OpenFirewallButtonText = "打开防火墙设\u2060置";

    public const string StatusReadyToShare = "已连上网络，可以发给对方了";
    public const string StatusWaitingPeer = "等待对方连接";

    public const string FailWrongPassword =
        "验证密码不对。请对方再点一次「复制码+密码」发给你，不要只发会话码。";

    public const string FailStaleCode =
        "对方已刷新会话，旧码失效了。请对方重新复制并发给你。";

    public const string FailRelayUnreachable =
        "现在连不上网络。请双方检查网络后重试。";

    public const string FailPeerNotAccepting =
        "对方还没打开「接受协助」。请对方在首页选择「接受协助」，再把码和密码发给你。";

    public static string MessageFor(FriendConnectFailKind kind) => kind switch
    {
        FriendConnectFailKind.WrongPassword => FailWrongPassword,
        FriendConnectFailKind.StaleCode => FailStaleCode,
        FriendConnectFailKind.RelayUnreachable => FailRelayUnreachable,
        FriendConnectFailKind.PeerNotAccepting => FailPeerNotAccepting,
        _ => FailRelayUnreachable
    };

    public static FriendConnectFailKind Classify(Exception? ex)
    {
        if (ex is null) return FriendConnectFailKind.None;
        return Classify(ex.Message, ex);
    }

    public static FriendConnectFailKind Classify(string? error, Exception? ex = null)
    {
        var text = (error ?? "") + " " + (ex?.GetType().FullName ?? "") + " " + (ex?.InnerException?.Message ?? "");
        text = text.Trim();
        if (text.Length == 0)
            return FriendConnectFailKind.None;

        var lower = text.ToLowerInvariant();

        // 先匹配中继语义错误，再判网络（避免 "wait timeout" 被 timed out 启发式误伤）
        if (lower.Contains("guest session expired", StringComparison.Ordinal) ||
            lower.Contains("session expired", StringComparison.Ordinal) ||
            lower.Contains("旧码", StringComparison.Ordinal) ||
            lower.Contains("已刷新", StringComparison.Ordinal))
            return FriendConnectFailKind.StaleCode;

        if (lower.Contains("peer not online", StringComparison.Ordinal) ||
            lower.Contains("no host", StringComparison.Ordinal) ||
            lower.Contains("agent not", StringComparison.Ordinal) ||
            lower.Contains("wait timeout", StringComparison.Ordinal))
            return FriendConnectFailKind.PeerNotAccepting;

        if (IsNetworkFailure(ex, lower))
            return FriendConnectFailKind.RelayUnreachable;

        if (lower.Contains("invalid code or password", StringComparison.Ordinal) ||
            lower.Contains("wrong password", StringComparison.Ordinal) ||
            lower.Contains("bad password", StringComparison.Ordinal) ||
            lower.Contains("password", StringComparison.Ordinal) && lower.Contains("invalid", StringComparison.Ordinal))
            return FriendConnectFailKind.WrongPassword;

        if (lower.Contains("invalid code", StringComparison.Ordinal) && !lower.Contains("password", StringComparison.Ordinal))
            return FriendConnectFailKind.StaleCode;

        if (lower.Contains("pairing failed", StringComparison.Ordinal) ||
            lower.Contains("会话码错误", StringComparison.Ordinal))
            return FriendConnectFailKind.StaleCode;

        return FriendConnectFailKind.Other;
    }

    public static string FriendlyOrRaw(Exception ex)
    {
        var kind = Classify(ex);
        if (kind is FriendConnectFailKind.None or FriendConnectFailKind.Other)
        {
            // 其它错误仍给可执行兜底，避免露出端口/入站术语
            var raw = ex.Message ?? "";
            if (LooksTechnicalLeak(raw))
                return FailRelayUnreachable;
            return string.IsNullOrWhiteSpace(raw) ? FailRelayUnreachable : raw;
        }
        return MessageFor(kind);
    }

    public static bool LooksTechnicalLeak(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.Contains("17890", StringComparison.Ordinal) ||
               text.Contains("17891", StringComparison.Ordinal) ||
               text.Contains("放行入站", StringComparison.Ordinal) ||
               text.Contains("121.196.45.81", StringComparison.Ordinal) ||
               text.Contains("relay_guest_", StringComparison.Ordinal) ||
               text.Contains("--insecure", StringComparison.Ordinal);
    }

    public static string FormatShareClipboard(string code, string password) =>
        $"会话码：{code}{Environment.NewLine}验证密码：{password}";

    /// <summary>
    /// 解析 R001「会话码：… / 验证密码：…」两行分享文本（也可接受同段粘贴）。
    /// </summary>
    public static bool TryParseShareText(string? text, out string code, out string? password)
    {
        code = "";
        password = null;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        // 载荷优先：交给上层 PairQrPayload
        if (trimmed.StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string? foundCode = null;
        string? foundPwd = null;

        foreach (var rawLine in trimmed.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var codeMatch = Regex.Match(line, @"^(?:会话码|session\s*code|code)\s*[:：]\s*(.+)$", RegexOptions.IgnoreCase);
            if (codeMatch.Success)
            {
                foundCode = SessionCodeGenerator.Normalize(codeMatch.Groups[1].Value);
                continue;
            }

            var pwdMatch = Regex.Match(line, @"^(?:验证密码|密码|password|pwd)\s*[:：]\s*(.+)$", RegexOptions.IgnoreCase);
            if (pwdMatch.Success)
            {
                foundPwd = SessionPasswordGenerator.Normalize(pwdMatch.Groups[1].Value);
                continue;
            }
        }

        if (foundCode is null && foundPwd is null)
        {
            // 单行纯会话码
            if (!trimmed.Contains('\n') && !trimmed.Contains(':') && !trimmed.Contains('：'))
            {
                var only = SessionCodeGenerator.Normalize(trimmed);
                if (only.Length >= 4)
                {
                    code = only;
                    return true;
                }
            }
            return false;
        }

        if (foundCode is null || foundCode.Length == 0)
            return false;

        code = foundCode;
        password = foundPwd;
        return true;
    }

    public static string FriendConnectionPreview(string code) =>
        string.IsNullOrWhiteSpace(code)
            ? "将通过网络连接"
            : $"将通过网络连接 · 码 {SessionCodeGenerator.Normalize(code)}";

    public static void OpenWindowsFirewallSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:windowsdefender-firewall",
                UseShellExecute = true
            });
            return;
        }
        catch { /* fall through */ }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "firewall.cpl",
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }

    private static bool IsNetworkFailure(Exception? ex, string lower)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is SocketException or TimeoutException or (IOException and not EndOfStreamException))
                return true;
            if (cur is System.Net.Http.HttpRequestException)
                return true;
        }

        return lower.Contains("actively refused", StringComparison.Ordinal) ||
               lower.Contains("connection refused", StringComparison.Ordinal) ||
               lower.Contains("无法连接", StringComparison.Ordinal) ||
               lower.Contains("timed out", StringComparison.Ordinal) ||
               lower.Contains("timeout", StringComparison.Ordinal) ||
               lower.Contains("no such host", StringComparison.Ordinal) ||
               lower.Contains("name or service not known", StringComparison.Ordinal) ||
               lower.Contains("network is unreachable", StringComparison.Ordinal) ||
               lower.Contains("host is down", StringComparison.Ordinal) ||
               lower.Contains("could not connect", StringComparison.Ordinal) ||
               lower.Contains("connection reset", StringComparison.Ordinal);
    }
}