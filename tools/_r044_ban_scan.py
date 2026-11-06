from pathlib import Path
import re
banned = re.compile(r"指纹|(?<![A-Za-z0-9])fp(?![A-Za-z0-9=])|TLS|安全载荷|fp=")
files = [
    "src-net48/RemoteAssist.Helper.UI/MainForm.cs",
    "src-net48/RemoteAssist.Agent/MainForm.cs",
    "src-net48/RemoteAssist.App/ModeSelectForm.cs",
    "src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs",
    "src-net48/RemoteAssist.Protocol/ConnectDraftParser.cs",
    "src-net48/RemoteAssist.Protocol/AssistClient.cs",
]
print("=== string-literal banned (non-comment) ===")
for f in files:
    text = Path(f).read_text(encoding="utf-8")
    for i, line in enumerate(text.splitlines(), 1):
        st = line.strip()
        if st.startswith("//") or st.startswith("///"):
            continue
        # allow advanced panel label
        if "Lbl(\"TLS指纹\"" in line or "Lbl(\"TLS 指纹\"" in line:
            print(f"ADV_OK {f}:{i}:{st[:100]}")
            continue
        for m in re.finditer(r'"([^"\\]|\\.)*"', line):
            s = m.group(0)
            if banned.search(s):
                print(f"HIT {f}:{i}:{s[:140]}")
print("=== logic anchors still present ===")
blob = Path("src-net48/RemoteAssist.Protocol/ConnectDraftParser.cs").read_text(encoding="utf-8")
for needle in [
    "IsWellFormedFingerprint",
    "NeedFingerprintHint",
    "NeedSecurePayloadHint",
    "MissingSecurityInfoHint",
    "!insecure && !advancedFpOk",
    "wantTls && !TlsCertUtil.IsWellFormedFingerprint",
]:
    print(("OK" if needle in blob else "MISSING"), needle)
ac = Path("src-net48/RemoteAssist.Protocol/AssistClient.cs").read_text(encoding="utf-8")
print("AssistClient still checks fp", "NormalizeFingerprint" in ac and "MissingSecurityInfoHint" in ac)
pw = Path("src-net48/RemoteAssist.Protocol/PairQrPayload.cs").read_text(encoding="utf-8")
print("BuildRelay unchanged fp require", "IsWellFormedFingerprint" in pw and "&fp=" in pw)
