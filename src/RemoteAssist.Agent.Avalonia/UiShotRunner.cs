using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace RemoteAssist.Agent;

/// <summary>R016：未连接无常用操作；连上后五键+引导；已连接按钮轻态；拒绝文案。</summary>
internal static class UiShotRunner
{
    public static async Task RunAsync(HomeWindow home, string outDir)
    {
        Directory.CreateDirectory(outDir);
        await Dispatcher.UIThread.InvokeAsync(() => { });
        await Task.Delay(400).ConfigureAwait(true);

        var helper = new HelperWindow(shotDir: outDir, enableReturnHome: true);
        helper.Show();
        await Task.Delay(350).ConfigureAwait(true);

        // 1) 仅登录未免密：无常用操作整块
        helper.PrepareLoggedInUnconnectedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r016-tools-hidden.png")).ConfigureAwait(true);

        // 2) 免密已连：五键 + 引导
        helper.PrepareOwnerConnectedForShot();
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r016-tools-five.png")).ConfigureAwait(true);

        // 3) 已连接按钮非实心绿（与五键同态，输出区写轻提示以区分 MD5）
        helper.PrepareOwnerConnectedForShot();
        helper.PrepareConnectedButtonLightForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r016-connected-button-light.png")).ConfigureAwait(true);

        // 4) 拒绝后文案
        helper.PrepareDeniedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r016-propose-denied.png")).ConfigureAwait(true);

        helper.PrepareClose();
        helper.Close();

        Console.WriteLine("ui-shot written to " + outDir);
        await Task.CompletedTask;
        _ = home;
    }

    private static async Task CaptureWindowAsync(Window window, string path)
    {
        window.Show();
        await Task.Delay(150).ConfigureAwait(true);
        window.InvalidateVisual();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        await Task.Delay(80).ConfigureAwait(true);

        var size = new Size(Math.Max(window.Width, 400), Math.Max(window.Height, 200));
        if (window.Bounds.Width > 1 && window.Bounds.Height > 1)
            size = window.Bounds.Size;

        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(size.Width * 1.25)),
            Math.Max(1, (int)Math.Ceiling(size.Height * 1.25)));
        var dpi = new Vector(96 * 1.25, 96 * 1.25);
        using var bmp = new RenderTargetBitmap(pixelSize, dpi);
        bmp.Render(window);
        bmp.Save(path);
        Console.WriteLine("shot " + path + " " + new FileInfo(path).Length + " bytes");
    }
}
