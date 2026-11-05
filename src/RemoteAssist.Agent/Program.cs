using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Agent;

internal static class Program
{
    private const int DefaultPort = 17890;
    private const int DefaultConfirmTimeoutSeconds = 60;
    private static readonly TimeSpan TokenTtl = TimeSpan.FromHours(2);

    private static string _sessionCode = "";
    private static readonly ConcurrentDictionary<string, SessionState> Sessions = new();
    private static readonly ConcurrentDictionary<string, PendingConfirmation> Pending = new();
    private static AuditLog _audit = null!;
    private static bool _autoYes;
    private static bool _headless;
    private static int _confirmTimeoutSeconds = DefaultConfirmTimeoutSeconds;

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var port = DefaultPort;
        _autoYes = args.Any(a => a is "--yes" or "--auto-yes");
        _headless = args.Any(a => a is "--headless");
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                port = p;
            if (args[i] is "--confirm-timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t))
                _confirmTimeoutSeconds = Math.Clamp(t, 5, 600);
        }

        var auditPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteAssist", "audit.log");
        _audit = new AuditLog(auditPath);

        _sessionCode = SessionCodeGenerator.Generate(6);

        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        var localIps = GetLanIPv4();
        Console.WriteLine("========================================");
        Console.WriteLine("  RemoteAssist Agent (局域网 v1)");
        Console.WriteLine("========================================");
        Console.WriteLine($"监听端口 : {port}");
        Console.WriteLine($"本机地址 : {string.Join(", ", localIps)}");
        Console.WriteLine($"会话码   : {_sessionCode}");
        Console.WriteLine($"审计日志 : {auditPath}");
        Console.WriteLine($"确认方式 : {(_autoYes ? "自动(--yes)" : _headless ? "控制台(--headless)" : "本机窗口(默认，配对+写操作)")}");
        Console.WriteLine($"确认超时 : {_confirmTimeoutSeconds}s");
        Console.WriteLine();
        Console.WriteLine("协助端示例:");
        var tipHost = localIps.FirstOrDefault() ?? "127.0.0.1";
        Console.WriteLine($"  dotnet run --project src/RemoteAssist.Helper -- pair {tipHost} {_sessionCode}");
        Console.WriteLine();
        if (_autoYes)
            Console.WriteLine("自动确认已开启 (--yes)，仅用于本地联调。");
        else if (_headless)
            Console.WriteLine("无 UI 模式：配对与写操作均在本控制台确认（超时自动拒绝）。");
        else
            Console.WriteLine("配对与写操作(Assist)会弹出本机确认窗口（串行排队）；可用 --headless 回退控制台。");
        Console.WriteLine("按 Ctrl+C 退出。");
        Console.WriteLine("----------------------------------------");

        _audit.Write("agent_start", $"port={port}; code={_sessionCode}; confirm={(_autoYes ? "auto" : _headless ? "console" : "ui")}; timeout={_confirmTimeoutSeconds}s");

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            listener.Stop();
        };

        try
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client));
            }
        }
        finally
        {
            listener.Stop();
            _audit.Write("agent_stop", "listener stopped");
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
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

    private static async Task<SessionState?> HandlePairAsync(NetworkStream stream, WireEnvelope envelope, string remote)
    {
        var req = FrameIo.DeserializePayload<PairRequest>(envelope);
        if (req is null)
        {
            await FrameIo.WriteAsync(stream, "pair_result",
                new PairResponse { Ok = false, Error = "Invalid pair payload" }, id: envelope.Id).ConfigureAwait(false);
            return null;
        }

        var code = SessionCodeGenerator.Normalize(req.SessionCode);
        if (!string.Equals(code, _sessionCode, StringComparison.Ordinal))
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
        Sessions[token] = session;
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

    private static async Task HandleToolAsync(NetworkStream stream, WireEnvelope envelope, SessionState? sessionHint)
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

    private static SessionState? ResolveSession(string? token, SessionState? hint)
    {
        if (!string.IsNullOrWhiteSpace(token) && Sessions.TryGetValue(token, out var s))
        {
            if (s.IsExpired)
            {
                Sessions.TryRemove(token, out _);
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

    private static string BuildSummary(ToolKind tool, Dictionary<string, string?> args)
    {
        return tool switch
        {
            ToolKind.RunCommand => $"RunCommand: {GetArgOr(args, "command", "?")}",
            _ => $"{tool}: {JsonSerializer.Serialize(args)}"
        };
    }

    private static string GetArgOr(Dictionary<string, string?> args, string key, string fallback) =>
        args.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v! : fallback;

    private static List<string> GetLanIPv4()
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