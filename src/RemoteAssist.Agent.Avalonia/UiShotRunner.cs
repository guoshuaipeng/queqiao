using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>R013：已确认改动列表 + 刷新会话码确认（真实控件截图）。</summary>
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

        // 1) 已确认列表有人话条目（文档例会话码 AB12CD/3471）
        win.SeedConfirmedForShot("运行一条命令 · 命令：echo r013-ok");
        win.OpenAdvancedForShot();
        win.ScrollListsIntoViewForShot();
        await Task.Delay(350).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r013-confirmed-list.png")).ConfigureAwait(true);

        // 2) 拒绝写入审计；滚到列表；已确认不得含拒绝，最近审计可见已拒绝
        win.SeedDeniedForShot("运行一条命令 · 命令：echo r013-deny-should-not-run");
        win.ScrollListsIntoViewForShot();
        await Task.Delay(350).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r013-denied-not-in-confirmed.png")).ConfigureAwait(true);

        // 3) 刷新确认弹窗
        var refreshDlg = win.ShowRefreshConfirmForShot();
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(refreshDlg, Path.Combine(outDir, "r013-refresh-confirm.png")).ConfigureAwait(true);
        refreshDlg.Close(false);
        await Task.Delay(150).ConfigureAwait(true);

        // 4) 取消刷新：收起高级，主界面仍文档例码 AB12CD（与列表图区分）
        win.CloseAdvancedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r013-refresh-cancel-keeps-code.png")).ConfigureAwait(true);

        // 5) 完全信任 → 刷新 → 新码 + 横幅消失
        win.ApplyTrustForShot(TrustLevelKind.FullTrust);
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r013-fulltrust-before-refresh.png")).ConfigureAwait(true);
        var newCode = win.ApplyRefreshForShot();
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r013-refresh-clears-fulltrust.png")).ConfigureAwait(true);
        Console.WriteLine("r013 refreshed code (live host, not doc example)=" + newCode);

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
