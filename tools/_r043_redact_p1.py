# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path("src-net48")

# --- 1) Strengthen RedactSecureMaterial + FriendlyOrRaw ---
p = ROOT / "RemoteAssist.Protocol/FriendConnectGuide.cs"
t = p.read_text(encoding="utf-8")
old_redact = '''    /// <summary>日志/异常展示用：掩码载荷中的 pwd 与 fp，避免完整凭证回显。</summary>
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
    }'''
new_redact = '''    /// <summary>日志/异常/审计用：掩码完整 PairQrPayload、pwd/fp/code、短凭证与指纹，避免完整凭证回显。</summary>
    public static string RedactSecureMaterial(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var s = text;
        try
        {
            // 整段 remoteassist:pair… 载荷
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)remoteassist:pair\\?[^\\s\\\"\\'\\]\\)\\>]+", "remoteassist:pair?[REDACTED]");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)([?&](?:pwd|fp|code)=)[^&\\s\\\"\\']+", "$1***");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)\\b(?:pwd|password|fp|fingerprint|code)\\s*[=:]\\s*[^;\\s,\\\"\\']+", m =>
                {
                    var raw = m.Value;
                    var idx = raw.IndexOfAny(new[] { '=', ':' });
                    return idx >= 0 ? raw.Substring(0, idx + 1) + "***" : "***";
                });
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)(验证密码|密码|password|pwd|会话码|session\\s*code|证书指纹|fingerprint)\\s*[:：]\\s*\\S+", "$1：***");
            // 口述短码 {会话码}-{8位}
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"\\b[A-Z2-9]{4,12}-\\d{4,12}\\b", "[REDACTED_CRED]");
            // 裸 64 hex 指纹
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"\\b[0-9A-Fa-f]{64}\\b", "[REDACTED_FP]");
        }
        catch { /* ignore */ }
        if (s.Length > 160) s = s.Substring(0, 160) + "…";
        return s;
    }'''
if old_redact not in t:
    raise SystemExit('RedactSecureMaterial block not found')
t = t.replace(old_redact, new_redact)

old_friendly = '''            var raw = ex.Message ?? "";
            if (LooksTechnicalLeak(raw))
                return FailRelayUnreachable;
            return string.IsNullOrWhiteSpace(raw) ? FailRelayUnreachable : raw;'''
new_friendly = '''            var raw = RedactSecureMaterial(ex.Message ?? "");
            if (LooksTechnicalLeak(raw))
                return FailRelayUnreachable;
            return string.IsNullOrWhiteSpace(raw) ? FailRelayUnreachable : raw;'''
if old_friendly not in t:
    raise SystemExit('FriendlyOrRaw raw block not found')
t = t.replace(old_friendly, new_friendly)
p.write_text(t, encoding="utf-8", newline="\n")
print("FriendConnectGuide OK")

# --- 2) SessionPasswordGenerator rejection sampling ---
p = ROOT / "RemoteAssist.Protocol/SessionPasswordGenerator.cs"
t = p.read_text(encoding="utf-8")
old_gen = '''    public static string Generate(int length = DefaultLength)
    {
        if (length < 4) length = 4;
        var bytes = new byte[length];
        Net48Crypto.FillRandom(bytes);
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append((char)('0' + (bytes[i] % 10)));
        return sb.ToString();
    }'''
new_gen = '''    public static string Generate(int length = DefaultLength)
    {
        if (length < 4) length = 4;
        var sb = new StringBuilder(length);
        var one = new byte[1];
        for (var i = 0; i < length; i++)
        {
            // 拒绝采样：丢弃 250..255，避免 byte%10 模偏
            byte b;
            do
            {
                Net48Crypto.FillRandom(one);
                b = one[0];
            } while (b >= 250);
            sb.Append((char)('0' + (b % 10)));
        }
        return sb.ToString();
    }'''
if old_gen not in t:
    raise SystemExit('SessionPasswordGenerator.Generate not found')
p.write_text(t.replace(old_gen, new_gen), encoding="utf-8", newline="\n")
print("SessionPasswordGenerator OK")

# --- 3) SessionCodeGenerator rejection sampling (same class of bug) ---
p = ROOT / "RemoteAssist.Protocol/SessionCodeGenerator.cs"
t = p.read_text(encoding="utf-8")
old_sc = '''        var chars = new char[length];
        var bytes = new byte[length];
        Net48Crypto.FillRandom(bytes);
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        return new string(chars);'''
new_sc = '''        var chars = new char[length];
        var one = new byte[1];
        var mod = Alphabet.Length;
        // 拒绝采样：仅用 < (256 - 256%mod) 的字节，避免 mod 偏
        var limit = 256 - (256 % mod);
        for (var i = 0; i < length; i++)
        {
            byte b;
            do
            {
                Net48Crypto.FillRandom(one);
                b = one[0];
            } while (b >= limit);
            chars[i] = Alphabet[b % mod];
        }
        return new string(chars);'''
if old_sc not in t:
    raise SystemExit('SessionCodeGenerator body not found')
p.write_text(t.replace(old_sc, new_sc), encoding="utf-8", newline="\n")
print("SessionCodeGenerator OK")

# --- 4) AuditLog redact on write ---
p = ROOT / "RemoteAssist.Agent/Services.cs"
t = p.read_text(encoding="utf-8")
if "RedactSecureMaterial" not in t:
    # ensure using RemoteAssist.Protocol exists
    if "using RemoteAssist.Protocol;" not in t:
        t = t.replace("namespace RemoteAssist.Agent;", "using RemoteAssist.Protocol;\n\nnamespace RemoteAssist.Agent;")
old_write = '''    public void Write(string eventType, string detail)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}\\t{eventType}\\t{detail}";
        lock (_lock)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
        }
        Console.WriteLine($"[AUDIT] {line}");
        try { EntryWritten?.Invoke(); } catch { /* ignore UI errors */ }
    }'''
new_write = '''    public void Write(string eventType, string detail)
    {
        // 审计/控制台均不得落完整凭证（载荷、密码、指纹、会话码）
        var safeDetail = FriendConnectGuide.RedactSecureMaterial(detail ?? "");
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}\\t{eventType}\\t{safeDetail}";
        lock (_lock)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
        }
        Console.WriteLine($"[AUDIT] {line}");
        try { EntryWritten?.Invoke(); } catch { /* ignore UI errors */ }
    }'''
if old_write not in t:
    raise SystemExit('AuditLog.Write not found')
p.write_text(t.replace(old_write, new_write), encoding="utf-8", newline="\n")
print("AuditLog OK")

print("phase1 done")
