using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class ProposeChangeCopyTests
{
    [Fact]
    public void FourStates_ExactCopy_Locked()
    {
        Assert.Equal("正在等对方在本机确认…", ProposeChangeCopy.WaitingStatus);
        Assert.Equal("对方已允许，改动已执行", ProposeChangeCopy.AllowedStatus);
        Assert.Equal("已拒绝，未做任何改动", WriteConfirmCopy.DeniedMessage);
        Assert.Equal("确认超时，未做任何改动", WriteConfirmCopy.TimedOutMessage);
        Assert.Equal("已连上。建议先点「查看正在运行的程序」。", ProposeChangeCopy.ConnectedResultHint);
        Assert.DoesNotContain("RunCommand", ProposeChangeCopy.WaitingStatus);
        Assert.DoesNotContain("PAIR_OK", ProposeChangeCopy.ConnectedResultHint);
        Assert.DoesNotContain("已提议一项改动", ProposeChangeCopy.AllowedStatus);
    }

    [Fact]
    public void FailureStatus_DeniedByUser_PrefersNonEmptyError_TimeoutNotCoveredAsDenied()
    {
        Assert.Equal(
            WriteConfirmCopy.TimedOutMessage,
            ProposeChangeCopy.FailureStatus(deniedByUser: true, error: WriteConfirmCopy.TimedOutMessage));
        Assert.Equal(
            WriteConfirmCopy.DeniedMessage,
            ProposeChangeCopy.FailureStatus(deniedByUser: true, error: WriteConfirmCopy.DeniedMessage));
        Assert.Equal(
            WriteConfirmCopy.DeniedMessage,
            ProposeChangeCopy.FailureStatus(deniedByUser: true, error: null));
        Assert.Equal(
            WriteConfirmCopy.DeniedMessage,
            ProposeChangeCopy.FailureStatus(deniedByUser: true, error: "  "));
        Assert.Equal("网络中断", ProposeChangeCopy.FailureStatus(deniedByUser: false, error: "网络中断"));
        Assert.Equal("失败", ProposeChangeCopy.FailureStatus(deniedByUser: false, error: null));
    }

    [Fact]
    public void WaitingOnlyForPropose_NotReadonlyTools()
    {
        Assert.True(ProposeChangeCopy.UsesWaitingStatus(ToolKind.RunCommand));
        Assert.False(ProposeChangeCopy.UsesWaitingStatus(ToolKind.ListProcesses));
        Assert.False(ProposeChangeCopy.UsesWaitingStatus(ToolKind.ListDir));
        Assert.False(ProposeChangeCopy.UsesWaitingStatus(ToolKind.ReadFile));
        Assert.False(ProposeChangeCopy.UsesWaitingStatus(ToolKind.ReadLogTail));
        Assert.Equal(ProposeChangeCopy.AllowedStatus, ProposeChangeCopy.AllowedStatusFor(ToolKind.RunCommand));
    }

    [Fact]
    public void R035_R037_R039_Unchanged()
    {
        Assert.Equal("对方已结束这次协助。", HelperSessionCopy.StatusPeerEndedAssist);
        Assert.Equal("已连接对方电脑", HelperSessionCopy.StatusConnected);
        Assert.Equal("状态：未连接", HelperSessionCopy.StatusDisconnectedSelf);
        Assert.Equal("连接失败", HelperSessionCopy.StatusConnectFailed);
        Assert.Equal("已连接", HelperSessionCopy.ConnectedButtonText);
        Assert.Equal("连接", HelperSessionCopy.ConnectButtonText);
        Assert.Equal("粘贴后点「连接」", HelperSessionCopy.PasteThenConnectHint);
        Assert.Equal(5, HelperSessionCopy.CommonToolButtonLabels.Length);
        Assert.True(HelperSessionCopy.ShowCommonTools(true));
    }
}
