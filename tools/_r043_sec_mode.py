from pathlib import Path
p=Path('src-net48/RemoteAssist.App/ModeSelectForm.cs')
t=p.read_text(encoding='utf-8')
t=t.replace(
    '粘贴对方发来的连接凭证或会话载荷，连接后查看进程 / 日志 / 文件，或提议执行命令。',
    '粘贴对方「复制连接凭证」的完整安全载荷，连接后查看进程 / 日志 / 文件，或提议执行命令。')
t=t.replace(
    '生成连接凭证发给亲友，对方粘贴即可经公网中继连过来帮你。危险操作需你本机确认。',
    '点「复制连接凭证」把完整安全载荷发给亲友，对方粘贴即可经公网中继加密连过来。危险操作需你本机确认。')
p.write_text(t,encoding='utf-8',newline='\n')
print('ModeSelect OK')
