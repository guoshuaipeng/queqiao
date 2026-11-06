from pathlib import Path
import re
t = Path("src-net48/RemoteAssist.Protocol/PairQrPayload.cs").read_text(encoding="utf-8")
i = t.find("public static string BuildRelay")
j = t.find("public static bool TryParse", i)
block = t[i:j]
print("BuildRelay_block_ok", "v=2" in block and "via=relay" in block and "&pwd=" in block and "&fp=" in block)
frags = re.findall(r'Append\("([^"]+)"\)', block)
print("append_frags:", frags)
u = Path("src-net48/RemoteAssist.Protocol/TlsCertUtil.cs").read_text(encoding="utf-8")
assert "IsWellFormedFingerprint" in u and "n.Length != 64" in u
assert "FormatShareClipboard" in Path("src-net48/RemoteAssist.Protocol/FriendConnectGuide.cs").read_text(encoding="utf-8")
assert "CopySecureCredential" in Path("src-net48/RemoteAssist.Agent/MainForm.cs").read_text(encoding="utf-8")
print("BOX_SRC_FORMAT_OK")
