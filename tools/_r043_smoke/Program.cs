using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RemoteAssist.Protocol;

class Prog {
  static int Fail(string m) { Console.WriteLine("FAIL " + m); return 1; }
  static async Task<int> Main() {
    var zeros = 0;
    for (int i = 0; i < 200; i++) {
      var p = SessionPasswordGenerator.Generate();
      if (p.Length != 8) return Fail("len="+p.Length);
      foreach (var ch in p) if (ch < '0' || ch > '9') return Fail("non-digit "+p);
      if (p[0] == '0') zeros++;
    }
    Console.WriteLine("OK gen8 samples=200 leadingZeroHits=" + zeros);

    var cred = FriendConnectGuide.FormatShareClipboard("AB12CD", "00481273");
    if (cred != "AB12CD-00481273") return Fail("fmt="+cred);
    if (!FriendConnectGuide.TryParseShareText(cred, out var c1, out var p1) || c1 != "AB12CD" || p1 != "00481273")
      return Fail("parse cred");
    Console.WriteLine("OK cred parse " + cred);

    var legacy = "会话码：XY9K2M\r\n验证密码：3471";
    if (!FriendConnectGuide.TryParseShareText(legacy, out var c2, out var p2) || c2 != "XY9K2M" || p2 != "3471")
      return Fail("legacy");
    Console.WriteLine("OK legacy");

    if (ConnectDraftParser.TryBuildOptions("AB12", null, RelayEndpoint.DefaultEndpoint, true, null, null, null, false, out _, out _, out var err))
      return Fail("incomplete should fail");
    if (err == null || err.IndexOf("连接凭证不完整", StringComparison.Ordinal) < 0) return Fail("msg="+err);
    Console.WriteLine("OK incomplete msg");

    // Raw JSONL guest_host only (do not wait for paired)
    var pwd = "00481273";
    using var tcp = new TcpClient();
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await tcp.ConnectAsync("121.196.45.81", 17891);
    var stream = tcp.GetStream();
    var req = "{\"op\":\"guest_host\",\"password\":\"" + pwd + "\",\"code\":\"\"}\n";
    var bytes = Encoding.UTF8.GetBytes(req);
    await stream.WriteAsync(bytes, 0, bytes.Length, cts.Token);
    var buf = new byte[4096];
    var n = await stream.ReadAsync(buf, 0, buf.Length, cts.Token);
    var line = Encoding.UTF8.GetString(buf, 0, n).Trim();
    Console.WriteLine("guest_host reply: " + line);
    if (line.IndexOf("\"ok\":true", StringComparison.Ordinal) < 0 && line.IndexOf("\"ok\": true", StringComparison.Ordinal) < 0)
      return Fail("guest_host not ok");
    if (line.IndexOf("code", StringComparison.OrdinalIgnoreCase) < 0) return Fail("no code");
    Console.WriteLine("OK public relay accepts 8-digit numeric password (min4, no max)");

    // TryBuildOptions full credential -> ViaRelay
    if (!ConnectDraftParser.TryBuildOptions("ZZ9K2M-00481273", null, RelayEndpoint.DefaultEndpoint, true, null, null, null, false, out var opts, out var preview, out var e2))
      return Fail("build opts "+e2);
    if (!opts.ViaRelay || opts.Password != "00481273" || opts.Code != "ZZ9K2M") return Fail("opts bad");
    Console.WriteLine("OK TryBuildOptions preview=" + preview);
    Console.WriteLine("ALL_OK");
    return 0;
  }
}
