using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class LanAddressSelectorTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.1.2.3")]
    [InlineData("169.254.10.20")]
    [InlineData("198.18.0.1")]
    [InlineData("198.19.255.1")]
    [InlineData("0.0.0.0")]
    [InlineData("")]
    [InlineData("not-an-ip")]
    public void IsExcluded_FiltersVirtualAndLoopback(string ip)
    {
        Assert.True(LanAddressSelector.IsExcluded(ip));
    }

    [Theory]
    [InlineData("192.168.1.10")]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.1")]
    public void IsExcluded_AllowsPrivateLan(string ip)
    {
        Assert.False(LanAddressSelector.IsExcluded(ip));
    }

    [Fact]
    public void Prefer_OrdersPrivateRanges_AndDropsVirtual()
    {
        var preferred = LanAddressSelector.Prefer(new[]
        {
            "198.18.0.1",
            "10.0.0.8",
            "169.254.1.1",
            "192.168.1.20",
            "172.16.0.3",
            "8.8.8.8"
        });

        Assert.Equal(new[] { "192.168.1.20", "10.0.0.8", "172.16.0.3", "8.8.8.8" }, preferred);
    }

    [Fact]
    public void Prefer_Empty_FallsBackToLoopback()
    {
        var preferred = LanAddressSelector.Prefer(new[] { "198.18.0.1", "169.254.1.1" });
        Assert.Equal(new[] { "127.0.0.1" }, preferred);
    }

    [Fact]
    public void PickDefault_Prefers192168()
    {
        Assert.Equal("192.168.0.2", LanAddressSelector.PickDefault(new[] { "10.1.1.1", "192.168.0.2" }));
    }

    [Fact]
    public void EnumerateLocalIPv4_DoesNotThrow()
    {
        var list = LanAddressSelector.EnumerateLocalIPv4();
        Assert.NotEmpty(list);
        Assert.All(list, ip => Assert.False(LanAddressSelector.IsExcluded(ip) && ip != "127.0.0.1"));
    }
}

public class PairTargetResolverTests
{
    private const string SampleFp = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void Resolve_HostAndCode()
    {
        Assert.True(PairTargetResolver.TryResolve(
            new[] { "192.168.1.5", "ab-c123" }, null, 17890, out var r, out _));
        Assert.Equal("192.168.1.5", r.Host);
        Assert.Equal(17890, r.Port);
        Assert.Equal("ABC123", r.Code);
        Assert.Null(r.Fingerprint);
        Assert.True(r.UseTls);
    }

    [Fact]
    public void Resolve_FullPayloadAsPositional()
    {
        var payload = PairQrPayload.Build("10.0.0.9", 19000, "ZZZZ01", SampleFp);
        Assert.True(PairTargetResolver.TryResolve(new[] { payload }, null, 17890, out var r, out _));
        Assert.Equal("10.0.0.9", r.Host);
        Assert.Equal(19000, r.Port);
        Assert.Equal("ZZZZ01", r.Code);
        Assert.Equal(SampleFp, r.Fingerprint);
        Assert.True(r.UseTls);
    }

    [Fact]
    public void Resolve_ExplicitPayload_WinsOverPositional()
    {
        var payload = PairQrPayload.Build("10.0.0.1", 17890, "AAAA11", SampleFp);
        Assert.True(PairTargetResolver.TryResolve(
            new[] { "ignored", "BBBB22" }, payload, 17890, out var r, out _));
        Assert.Equal("10.0.0.1", r.Host);
        Assert.Equal("AAAA11", r.Code);
        Assert.Equal(SampleFp, r.Fingerprint);
    }

    [Fact]
    public void Resolve_FailsWithoutArgs()
    {
        Assert.False(PairTargetResolver.TryResolve(Array.Empty<string>(), null, 17890, out _, out var err));
        Assert.Contains("用法", err);
    }
}
