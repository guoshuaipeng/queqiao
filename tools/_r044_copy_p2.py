# -*- coding: utf-8 -*-
from pathlib import Path
ROOT = Path("src-net48")

# ---- Helper.UI MainForm ----
p = ROOT / "RemoteAssist.Helper.UI/MainForm.cs"
t = p.read_text(encoding="utf-8")
repls = [
(
'Text = "粘贴对方「复制连接凭证」的完整安全载荷，点「连接」",',
'Text = FriendConnectGuide.PasteCredentialHint,',
),
(
'''            Text = "完整安全载荷（remoteassist:pair…，含指纹；不要只贴口述短码）",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)''',
'''            Text = FriendConnectGuide.PasteCredentialLabel,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)'''
),
(
'Text = "请粘贴完整安全载荷后连接"',
'Text = FriendConnectGuide.PasteCredentialHint'
),
(
'_previewLabel.Text = string.IsNullOrEmpty(err) ? "请粘贴完整安全载荷后连接" : err;',
'_previewLabel.Text = string.IsNullOrEmpty(err) ? FriendConnectGuide.PasteCredentialHint : err;',
),
(
'''                SetStatus("无法安全连接");
                SetFailHint(
                    FriendConnectGuide.NeedFingerprintHint
                    + Environment.NewLine
                    + "（仅本机调试可勾选「明文调试」，公网请勿使用。）");
                return;''',
'''                SetStatus("无法连接");
                SetFailHint(
                    FriendConnectGuide.MissingSecurityInfoHint
                    + Environment.NewLine
                    + "（仅本机调试可勾选「明文调试」，公网请勿使用。）");
                return;'''
),
(
'SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly) + "\\r\\n下一步：请对方重新「复制连接凭证」发完整安全载荷后重试。");',
'SetFailHint(FriendConnectGuide.RedactSecureMaterial(friendly) + "\\r\\n" + FriendConnectGuide.MissingSecurityInfoHint);',
),
]
# MakeButton("连接" - ensure uses constant if literal
if 'MakeButton("连接"' in t:
    t = t.replace('MakeButton("连接"', 'MakeButton(FriendConnectGuide.ConnectButtonText', 1)

for a,b in repls:
    if a not in t:
        raise SystemExit("Helper missing:\\n"+repr(a[:100]))
    t = t.replace(a,b,1)
p.write_text(t, encoding="utf-8", newline="\n")
print("Helper OK")

# ---- ModeSelectForm ----
p = ROOT / "RemoteAssist.App/ModeSelectForm.cs"
t = p.read_text(encoding="utf-8")
old1 = '"点「复制连接凭证」把完整安全载荷发给亲友，对方粘贴即可经公网中继加密连过来。危险操作需你本机确认。",'
new1 = 'FriendConnectGuide.AcceptGuideHint + "危险操作需你本机确认。",'
# Wait - AcceptGuideHint already ends with period. Spec D1 ends with 。 
# D1 = 点「复制连接凭证」，把复制出来的整段发给要帮你的人。
# Append danger note without banned words:
new1 = 'FriendConnectGuide.AcceptGuideHint + "危险操作需你本机确认。",'
# Actually string concat in MakeModeButton arg - need expression not just string. Check call site - it's a string arg. Can use concatenation.
old1 = '''            "点「复制连接凭证」把完整安全载荷发给亲友，对方粘贴即可经公网中继加密连过来。危险操作需你本机确认。",'''
new1 = '''            FriendConnectGuide.AcceptGuideHint + "危险操作需你本机确认。",'''
old2 = '''            "粘贴对方「复制连接凭证」的完整安全载荷，连接后查看进程 / 日志 / 文件，或提议执行命令。",'''
new2 = '''            FriendConnectGuide.PasteCredentialHint + "连接后查看进程 / 日志 / 文件，或提议执行命令。",'''
# PasteCredentialHint already ends with 。 so we get "。连接后" - better:
new2 = '''            FriendConnectGuide.PasteCredentialHint.TrimEnd(\'。\') + "，连接后查看进程 / 日志 / 文件，或提议执行命令。",'''
# Simpler hardcoded without banned words matching spirit:
new1 = '''            "点「复制连接凭证」，把复制出来的整段发给要帮你的人。危险操作需你本机确认。",'''
new2 = '''            "把对方通过「复制连接凭证」发来的整段粘贴到这里。连接后查看进程 / 日志 / 文件，或提议执行命令。",'''
if old1 not in t or old2 not in t:
    raise SystemExit("ModeSelect strings missing")
t = t.replace(old1, new1).replace(old2, new2)
# ensure using Protocol if constants used - using hardcoded above, OK
p.write_text(t, encoding="utf-8", newline="\n")
print("ModeSelect OK")

print("phase2 helper+mode done")
