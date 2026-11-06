from pathlib import Path
t=Path('src-net48/RemoteAssist.Agent/AgentHost.cs').read_text(encoding='utf-8')
for n in ['HandleStreamAsync','HandleClientAsync','AuthenticateAsServer','Dispose','IDisposable']:
    i=0
    while True:
        j=t.find(n,i)
        if j<0: break
        print(t.count('\n',0,j)+1, t[j:j+180].replace('\n',' | '))
        i=j+len(n)
print('===== AgentDesktopController net48 =====')
print(Path('src-net48/RemoteAssist.Agent/AgentDesktopController.cs').read_text(encoding='utf-8')[:2500])
print('===== Helper MainForm BuildUi snippet =====')
h=Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs').read_text(encoding='utf-8')
# find BuildUi or ctor UI
for n in ['即将支持','_hostBox','_portBox','_fpBox','_relay','_codeBox','_pwdBox','BuildUi','Initialize']:
    print(n, h.find(n))
