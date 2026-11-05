using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
    private readonly AuditLog _audit;
    private readonly bool _autoYes;
    private readonly bool _headless;
    private readonly int _confirmTimeoutSeconds;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public int Port { get; }
    public string AuditPath { get; }
    public IReadOnlyList<string> LanIps { get; }

    public event Action? SessionChanged;

    public AgentHost(
        int port,
        bool autoYes,
        bool headless,
        int confirmTimeoutSeconds,
        AuditLog audit,
        string auditPath)
    {
        Port = port;
        _autoYes = autoYes;
        _headless = headless;
        _confirmTimeoutSeconds = Math.Clamp(confirmTimeoutSeconds, 5, 600);
        _audit = audit;
        AuditPath = auditPath;
        LanIps = GetLanIPv4();
        _sessionCode = SessionCodeGenerator.Generate(6);
    }

    public string SessionCode
    {
        get { lock (_codeLock) return _sessionCode; }
    }

    public string PrimaryHost => LanIps.FirstOrDefault() ?? "127.0.0.1";

    public string QrPayload
    {
        get
        {
            lock (_codeLock)
                return PairQrPayload.Build(PrimaryHost, Port, _sessionCode);
        }
    }

    public string RefreshSessionCode()
    {
        string code;
        lock (_codeLock)
        {
            _sessionCode = SessionCodeGenerator.Generate(6);
            code = _sessionCode;
        }

        foreach (var key in _sessions.Keys)
            _sessions.TryRemove(key, out _);

        _audit.Write("session_refresh", $"code={code}");
        Console.WriteLine($"[会话码已刷新] {code}");
        SessionChanged?.Invoke();
        return code;
    }

    public void PrintBanner()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  RemoteAssist Agent (局域网 v1)");
        Console.WriteLine("========================================");
        Console.WriteLine($"监听端口 : {Port}");
        Console.WriteLine($"本机地址 : {string.Join(", ", LanIps)}");
        Console.WriteLine($"会话码   : {SessionCode}");
        Console.WriteLine($"二维码载荷: {QrPayload}");
        Console.WriteLine($"审计日志 : {AuditPath}");
        Console.WriteLine($"确认方式 : {(_autoYes ? "自动(--yes)" : _headless ? "控制台(--headless)" : "本机窗口+托盘(默认)")}");
        Console.WriteLine($"确认超时 : {_confirmTimeoutSeconds}s");
        Console.WriteLine();
        Console.WriteLine("协助端示例（手动输入会话码）:");
        Console.WriteLine($"  dotnet run --project src/RemoteAssist.Helper -- pair {PrimaryHost} {SessionCode}");
        Console.WriteLine();
        if (_autoYes)
            Console.WriteLine("自动确认已开启 (--yes)，仅用于本地联调。");
        else if (_headless)
            Console.WriteLine("无 UI 模式：不启托盘/窗体；配对与写操作在本控制台确认。");
        else
            Console.WriteLine("默认托盘常驻：会话信息窗可关到托盘；配对/写操作弹确认窗。可用 --headless 回退。");
        Console.WriteLine(_headless || _autoYes ? "按 Ctrl+C 退出。" : "托盘菜单「退出」或 Ctrl+C 结束。");
        Console.WriteLine("----------------------------------------");
    }

    public void Start()
    {
        if (_listener is not null)
            return;

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        _audit.Write("agent_start",
            $"port={Port}; code={SessionCode}; confirm={(_autoYes ? "auto" : _headless ? "console" : "ui-tray")}; timeout={_confirmTimeoutSeconds}s");
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _listener?.Stop(); } catch { /* ignore */ }
        _listener = null;
        _audit.Write("agent_stop", "listener stopped");
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
        _audit.Write("client_connect", remote);
        SessionState? session = null;

        try
        {
            using (client)
            {
                var stream = client.GetStream();
                while (true)
                {
                    var envelope = await FrameIo.ReadAsync(stream).ConfigureAwait(false);
                    if (envelope is null)
                        break;

                    switch (envelope.Type.ToLowerInvariant())
                    {
                        case "pair":
                            session = await HandlePairAsync(stream, envelope, remote).ConfigureAwait(false);
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
        }
        catch (Exception ex)
        {
            _audit.Write("client_error", $"{remote}; {ex.Message}");
        }
        finally
        {
            _audit.Write("client_disconnect", remote);
        }
    }

    private async Task<SessionState?> HandlePairAsync(NetworkStream stream, WireEnvelope envelope, string remote)
    {
        var req = FrameIo.DeserializePayload<PairRequest>(envelope);
        if (req is null)
        {
            await FrameIo.WriteAsync(stream, "pair_result",
                new PairResponse { Ok = false, Error = "Invalid pair payload" }, id: envelope.Id).ConfigureAwait(false);
            return null;
        }

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

        var requested = Enum.TryParse<PermissionLevel>(req.RequestedPermission, true, out var lvl)
            ? lvl
            : PermissionLevel.Assist;

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

        var granted = pairOutcome.GrantedPermission.Value;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var expires = DateTimeOffset.UtcNow.Add(TokenTtl);
        var session = new SessionState
        {
            Token = token,
            Permission = granted,
            ExpiresAtUtc = expires,
            ClientName = req.ClientName
        };
        _sessions[token] = session;
        _audit.Write("pair_ok", pairOutcome.AuditDetail(remote, req.ClientName, requested));

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

    private async Task HandleToolAsync(NetworkStream stream, WireEnvelope envelope, SessionState? sessionHint)
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
                return null;
            }
            return s;
        }
        if (hint is not null && !hint.IsExpired)
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

    internal static List<string> GetLanIPv4()
    {
        try
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.ToString())
                .Where(a => !a.StartsWith("127."))
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }
}
