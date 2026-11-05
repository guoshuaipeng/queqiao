using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Agent;

/// <summary>
/// Agent 运行时：会话码、监听、配对/工具处理。供控制台与托盘 UI 共用。
/// </summary>
internal sealed class AgentHost
{
    private const int DefaultConfirmTimeoutSeconds = 60;
    private static readonly TimeSpan TokenTtl = TimeSpan.FromHours(2);

    private readonly object _codeLock = new();
    private string _sessionCode;
    private string? _selectedHost;
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
    private readonly AuditLog _audit;
    private readonly bool _autoYes;
    private readonly bool _headless;
    private readonly int _confirmTimeoutSeconds;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly bool _insecure;
    private X509Certificate2? _serverCert;
    private string? _certFingerprint;
    private string _sessionPassword;
    private readonly bool _preferRelay;
    private readonly bool _enableLanListen;
    private readonly string _relayEndpoint;
    private readonly string? _accountToken;
    private readonly string _deviceId;
    private readonly string _deviceDisplayName;

    public int Port { get; }
    public string AuditPath { get; }
    public AuditLog Audit => _audit;
    public IReadOnlyList<string> LanIps { get; }

    public event Action? SessionChanged;
    public event Action? PairedClientsChanged;

    public AgentHost(
        int port,
        bool autoYes,
        bool headless,
        int confirmTimeoutSeconds,
        AuditLog audit,
        string auditPath,
        bool insecure = false,
        bool preferRelay = true,
        bool enableLanListen = false,
        string? relayEndpoint = null,
        string? accountToken = null,
        string? deviceId = null,
        string? deviceDisplayName = null)
    {
        Port = port;
        _autoYes = autoYes;
        _headless = headless;
        _confirmTimeoutSeconds = Math.Clamp(confirmTimeoutSeconds, 5, 600);
        _audit = audit;
        AuditPath = auditPath;
        _insecure = insecure;
        _preferRelay = preferRelay;
        _enableLanListen = enableLanListen || !preferRelay;
        _relayEndpoint = string.IsNullOrWhiteSpace(relayEndpoint)
            ? RelayEndpoint.DefaultEndpoint
            : relayEndpoint.Trim();
        _accountToken = string.IsNullOrWhiteSpace(accountToken) ? null : accountToken.Trim();
        _deviceId = string.IsNullOrWhiteSpace(deviceId)
            ? HelperUiSettings.EnsureDeviceId()
            : deviceId.Trim();
        _deviceDisplayName = string.IsNullOrWhiteSpace(deviceDisplayName)
            ? Environment.MachineName
            : deviceDisplayName.Trim();
        LanIps = LanAddressSelector.EnumerateLocalIPv4();
        _sessionCode = SessionCodeGenerator.Generate(6);
        _sessionPassword = SessionPasswordGenerator.Generate(4);
        _selectedHost = LanIps.FirstOrDefault() ?? "127.0.0.1";
        if (!_insecure)
        {
            _serverCert = TlsCertUtil.CreateEphemeralServerCert();
            _certFingerprint = TlsCertUtil.Sha256Fingerprint(_serverCert);
        }
    }

    public string SessionCode
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
    public bool AccountBound => !string.IsNullOrWhiteSpace(_accountToken);
    public string DeviceId => _deviceId;

    /// <summary>写入 QR 载荷的主机 IP（可在会话窗下拉切换；可显式设为 127.0.0.1 等）。</summary>
    public string SelectedHost
    {
        get
        {
            lock (_codeLock)
            {
                if (!string.IsNullOrWhiteSpace(_selectedHost))
                    return _selectedHost!;
                return LanIps.FirstOrDefault() ?? "127.0.0.1";
            }
        }
    }

    public string PrimaryHost => SelectedHost;

    /// <summary>当前 TLS 证书 SHA-256 指纹；明文模式为 null。</summary>
    public string? CertificateFingerprint
    {
        get { lock (_codeLock) return _certFingerprint; }
    }

