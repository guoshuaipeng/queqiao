using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>R036：会话码主按钮连上后「已连接」；结束后恢复「连接」（ui-shot）。</summary>
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

        helper.PreparePeerConnectedSessionForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r036-avalonia-helper-connected-btn.png")).ConfigureAwait(true);

        helper.PreparePeerEndedAssistForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r036-avalonia-helper-peer-ended-reconnect.png")).ConfigureAwait(true);

        helper.PrepareSelfDisconnectedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r036-avalonia-helper-self-disconnect-reconnect.png")).ConfigureAwait(true);

        try { helper.Close(); } catch { /* ignore */ }

        Console.WriteLine("ui-shot r036 written to " + outDir);
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
