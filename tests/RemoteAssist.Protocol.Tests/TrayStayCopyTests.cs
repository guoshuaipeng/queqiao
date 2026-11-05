using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class TrayStayCopyTests
{
    [Fact]
    public void Balloon_UsesExistingCopy_NoThirdPhrase()
    {
        Assert.Equal(
            "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。",
            TrayStayCopy.BalloonDefault);
        Assert.Equal(
            "被控模式已启动。可返回首页切换为控制端；关闭主窗口会留在托盘。",
            TrayStayCopy.BalloonWithReturnHome);
        Assert.Equal(TrayStayCopy.BalloonDefault, TrayStayCopy.BalloonFor(false));
        Assert.Equal(TrayStayCopy.BalloonWithReturnHome, TrayStayCopy.BalloonFor(true));
        Assert.DoesNotContain("就绪。关闭窗口后会继续在托盘运行。", TrayStayCopy.BalloonDefault);
        Assert.DoesNotContain("就绪。关闭窗口后会继续在托盘运行。", TrayStayCopy.BalloonWithReturnHome);
    }

    [Fact]
    public void HiddenToTrayFeedback_IsOneShotCopy()
    {
        Assert.Equal(
            "已隐藏到托盘。双击托盘图标可重新打开。",
            TrayStayCopy.HiddenToTrayFeedback);
        Assert.NotEqual(TrayStayCopy.HiddenToTrayFeedback, TrayStayCopy.RemovedDefaultStatusBar);
        Assert.Contains("托盘", TrayStayCopy.HiddenToTrayFeedback);
    }

    [Fact]
    public void RemovedDefaultStatusBar_IsExactFormerPhrase()
    {
        Assert.Equal("就绪。关闭窗口后会继续在托盘运行。", TrayStayCopy.RemovedDefaultStatusBar);
    }
}
