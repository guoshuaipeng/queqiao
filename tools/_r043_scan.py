from pathlib import Path

def head_methods(path, needles):
    t = Path(path).read_text(encoding='utf-8')
    print('===', path, 'len', len(t), 'lines', t.count('\n')+1)
    for n in needles:
        i = t.find(n)
        print(f'  {n!r}: {i}')
    # brand strings
    for s in ['远端协助', '鹊桥', '即将支持', '启动被控', 'MessageBox', 'PreferRelay', 'SessionPassword', 'guest_host', 'GuestHost']:
        c = t.count(s)
        if c: print(f'  count[{s}]={c}')

for p, ns in [
    ('src-net48/RemoteAssist.Agent/AgentHost.cs', ['StartAsync', 'TcpListener', 'SessionPassword', 'Relay', 'guest']),
    ('src-net48/RemoteAssist.Agent/MainForm.cs', ['复制', '会话', '密码', 'Text =', '即将']),
    ('src-net48/RemoteAssist.Helper.UI/MainForm.cs', ['Host', 'Port', '即将', 'Connect', '指纹', '高级']),
    ('src-net48/RemoteAssist.App/AppShell.cs', ['EnterAcceptMode', 'MessageBox', '启动被控']),
    ('src-net48/RemoteAssist.App/ModeSelectForm.cs', ['远端', '鹊桥', '协助']),
    ('src/RemoteAssist.Agent/AgentHost.cs', ['RelayGuestLoopAsync', 'SessionPassword', '_sessionPassword', 'GuestHostAsync']),
]:
    head_methods(p, ns)
