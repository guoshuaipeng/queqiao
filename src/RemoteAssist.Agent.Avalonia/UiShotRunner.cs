using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace RemoteAssist.Agent;

/// <summary>R015：设备列表人话 + 免密连接实心可点态 + 离线提示 + 常用操作可用。</summary>
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

        helper.PrepareDeviceListHumanForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r015-device-list-human.png")).ConfigureAwait(true);

        helper.PrepareAccountForShot(selected: false);
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r015-connect-unselected.png")).ConfigureAwait(true);

        helper.PrepareAccountForShot(selected: true);
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r015-connect-online.png")).ConfigureAwait(true);

        helper.PrepareOfflineSelectedForShot();
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r015-connect-offline.png")).ConfigureAwait(true);

        helper.PrepareOwnerConnectedForShot();
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r015-owner-tools-enabled.png")).ConfigureAwait(true);

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
