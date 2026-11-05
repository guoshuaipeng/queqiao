using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>R014：Avalonia 账号登录免密 + 最近审计人话（真实控件截图）。</summary>
internal static class UiShotRunner
{
    public static async Task RunAsync(HomeWindow home, string outDir)
    {
        Directory.CreateDirectory(outDir);
        await Dispatcher.UIThread.InvokeAsync(() => { });
        await Task.Delay(400).ConfigureAwait(true);

        // ---- Helper：账号标签 / 未选中禁用 / 选中可连 ----
        var helper = new HelperWindow(shotDir: outDir, enableReturnHome: true);
        helper.Show();
        await Task.Delay(350).ConfigureAwait(true);

        // 标签可见：有账号/密码标签与输入框，设备列表为空（与未选中有列表区分 MD5）
        helper.PrepareAccountLabelsOnlyForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r014-helper-account-labels.png")).ConfigureAwait(true);

        helper.PrepareAccountForShot(selected: false);
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r014-helper-account-unselected.png")).ConfigureAwait(true);

        helper.PrepareAccountForShot(selected: true);
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r014-helper-account-selected.png")).ConfigureAwait(true);

        helper.PrepareOwnerConnectedForShot();
        await Task.Delay(200).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r014-helper-owner-connected.png")).ConfigureAwait(true);

        helper.PrepareClose();
        helper.Close();
        await Task.Delay(150).ConfigureAwait(true);

        // ---- 被控：最近审计人话（无 preference=）----
        var host = Program.SharedHost ?? Program.CreateHost();
        host.SetTrustLevelPreference(TrustLevelKind.AskEveryTime);

        var win = new MainWindow(host, shotDir: outDir, enableReturnHome: true);
        win.Show();
        await Task.Delay(350).ConfigureAwait(true);
        win.SeedTrustAuditForShot();
        win.OpenAdvancedForShot();
        win.ScrollListsIntoViewForShot();
        await Task.Delay(350).ConfigureAwait(true);
        await CaptureWindowAsync(win, Path.Combine(outDir, "r014-audit-human.png")).ConfigureAwait(true);

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
