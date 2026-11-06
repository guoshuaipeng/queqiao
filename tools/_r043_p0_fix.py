from pathlib import Path

# ---- AgentHost: QrPayload + skipTls ----
p = Path('src-net48/RemoteAssist.Agent/AgentHost.cs')
t = p.read_text(encoding='utf-8')

old_qr = '''                if (_preferRelay)
                {
                    // 亲友主路径：码+密码；业务管道在中继上走明文（与协助端无指纹时一致）
                    return PairQrPayload.BuildRelay(
                        _relayEndpoint, _sessionCode, _sessionPassword,
                        fingerprint: null, useTls: false);
                }'''
new_qr = '''                if (_preferRelay)
                {
                    // R043 安全纠错：中继 pipe 上仍须业务 TLS + 指纹（architecture）；短凭证不含 fp
                    if (_insecure)
                        return PairQrPayload.BuildRelay(
                            _relayEndpoint, _sessionCode, _sessionPassword,
                            fingerprint: null, useTls: false);
                    var fpR = _certFingerprint
                        ?? throw new InvalidOperationException("TLS 证书尚未生成，请先 Start()");
                    return PairQrPayload.BuildRelay(
                        _relayEndpoint, _sessionCode, _sessionPassword,
                        fingerprint: fpR, useTls: true);
                }'''
if old_qr not in t:
    raise SystemExit('QrPayload block missing')
t = t.replace(old_qr, new_qr, 1)

old_call = '''                // 亲友主路径：中继业务管道明文（协助端无指纹时同样明文），TLS 仅局域网高级
                await HandleStreamAsync(relay, relay.Stream, $"relay-guest/{host}:{port}", skipTls: true, ct)
                    .ConfigureAwait(false);'''
new_call = '''                // R043 安全纠错：与 net8 / architecture 一致 — 中继 pipe 内业务 TLS（非 --insecure）
                await HandleStreamAsync(relay, relay.Stream, $"relay-guest/{host}:{port}", skipTls: false, ct)
                    .ConfigureAwait(false);'''
if old_call not in t:
    raise SystemExit('skipTls call missing')
t = t.replace(old_call, new_call, 1)

# Optionally simplify HandleStreamAsync: keep skipTls param but document only false for relay; LAN uses HandleClientAsync
p.write_text(t, encoding='utf-8', newline='\n')
print('AgentHost OK')

# ---- Helper MainForm: remove force Insecure ----
h = Path('src-net48/RemoteAssist.Helper.UI/MainForm.cs')
ht = h.read_text(encoding='utf-8')
old_force = '''            // 中继访客默认允许明文业务管道（指纹可在高级补）
            if (opts.ViaRelay && !opts.Insecure && string.IsNullOrWhiteSpace(opts.Fingerprint))
            {
                opts = new AssistConnectOptions
                {
                    Host = opts.Host,
                    Port = opts.Port,
                    Code = opts.Code,
                    Permission = opts.Permission,
                    Fingerprint = null,
                    Insecure = true,
                    ViaRelay = true,
                    RelayEndpoint = opts.RelayEndpoint,
                    Password = opts.Password
                };
            }

            var client = await AssistClient.PairAsync(opts, "helper-ui").ConfigureAwait(true);'''
new_force = '''            // R043 安全纠错：禁止公网主路径无指纹时静默降明文。须载荷含 fp= 或高级填写指纹；
            // 显式勾选「明文调试」才走 --insecure。短凭证 {码}-{验证码} 不含指纹 → 安全阻断并人话引导。
            if (opts.ViaRelay && !opts.Insecure && string.IsNullOrWhiteSpace(opts.Fingerprint))
            {
                SetStatus("无法安全连接");
                SetFailHint(
                    "还缺少安全校验（证书指纹）。「连接凭证」只有会话码和验证码，不能单独走加密通道。"
                    + Environment.NewLine
                    + "下一步：请对方用「复制载荷」发来含指纹的完整连接信息并粘贴；或展开「高级」填写 TLS 指纹后再连接。"
                    + Environment.NewLine
                    + "（仅本机调试可勾选「明文调试」，公网请勿使用。）");
                return;
            }

            var client = await AssistClient.PairAsync(opts, "helper-ui").ConfigureAwait(true);'''
if old_force not in ht:
    raise SystemExit('Helper force block missing')
ht = ht.replace(old_force, new_force, 1)
# Ensure finally still re-enables button when we return early — we're inside try after _busy=true _btnConnect.Enabled=false
# Early return before try's finally? Looking at structure - the block is INSIDE try, so return will skip finally!
# Need to check ConnectAsync structure
h.write_text(ht, encoding='utf-8', newline='\n')
print('Helper patched — verify finally')
