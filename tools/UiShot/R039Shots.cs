using System.Drawing;
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
