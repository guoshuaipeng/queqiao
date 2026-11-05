using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>把真实 Avalonia 控件渲成 PNG（R011 验收截图；非标注条伪造）。</summary>
internal static class UiShotRunner
{
    public static async Task RunAsync(HomeWindow home, string outDir)
    {
        Directory.CreateDirectory(outDir);
        await Dispatcher.UIThread.InvokeAsync(() => { });
        await Task.Delay(400).ConfigureAwait(true);

        // 1) 首页双卡片
        await CaptureWindowAsync(home, Path.Combine(outDir, "r011-home.png")).ConfigureAwait(true);

        // 2) 接受协助 → R010 被控窗（文案未改）
        var accept = new MainWindow(
            Program.SharedHost ?? Program.CreateHost(),
            shotDir: outDir,
            enableReturnHome: true);
        accept.Show();
        await Task.Delay(350).ConfigureAwait(true);
        await CaptureWindowAsync(accept, Path.Combine(outDir, "r011-accept-main.png")).ConfigureAwait(true);

        // 关窗提示仍在被控路径
        var closeWarn = new CloseWarnWindow();
        closeWarn.Show(accept);
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(closeWarn, Path.Combine(outDir, "r011-close-warn.png")).ConfigureAwait(true);
        closeWarn.Close();
        accept.AllowCloseWithoutPrompt();
        accept.Close();

        // 3) 协助端空态（连上前无可点灰操作）
        var helper = new HelperWindow(shotDir: outDir, enableReturnHome: true);
        helper.Show();
        helper.PrepareIdleForShot();
        await Task.Delay(350).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r011-helper-idle.png")).ConfigureAwait(true);

        // 4) 一次粘贴连接草稿
        helper.PreparePasteForShot(FriendConnectGuide.FormatShareClipboard("K7MQ2P", "5821"));
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r011-helper-paste.png")).ConfigureAwait(true);

        // 5) 连不上引导（非「点允许连网」）
        helper.PrepareFailForShot(FriendConnectGuide.FailPeerNotAccepting);
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r011-helper-fail-guide.png")).ConfigureAwait(true);

        // 6) 只读成功
        helper.PrepareConnectedReadonlyForShot(
            "[ListProcesses] OK\n" +
            "PID   NAME\n" +
            "1     systemd\n" +
            "882   bamfdaemon-dbus-runner\n" +
            "[ReadLogTail] OK\n" +
            "r011-demo log line 1\n" +
            "r011-demo log line 2\n");
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r011-helper-readonly.png")).ConfigureAwait(true);

        // 7) 对方拒绝写操作
        helper.PrepareDeniedForShot();
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r011-helper-denied.png")).ConfigureAwait(true);
        helper.PrepareClose();
        helper.Close();

        // 8) 写确认弹窗（被控侧，证明路径仍在）
        var hostWin = new MainWindow(Program.SharedHost ?? Program.CreateHost(), shotDir: outDir, enableReturnHome: true);
        hostWin.Show();
        var writeInfo = new ConfirmRequestInfo(
            Source: "guest",
            Tool: "RunCommand",
            Summary: "运行一条命令",
            TimeoutSeconds: 60,
            Detail: "命令：echo r011-should-not-run >/tmp/r011-deny-marker");
        var write = new WriteConfirmWindow(writeInfo);
        write.Show(hostWin);
        await Task.Delay(350).ConfigureAwait(true);
        await CaptureWindowAsync(write, Path.Combine(outDir, "r011-write-confirm.png")).ConfigureAwait(true);
        write.Close();
        hostWin.AllowCloseWithoutPrompt();
        hostWin.Close();

        Console.WriteLine("ui-shot written to " + outDir);
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
