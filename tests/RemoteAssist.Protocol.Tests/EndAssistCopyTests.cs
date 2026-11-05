using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class EndAssistCopyTests
{
    [Fact]
    public void ConfirmCopy_ExactProductWording()
    {
        Assert.Equal("结束这次协助", EndAssistCopy.ButtonText);
        Assert.Equal("结束后对方将立刻连不上。", EndAssistCopy.ConfirmBody);
        Assert.Equal("结束这次协助", EndAssistCopy.ConfirmOk);
        Assert.Equal("取消", EndAssistCopy.ConfirmCancel);
        Assert.Equal(EndAssistCopy.ButtonText, EndAssistCopy.ConfirmOk);
    }

    [Fact]
    public void ConfirmCopy_NoTechnicalLeak()
    {
        Assert.DoesNotContain("17890", EndAssistCopy.ConfirmBody);
        Assert.DoesNotContain("token", EndAssistCopy.ConfirmBody);
        Assert.DoesNotContain("Stop", EndAssistCopy.ConfirmBody);
    }
}
