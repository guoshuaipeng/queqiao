from pathlib import Path
p = Path("src/RemoteAssist.Protocol/AssistClient.cs")
t = p.read_text(encoding="utf-8")
t = t.replace("FrameIo.Envelope?", "WireEnvelope?")
old = """    public void Dispose()
    {
        try { _stream.Dispose(); } catch { /* ignore */ }
        try { _transport.Dispose(); } catch { /* ignore */ }
    }
}
"""
new = """    public void Dispose()
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
if old not in t:
    raise SystemExit("dispose missing")
t = t.replace(old, new, 1)
p.write_text(t, encoding="utf-8", newline="\n")
print("fixed")
