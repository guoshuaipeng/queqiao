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
        "请点「允许」，让鹊桥连网。弹窗里也许写着 RemoteAssist.Agent，就是本软件。\n系统设置一般不用改。";

    public const string MomentADenied =
        "刚才可能点了取消，对方将连不上。";

    public const string MomentAFirewallHint =
        "找到本软件（可能叫 RemoteAssist.Agent），在列表里勾选允许";

    public const string OpenFirewallButtonText = "打开防火墙设置";

    public const string RefreshSessionConfirm =
        "刷新后旧码会失效，已经发出去的码对方将连不上。";

    public const string RefreshSessionConfirmOk = "刷新会话码";
    public const string RefreshSessionConfirmCancel = "取消";

    public const string StatusReadyToShare = "已连上网络，可以发给对方了";
    public const string StatusWaitingPeer = "等待对方连接";

    /// <summary>协助方已真正连上本机（不是只连上中继还在等人）。</summary>
    public const string StatusPeerConnected = "对方已连上。改动这台电脑前会先问你。";

    /// <summary>已连接协助方列表：没有称呼时的人话回退。</summary>
    public const string PairedClientFallbackName = "协助方";

    /// <summary>已连接协助方列表空态。</summary>
    public const string PairedClientsEmpty = "（暂无）";

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
        if (Net48String.Contains(lower, "guest session expired", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "session expired", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "旧码", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "已刷新", StringComparison.Ordinal))
            return FriendConnectFailKind.StaleCode;

        if (Net48String.Contains(lower, "peer not online", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "no host", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "agent not", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "wait timeout", StringComparison.Ordinal))
            return FriendConnectFailKind.PeerNotAccepting;

        if (IsNetworkFailure(ex, lower))
            return FriendConnectFailKind.RelayUnreachable;

        if (Net48String.Contains(lower, "invalid code or password", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "wrong password", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "bad password", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "password", StringComparison.Ordinal) && Net48String.Contains(lower, "invalid", StringComparison.Ordinal))
            return FriendConnectFailKind.WrongPassword;

        if (Net48String.Contains(lower, "invalid code", StringComparison.Ordinal) && !Net48String.Contains(lower, "password", StringComparison.Ordinal))
            return FriendConnectFailKind.StaleCode;

        if (Net48String.Contains(lower, "pairing failed", StringComparison.Ordinal) ||
            Net48String.Contains(lower, "会话码错误", StringComparison.Ordinal))
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
        return Net48String.Contains(text, "17890", StringComparison.Ordinal) ||
               Net48String.Contains(text, "17891", StringComparison.Ordinal) ||
               Net48String.Contains(text, "放行入站", StringComparison.Ordinal) ||
               Net48String.Contains(text, "121.196.45.81", StringComparison.Ordinal) ||
               Net48String.Contains(text, "relay_guest_", StringComparison.Ordinal) ||
               Net48String.Contains(text, "--insecure", StringComparison.Ordinal);
    }

    /// <summary>
    /// 已连接协助方列表显示名：有称呼用称呼；空/默认 helper 写「协助方」；账号免密前缀可读。
    /// </summary>
    public static string FormatPairedClientLabel(string? clientName, bool ownerLink = false)
    {
        var name = string.IsNullOrWhiteSpace(clientName) ? PairedClientFallbackName : clientName.Trim();
        if (string.Equals(name, "helper", StringComparison.OrdinalIgnoreCase))
            name = PairedClientFallbackName;
        return ownerLink ? "账号免密 · " + name : name;
    }

    /// <summary>
    /// R043 主路径「复制连接凭证」：完整安全载荷（PairQrPayload.BuildRelay，含业务 fp）。
    /// 短串 FormatConnectionCredential 仅供口述展示，不可作为默认可连主路径。
    /// </summary>
    public static string FormatShareClipboard(
        string relayEndpoint,
        string code,
        string password,
        string fingerprint) =>
        PairQrPayload.BuildRelay(relayEndpoint, code, password, fingerprint, useTls: true);

    /// <summary>口述用短形态 {会话码}-{验证码}（不含指纹，不能单独加密连接）。</summary>
    public static string FormatConnectionCredential(string code, string password) =>
        SessionCodeGenerator.Normalize(code) + "-" + SessionPasswordGenerator.Normalize(password);

    /// <summary>旧两行格式（兼容期）；无 fp，协助端须另有指纹或改用完整载荷）。</summary>
    public static string FormatShareClipboardLegacy(string code, string password) =>
        "会话码：" + code + Environment.NewLine + "验证密码：" + password;

    public const string NeedSecurePayloadHint =
        "请粘贴对方「复制连接凭证」得到的完整安全载荷（remoteassist:pair…，含指纹），不要只粘贴口述短码。";

    public const string NeedFingerprintHint =
        "还缺少安全校验（证书指纹）。下一步：请对方重新点「复制连接凭证」发来完整载荷；或展开「高级」填写 TLS 指纹。";

    /// <summary>日志/异常展示用：掩码载荷中的 pwd 与 fp，避免完整凭证回显。</summary>
    public static string RedactSecureMaterial(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var s = text;
        try
        {
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)([?&]pwd=)[^&\s]+", "$1***");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)([?&]fp=)[^&\s]+", "$1***");
        }
        catch { /* ignore */ }
        if (s.Length > 160) s = s.Substring(0, 160) + "…";
        return s;
    }

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

                // R043：单行 {会话码}-{验证码}（自右按最后一个 '-' 拆；右侧作验证码）
        if (!trimmed.Contains('\n') && !trimmed.Contains(':') && !trimmed.Contains('：'))
        {
            var dash = trimmed.LastIndexOf('-');
            if (dash > 0 && dash < trimmed.Length - 1)
            {
                var left = SessionCodeGenerator.Normalize(trimmed.Substring(0, dash));
                var right = SessionPasswordGenerator.Normalize(trimmed.Substring(dash + 1));
                if (left.Length >= 4 && right.Length >= 4)
                {
                    code = left;
                    password = right;
                    return true;
                }
            }
        }

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
            if (cur.GetType().FullName == "System.Net.Http.HttpRequestException")
                return true;
        }

        return Net48String.Contains(lower, "actively refused", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "connection refused", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "无法连接", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "timed out", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "timeout", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "no such host", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "name or service not known", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "network is unreachable", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "host is down", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "could not connect", StringComparison.Ordinal) ||
               Net48String.Contains(lower, "connection reset", StringComparison.Ordinal);
    }
}