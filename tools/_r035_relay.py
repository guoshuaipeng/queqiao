from pathlib import Path
p = Path("src/RemoteAssist.Protocol/RelayClient.cs")
t = p.read_text(encoding="utf-8")
old = "    public Stream Stream => _stream;\n"
new = """    public Stream Stream => _stream;

    /// <summary>底层 TCP，供协助端探测对端是否已关掉会话（R035）。</summary>
    public System.Net.Sockets.Socket? TcpSocket
    {
        get
        {
            try { return _tcp.Client; }
            catch { return null; }
        }
    }

"""
if "TcpSocket" not in t:
    if old not in t:
        raise SystemExit("RelayClient Stream anchor missing")
    t = t.replace(old, new, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("RelayClient OK")
else:
    print("RelayClient already")
