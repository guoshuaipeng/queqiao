using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class PairQrPayloadTests
{
    private const string SampleFp = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void Build_ContainsHostPortCodeTlsFp()
    {
        var payload = PairQrPayload.Build("192.168.1.10", 17890, "ab-c123", SampleFp, useTls: true);
        Assert.StartsWith("remoteassist:pair?", payload);
        Assert.Contains("host=192.168.1.10", payload);
        Assert.Contains("port=17890", payload);
        Assert.Contains("code=ABC123", payload);
        Assert.Contains("v=1", payload);
        Assert.Contains("tls=1", payload);
        Assert.Contains($"fp={SampleFp}", payload);
    }

    [Fact]
    public void Build_Insecure_OmitsFp()
    {
        var payload = PairQrPayload.Build("10.0.0.1", 17890, "XYZ789", fingerprint: null, useTls: false);
        Assert.Contains("tls=0", payload);
        Assert.DoesNotContain("fp=", payload);
    }

    [Fact]
    public void Build_TlsWithoutFp_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PairQrPayload.Build("10.0.0.1", 17890, "XYZ789", fingerprint: null, useTls: true));
    }

    [Fact]
    public void TryParse_RoundTrips_WithFingerprint()
    {
        var payload = PairQrPayload.Build("10.0.0.2", 17890, "XYZ789", SampleFp);
        Assert.True(PairQrPayload.TryParse(payload, out var host, out var port, out var code, out var fp, out var tls));
        Assert.Equal("10.0.0.2", host);
        Assert.Equal(17890, port);
        Assert.Equal("XYZ789", code);
        Assert.Equal(SampleFp, fp);
        Assert.True(tls);
    }

    [Fact]
    public void TryParse_InsecurePayload()
    {
        var payload = PairQrPayload.Build("192.168.0.1", 17890, "HELLO1", useTls: false);
        Assert.True(PairQrPayload.TryParse(payload, out _, out _, out _, out var fp, out var tls));
        Assert.False(tls);
        Assert.Null(fp);
    }

    [Fact]
    public void TryParse_NormalizesColonFingerprint()
    {
        var colon = string.Join(':', Enumerable.Range(0, 32).Select(i => SampleFp.Substring(i * 2, 2)));
        var payload = $"remoteassist:pair?v=1&host=1.2.3.4&port=17890&code=ABC123&tls=1&fp={Uri.EscapeDataString(colon)}";
        Assert.True(PairQrPayload.TryParse(payload, out _, out _, out _, out var fp, out var tls));
        Assert.True(tls);
        Assert.Equal(SampleFp, fp);
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
        var payload = "  " + PairQrPayload.Build("192.168.0.1", 17890, "HELLO1", SampleFp) + "\r\n";
        Assert.True(PairQrPayload.TryParse(payload, out var host, out var port, out var code));
        Assert.Equal("192.168.0.1", host);
        Assert.Equal(17890, port);
        Assert.Equal("HELLO1", code);
    }
}