    public bool UseTls => !_insecure;

    public string QrPayload
    {
        get
        {
            lock (_codeLock)
            {
                if (_preferRelay)
                {
                    if (_insecure)
                        return PairQrPayload.BuildRelay(_relayEndpoint, _sessionCode, _sessionPassword, fingerprint: null, useTls: false);
                    var fpR = _certFingerprint
                        ?? throw new InvalidOperationException("TLS 证书尚未生成，请先 Start()");
                    return PairQrPayload.BuildRelay(_relayEndpoint, _sessionCode, _sessionPassword, fingerprint: fpR, useTls: true);
                }

                if (_insecure)
                    return PairQrPayload.BuildLan(SelectedHost, Port, _sessionCode, fingerprint: null, useTls: false);
                var fp = _certFingerprint
                    ?? throw new InvalidOperationException("TLS 证书尚未生成，请先 Start()");
                return PairQrPayload.BuildLan(SelectedHost, Port, _sessionCode, fingerprint: fp, useTls: true);
            }
        }
    }

    public void SetSelectedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return;
        host = host.Trim();
        lock (_codeLock)
        {
            if (string.Equals(_selectedHost, host, StringComparison.Ordinal))
                return;
            _selectedHost = host;
        }
        _audit.Write("host_selected", $"host={host}");
        SessionChanged?.Invoke();
    }

    public IReadOnlyList<PairedClientInfo> ListPairedClients()
    {
        var now = DateTimeOffset.UtcNow;
        return _sessions.Values
            .Where(s => s.ExpiresAtUtc > now)
            .OrderBy(s => s.PairedAtUtc)
            .Select(s => new PairedClientInfo(
                s.Token,
                s.ClientName,
                s.RemoteEndpoint,
                s.Permission,
                s.PairedAtUtc,
                s.ExpiresAtUtc))
            .ToList();
    }

    /// <summary>撤销令牌并踢掉对应 TCP 连接。</summary>
    public bool DisconnectSession(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        if (!_sessions.TryRemove(token, out var session))
            return false;

        try { session.ConnectionCts?.Cancel(); } catch { /* ignore */ }

        _audit.Write("session_kick",
            $"client={session.ClientName}; remote={session.RemoteEndpoint}; perm={session.Permission}; token={token[..Math.Min(8, token.Length)]}…");
        Console.WriteLine($"[已断开] {session.ClientName} @ {session.RemoteEndpoint}");
        PairedClientsChanged?.Invoke();
        return true;
    }

    public int DisconnectAllSessions()
    {
        var tokens = _sessions.Keys.ToList();
        var n = 0;
        foreach (var t in tokens)
        {
            if (DisconnectSession(t))
                n++;
        }
        return n;
    }

    public string RefreshSessionCode()
    {
        string code;
        lock (_codeLock)
        {
            _sessionCode = SessionCodeGenerator.Generate(6);
            _sessionPassword = SessionPasswordGenerator.Generate(4);
            code = _sessionCode;
        }

        foreach (var key in _sessions.Keys.ToList())
        {
            if (_sessions.TryRemove(key, out var s))
            {
                try { s.ConnectionCts?.Cancel(); } catch { /* ignore */ }
            }
        }

        _audit.Write("session_refresh", $"code={code}; kicked_all=true");
        Console.WriteLine($"[会话码已刷新] {code}");
        SessionChanged?.Invoke();
        PairedClientsChanged?.Invoke();
        return code;
    }

    public void PrintBanner()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  RemoteAssist Agent (中继默认 / LAN 可调试)");
        Console.WriteLine("========================================");
        Console.WriteLine($"监听端口 : {Port}");
        Console.WriteLine($"传输加密 : {(_insecure ? "明文 --insecure（仅调试）" : "TLS 1.2/1.3 + 证书指纹钉扎（默认）")}");
        if (!_insecure && _certFingerprint is not null)
            Console.WriteLine($"证书指纹 : {_certFingerprint}");
        Console.WriteLine($"本机地址 : {string.Join(", ", LanIps)}");
        Console.WriteLine($"QR 主机  : {SelectedHost}");
        Console.WriteLine($"会话码   : {SessionCode}");
        Console.WriteLine($"二维码载荷: {QrPayload}");
        Console.WriteLine($"审计日志 : {AuditPath}");
        Console.WriteLine($"确认方式 : {(_autoYes ? "自动(--yes)" : _headless ? "控制台(--headless)" : "本机窗口+托盘(默认)")}");
        Console.WriteLine($"确认超时 : {_confirmTimeoutSeconds}s");
        Console.WriteLine();
        Console.WriteLine("协助端示例:");
        Console.WriteLine($"  dotnet run --project src/RemoteAssist.Helper -- pair {SelectedHost} {SessionCode}");
        Console.WriteLine($"  dotnet run --project src/RemoteAssist.Helper -- pair \"{QrPayload}\"");
        Console.WriteLine();
        if (_autoYes)
            Console.WriteLine("自动确认已开启 (--yes)，仅用于本地联调。");
        else if (_headless)
            Console.WriteLine("无 UI 模式：不启托盘/窗体；配对与写操作在本控制台确认。");
        else
            Console.WriteLine("默认托盘常驻：会话窗可选主机 IP；托盘可断开已配对客户端。");
        Console.WriteLine(_headless || _autoYes ? "按 Ctrl+C 退出。" : "托盘菜单「退出」或 Ctrl+C 结束。");
        Console.WriteLine("----------------------------------------");
    }

    public void Start()
    {
        if (_cts is not null)
            return;

        if (!_insecure && _serverCert is not null)
            _audit.Write("tls_cert", $"fp={_certFingerprint}");
        else if (_insecure)
            _audit.Write("tls_off", "insecure plaintext");

        _cts = new CancellationTokenSource();
        _audit.Write("agent_start",
            $"relay={_preferRelay}; relayEp={_relayEndpoint}; port={Port}; code={SessionCode}; host={SelectedHost}; tls={(!_insecure).ToString().ToLowerInvariant()}; confirm={(_autoYes ? "auto" : _headless ? "console" : "ui-tray")}; timeout={_confirmTimeoutSeconds}s; account={AccountBound}");

        if (_preferRelay)
        {
            _ = Task.Run(() => RelayGuestLoopAsync(_cts.Token));
            if (AccountBound)
                _ = Task.Run(() => RelayDeviceLoopAsync(_cts.Token));
        }

        // 默认中继时不对外开 LAN 端口；高级/调试显式 EnableLanListen 才监听
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
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _listener?.Stop(); } catch { /* ignore */ }
        _listener = null;
        foreach (var key in _sessions.Keys.ToList())
        {
            if (_sessions.TryRemove(key, out var s))
            {
                try { s.ConnectionCts?.Cancel(); } catch { /* ignore */ }
            }
        }
        _audit.Write("agent_stop", "listener stopped");
        try { _serverCert?.Dispose(); } catch { /* ignore */ }
        _serverCert = null;
        lock (_codeLock) _certFingerprint = null;
        PairedClientsChanged?.Invoke();
    }


    private async Task RelayGuestLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string password;
            lock (_codeLock) password = _sessionPassword;

            if (!RelayEndpoint.TryParse(_relayEndpoint, out var host, out var port, out var err))
            {
                _audit.Write("relay_bad_endpoint", err);
                try { await Task.Delay(5000, ct).ConfigureAwait(false); } catch { break; }
                continue;
            }

            RelayClient? relay = null;
            try
            {
                _audit.Write("relay_guest_dial", $"ep={host}:{port}");
                // JSONL: guest_host 由服务端签发 code；配对完成后再跑业务 TLS
                var (client, code) = await RelayClient.GuestHostAsync(
                        host, port, password, preferredCode: null,
                        onCodeAssigned: c =>
                        {
                            lock (_codeLock) _sessionCode = c;
                            SessionChanged?.Invoke();
                            _audit.Write("relay_guest_code", $"code={c}");
                        },
                        ct: ct)
                    .ConfigureAwait(false);
                relay = client;
                lock (_codeLock) _sessionCode = code;
                SessionChanged?.Invoke();
                _audit.Write("relay_guest_paired", $"code={code}");
                await HandleStreamAsync(relay, relay.Stream, $"relay-guest/{host}:{port}", ownerLink: false, ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _audit.Write("relay_guest_error", ex.Message);
                try { await Task.Delay(2000, ct).ConfigureAwait(false); } catch { break; }
            }
            finally
            {
                try { relay?.Dispose(); } catch { /* ignore */ }
            }
        }
    }

    private async Task RelayDeviceLoopAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_accountToken))
            return;

        while (!ct.IsCancellationRequested)
        {
            if (!RelayEndpoint.TryParse(_relayEndpoint, out var host, out var port, out var err))
            {
                _audit.Write("relay_bad_endpoint", err);
                try { await Task.Delay(5000, ct).ConfigureAwait(false); } catch { break; }
                continue;
            }

            RelayClient? relay = null;
            try
            {
                _audit.Write("relay_device_online", $"device={_deviceId}");
                relay = await RelayClient.DeviceOnlineAsync(host, port, _accountToken!, _deviceId, _deviceDisplayName, ct)
                    .ConfigureAwait(false);
                await HandleStreamAsync(relay, relay.Stream, $"relay-device/{_deviceId}", ownerLink: true, ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _audit.Write("relay_device_error", ex.Message);
                try { await Task.Delay(3000, ct).ConfigureAwait(false); } catch { break; }
            }
            finally
            {
                try { relay?.Dispose(); } catch { /* ignore */ }
            }
        }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        var listener = _listener;
        if (listener is null) return;

        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            _ = Task.Run(() => HandleClientAsync(client), CancellationToken.None);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "?";
        await HandleStreamAsync(client, client.GetStream(), remote, ownerLink: false, CancellationToken.None)
            .ConfigureAwait(false);
    }

    private async Task HandleStreamAsync(
        IDisposable transport,
        Stream rawStream,
        string remote,
        bool ownerLink,
        CancellationToken outerCt)
    {
        _audit.Write("client_connect", remote + (ownerLink ? "; ownerLink=1" : ""));
        SessionState? session = null;
        using var connCts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);

        try
        {
            Stream stream = rawStream;
            System.Net.Security.SslStream? ssl = null;
            try
            {
                if (!_insecure)
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
                            session = await HandlePairAsync(stream, envelope, remote, connCts, ownerLink)
                                .ConfigureAwait(false);
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

    private async Task<SessionState?> HandlePairAsync(
        Stream stream,
        WireEnvelope envelope,
        string remote,
        CancellationTokenSource connCts,
        bool ownerLinkChannel)
    {
        var req = FrameIo.DeserializePayload<PairRequest>(envelope);
        if (req is null)
        {
            await FrameIo.WriteAsync(stream, "pair_result",
                new PairResponse { Ok = false, Error = "Invalid pair payload" }, id: envelope.Id).ConfigureAwait(false);
            return null;
        }

        var requested = Enum.TryParse<PermissionLevel>(req.RequestedPermission, true, out var lvl)
            ? lvl
            : PermissionLevel.Assist;

        PermissionLevel granted;
        if (ownerLinkChannel && req.OwnerLink)
        {
            // 账号免密：本机已用账号在中继上线，自动授予 Assist（仍审计）
            granted = requested == PermissionLevel.Admin ? PermissionLevel.Assist : requested;
            if (granted == PermissionLevel.Guest)
                granted = PermissionLevel.Assist;
            _audit.Write("pair_owner_auto", $"client={req.ClientName}; remote={remote}; perm={granted}");
        }
        else
        {
            var code = SessionCodeGenerator.Normalize(req.SessionCode);
            string current;
            lock (_codeLock) current = _sessionCode;
            if (!string.Equals(code, current, StringComparison.Ordinal))
            {
                _audit.Write("pair_reject", $"bad_code from {remote}");
                await FrameIo.WriteAsync(stream, "pair_result",
                    new PairResponse { Ok = false, Error = "会话码错误" }, id: envelope.Id).ConfigureAwait(false);
                return null;
            }

            var pairInfo = new PairRequestInfo(
                RemoteEndpoint: remote,
                ClientName: req.ClientName,
                RequestedPermission: requested,
                TimeoutSeconds: _confirmTimeoutSeconds);
            var pairOutcome = await ConfirmPrompt.AskPairAsync(pairInfo, _autoYes, _headless).ConfigureAwait(false);

            if (!pairOutcome.Approved || pairOutcome.GrantedPermission is null)
            {
                var reason = pairOutcome.Decision == ConfirmDecision.TimedOut ? "pair_timeout" : "pair_reject";
                _audit.Write(reason, pairOutcome.AuditDetail(remote, req.ClientName, requested));
                var err = pairOutcome.Decision == ConfirmDecision.TimedOut
                    ? "配对确认超时，已自动拒绝"
                    : "对方拒绝了配对";
                await FrameIo.WriteAsync(stream, "pair_result",
                    new PairResponse { Ok = false, Error = err }, id: envelope.Id).ConfigureAwait(false);
                return null;
            }

            granted = pairOutcome.GrantedPermission.Value;
            _audit.Write("pair_ok", pairOutcome.AuditDetail(remote, req.ClientName, requested));
        }
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var expires = DateTimeOffset.UtcNow.Add(TokenTtl);
        var session = new SessionState
        {
            Token = token,
            Permission = granted,
            ExpiresAtUtc = expires,
            ClientName = req.ClientName,
            RemoteEndpoint = remote,
            PairedAtUtc = DateTimeOffset.UtcNow,
            ConnectionCts = connCts
        };
        _sessions[token] = session;
        if (!(ownerLinkChannel && req.OwnerLink))
        {
            // pair_ok already audited in confirm branch; owner path audited as pair_owner_auto
        }
        else
        {
            // already audited
        }
        PairedClientsChanged?.Invoke();

        await FrameIo.WriteAsync(stream, "pair_result", new PairResponse
        {
            Ok = true,
            Token = token,
            Permission = granted.ToString(),
            ExpiresAtUtc = expires,
            AgentName = Environment.MachineName
        }, id: envelope.Id).ConfigureAwait(false);

        Console.WriteLine($"[已配对] {req.ClientName} @ {remote} 权限={granted} token={token[..8]}…");
        return session;
    }

    private async Task HandleToolAsync(Stream stream, WireEnvelope envelope, SessionState? sessionHint)
    {
        var session = ResolveSession(envelope.Token, sessionHint);
        if (session is null)
        {
            await FrameIo.WriteAsync(stream, "tool_result",
                new ToolResponse { Ok = false, Error = "未授权或令牌过期，请重新配对" }, id: envelope.Id)
                .ConfigureAwait(false);
            return;
        }

        var req = FrameIo.DeserializePayload<ToolRequest>(envelope);
        if (req is null || !Enum.TryParse<ToolKind>(req.Tool, true, out var tool))
        {
            await FrameIo.WriteAsync(stream, "tool_result",
                new ToolResponse { Ok = false, Error = "未知工具" }, id: envelope.Id).ConfigureAwait(false);
            return;
        }

        if (!ToolCapabilities.IsAllowed(tool, session.Permission))
        {
            _audit.Write("tool_denied_perm", $"tool={tool}; perm={session.Permission}");
            await FrameIo.WriteAsync(stream, "tool_result",
                new ToolResponse { Ok = false, Error = $"当前权限 {session.Permission} 不允许 {tool}" },
                id: envelope.Id).ConfigureAwait(false);
            return;
        }

        var args = req.Arguments ?? new Dictionary<string, string?>();
        var source = $"{session.ClientName}";

        if (ToolCapabilities.RequiresConfirmation(tool, session.Permission))
        {
            var summary = BuildSummary(tool, args);
            var info = new ConfirmRequestInfo(source, tool.ToString(), summary, _confirmTimeoutSeconds);
            var outcome = await ConfirmPrompt.AskAsync(info, _autoYes, _headless).ConfigureAwait(false);
            if (!outcome.Approved)
            {
                _audit.Write("tool_denied_user", outcome.AuditDetail(tool.ToString(), summary, source));
                var err = outcome.Decision == ConfirmDecision.TimedOut
                    ? "确认超时，已自动拒绝"
                    : "用户拒绝执行";
                await FrameIo.WriteAsync(stream, "tool_result",
                    new ToolResponse { Ok = false, Error = err, DeniedByUser = true },
                    id: envelope.Id).ConfigureAwait(false);
                return;
            }
            _audit.Write("tool_confirmed", outcome.AuditDetail(tool.ToString(), summary, source));
        }
        else if (!ToolCapabilities.IsReadOnly(tool) && session.Permission == PermissionLevel.Admin)
        {
            _audit.Write("tool_admin_auto", $"tool={tool}; source={source}; {BuildSummary(tool, args)}");
        }

        try
        {
            var result = await Task.Run(() => Execute(tool, args)).ConfigureAwait(false);
            _audit.Write("tool_ok", $"tool={tool}; bytes={result.Length}");
            await FrameIo.WriteAsync(stream, "tool_result",
                new ToolResponse { Ok = true, Result = result }, id: envelope.Id).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _audit.Write("tool_error", $"tool={tool}; {ex.Message}");
            await FrameIo.WriteAsync(stream, "tool_result",
                new ToolResponse { Ok = false, Error = ex.Message }, id: envelope.Id).ConfigureAwait(false);
        }
    }

    private SessionState? ResolveSession(string? token, SessionState? hint)
    {
        if (!string.IsNullOrWhiteSpace(token) && _sessions.TryGetValue(token, out var s))
        {
            if (s.IsExpired)
            {
                _sessions.TryRemove(token, out _);
                PairedClientsChanged?.Invoke();
                return null;
            }
            return s;
        }
        if (hint is not null && !hint.IsExpired && _sessions.ContainsKey(hint.Token))
            return hint;
        return null;
    }

    private static string Execute(ToolKind tool, Dictionary<string, string?> args) => tool switch
    {
        ToolKind.ListProcesses => ToolExecutor.ListProcesses(),
        ToolKind.ReadLogTail => ToolExecutor.ReadLogTail(
            GetArg(args, "path"),
            int.TryParse(GetArg(args, "lines"), out var n) ? n : 80),
        ToolKind.ListDir => ToolExecutor.ListDir(GetArg(args, "path")),
        ToolKind.ReadFile => ToolExecutor.ReadFile(GetArg(args, "path")),
        ToolKind.RunCommand => ToolExecutor.RunCommand(GetArg(args, "command")),
        _ => throw new InvalidOperationException("unsupported tool")
    };

    private static string GetArg(Dictionary<string, string?> args, string key)
    {
        if (args.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            return v!;
        throw new ArgumentException($"缺少参数: {key}");
    }

    private static string BuildSummary(ToolKind tool, Dictionary<string, string?> args) =>
        tool switch
        {
            ToolKind.RunCommand => $"RunCommand: {GetArgOr(args, "command", "?")}",
            _ => $"{tool}: {JsonSerializer.Serialize(args)}"
        };

    private static string GetArgOr(Dictionary<string, string?> args, string key, string fallback) =>
        args.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v! : fallback;
}


