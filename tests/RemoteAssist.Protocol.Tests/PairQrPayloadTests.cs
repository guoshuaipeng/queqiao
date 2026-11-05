using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class PairQrPayloadTests
{
    [Fact]
    public void Build_ContainsHostPortCode()
    {
        var payload = PairQrPayload.Build("192.168.1.10", 17890, "ab-c123");
        Assert.StartsWith("remoteassist:pair?", payload);
        Assert.Contains("host=192.168.1.10", payload);
        Assert.Contains("port=17890", payload);
        Assert.Contains("code=ABC123", payload);
        Assert.Contains("v=1", payload);
    }

    [Fact]
    public void TryParse_RoundTrips()
    {
        var payload = PairQrPayload.Build("10.0.0.2", 17890, "XYZ789");
        Assert.True(PairQrPayload.TryParse(payload, out var host, out var port, out var code));
        Assert.Equal("10.0.0.2", host);
        Assert.Equal(17890, port);
        Assert.Equal("XYZ789", code);
    }

    [Fact]
    public void TryParse_RejectsGarbage()
    {
        Assert.False(PairQrPayload.TryParse("https://example.com", out _, out _, out _));
        Assert.False(PairQrPayload.TryParse("remoteassist:pair?", out _, out _, out _));
    }

    [Fact]
    public void TryParse_TrimsWhitespace()
    {
        var payload = "  " + PairQrPayload.Build("192.168.0.1", 17890, "HELLO1") + "\r\n";
        Assert.True(PairQrPayload.TryParse(payload, out var host, out var port, out var code));
        Assert.Equal("192.168.0.1", host);
        Assert.Equal(17890, port);
        Assert.Equal("HELLO1", code);
    }
}
