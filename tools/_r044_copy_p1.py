# -*- coding: utf-8 -*-
"""R044 net48 friend-facing copy only. No logic/protocol changes."""
from pathlib import Path

ROOT = Path("src-net48")

# ---- FriendConnectGuide: authoritative string constants ----
p = ROOT / "RemoteAssist.Protocol/FriendConnectGuide.cs"
t = p.read_text(encoding="utf-8")
old = '''    public const string NeedSecurePayloadHint =
        "请粘贴对方「复制连接凭证」得到的完整安全载荷（remoteassist:pair…，含指纹），不要只粘贴口述短码。";

    public const string NeedFingerprintHint =
        "还缺少安全校验（证书指纹）。下一步：请对方重新点「复制连接凭证」发来完整载荷；或展开「高级」填写 TLS 指纹。";
'''
new = '''    // ---- R044 亲友逐字文案（docs/round-044.md / quality-connection-credential.md）----
    public const string PasteCredentialLabel = "粘贴完整连接凭证";
    public const string PasteCredentialHint =
        "把对方通过「复制连接凭证」发来的整段粘贴到这里。";
    public const string ConnectButtonText = "连接";
    public const string CopyCredentialButtonText = "复制连接凭证";
    public const string CopyCredentialSucceededHint = "连接凭证已复制，请完整发给对方。";
    public const string CopyCredentialFailedHint =
        "未能复制，请选中完整连接凭证，手动复制后发给对方。";
    public const string MissingSecurityInfoHint =
        "连接凭证缺少安全验证信息，暂时无法连接。请让对方点击「复制连接凭证」，重新发送完整内容。";
    public const string AcceptGuideHint =
        "点「复制连接凭证」，把复制出来的整段发给要帮你的人。";
    public const string OralShortCodeHint = "连接时请发送完整连接凭证";

    /// <summary>缺安全验证信息阻断（短码/旧两行/缺 fp）；与 MissingSecurityInfoHint 同文。</summary>
    public const string NeedSecurePayloadHint = MissingSecurityInfoHint;

    /// <summary>缺安全验证信息阻断（同 MissingSecurityInfoHint；高级填验证信息仍走同一阻断文案）。</summary>
    public const string NeedFingerprintHint = MissingSecurityInfoHint;
'''
if old not in t:
    raise SystemExit("FriendConnectGuide hints block missing")
p.write_text(t.replace(old, new), encoding="utf-8", newline="\n")
print("FriendConnectGuide OK")

# ---- ConnectDraftParser tls=0 friend message ----
p = ROOT / "RemoteAssist.Protocol/ConnectDraftParser.cs"
t = p.read_text(encoding="utf-8")
old = 'error = "载荷标明非加密通道。公网请让对方重新「复制连接凭证」（加密载荷）；本机调试请勾选「明文调试」。";'
new = 'error = "该连接凭证未按加密方式发送。请让对方重新「复制连接凭证」；本机调试请勾选「明文调试」。";'
if old not in t:
    raise SystemExit("ConnectDraftParser tls0 msg missing")
p.write_text(t.replace(old, new), encoding="utf-8", newline="\n")
print("ConnectDraftParser OK")

# ---- AssistClient ----
p = ROOT / "RemoteAssist.Protocol/AssistClient.cs"
t = p.read_text(encoding="utf-8")
repls = [
(
'''            throw new InvalidOperationException(
                "局域网 TLS 需要证书指纹：请展开「高级」填写，或勾选明文调试。");''',
'''            throw new InvalidOperationException(
                FriendConnectGuide.MissingSecurityInfoHint);'''
),
(
'''                    throw new InvalidOperationException(
                        "加密连接需要证书指纹：请粘贴含 fp= 的完整载荷，或展开「高级」填写 TLS 指纹（短连接凭证不含指纹）。");''',
'''                    throw new InvalidOperationException(
                        FriendConnectGuide.MissingSecurityInfoHint);'''
),
]
for a,b in repls:
    if a not in t:
        raise SystemExit("AssistClient block missing:\\n"+a[:80])
    t = t.replace(a,b)
# ensure using / FriendConnectGuide in same namespace Protocol - OK same assembly
p.write_text(t, encoding="utf-8", newline="\n")
print("AssistClient OK")

print("phase1 done")
