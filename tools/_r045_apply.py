from pathlib import Path
import re, hashlib

# 1) FriendConnectGuide fail strings
p = Path("src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs")
t = p.read_text(encoding="utf-8")
repls = [
(
'''    public const string FailWrongPassword =
        "验证密码不对。请对方再点一次「复制码+密码」发给你，不要只发会话码。";''',
'''    public const string FailWrongPassword =
        "验证密码不对。请对方再点一次「复制连接凭证」，把完整内容发给你。";'''
),
(
'''    public const string FailStaleCode =
        "对方已刷新会话，旧码失效了。请对方重新复制并发给你。";''',
'''    public const string FailStaleCode =
        "对方已刷新会话，旧连接凭证失效了。请对方重新「复制连接凭证」发给你。";'''
),
(
'''    public const string FailPeerNotAccepting =
        "对方还没打开「接受协助」。请对方在首页选择「接受协助」，再把码和密码发给你。";''',
'''    public const string FailPeerNotAccepting =
        "对方还没打开「接受协助」。请对方在首页选择「接受协助」，再把完整连接凭证发给你。";'''
),
]
for a,b in repls:
    if a not in t:
        raise SystemExit("missing block:\n"+a[:80])
    t = t.replace(a,b)
p.write_text(t, encoding="utf-8", newline="\n")
print("FriendConnectGuide OK")

# 2) Helper catch — remove unconditional Missing append; keep Redact
p = Path("src-net48/RemoteAssist.Helper.UI/MainForm.cs")
t = p.read_text(encoding="utf-8")
old = 'SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly) + "\\r\\n" + FriendConnectGuide.MissingSecurityInfoHint);'
new = 'SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly));'
if old not in t:
    # try actual CRLF content as single line from file
    if 'MissingSecurityInfoHint);' not in t or 'RedactSecureMaterial(friendly)' not in t:
        raise SystemExit("catch pattern not found")
    # more flexible
    t2, n = re.subn(
        r'SetFailHint\(FriendConnectGuide\.RedactSecureMaterial\(friendly\)\s*\+\s*"\\r\\n"\s*\+\s*FriendConnectGuide\.MissingSecurityInfoHint\);',
        'SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly));',
        t,
        count=1,
    )
    if n != 1:
        # show context
        i = t.find('RedactSecureMaterial(friendly)')
        print(repr(t[i:i+120]))
        raise SystemExit(f"regex replace failed n={n}")
    t = t2
else:
    t = t.replace(old, new)
p.write_text(t, encoding="utf-8", newline="\n")
print("Helper catch OK")

# verify whitelist still present
ht = Path("src-net48/RemoteAssist.Helper.UI/MainForm.cs").read_text(encoding="utf-8")
assert "MissingSecurityInfoHint" in ht  # preflight still
assert ht.count("MissingSecurityInfoHint") >= 1
# catch should not append anymore — only preflight block should reference it near SetFailHint with Missing
assert '+ FriendConnectGuide.MissingSecurityInfoHint' not in ht
assert '+"\\r\\n" + FriendConnectGuide.MissingSecurityInfoHint' not in ht
print("catch no longer appends Missing; preflight still uses it:", "MissingSecurityInfoHint" in ht)

print("code phase done")