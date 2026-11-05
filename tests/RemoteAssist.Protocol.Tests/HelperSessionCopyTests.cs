using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class HelperSessionCopyTests
{
    [Fact]
    public void PeerEnded_ExactCopy_NotConnectFailed()
    {
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.NotEqual(HelperSessionCopy.StatusConnectFailed, HelperSessionCopy.StatusPeerEndedAssist);
        Assert.DoesNotContain("连接失败", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.DoesNotContain("失败", HelperSessionCopy.StatusPeerEndedAssist);
    }

    [Fact]
    public void ThreeScenarios_StatusBranches()
    {
        Assert.Equal(
            HelperSessionCopy.StatusPeerEndedAssist,
            HelperSessionCopy.StatusAfterConnectedSessionEnd(selfInitiatedDisconnect: false));
        Assert.Equal(
            HelperSessionCopy.StatusDisconnectedSelf,
            HelperSessionCopy.StatusAfterConnectedSessionEnd(selfInitiatedDisconnect: true));
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }

    [Fact]
    public void ConnectFailHints_R003_Unchanged()
    {
        Assert.Equal(
            "对方已刷新会话，旧码失效了。请对方重新复制并发给你。",
            FriendConnectGuide.FailStaleCode);
        Assert.Equal(
            "对方还没打开「接受协助」。请对方在首页选择「接受协助」，再把码和密码发给你。",
            FriendConnectGuide.FailPeerNotAccepting);
        Assert.NotEqual(FriendConnectGuide.FailStaleCode, HelperSessionCopy.StatusPeerEndedAssist);
        Assert.NotEqual(FriendConnectGuide.FailPeerNotAccepting, HelperSessionCopy.StatusPeerEndedAssist);
    }

    [Fact]
    public void CommonTools_HiddenWhenNotConnected()
    {
        Assert.True(HelperSessionCopy.ShowCommonTools(connected: true));
        Assert.False(HelperSessionCopy.ShowCommonTools(connected: false));
        Assert.False(HelperSessionCopy.ShowCommonTools(
            connected: HelperSessionCopy.ShowCommonTools(false))); // after peer end → false
    }

    [Fact]
    public void SessionConnectButton_ConnectedLabel_NotSolidConnect()
    {
        Assert.Equal("连接", HelperSessionCopy.ConnectButtonText);
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("已连接", HelperSessionCopy.SessionConnectButtonLabel(connected: true));
        Assert.Equal("连接", HelperSessionCopy.SessionConnectButtonLabel(connected: false));
        Assert.NotEqual(HelperSessionCopy.ConnectButtonText, HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
    }
}
