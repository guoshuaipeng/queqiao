using RemoteAssist.Protocol;
using RemoteAssist.Relay.Stub;

namespace RemoteAssist.Protocol.Tests;

public class ConnectDraftParserTests
{
    private const string SampleFp = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void Parse_FullPayload_FillsOptions()
    {
        var payload = PairQrPayload.Build("10.0.0.5", 19000, "UI001A", SampleFp);
        Assert.True(ConnectDraftParser.TryParse(
            payload, hostField: null, portField: null, fingerprintField: null,
            PermissionLevel.Assist, insecure: false,
            out var opts, out var preview, out _));
        Assert.Equal("10.0.0.5", opts.Host);
        Assert.Equal(19000, opts.Port);
        Assert.Equal("UI001A", opts.Code);
        Assert.Equal(SampleFp, opts.Fingerprint);
        Assert.False(opts.Insecure);
        Assert.Contains("10.0.0.5:19000", preview);
        Assert.Contains("TLS", preview);
    }

    [Fact]
    public void Parse_CodePlusHost_RequiresFpUnlessInsecure()
    {
        Assert.False(ConnectDraftParser.TryParse(
            "ABC123", "127.0.0.1", "17890", null,
            PermissionLevel.Guest, insecure: false,
            out _, out _, out var err));
        Assert.Contains("指纹", err);

        Assert.True(ConnectDraftParser.TryParse(
            "ABC123", "127.0.0.1", "17890", null,
            PermissionLevel.Guest, insecure: true,
            out var opts, out _, out _));
        Assert.True(opts.Insecure);
        Assert.Equal(PermissionLevel.Guest, opts.Permission);
    }

    [Fact]
    public void Parse_ManualFields_OverridePayload()
    {
        var payload = PairQrPayload.Build("10.0.0.1", 17890, "OV0010", SampleFp);
        Assert.True(ConnectDraftParser.TryParse(
            payload, hostField: "192.168.0.9", portField: "20001", fingerprintField: SampleFp,
            PermissionLevel.Admin, insecure: false,
            out var opts, out _, out _));
        Assert.Equal("192.168.0.9", opts.Host);
        Assert.Equal(20001, opts.Port);
        Assert.Equal("OV0010", opts.Code);
        Assert.Equal(PermissionLevel.Admin, opts.Permission);
    }

    [Fact]
    public void PeekPayload_ExtractsFields()
    {
        var payload = PairQrPayload.Build("172.16.1.2", 18181, "PEEK01", SampleFp);
        Assert.True(ConnectDraftParser.TryPeekPayload(payload, out var host, out var port, out var code, out var fp, out var tls));
        Assert.Equal("172.16.1.2", host);
        Assert.Equal(18181, port);
        Assert.Equal("PEEK01", code);
        Assert.Equal(SampleFp, fp);
        Assert.True(tls);
    }

    [Fact]
    public void FormatPreview_ShortensFingerprint()
    {
        var opts = new AssistConnectOptions
        {
            Host = "1.2.3.4",
            Port = 17890,
            Code = "ABCD12",
            Fingerprint = SampleFp,
            Insecure = false
        };
        var preview = ConnectDraftParser.FormatPreview(opts);
        Assert.Contains("1.2.3.4:17890", preview);
        Assert.Contains("01234567…", preview);
        Assert.DoesNotContain(SampleFp, preview);
    }
}

public class RelayEndpointTests
{
    [Theory]
    [InlineData("1.2.3.4:443", "1.2.3.4", 443)]
    [InlineData("relay.example.com:8443", "relay.example.com", 8443)]
    [InlineData("[2001:db8::1]:443", "2001:db8::1", 443)]
    public void Parse_Valid(string text, string host, int port)
    {
        Assert.True(RelayPlaceholder.TryParseEndpoint(text, out var h, out var p, out _));
        Assert.Equal(host, h);
        Assert.Equal(port, p);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.3.4")]
    [InlineData("1.2.3.4:99999")]
    [InlineData(":443")]
    public void Parse_Invalid(string text)
    {
        Assert.False(RelayPlaceholder.TryParseEndpoint(text, out _, out _, out var err));
        Assert.False(string.IsNullOrWhiteSpace(err));
    }

    [Fact]
    public void UpcomingHint_MentionsIp()
    {
        Assert.Contains("IP", RelayPlaceholder.UpcomingHint);
        Assert.Equal("NotImplemented_v1_LAN_only", RelayPlaceholder.Status);
    }
}
