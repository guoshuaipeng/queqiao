from pathlib import Path

Path("tools/UiShot/R039Shots.cs").write_text(r'''using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace UiShot;

/// <summary>R039：确认 Windows 已连上时五键仍在首屏可见（未改 Win 布局）。</summary>
internal static class R039Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        using var helper = new RemoteAssist.Helper.UI.MainForm(enableReturnHome: true);
        helper.Show();
        Application.DoEvents();
        Thread.Sleep(300);
        Application.DoEvents();

        helper.PreparePeerConnectedForShot();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r039-helper-connected-tools-visible.png"));
        Verify(helper,
            HelperSessionCopy.StatusConnected,
            HelperSessionCopy.ConnectedButtonText,
            HelperSessionCopy.CommonToolButtonLabels[0],
            HelperSessionCopy.CommonToolButtonLabels[1],
            HelperSessionCopy.CommonToolButtonLabels[2],
            HelperSessionCopy.CommonToolButtonLabels[3],
            HelperSessionCopy.CommonToolButtonLabels[4]);
        VerifyBtnConnected(helper);

        helper.Hide();
        Console.WriteLine("OK wrote r039 Windows helper shot (ui-shot) to " + outDir);
        return 0;
    }

    private static void VerifyBtnConnected(Form root)
    {
        Button? found = null;
        void Walk(Control c)
        {
            if (c is Button b &&
                b.Text == HelperSessionCopy.ConnectedButtonText &&
                b.Width >= 100)
            {
                if (found is null || b.Width > found.Width)
                    found = b;
            }
            foreach (Control child in c.Controls) Walk(child);
        }
        Walk(root);
        if (found is null)
            throw new InvalidOperationException("connected button missing");
        if (found.Enabled)
            throw new InvalidOperationException("connected button should be disabled");
    }

    private static void Verify(Control root, params string[] needles)
    {
        var blob = WalkVisible(root);
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r039 missing: " + n);
        }
    }

    private static string WalkVisible(Control root)
    {
        var sb = new System.Text.StringBuilder();
        void Rec(Control c)
        {
            if (c.Visible && !string.IsNullOrEmpty(c.Text)) sb.AppendLine(c.Text);
            foreach (Control child in c.Controls) Rec(child);
        }
        Rec(root);
        return sb.ToString();
    }

    private static void Save(Form form, string path)
    {
        form.Update();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} {form.Width}x{form.Height}");
    }
}
''', encoding='utf-8', newline='\n')

# Wire Program.cs
p = Path("tools/UiShot/Program.cs")
t = p.read_text(encoding="utf-8")
needle = '        if (args.Any(a => a is "--r037"))\n            return R037Shots.Run(outDir);\n'
insert = needle + '\n        if (args.Any(a => a is "--r039"))\n            return R039Shots.Run(outDir);\n'
if "--r039" not in t:
    if needle not in t:
        raise SystemExit("r037 hook missing")
    t = t.replace(needle, insert, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("Program wired")
else:
    print("Program already has r039")

# Avalonia UiShotRunner
Path("src/RemoteAssist.Agent.Avalonia/UiShotRunner.cs").write_text('''using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>R039：Avalonia 连上后五键紧挨主状态首屏可见；结束后再隐藏（ui-shot）。</summary>
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
        await Task.Delay(300).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r039-avalonia-helper-connected-tools-first-screen.png")).ConfigureAwait(true);

        helper.PreparePeerEndedAssistForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r039-avalonia-helper-peer-ended-tools-hidden.png")).ConfigureAwait(true);

        helper.PrepareSelfDisconnectedForShot();
        await Task.Delay(250).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r039-avalonia-helper-self-disconnect-tools-hidden.png")).ConfigureAwait(true);

        try { helper.Close(); } catch { /* ignore */ }

        Console.WriteLine("ui-shot r039 written to " + outDir);
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
''', encoding='utf-8', newline='\n')
print("UiShotRunner OK")
