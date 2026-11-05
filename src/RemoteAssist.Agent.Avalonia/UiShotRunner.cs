using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>把真实 Avalonia 控件渲成 PNG（R012 信任等级验收截图；非标注条伪造）。</summary>
internal static class UiShotRunner
{
    public static async Task RunAsync(HomeWindow home, string outDir)
    {
        Directory.CreateDirectory(outDir);
        await Dispatcher.UIThread.InvokeAsync(() => { });
        await Task.Delay(400).ConfigureAwait(true);

        var host = Program.SharedHost ?? Program.CreateHost();
        host.SetTrustLevelPreference(TrustLevelKind.AskEveryTime);

        var win = new MainWindow(host, shotDir: outDir, enableReturnHome: true);
        win.Show();
        await Task.Delay(350).ConfigureAwait(true);

        win.OpenAdvancedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r012-linux-advanced-ask.png")).ConfigureAwait(true);

        var confirm = win.ShowFullTrustConfirmForShot();
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(confirm, Path.Combine(outDir, "r012-linux-fulltrust-confirm.png")).ConfigureAwait(true);
        confirm.Close(false);
        await Task.Delay(150).ConfigureAwait(true);

        win.ApplyTrustForShot(TrustLevelKind.AskEveryTime);
        win.CloseAdvancedForShot();
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r012-linux-fulltrust-cancel-stays.png")).ConfigureAwait(true);

        win.ApplyTrustForShot(TrustLevelKind.Relaxed);
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r012-linux-banner-relaxed.png")).ConfigureAwait(true);

        win.ApplyTrustForShot(TrustLevelKind.FullTrust);
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r012-linux-banner-fulltrust.png")).ConfigureAwait(true);

        win.ApplyTrustForShot(TrustLevelKind.AskEveryTime);
        var writeInfo = new ConfirmRequestInfo(
            Source: "guest",
            Tool: "RunCommand",
            Summary: "运行一条命令",
            TimeoutSeconds: 60,
            Detail: "命令：echo r012-ask");
        var write = new WriteConfirmWindow(writeInfo);
        write.Show(win);
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(write, Path.Combine(outDir, "r012-linux-write-confirm-ask.png")).ConfigureAwait(true);
        write.Close();

        win.AllowCloseWithoutPrompt();
        win.Close();

        try { host.SetTrustLevelPreference(TrustLevelKind.AskEveryTime); } catch { /* ignore */ }

        Console.WriteLine("ui-shot written to " + outDir);
        await Task.CompletedTask;
        _ = home;
    }

    private static async Task CaptureWindowAsync(Window window, string path)
    {
        window.Show();
        await Task.Delay(120).ConfigureAwait(true);
        window.InvalidateVisual();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

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
