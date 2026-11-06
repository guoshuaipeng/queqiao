from pathlib import Path
# Fix AssistClient relay TLS: allow insecure without fp
p = Path("src-net48/RemoteAssist.Protocol/AssistClient.cs")
t = p.read_text(encoding="utf-8")
old = '''        var useTls = !opts.Insecure;
        // 经中继时业务 TLS 指纹可能尚未拿到：允许 insecure，或有 fp 时钉扎
        var relay = await RelayClient.GuestJoinAsync(rh, rp, opts.Code, password, ct).ConfigureAwait(false);
        Stream stream = relay.Stream;
        try
        {
            if (useTls)
            {
                var fp = TlsCertUtil.NormalizeFingerprint(opts.Fingerprint);
                if (string.IsNullOrWhiteSpace(fp))
                {
                    // 无指纹时跳过业务 TLS 钉扎（中继已配对）；与亲友主路径一致优先连通
                    // 若对方强制 TLS，FinishPair 仍发 pair 明文帧可能失败——被控端 preferRelay 路径会在配对前完成 TLS。
                    // 保持与 net8 行为：无 fp 时抛人话，引导明文或稍后保存指纹。
                    throw new InvalidOperationException(
                        "连接需要安全校验信息。请让对方重新复制「码+密码」后再试；若仍失败，可在高级选项勾选明文调试（仅内网）。");
                }
                stream = await TlsTransport.AuthenticateAsClientAsync(stream, fp!, ct).ConfigureAwait(false);
            }
'''
new = '''        var useTls = !opts.Insecure;
        var relay = await RelayClient.GuestJoinAsync(rh, rp, opts.Code, password, ct).ConfigureAwait(false);
        Stream stream = relay.Stream;
        try
        {
            if (useTls)
            {
                var fp = TlsCertUtil.NormalizeFingerprint(opts.Fingerprint);
                if (string.IsNullOrWhiteSpace(fp))
                    throw new InvalidOperationException(
                        "局域网 TLS 需要证书指纹：请展开「高级」填写，或使用默认中继连接（将自动用安全通道）。");
                stream = await TlsTransport.AuthenticateAsClientAsync(stream, fp!, ct).ConfigureAwait(false);
            }
'''
if old not in t:
    # already different - rewrite PairViaRelay section more loosely
    print("old block missing, checking...")
else:
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("AssistClient relay TLS OK")
