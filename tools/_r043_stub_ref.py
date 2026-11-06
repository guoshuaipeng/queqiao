from pathlib import Path
cs=Path('src-net48/RemoteAssist.Relay.Stub/RemoteAssist.Relay.Stub.csproj')
t=cs.read_text(encoding='utf-8')
if 'RemoteAssist.Protocol' not in t:
    t=t.replace('</PropertyGroup>', '''</PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\RemoteAssist.Protocol\RemoteAssist.Protocol.csproj" />
  </ItemGroup>''', 1)
    cs.write_text(t,encoding='utf-8',newline='\n')
    print('added Protocol ref')
else:
    print('already has ref')
print(cs.read_text(encoding='utf-8'))
