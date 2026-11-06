from pathlib import Path
import re

# AssistClient from net8
t = Path("src/RemoteAssist.Protocol/AssistClient.cs").read_text(encoding="utf-8")
t = t.replace(
    "await tcp.ConnectAsync(opts.Host, opts.Port, ct).ConfigureAwait(false);",
    "await Net48Sockets.ConnectAsync(tcp, opts.Host, opts.Port, ct).ConfigureAwait(false);",
)
Path("src-net48/RemoteAssist.Protocol/AssistClient.cs").write_text(t, encoding="utf-8", newline="\n")
print("AssistClient OK", "tcp.ConnectAsync" in t, t.count("Net48Sockets.ConnectAsync"))

# FriendConnectGuide Contains/Replace
p = Path("src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs")
t = p.read_text(encoding="utf-8")
t2 = re.sub(
    r"(\w+)\.Contains\(([^,]+), (StringComparison\.\w+)\)",
    r"Net48String.Contains(\1, \2, \3)",
    t,
)
t2 = re.sub(
    r"(\w+)\.Replace\(([^,]+), ([^,]+), (StringComparison\.\w+)\)",
    r"Net48String.Replace(\1, \2, \3, \4)",
    t2,
)
p.write_text(t2, encoding="utf-8", newline="\n")
left = [f"{i}:{l}" for i,l in enumerate(t2.splitlines(),1) if "Contains(" in l and "StringComparison" in l and "Net48String" not in l]
print("left contains", left[:5])

# PairQrPayload / others may use Contains with StringComparison
for rel in ["PairQrPayload.cs", "ConnectDraftParser.cs", "AssistConnectOptions.cs", "RelayClient.cs", "SessionPasswordGenerator.cs"]:
    p = Path("src-net48/RemoteAssist.Protocol") / rel
    if not p.exists():
        continue
    t = p.read_text(encoding="utf-8")
    t2 = re.sub(r"(\w+)\.Contains\(([^,]+), (StringComparison\.\w+)\)", r"Net48String.Contains(\1, \2, \3)", t)
    t2 = re.sub(r"(\w+)\.Replace\(([^,]+), ([^,]+), (StringComparison\.\w+)\)", r"Net48String.Replace(\1, \2, \3, \4)", t2)
    # StartsWith/EndsWith with StringComparison is OK on net48
    if t2 != t:
        p.write_text(t2, encoding="utf-8", newline="\n")
        print("patched", rel)
