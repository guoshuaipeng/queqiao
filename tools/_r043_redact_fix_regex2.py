from pathlib import Path
p = Path("src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs")
lines = p.read_text(encoding="utf-8").splitlines(keepends=True)
# find method start/end by markers
start = None
end = None
for i, l in enumerate(lines):
    if "public static string RedactSecureMaterial" in l:
        # include preceding summary comments
        start = i
        while start > 0 and lines[start-1].strip().startswith("///"):
            start -= 1
        break
if start is None:
    raise SystemExit("start not found")
depth = 0
seen = False
for j in range(start, len(lines)):
    if "{" in lines[j]:
        depth += lines[j].count("{")
        seen = True
    if "}" in lines[j]:
        depth -= lines[j].count("}")
    if seen and depth == 0:
        end = j + 1
        break
if end is None:
    raise SystemExit("end not found")

new_method = '''    /// <summary>日志/异常/审计用：掩码完整 PairQrPayload、pwd/fp/code、短凭证与指纹，避免完整凭证回显。</summary>
    public static string RedactSecureMaterial(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var s = text;
        try
        {
            // 整段 remoteassist:pair… 载荷
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)remoteassist:pair\\?\\S+", "remoteassist:pair?[REDACTED]");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)([?&](?:pwd|fp|code)=)[^&\\s]+", "$1***");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)\\b(pwd|password|fp|fingerprint|code)\\s*=\\s*[^;\\s,]+", "$1=***");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"(?i)(验证密码|密码|password|pwd|会话码|session\\s*code|证书指纹|fingerprint)\\s*[:：]\\s*\\S+", "$1：***");
            // 口述短码 {会话码}-{数字}
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"\\b[A-Z2-9]{4,12}-\\d{4,12}\\b", "[REDACTED_CRED]");
            // 裸 64 hex 指纹
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @"\\b[0-9A-Fa-f]{64}\\b", "[REDACTED_FP]");
        }
        catch { /* ignore */ }
        if (s.Length > 160) s = s.Substring(0, 160) + "…";
        return s;
    }
'''
# Wait - in a normal Python string written to C# file, we want C# verbatim @"..." with single backslashes for regex.
# So the file content should contain: @"(?i)remoteassist:pair\?\S+"
# In Python normal string that is: '@"(?i)remoteassist:pair\\?\\S+"'  which writes \?\S+  GOOD for C# verbatim.

new_method = """    /// <summary>日志/异常/审计用：掩码完整 PairQrPayload、pwd/fp/code、短凭证与指纹，避免完整凭证回显。</summary>
    public static string RedactSecureMaterial(string? text)
    {
        if (string.IsNullOrEmpty(text)) return \"\";
        var s = text;
        try
        {
            // 整段 remoteassist:pair… 载荷
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @\"(?i)remoteassist:pair\\?\\S+\", \"remoteassist:pair?[REDACTED]\");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @\"(?i)([?&](?:pwd|fp|code)=)[^&\\s]+\", \"$1***\");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @\"(?i)\\b(pwd|password|fp|fingerprint|code)\\s*=\\s*[^;\\s,]+\", \"$1=***\");
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @\"(?i)(验证密码|密码|password|pwd|会话码|session\\s*code|证书指纹|fingerprint)\\s*[:：]\\s*\\S+\", \"$1：***\");
            // 口述短码 {会话码}-{数字}
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @\"\\b[A-Z2-9]{4,12}-\\d{4,12}\\b\", \"[REDACTED_CRED]\");
            // 裸 64 hex 指纹
            s = System.Text.RegularExpressions.Regex.Replace(
                s, @\"\\b[0-9A-Fa-f]{64}\\b\", \"[REDACTED_FP]\");
        }
        catch { /* ignore */ }
        if (s.Length > 160) s = s.Substring(0, 160) + \"…\";
        return s;
    }
"""

# Actually using escaped quotes in """ is messy. Use list of lines without escapes confusion.
nm = []
nm.append('    /// <summary>日志/异常/审计用：掩码完整 PairQrPayload、pwd/fp/code、短凭证与指纹，避免完整凭证回显。</summary>\n')
nm.append('    public static string RedactSecureMaterial(string? text)\n')
nm.append('    {\n')
nm.append('        if (string.IsNullOrEmpty(text)) return "";\n')
nm.append('        var s = text;\n')
nm.append('        try\n')
nm.append('        {\n')
nm.append('            // 整段 remoteassist:pair… 载荷\n')
nm.append('            s = System.Text.RegularExpressions.Regex.Replace(\n')
nm.append('                s, @"(?i)remoteassist:pair\\?\\S+", "remoteassist:pair?[REDACTED]");\n')
nm.append('            s = System.Text.RegularExpressions.Regex.Replace(\n')
nm.append('                s, @"(?i)([?&](?:pwd|fp|code)=)[^&\\s]+", "$1***");\n')
nm.append('            s = System.Text.RegularExpressions.Regex.Replace(\n')
nm.append('                s, @"(?i)\\b(pwd|password|fp|fingerprint|code)\\s*=\\s*[^;\\s,]+", "$1=***");\n')
nm.append('            s = System.Text.RegularExpressions.Regex.Replace(\n')
nm.append('                s, @"(?i)(验证密码|密码|password|pwd|会话码|session\\s*code|证书指纹|fingerprint)\\s*[:：]\\s*\\S+", "$1：***");\n')
nm.append('            // 口述短码 {会话码}-{数字}\n')
nm.append('            s = System.Text.RegularExpressions.Regex.Replace(\n')
nm.append('                s, @"\\b[A-Z2-9]{4,12}-\\d{4,12}\\b", "[REDACTED_CRED]");\n')
nm.append('            // 裸 64 hex 指纹\n')
nm.append('            s = System.Text.RegularExpressions.Regex.Replace(\n')
nm.append('                s, @"\\b[0-9A-Fa-f]{64}\\b", "[REDACTED_FP]");\n')
nm.append('        }\n')
nm.append('        catch { /* ignore */ }\n')
nm.append('        if (s.Length > 160) s = s.Substring(0, 160) + "…";\n')
nm.append('        return s;\n')
nm.append('    }\n')

out = lines[:start] + nm + lines[end:]
p.write_text(''.join(out), encoding='utf-8', newline='\n')
print(f'replaced lines {start+1}-{end}')
# print the method
for i,l in enumerate(p.read_text(encoding='utf-8').splitlines()[start:start+30], start+1):
    print(f'{i}|{l}')
