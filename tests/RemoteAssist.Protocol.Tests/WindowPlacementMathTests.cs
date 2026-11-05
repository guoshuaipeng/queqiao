using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class WindowPlacementMathTests
{
    private static readonly RectI Primary = new(0, 0, 1920, 1040);
    private static readonly RectI Secondary = new(1920, 0, 1600, 900);

    [Fact]
    public void Resolve_NoPreference_CentersOnPrimary()
    {
        var r = WindowPlacementMath.Resolve(
            preferred: null,
            workingAreas: new[] { Primary },
            primaryWorking: Primary,
            defaultWidth: 560,
            defaultHeight: 420,
            restoreSize: false);

        Assert.Equal(560, r.Width);
        Assert.Equal(420, r.Height);
        Assert.Equal(Primary.X + (Primary.Width - 560) / 2, r.X);
        Assert.Equal(Primary.Y + (Primary.Height - 420) / 2, r.Y);
    }

    [Fact]
    public void Resolve_SavedOnSecondary_RestoresThere()
    {
        var preferred = new RectI(2000, 100, 760, 640);
        var r = WindowPlacementMath.Resolve(
            preferred,
            new[] { Primary, Secondary },
            Primary,
            defaultWidth: 760,
            defaultHeight: 640,
            restoreSize: false);

        Assert.Equal(2000, r.X);
        Assert.Equal(100, r.Y);
        Assert.Equal(760, r.Width);
        Assert.Equal(640, r.Height);
    }

    [Fact]
    public void Resolve_MonitorGone_FallsBackToPrimaryVisible()
    {
        // 曾在第二屏，现在只剩主屏
        var preferred = new RectI(2200, 80, 760, 640);
        var r = WindowPlacementMath.Resolve(
            preferred,
            new[] { Primary },
            Primary,
            defaultWidth: 760,
            defaultHeight: 640,
            restoreSize: false);

        Assert.True(WindowPlacementMath.IsSufficientlyVisible(r, new[] { Primary }));
        Assert.True(r.X >= Primary.X);
        Assert.True(r.Right <= Primary.Right + 1);
    }

    [Fact]
    public void IsSufficientlyVisible_CompletelyOffScreen_False()
    {
        var off = new RectI(-5000, -5000, 800, 600);
        Assert.False(WindowPlacementMath.IsSufficientlyVisible(off, new[] { Primary }));
    }

    [Fact]
    public void CenterDialog_FollowsHostScreen()
    {
        var host = new RectI(2100, 50, 780, 860);
        var dlg = WindowPlacementMath.CenterDialogOnScreen(
            520, 280, host, new[] { Primary, Secondary }, Primary);

        Assert.True(dlg.X >= Secondary.X);
        Assert.True(dlg.Right <= Secondary.Right + 1);
        Assert.True(WindowPlacementMath.IsSufficientlyVisible(dlg, new[] { Secondary }));
    }

    [Fact]
    public void HelperUiSettings_RoundTripsWindowPlacement()
    {
        var dir = Path.Combine(Path.GetTempPath(), "RemoteAssistTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var s = new HelperUiSettings
            {
                WindowPlacement = new WindowPlacementState
                {
                    X = 100,
                    Y = 200,
                    Width = 760,
                    Height = 640,
                    State = "Normal"
                }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(s);
            var back = System.Text.Json.JsonSerializer.Deserialize<HelperUiSettings>(json);
            Assert.NotNull(back?.WindowPlacement);
            Assert.Equal(100, back!.WindowPlacement!.X);
            Assert.Equal(200, back.WindowPlacement.Y);
            Assert.Equal(760, back.WindowPlacement.Width);
            Assert.Equal(640, back.WindowPlacement.Height);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }
}
