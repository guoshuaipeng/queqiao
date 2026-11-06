from pathlib import Path

# ---- TlsCertUtil: well-formed fp (64 hex after normalize) ----
p = Path('src-net48/RemoteAssist.Protocol/TlsCertUtil.cs')
t = p.read_text(encoding='utf-8')
if 'IsWellFormedFingerprint' not in t:
    needle = '''    public static bool FingerprintsEqual(string? a, string? b) =>
        string.Equals(NormalizeFingerprint(a), NormalizeFingerprint(b), StringComparison.Ordinal);
}'''
    insert = '''    public static bool FingerprintsEqual(string? a, string? b) =>
        string.Equals(NormalizeFingerprint(a), NormalizeFingerprint(b), StringComparison.Ordinal);

    /// <summary>SHA-256 指纹：规范化后须为 64 位十六进制（与 Sha256Fingerprint 输出一致）。</summary>
    public static bool IsWellFormedFingerprint(string? fingerprint)
    {
        var n = NormalizeFingerprint(fingerprint);
        if (n.Length != 64) return false;
        for (var i = 0; i < n.Length; i++)
        {
            var c = n[i];
            var hex = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F');
            if (!hex) return false;
        }
        return true;
    }
}'''
    if needle not in t:
        raise SystemExit('TlsCertUtil anchor missing')
    p.write_text(t.replace(needle, insert, 1), encoding='utf-8', newline='\n')
    print('TlsCertUtil OK')
else:
    print('TlsCertUtil already has IsWellFormedFingerprint')

# ---- FriendConnectGuide: secure share helpers + redact ----
fg = Path('src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs')
ft = fg.read_text(encoding='utf-8')
old_share = '''    /// <summary>R043 主路径：单行连接凭证 {会话码}-{验证码}。</summary>
    public static string FormatShareClipboard(string code, string password) =>
        FormatConnectionCredential(code, password);

    public static string FormatConnectionCredential(string code, string password) =>
        SessionCodeGenerator.Normalize(code) + "-" + SessionPasswordGenerator.Normalize(password);

    /// <summary>旧两行格式（兼容期仍可解析，不再作为新复制默认）。</summary>
    public static string FormatShareClipboardLegacy(string code, string password) =>
        "会话码：" + code + Environment.NewLine + "验证密码：" + password;
'''
new_share = '''    /// <summary>
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
                s, @"(?i)([?&]pwd=)[^&\\s]+", "$1***");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)([?&]fp=)[^&\\s]+", "$1***");
        }
        catch { /* ignore */ }
        if (s.Length > 160) s = s.Substring(0, 160) + "…";
        return s;
    }
'''
if old_share not in ft:
    if 'FormatShareClipboard(\n        string relayEndpoint' in ft or 'FormatShareClipboard(\r\n        string relayEndpoint' in ft:
        print('FriendConnectGuide share already updated')
    else:
        # try find and show
        i = ft.find('FormatShareClipboard')
        print(repr(ft[i:i+400]))
        raise SystemExit('share block mismatch')
else:
    ft = ft.replace(old_share, new_share, 1)
    fg.write_text(ft, encoding='utf-8', newline='\n')
    print('FriendConnectGuide OK')
