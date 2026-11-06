from pathlib import Path
p = Path('src-net48/RemoteAssist.App/ModeSelectForm.cs')
t = p.read_text(encoding='utf-8')
t = t.replace(
    '生成本机会话码 / 二维码，让亲友或同事连过来帮你排查。危险操作需你本机确认。',
    '生成连接凭证发给亲友，对方粘贴即可经公网中继连过来帮你。危险操作需你本机确认。')
t = t.replace(
    '粘贴对方会话载荷或会话码，连接后查看进程 / 日志 / 文件，或提议执行命令。',
    '粘贴对方发来的连接凭证或会话载荷，连接后查看进程 / 日志 / 文件，或提议执行命令。')
t = t.replace(
    'v1 默认局域网直连（可 TLS）。公网中继后续版本启用；也可用命令行参数 --accept / --control 直达对应模式。',
    '默认经公网中继连接。局域网 Host/端口仅在协助端「高级」里。也可用 --accept / --control 直达对应模式。')
p.write_text(t, encoding='utf-8', newline='\n')

cs = Path('src-net48/RemoteAssist.App/RemoteAssist.App.csproj')
c = cs.read_text(encoding='utf-8')
c = c.replace('<Product>RemoteAssist (.NET Framework 4.8)</Product>', '<Product>鹊桥</Product>')
if '<AssemblyTitle>' not in c:
    c = c.replace('<Product>鹊桥</Product>', '<Product>鹊桥</Product>\n    <AssemblyTitle>鹊桥</AssemblyTitle>\n    <Description>鹊桥远程协助</Description>')
cs.write_text(c, encoding='utf-8', newline='\n')
print('ModeSelect + csproj OK')
