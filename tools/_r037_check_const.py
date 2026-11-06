from pathlib import Path
t = Path("src/RemoteAssist.Protocol/HelperSessionCopy.cs").read_text(encoding="utf-8")
assert 'PasteThenConnectHint = "粘贴后点「连接」"' in t
assert 'ConnectedButtonText = "已连接"' in t
assert 'ConnectButtonText = "连接"' in t
assert 'StatusPeerEndedAssist = "对方已结束这次协助。"' in t
assert 'StatusDisconnectedSelf = "状态：未连接"' in t
assert 'StatusConnectFailed = "连接失败"' in t
print("consts OK")
