from pathlib import Path
import re
p = Path("src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs")
t = p.read_text(encoding="utf-8")
# Replace entire RedactSecureMaterial method with clean version
pat = re.compile(
    r"    /// <summary>日志/异常/审计用：.*?</summary>\n"
    r"    public static string RedactSecureMaterial\(string\? text\)\n"
    r"    \{.*?\n    \}\n",
    re.S,
)
new = r'''    /// <summary>日志/异常/审计用：掩码完整 PairQrPayload、pwd/fp/code、短凭证与指纹，避免完整凭证回显。</summary>
    public static string RedactSecureMaterial(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var s = text;
        try
        {
            // 整段 remoteassist:pair… 载荷
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)remoteassist:pair\?\S+", "remoteassist:pair?[REDACTED]");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)([?&](?:pwd|fp|code)=)[^&\s]+", "$1***");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)\b(pwd|password|fp|fingerprint|code)\s*=\s*[^;\s,]+", "$1=***");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)(验证密码|密码|password|pwd|会话码|session\s*code|证书指纹|fingerprint)\s*[:：]\s*\S+", "$1：***");
            // 口述短码 {会话码}-{数字}
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"\b[A-Z2-9]{4,12}-\d{4,12}\b", "[REDACTED_CRED]");
            // 裸 64 hex 指纹
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"\b[0-9A-Fa-f]{64}\b", "[REDACTED_FP]");
        }
        catch { /* ignore */ }
        if (s.Length > 160) s = s.Substring(0, 160) + "…";
        return s;
    }
'''
m = pat.search(t)
if not m:
    raise SystemExit('method not found for replace')
t = pat.sub(new + "\n", t, count=1)
p.write_text(t, encoding='utf-8', newline='\n')
print('Redact rewrite OK')
# show lines
lines = p.read_text(encoding='utf-8').splitlines()
for i,l in enumerate(lines,1):
    if 'RedactSecureMaterial' in l or 'remoteassist:pair' in l and 'REDACTED' in l:
        print(f'{i}:{l}')
