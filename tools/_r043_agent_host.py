# -*- coding: utf-8 -*-
from pathlib import Path

p = Path('src-net48/RemoteAssist.Agent/AgentHost.cs')
t = p.read_text(encoding='utf-8')

# 1) Add fields after _certFingerprint
old_fields = '''    private X509Certificate2? _serverCert;
    private string? _certFingerprint;

    public int Port { get; }'''
new_fields = '''    private X509Certificate2? _serverCert;
    private string? _certFingerprint;
    private string _sessionPassword;
    private readonly bool _preferRelay;
    private readonly bool _enableLanListen;
    private readonly string _relayEndpoint;
    private volatile string _relayPhase = "starting";
    private volatile string? _lastRelayError;

    public int Port { get; }'''
if old_fields not in t:
    raise SystemExit('fields anchor missing')
t = t.replace(old_fields, new_fields, 1)

# 2) Add events
old_ev = '''    public event Action? SessionChanged;
    public event Action? PairedClientsChanged;

    public AgentHost('''
new_ev = '''    public event Action? SessionChanged;
    public event Action? PairedClientsChanged;
    public event Action? RelayStatusChanged;

    public AgentHost('''
t = t.replace(old_ev, new_ev, 1)

# 3) Constructor
old_ctor = '''    public AgentHost(
        int port,
        bool autoYes,
        bool headless,
        int confirmTimeoutSeconds,
        AuditLog audit,
        string auditPath,
        bool insecure = false)
    {
        Port = port;
        _autoYes = autoYes;
        _headless = headless;
        _confirmTimeoutSeconds = Net48Math.Clamp(confirmTimeoutSeconds, 5, 600);
        _audit = audit;
        AuditPath = auditPath;
        _insecure = insecure;
        LanIps = LanAddressSelector.EnumerateLocalIPv4();
        _sessionCode = SessionCodeGenerator.Generate(6);
        _selectedHost = LanIps.FirstOrDefault() ?? "127.0.0.1";
        if (!_insecure)
        {
            _serverCert = TlsCertUtil.CreateEphemeralServerCert();
            _certFingerprint = TlsCertUtil.Sha256Fingerprint(_serverCert);
        }
    }'''
new_ctor = '''    public AgentHost(
        int port,
        bool autoYes,
        bool headless,
        int confirmTimeoutSeconds,
        AuditLog audit,
        string auditPath,
        bool insecure = false,
        bool preferRelay = true,
        bool enableLanListen = false,
        string? relayEndpoint = null)
    {
        Port = port;
        _autoYes = autoYes;
        _headless = headless;
        _confirmTimeoutSeconds = Net48Math.Clamp(confirmTimeoutSeconds, 5, 600);
        _audit = audit;
        AuditPath = auditPath;
        _insecure = insecure;
        _preferRelay = preferRelay;
        _enableLanListen = enableLanListen || !preferRelay;
        _relayEndpoint = string.IsNullOrWhiteSpace(relayEndpoint)
            ? RelayEndpoint.DefaultEndpoint
            : relayEndpoint!.Trim();
        LanIps = LanAddressSelector.EnumerateLocalIPv4();
        _sessionCode = SessionCodeGenerator.Generate(6);
        _sessionPassword = SessionPasswordGenerator.Generate();
        _selectedHost = LanIps.FirstOrDefault() ?? "127.0.0.1";
        if (!_insecure)
        {
            _serverCert = TlsCertUtil.CreateEphemeralServerCert();
            _certFingerprint = TlsCertUtil.Sha256Fingerprint(_serverCert);
        }
    }'''
if old_ctor not in t:
    raise SystemExit('ctor missing')
t = t.replace(old_ctor, new_ctor, 1)

# 4) Properties after SessionCode
old_sc = '''    public string SessionCode
    {
        get { lock (_codeLock) return _sessionCode; }
    }

    /// <summary>写入 QR 载荷的主机 IP'''
new_sc = '''    public string SessionCode
    {
        get { lock (_codeLock) return _sessionCode; }
    }

    public string SessionPassword
    {
        get { lock (_codeLock) return _sessionPassword; }
    }

    public bool PreferRelay => _preferRelay;
    public bool EnableLanListen => _enableLanListen;
    public string RelayEndpointAddress => _relayEndpoint;

    /// <summary>starting / dialing / ready / waiting / paired / error</summary>
    public string RelayPhase => _relayPhase;
    public string? LastRelayError => _lastRelayError;

    /// <summary>写入 QR 载荷的主机 IP'''
if old_sc not in t:
    raise SystemExit('SessionCode block missing')
t = t.replace(old_sc, new_sc, 1)

