from pathlib import Path

hw = Path("src/RemoteAssist.Agent.Avalonia/Views/HelperWindow.cs")
ht = hw.read_text(encoding="utf-8")
needle = '''    /// <summary>UiShot：已点「复制安装说明」后人话反馈（Windows 同等文案）。</summary>
    internal void PrepareInstallCopiedForShot()
    {
        PrepareInstallGuideEntryForShot();
        _preview.Text = WriteConfirmCopy.InstallCopiedHint;
        _preview.Foreground = Brush.Parse("#0F9D58");
        // 状态行保持「状态：未连接」，避免与预览行重复同一句反馈
        _mcpHint.Text = "";
    }'''

repl = '''    /// <summary>UiShot：已点「复制安装说明」后人话反馈（Windows 同等文案；只出现一次）。</summary>
    internal void PrepareInstallCopiedForShot()
    {
        PrepareInstallGuideEntryForShot();
        _preview.Text = WriteConfirmCopy.InstallCopiedHint;
        _preview.Foreground = Brush.Parse("#0F9D58");
        // 状态行保持「状态：未连接」，避免与预览行重复同一句反馈
        _mcpHint.Text = "";
        _failHint.Text = "";
    }

    /// <summary>UiShot：预览区展示将复制到剪贴板的安装说明正文（与常量同一份；不含反馈叠字）。</summary>
    internal void PrepareInstallClipboardPreviewForShot()
    {
        PrepareInstallGuideEntryForShot();
        _preview.Text = WriteConfirmCopy.InstallInstructionsClipboard;
        _preview.Foreground = Brush.Parse("#202124");
        _mcpHint.Text = "";
        _failHint.Text = "";
        Height = Math.Max(Height, 980);
    }'''

if needle not in ht:
    raise SystemExit("PrepareInstallCopiedForShot block not found")
hw.write_text(ht.replace(needle, repl), encoding="utf-8")
print("HelperWindow shot helpers updated")

# Replace UiShotRunner for R021 helper shots
Path("src/RemoteAssist.Agent.Avalonia/UiShotRunner.cs").write_text('''using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace RemoteAssist.Agent;

/// <summary>R021：去协助端「复制安装说明」带官网下载地址 + 复制反馈（只一次）。</summary>
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
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r021-install-entry.png")).ConfigureAwait(true);

        helper.PrepareInstallCopiedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r021-install-copied.png")).ConfigureAwait(true);

        helper.PrepareInstallClipboardPreviewForShot();
        await Task.Delay(280).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r021-clipboard-preview.png")).ConfigureAwait(true);

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
''', encoding="utf-8")
print("UiShotRunner.cs rewritten for R021")
