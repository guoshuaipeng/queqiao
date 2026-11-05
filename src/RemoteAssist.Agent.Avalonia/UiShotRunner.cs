using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>把真实 Avalonia 控件渲成 PNG（验收截图；非标注条伪造）。</summary>
internal static class UiShotRunner
{
    public static async Task RunAsync(MainWindow main, string outDir)
    {
        Directory.CreateDirectory(outDir);
        await Dispatcher.UIThread.InvokeAsync(() => { });
        await Task.Delay(400).ConfigureAwait(true);

        await CaptureWindowAsync(main, Path.Combine(outDir, "r010-agent-main.png")).ConfigureAwait(true);

        // 配对确认
        var pair = new PairConfirmWindow(new PairRequestInfo(
            "relay", "协助方", PermissionLevel.Assist, TimeoutSeconds: 30));
        pair.Show(main);
        await Task.Delay(350).ConfigureAwait(true);
        await CaptureWindowAsync(pair, Path.Combine(outDir, "r010-pair-confirm.png")).ConfigureAwait(true);
        pair.Close();

        // 写确认
        var writeInfo = new ConfirmRequestInfo(
            Source: "guest",
            Tool: "RunCommand",
            Summary: "运行一条命令",
            TimeoutSeconds: 60,
            Detail: "命令：echo r010-should-not-run >/tmp/r010-deny-marker");
        var write = new WriteConfirmWindow(writeInfo);
        write.Show(main);
        await Task.Delay(350).ConfigureAwait(true);
        await CaptureWindowAsync(write, Path.Combine(outDir, "r010-write-confirm.png")).ConfigureAwait(true);
        write.Close();

        // 拒绝结果可见原文
        var denied = new Window
        {
            Title = WriteConfirmCopy.DialogTitle,
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new TextBlock
            {
                Text = WriteConfirmCopy.DeniedMessage,
                FontSize = 16,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Margin = new Thickness(24),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            }
        };
        denied.Show(main);
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(denied, Path.Combine(outDir, "r010-write-denied.png")).ConfigureAwait(true);
        denied.Close();

        // 关窗提示
        var closeWarn = new CloseWarnWindow();
        closeWarn.Show(main);
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(closeWarn, Path.Combine(outDir, "r010-close-warn.png")).ConfigureAwait(true);
        closeWarn.Close();

        // 结束按钮所在主窗已有；再拍一张强调结束按钮的状态条
        Console.WriteLine("ui-shot written to " + outDir);
    }

    private static async Task CaptureWindowAsync(Window window, string path)
    {
        window.Show();
        await Task.Delay(100).ConfigureAwait(true);
        // 强制布局
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