# 5) QrPayload
old_qr = '''    public string QrPayload
    {
        get
        {
            lock (_codeLock)
            {
                if (_insecure)
                    return PairQrPayload.Build(SelectedHost, Port, _sessionCode, fingerprint: null, useTls: false);
                var fp = _certFingerprint
                    ?? throw new InvalidOperationException("TLS 证书尚未生成，请先 Start()");
                return PairQrPayload.Build(SelectedHost, Port, _sessionCode, fingerprint: fp, useTls: true);
            }
        }
    }'''
# Check if Build or BuildLan
if 'PairQrPayload.Build(' in t and 'QrPayload' in t:
    # try find actual
    i = t.find('public string QrPayload')
    print('QrPayload at', i)
    print(repr(t[i:i+500]))
new_qr = '''    public string QrPayload
    {
        get
        {
            lock (_codeLock)
            {
                if (_preferRelay)
                {
                    // 亲友主路径：码+密码；业务管道在中继上走明文（与协助端无指纹时一致）
                    return PairQrPayload.BuildRelay(
                        _relayEndpoint, _sessionCode, _sessionPassword,
                        fingerprint: null, useTls: false);
                }

                if (_insecure)
                    return PairQrPayload.BuildLan(SelectedHost, Port, _sessionCode, fingerprint: null, useTls: false);
                var fp = _certFingerprint
                    ?? throw new InvalidOperationException("TLS 证书尚未生成，请先 Start()");
                return PairQrPayload.BuildLan(SelectedHost, Port, _sessionCode, fingerprint: fp, useTls: true);
            }
        }
    }'''
# Replace whatever QrPayload block exists
import re
m = re.search(r'    public string QrPayload\s*\{.*?\n    \}', t, re.S)
if not m:
    raise SystemExit('QrPayload regex fail')
t = t[:m.start()] + new_qr + t[m.end():]
print('QrPayload replaced')

# 6) Start()
old_start = '''    public void Start()
    {
        if (_listener is not null)
            return;

        if (!_insecure && _serverCert is not null)
            _audit.Write("tls_cert", $"fp={_certFingerprint}");
        else if (_insecure)
            _audit.Write("tls_off", "insecure plaintext");

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        _audit.Write("agent_start",
            $"port={Port}; code={SessionCode}; host={SelectedHost}; tls={(!_insecure).ToString().ToLowerInvariant()}; confirm={(_autoYes ? "auto" : _headless ? "console" : "ui-tray")}; timeout={_confirmTimeoutSeconds}s");
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }'''
new_start = '''    public void Start()
    {
        if (_cts is not null)
            return;

        if (!_insecure && _serverCert is not null)
            _audit.Write("tls_cert", $"fp={_certFingerprint}");
        else if (_insecure)
            _audit.Write("tls_off", "insecure plaintext");

        _cts = new CancellationTokenSource();
        _audit.Write("agent_start",
            $"relay={_preferRelay}; relayEp={_relayEndpoint}; port={Port}; code={SessionCode}; host={SelectedHost}; tls={(!_insecure).ToString().ToLowerInvariant()}; confirm={(_autoYes ? "auto" : _headless ? "console" : "ui-tray")}; timeout={_confirmTimeoutSeconds}s");

        if (_preferRelay)
        {
            SetRelayPhase("starting", null);
            _ = Task.Run(() => RelayGuestLoopAsync(_cts.Token));
        }

        if (_enableLanListen)
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
            _audit.Write("lan_listen", $"port={Port}");
        }
        else
        {
            _audit.Write("lan_listen", "skipped (relay-only)");
        }
    }'''
if old_start not in t:
    raise SystemExit('Start missing')
t = t.replace(old_start, new_start, 1)

# 7) RefreshSessionCode — also refresh password
old_ref = '''    public string RefreshSessionCode()
    {
        string code;
        lock (_codeLock)
        {
            _sessionCode = SessionCodeGenerator.Generate(6);
            code = _sessionCode;
        }'''
new_ref = '''    public string RefreshSessionCode()
    {
        string code;
        lock (_codeLock)
        {
            _sessionCode = SessionCodeGenerator.Generate(6);
            _sessionPassword = SessionPasswordGenerator.Generate();
            code = _sessionCode;
        }'''
if old_ref not in t:
    raise SystemExit('Refresh missing')
t = t.replace(old_ref, new_ref, 1)

