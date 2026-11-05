using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>R031：接受协助端对方连上 / 断开后人话主状态 + 协助方列表（ui-shot）。</summary>
internal static class UiShotRunner
{
    public static async Task RunAsync(HomeWindow home, string outDir)
    {
        Directory.CreateDirectory(outDir);
        await Dispatcher.UIThread.InvokeAsync(() => { });
        await Task.Delay(400).ConfigureAwait(true);

        var host = Program.SharedHost ?? Program.CreateHost();
        var accept = new MainWindow(host, shotDir: outDir, enableReturnHome: true);
        accept.Show();
        await Task.Delay(350).ConfigureAwait(true);

        accept.PreparePeerIdleForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(accept, Path.Combine(outDir, "r031-avalonia-idle.png")).ConfigureAwait(true);

        accept.PreparePeerConnectedForShot("小明");
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(accept, Path.Combine(outDir, "r031-avalonia-peer-connected.png")).ConfigureAwait(true);

        accept.PreparePeerDisconnectedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(accept, Path.Combine(outDir, "r031-avalonia-peer-disconnected.png")).ConfigureAwait(true);

        accept.AllowCloseWithoutPrompt();
        accept.Close();

        Console.WriteLine("ui-shot r031 written to " + outDir);
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
