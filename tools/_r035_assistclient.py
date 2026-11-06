from pathlib import Path
p = Path("src/RemoteAssist.Protocol/AssistClient.cs")
t = p.read_text(encoding="utf-8")

# Replace class body start fields and constructor + add events/watch; replace Dispose and InvokeToolAsync failure

old_fields = """public sealed class AssistClient : IDisposable
{
    private readonly IDisposable _transport;
    private readonly Stream _stream;
    private string? _token;
    private PermissionLevel _permission;

    private AssistClient(IDisposable transport, Stream stream, string token, PermissionLevel permission)
    {
        _transport = transport;
        _stream = stream;
        _token = token;
        _permission = permission;
    }

    public string? Token => _token;
    public PermissionLevel Permission => _permission;
"""

new_fields = """public sealed class AssistClient : IDisposable
{
    private readonly IDisposable _transport;
    private readonly Stream _stream;
    private string? _token;
    private PermissionLevel _permission;
    private readonly CancellationTokenSource _watchCts = new();
    private int _connectionLostRaised;
    private int _disposed;

    private AssistClient(IDisposable transport, Stream stream, string token, PermissionLevel permission)
    {
        _transport = transport;
        _stream = stream;
        _token = token;
        _permission = permission;
        _ = Task.Run(() => WatchTransportAsync(_watchCts.Token));
    }

    public string? Token => _token;
    public PermissionLevel Permission => _permission;

    /// <summary>已配对后底层传输被对端关掉（对方结束协助 / 连接中断）。自己 Dispose 不会触发。</summary>
    public event Action? ConnectionLost;
"""

if "ConnectionLost" not in t:
    if old_fields not in t:
        raise SystemExit("AssistClient fields missing")
    t = t.replace(old_fields, new_fields, 1)

old_invoke_read = """        var response = await FrameIo.ReadAsync(_stream, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Connection closed during tool call.");
"""
new_invoke_read = """        FrameIo.Envelope? response;
        try
        {
            response = await FrameIo.ReadAsync(_stream, ct).ConfigureAwait(false);
        }
        catch (Exception)
        {
            RaiseConnectionLost();
            throw;
        }
        if (response is null)
        {
            RaiseConnectionLost();
            throw new InvalidOperationException("Connection closed during tool call.");
        }
"""
if "RaiseConnectionLost" not in t or "Connection closed during tool call" in t:
    if old_invoke_read in t:
        t = t.replace(old_invoke_read, new_invoke_read, 1)

old_dispose = """    public void Dispose()
    {
        try { _stream.Dispose(); } catch { /* ignore */ }
        try { _transport.Dispose(); } catch { /* ignore */ }
    }
}
"""
new_dispose = """    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        try { _watchCts.Cancel(); } catch { /* ignore */ }
        try { _watchCts.Dispose(); } catch { /* ignore */ }
        try { _stream.Dispose(); } catch { /* ignore */ }
        try { _transport.Dispose(); } catch { /* ignore */ }
    }

    private async Task WatchTransportAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(400, ct).ConfigureAwait(false);
                if (IsTransportClosed())
                {
                    RaiseConnectionLost();
                    return;
                }
            }
        }
        catch (OperationCanceledException) { /* dispose */ }
        catch
        {
            RaiseConnectionLost();
        }
    }

    private bool IsTransportClosed()
    {
        try
        {
            var sock = TryGetSocket();
            if (sock is null)
                return false;
            if (!sock.Connected)
                return true;
            // Poll readable + 无缓冲数据 ⇒ 对端已关
            return sock.Poll(0, SelectMode.SelectRead) && sock.Available == 0;
        }
        catch
        {
            return true;
        }
    }

    private Socket? TryGetSocket()
    {
        try
        {
            return _transport switch
            {
                TcpClient tcp => tcp.Client,
                RelayClient relay => relay.TcpSocket,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private void RaiseConnectionLost()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;
        if (Interlocked.Exchange(ref _connectionLostRaised, 1) != 0)
            return;
        try { ConnectionLost?.Invoke(); } catch { /* UI */ }
    }
}
"""
if "WatchTransportAsync" not in t:
    if old_dispose not in t:
        raise SystemExit("Dispose block missing")
    t = t.replace(old_dispose, new_dispose, 1)

# Ensure usings
if "using System.Net.Sockets;" not in t:
    t = "using System.Net.Sockets;\n" + t
if "using System.Threading;" not in t and "Interlocked" in t:
    # ImplicitUsings may cover it for net8
    pass

p.write_text(t, encoding="utf-8", newline="\n")
print("AssistClient OK")
