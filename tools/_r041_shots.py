from pathlib import Path

Path("tools/UiShot/R041Shots.cs").write_text(r'''using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace UiShot;

/// <summary>R041：刚连上无 PAIR_OK；提议四态人话（ui-shot）。</summary>
internal static class R041Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        using var helper = new RemoteAssist.Helper.UI.MainForm(enableReturnHome: true);
        helper.Show();
        Application.DoEvents();
        Thread.Sleep(280);
        Application.DoEvents();

        helper.PreparePeerConnectedForShot();
        Flush();
        Save(helper, Path.Combine(outDir, "r041-helper-connected-no-pair-ok.png"));
        Verify(helper, HelperSessionCopy.StatusConnected, ProposeChangeCopy.ConnectedResultHint);
        VerifyNot(helper, "PAIR_OK");

        helper.PrepareProposeWaitingForShot();
        Flush();
        Save(helper, Path.Combine(outDir, "r041-helper-propose-waiting.png"));
        Verify(helper, ProposeChangeCopy.WaitingStatus);
        VerifyNot(helper, "RunCommand");

        helper.PrepareProposeAllowedForShot();
        Flush();
        Save(helper, Path.Combine(outDir, "r041-helper-propose-allowed.png"));
        Verify(helper, ProposeChangeCopy.AllowedStatus);
        VerifyNot(helper, "已提议一项改动");

        helper.PrepareProposeDeniedForShot();
        Flush();
        Save(helper, Path.Combine(outDir, "r041-helper-propose-denied.png"));
        Verify(helper, WriteConfirmCopy.DeniedMessage);

        helper.PrepareProposeTimedOutForShot();
        Flush();
        Save(helper, Path.Combine(outDir, "r041-helper-propose-timed-out.png"));
        Verify(helper, WriteConfirmCopy.TimedOutMessage);
        VerifyNot(helper, "已拒绝，未做任何改动"); // timeout frame must not show denied as status — wait, DeniedMessage is different string. TimedOut is "确认超时...". Denied is "已拒绝...". Verify status has timeout. Don't VerifyNot Denied if output could somehow... status should be timeout only. Good.

        // Actually VerifyNot denied message: the timeout status shouldn't equal denied. WalkVisible might still be fine if only timeout shown.
        // TimedOutMessage doesn't contain 已拒绝. VerifyNot DeniedMessage is correct for this frame.

        helper.Hide();
        Console.WriteLine("OK wrote r041 Windows helper shots (ui-shot) to " + outDir);
        return 0;
    }

    private static void Flush()
    {
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();
    }

    private static void Verify(Control root, params string[] needles)
    {
        var blob = WalkVisible(root);
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r041 missing: " + n + " in " + blob[..Math.Min(200, blob.Length)]);
        }
    }

    private static void VerifyNot(Control root, params string[] banned)
    {
        var blob = WalkVisible(root);
        foreach (var n in banned)
        {
            if (blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r041 unexpected: " + n);
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

p = Path("tools/UiShot/Program.cs")
t = p.read_text(encoding="utf-8")
needle = '        if (args.Any(a => a is "--r039"))\n            return R039Shots.Run(outDir);\n'
insert = needle + '\n        if (args.Any(a => a is "--r041"))\n            return R041Shots.Run(outDir);\n'
if "--r041" not in t:
    if needle not in t:
        raise SystemExit("r039 hook missing")
    t = t.replace(needle, insert, 1)
    p.write_text(t, encoding="utf-8", newline="\n")
    print("Program wired")

Path("src/RemoteAssist.Agent.Avalonia/UiShotRunner.cs").write_text('''using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>R041：刚连上无 PAIR_OK；提议四态人话（ui-shot）。</summary>
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
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r041-avalonia-helper-connected-no-pair-ok.png")).ConfigureAwait(true);

        helper.PrepareProposeWaitingForShot();
        await Task.Delay(220).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r041-avalonia-helper-propose-waiting.png")).ConfigureAwait(true);

        helper.PrepareProposeAllowedForShot();
        await Task.Delay(220).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r041-avalonia-helper-propose-allowed.png")).ConfigureAwait(true);

        helper.PrepareProposeDeniedForShot();
        await Task.Delay(220).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r041-avalonia-helper-propose-denied.png")).ConfigureAwait(true);

        helper.PrepareProposeTimedOutForShot();
        await Task.Delay(220).ConfigureAwait(true);
        await CaptureWindowAsync(helper, Path.Combine(outDir, "r041-avalonia-helper-propose-timed-out.png")).ConfigureAwait(true);

        try { helper.Close(); } catch { /* ignore */ }

        Console.WriteLine("ui-shot r041 written to " + outDir);
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
print("shots wired")
