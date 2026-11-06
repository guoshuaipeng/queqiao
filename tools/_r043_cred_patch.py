# -*- coding: utf-8 -*-
"""R043: Protocol credential + AgentHost relay + AppShell/desktop wiring."""
from pathlib import Path

ROOT = Path('.')

# ---- SessionPasswordGenerator: 8-digit numeric ----
ROOT.joinpath('src-net48/RemoteAssist.Protocol/SessionPasswordGenerator.cs').write_text("""using System;
using System.Security.Cryptography;
using System.Text;

namespace RemoteAssist.Protocol;

/// <summary>验证码生成：新码固定 8 位纯数字（保留前导零）；Normalize 不去掉内部字符。</summary>
public static class SessionPasswordGenerator
{
    /// <summary>新生成默认长度（R043 连接凭证）。</summary>
    public const int DefaultLength = 8;

    public static string Generate(int length = DefaultLength)
    {
        if (length < 4) length = 4;
        var bytes = new byte[length];
        Net48Crypto.FillRandom(bytes);
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append((char)('0' + (bytes[i] % 10)));
        return sb.ToString();
    }

    /// <summary>仅 Trim；保留前导零与大小写（数字码无大小写问题）。</summary>
    public static string Normalize(string? password) =>
        string.IsNullOrWhiteSpace(password) ? "" : password.Trim();
}
""", encoding='utf-8', newline='\n')
print('SessionPasswordGenerator OK')

# ---- FriendConnectGuide: FormatShareClipboard + TryParse credential ----
fg = ROOT.joinpath('src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs')
t = fg.read_text(encoding='utf-8')
old_fmt = '''    public static string FormatShareClipboard(string code, string password) =>
        $"会话码：{code}{Environment.NewLine}验证密码：{password}";'''
new_fmt = '''    /// <summary>R043 主路径：单行连接凭证 {会话码}-{验证码}。</summary>
    public static string FormatShareClipboard(string code, string password) =>
        FormatConnectionCredential(code, password);

    public static string FormatConnectionCredential(string code, string password) =>
        SessionCodeGenerator.Normalize(code) + "-" + SessionPasswordGenerator.Normalize(password);

    /// <summary>旧两行格式（兼容期仍可解析，不再作为新复制默认）。</summary>
    public static string FormatShareClipboardLegacy(string code, string password) =>
        "会话码：" + code + Environment.NewLine + "验证密码：" + password;'''
if old_fmt not in t:
    raise SystemExit('FormatShareClipboard block missing')
t = t.replace(old_fmt, new_fmt, 1)

# Insert credential parse before labeled lines in TryParseShareText
needle = '''        string? foundCode = null;
        string? foundPwd = null;

        foreach (var rawLine in trimmed.Replace("\\r\\n", "\\n").Replace('\\r', '\\n').Split('\\n'))'''
# Use actual content
idx = t.find('string? foundCode = null;')
if idx < 0:
    raise SystemExit('foundCode missing')
# Find the foreach after foundPwd
idx2 = t.find('foreach (var rawLine in trimmed', idx)
insert = '''        // R043：单行 {会话码}-{验证码}（自右按最后一个 '-' 拆；右侧作验证码）
        if (!trimmed.Contains('\\n') && !trimmed.Contains(':') && !trimmed.Contains('：'))
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

        '''
if 'LastIndexOf' in t[idx:idx2]:
    print('credential parse already present')
else:
    t = t[:idx2] + insert + t[idx2:]
    print('credential parse inserted')

# Update incomplete single-code path message? keep
fg.write_text(t, encoding='utf-8', newline='\n')
print('FriendConnectGuide OK')

# ---- ConnectDraftParser incomplete credential message ----
cd = ROOT.joinpath('src-net48/RemoteAssist.Protocol/ConnectDraftParser.cs')
ct = cd.read_text(encoding='utf-8')
ct = ct.replace(
    'error = "还需要验证密码";',
    'error = "连接凭证不完整，请复制完整的「会话码-8位验证码」";')
ct = ct.replace(
    'error = "请粘贴对方发来的会话码和验证密码";',
    'error = "连接凭证不完整，请复制完整的「会话码-8位验证码」";')
cd.write_text(ct, encoding='utf-8', newline='\n')
print('ConnectDraftParser OK')

print('protocol credential done')
