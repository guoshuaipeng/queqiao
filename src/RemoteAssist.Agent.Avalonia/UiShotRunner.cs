using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace RemoteAssist.Agent;

/// <summary>R022：安装说明连网话术中性 + 剪贴板仍含官网下载地址。</summary>
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

        helper.PrepareInstallGuideEntryForShot();
        await Task.Delay(280).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r022-install-entry.png")).ConfigureAwait(true);

        helper.PrepareInstallCopiedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r022-install-copied.png")).ConfigureAwait(true);

        helper.PrepareInstallClipboardPreviewForShot();
        await Task.Delay(280).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r022-clipboard-preview.png")).ConfigureAwait(true);

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
