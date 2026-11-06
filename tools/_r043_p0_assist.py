from pathlib import Path
ac = Path('src-net48/RemoteAssist.Protocol/AssistClient.cs')
at = ac.read_text(encoding='utf-8')
if '将自动用安全通道' in at:
    at = at.replace(
        '局域网 TLS 需要证书指纹：请展开「高级」填写，或使用默认中继连接（将自动用安全通道）。',
        '加密连接需要证书指纹：请粘贴含 fp= 的完整载荷，或展开「高级」填写 TLS 指纹（短连接凭证不含指纹）。')
    ac.write_text(at, encoding='utf-8', newline='\n')
    print('AssistClient fixed')
elif '短连接凭证不含指纹' in at:
    print('AssistClient already good')
else:
    # find the throw near PairViaRelay
    i = at.find('PairViaRelayGuestAsync')
    print(at[i:i+900])