# 8) Insert RelayGuestLoop + helpers before HandleClientAsync
relay_methods = '''
    private void SetRelayPhase(string phase, string? error)
    {
        _relayPhase = phase;
        _lastRelayError = error;
        try { RelayStatusChanged?.Invoke(); } catch { /* UI */ }
    }

    private async Task RelayGuestLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string password;
            lock (_codeLock) password = _sessionPassword;

            if (!RelayEndpoint.TryParse(_relayEndpoint, out var host, out var port, out var err))
            {
                SetRelayPhase("error", err);
                _audit.Write("relay_bad_endpoint", err ?? "");
                try { await Task.Delay(5000, ct).ConfigureAwait(false); } catch { break; }
                continue;
            }

            RelayClient? relay = null;
            try
            {
                SetRelayPhase("dialing", null);
                _audit.Write("relay_guest_dial", $"ep={host}:{port}");
                var (client, code) = await RelayClient.GuestHostAsync(
                        host, port, password, preferredCode: null,
                        onCodeAssigned: c =>
                        {
                            lock (_codeLock) _sessionCode = c;
                            SetRelayPhase("ready", null);
                            try { SessionChanged?.Invoke(); } catch { /* UI */ }
                            _audit.Write("relay_guest_code", $"code={c}");
                        },
                        ct: ct)
                    .ConfigureAwait(false);
                relay = client;
                lock (_codeLock) _sessionCode = code;
                SetRelayPhase("waiting", null);
                try { SessionChanged?.Invoke(); } catch { /* UI */ }
                _audit.Write("relay_guest_paired", $"code={code}");
                SetRelayPhase("paired", null);
                // 亲友主路径：中继业务管道明文（协助端无指纹时同样明文），TLS 仅局域网高级
                await HandleStreamAsync(relay, relay.Stream, $"relay-guest/{host}:{port}", skipTls: true, ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                SetRelayPhase("error", FriendConnectGuide.FriendlyOrRaw(ex));
                _audit.Write("relay_guest_error", ex.ToString());
                try { await Task.Delay(2000, ct).ConfigureAwait(false); } catch { break; }
            }
            finally
            {
                try { relay?.Dispose(); } catch { /* ignore */ }
            }
        }
    }

    private async Task HandleStreamAsync(
        IDisposable transport,
        Stream rawStream,
        string remote,
        bool skipTls,
        CancellationToken outerCt)
    {
        _audit.Write("client_connect", remote);
        SessionState? session = null;
        using var connCts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);

        try
        {
            Stream stream = rawStream;
            System.Net.Security.SslStream? ssl = null;
            try
            {
                if (!skipTls && !_insecure)
                {
                    if (_serverCert is null)
                        throw new InvalidOperationException("TLS enabled but server certificate missing");
                    ssl = await TlsTransport.AuthenticateAsServerAsync(stream, _serverCert, connCts.Token)
                        .ConfigureAwait(false);
                    stream = ssl;
                }

                while (!connCts.IsCancellationRequested)
                {
                    WireEnvelope? envelope;
                    try
                    {
                        envelope = await FrameIo.ReadAsync(stream, connCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (envelope is null)
                        break;

                    switch (envelope.Type.ToLowerInvariant())
                    {
                        case "pair":
                            session = await HandlePairAsync(stream, envelope, remote, connCts).ConfigureAwait(false);
                            break;
                        case "tool":
                            await HandleToolAsync(stream, envelope, session).ConfigureAwait(false);
                            break;
                        case "ping":
                            await FrameIo.WriteAsync(stream, "pong", new { }, token: session?.Token, id: envelope.Id)
                                .ConfigureAwait(false);
                            break;
                        default:
                            await FrameIo.WriteAsync(stream, "error",
                                new ErrorPayload { Message = $"Unknown type: {envelope.Type}" },
                                id: envelope.Id).ConfigureAwait(false);
                            break;
                    }
                }
            }
            finally
            {
                try { ssl?.Dispose(); } catch { /* ignore */ }
            }
        }
        catch (Exception ex)
        {
            _audit.Write("client_error", $"{remote}; {ex.Message}");
        }
        finally
        {
            if (session is not null &&
                _sessions.TryGetValue(session.Token, out var cur) &&
                ReferenceEquals(cur, session))
            {
                _sessions.TryRemove(session.Token, out _);
                PairedClientsChanged?.Invoke();
            }
            _audit.Write("client_disconnect", remote);
            try { transport.Dispose(); } catch { /* ignore */ }
        }
    }

'''

anchor = '    private async Task HandleClientAsync(TcpClient client)'
if anchor not in t:
    raise SystemExit('HandleClientAsync anchor missing')
t = t.replace(anchor, relay_methods + anchor, 1)

# 9) Simplify HandleClientAsync to delegate? Keep as-is for LAN; it duplicates logic which is OK.
# Update PrintBanner slightly
t = t.replace(
    'RemoteAssist Agent (局域网',
    '鹊桥 Agent (')

p.write_text(t, encoding='utf-8', newline='\n')
print('AgentHost patched, lines', t.count(chr(10))+1)
