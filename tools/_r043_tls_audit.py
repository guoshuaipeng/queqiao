from pathlib import Path

def show(path, needles, ctx=3):
    t = Path(path).read_text(encoding='utf-8')
    print('\n========', path)
    for n in needles:
        i = 0
        while True:
            j = t.find(n, i)
            if j < 0: break
            line = t.count('\n', 0, j) + 1
            start = t.rfind('\n', 0, j)
            # print surrounding lines
            a = max(0, t.rfind('\n', 0, max(0,j-200)))
            b = t.find('\n', j+120)
            snippet = t[a:b if b>0 else j+200]
            print(f'--- L{line} [{n}] ---')
            print(snippet)
            i = j + len(n)

show('src-net48/RemoteAssist.Agent/AgentHost.cs', [
    'skipTls', 'BuildRelay', 'PreferRelay', 'HandleStreamAsync', 'AuthenticateAsServer', '_insecure', 'HandlePairAsync', 'Confirm'
])
show('src-net48/RemoteAssist.Helper.UI/MainForm.cs', [
    'Insecure = true', 'ViaRelay', 'Fingerprint', 'force'
])
show('src-net48/RemoteAssist.Protocol/AssistClient.cs', [
    'PairViaRelayGuest', 'useTls', 'AuthenticateAsClient', 'FinishPair', 'Insecure'
])
