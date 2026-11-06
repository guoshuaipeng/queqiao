using System.Drawing;
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
