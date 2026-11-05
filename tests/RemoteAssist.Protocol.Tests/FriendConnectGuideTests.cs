using System.Net.Sockets;
using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class FriendConnectGuideTests
{
    [Theory]
    [InlineData("invalid code or password", FriendConnectFailKind.WrongPassword)]
    [InlineData("guest session expired", FriendConnectFailKind.StaleCode)]
    [InlineData("peer not online", FriendConnectFailKind.PeerNotAccepting)]
    [InlineData("wait timeout", FriendConnectFailKind.PeerNotAccepting)]
    public void Classify_RelayErrors(string err, FriendConnectFailKind kind)
    {
        Assert.Equal(kind, FriendConnectGuide.Classify(err));
        Assert.False(FriendConnectGuide.LooksTechnicalLeak(FriendConnectGuide.MessageFor(kind)));
        Assert.DoesNotContain("17890", FriendConnectGuide.MessageFor(kind));
        Assert.DoesNotContain("17891", FriendConnectGuide.MessageFor(kind));
        Assert.DoesNotContain("放行入站", FriendConnectGuide.MessageFor(kind));
    }

    [Fact]
    public void Classify_Socket_IsUnreachable()
    {
        var ex = new SocketException(10061);
        Assert.Equal(FriendConnectFailKind.RelayUnreachable, FriendConnectGuide.Classify(ex));
        Assert.Equal(FriendConnectGuide.FailRelayUnreachable, FriendConnectGuide.FriendlyOrRaw(ex));
    }

    [Fact]
    public void TryParseShareText_TwoLines()
    {
        var text = "会话码：AB12CD\n验证密码：xyzw";
        Assert.True(FriendConnectGuide.TryParseShareText(text, out var code, out var pwd));
        Assert.Equal("AB12CD", code);
        Assert.Equal("XYZW", pwd);
    }

    [Fact]
    public void TryParseShareText_RejectsPayload()
    {
        Assert.False(FriendConnectGuide.TryParseShareText("remoteassist:pair?h=1&p=2&c=ABC", out _, out _));
    }

    [Fact]
    public void FormatShareClipboard_RoundTrip()
    {
        var clip = FriendConnectGuide.FormatShareClipboard("QQ11WW", "abcd");
        Assert.Contains("会话码：", clip);
        Assert.Contains("验证密码：", clip);
        Assert.True(FriendConnectGuide.TryParseShareText(clip, out var code, out var pwd));
        Assert.Equal("QQ11WW", code);
        Assert.False(string.IsNullOrWhiteSpace(pwd));
    }

    [Fact]
    public void FriendPreview_NoPortsOrRelayIp()
    {
        var p = FriendConnectGuide.FriendConnectionPreview("ZZ99AA");
        Assert.Contains("ZZ99AA", p);
        Assert.DoesNotContain("17890", p);
        Assert.DoesNotContain("17891", p);
        Assert.DoesNotContain("121.196", p);
        Assert.DoesNotContain("remoteassist", p);
    }

    [Fact]
    public void MomentA_Copy_NoPorts_AndPlainSetting()
    {
        Assert.Contains("RemoteAssist.Agent", FriendConnectGuide.MomentAAllow);
        Assert.DoesNotContain("17890", FriendConnectGuide.MomentAAllow);
        Assert.DoesNotContain("放行入站", FriendConnectGuide.MomentAAllow);
        Assert.Equal("打开防火墙设置", FriendConnectGuide.OpenFirewallButtonText);
        Assert.Contains("系统设置", FriendConnectGuide.MomentAAllow);
        Assert.Contains('\n', FriendConnectGuide.MomentAAllow);
        // 禁止零宽/BOM：用 Ordinal IndexOf（文化敏感 Contains 会把 U+2060 当成可忽略字符误报）
        Assert.Equal(-1, FriendConnectGuide.MomentAAllow.IndexOf('\u2060'));
        Assert.Equal(-1, FriendConnectGuide.MomentAAllow.IndexOf('\uFEFF'));
        Assert.Equal(-1, FriendConnectGuide.OpenFirewallButtonText.IndexOf('\u2060'));
        Assert.Equal(-1, FriendConnectGuide.OpenFirewallButtonText.IndexOf('\uFEFF'));
    }
}
