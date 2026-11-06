from pathlib import Path
Path("tools/UiShot/R037Shots.cs").write_text(r'''using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace UiShot;

/// <summary>R037：已连上隐藏「粘贴后点「连接」」；结束后再显示原文。</summary>
internal static class R037Shots
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
        Save(helper, Path.Combine(outDir, "r037-helper-connected-no-paste-hint.png"));
        VerifyBtn(helper, connected: true);
        Verify(helper, HelperSessionCopy.StatusConnected, HelperSessionCopy.ConnectedButtonText);
        VerifyNotVisibleHint(helper);
        VerifyNot(helper, HelperSessionCopy.StatusPeerEndedAssist);

        helper.PreparePeerEndedAssistForShot();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r037-helper-peer-ended-hint-back.png"));
        VerifyBtn(helper, connected: false);
        Verify(helper,
            HelperSessionCopy.StatusPeerEndedAssist,
            HelperSessionCopy.ConnectButtonText,
            HelperSessionCopy.PasteThenConnectHint);
        VerifyHintVisible(helper);

        helper.PrepareSelfDisconnectedForShot();
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r037-helper-self-disconnect-hint-back.png"));
        VerifyBtn(helper, connected: false);
        Verify(helper,
            HelperSessionCopy.StatusDisconnectedSelf,
            HelperSessionCopy.ConnectButtonText,
            HelperSessionCopy.PasteThenConnectHint);
        VerifyHintVisible(helper);
        VerifyNot(helper, HelperSessionCopy.StatusPeerEndedAssist);

        helper.Hide();
        Console.WriteLine("OK wrote r037 Windows helper shots (ui-shot) to " + outDir);
        return 0;
    }

    private static void VerifyHintVisible(Form root)
    {
        var tip = FindPreviewTip(root)
            ?? throw new InvalidOperationException("preview tip label not found");
        if (!tip.Visible)
            throw new InvalidOperationException("paste hint should be visible");
        if (!string.Equals(tip.Text, HelperSessionCopy.PasteThenConnectHint, StringComparison.Ordinal))
            throw new InvalidOperationException("paste hint text changed: " + tip.Text);
    }

    private static void VerifyNotVisibleHint(Form root)
    {
        var tip = FindPreviewTip(root);
        if (tip is not null &&
            tip.Visible &&
            string.Equals(tip.Text, HelperSessionCopy.PasteThenConnectHint, StringComparison.Ordinal))
            throw new InvalidOperationException("paste hint still visible while connected");
        var blob = WalkVisible(root);
        if (blob.Contains(HelperSessionCopy.PasteThenConnectHint, StringComparison.Ordinal))
            throw new InvalidOperationException("paste hint still in visible text while connected");
    }

    private static Label? FindPreviewTip(Control root)
    {
        Label? found = null;
        void Walk(Control c)
        {
            if (c is Label lb &&
                string.Equals(lb.Text, HelperSessionCopy.PasteThenConnectHint, StringComparison.Ordinal))
                found = lb;
            foreach (Control child in c.Controls) Walk(child);
        }
        Walk(root);
        return found;
    }

    private static void VerifyBtn(Form root, bool connected)
    {
        var btn = FindConnectButton(root)
            ?? throw new InvalidOperationException("session connect button not found");
        var expect = HelperSessionCopy.SessionConnectButtonLabel(connected);
        if (!string.Equals(btn.Text, expect, StringComparison.Ordinal))
            throw new InvalidOperationException($"btn text want {expect} got {btn.Text}");
        if (btn.Enabled == connected)
            throw new InvalidOperationException($"btn Enabled={btn.Enabled} connected={connected}");
        var green = Color.FromArgb(0x0F, 0x9D, 0x58).ToArgb();
        var gray = Color.FromArgb(0xE8, 0xEA, 0xED).ToArgb();
        if (connected)
        {
            if (btn.BackColor.ToArgb() == green)
                throw new InvalidOperationException("connected btn still solid green");
            if (btn.BackColor.ToArgb() != gray)
                throw new InvalidOperationException("connected btn not light gray: " + btn.BackColor);
        }
        else if (btn.BackColor.ToArgb() != green)
        {
            throw new InvalidOperationException("disconnected btn not solid green: " + btn.BackColor);
        }
    }

    private static Button? FindConnectButton(Control root)
    {
        Button? found = null;
        void Walk(Control c)
        {
            if (c is Button b &&
                (b.Text == HelperSessionCopy.ConnectButtonText ||
                 b.Text == HelperSessionCopy.ConnectedButtonText) &&
                b.Width >= 100)
            {
                if (found is null || b.Width > found.Width)
                    found = b;
            }
            foreach (Control child in c.Controls) Walk(child);
        }
        Walk(root);
        return found;
    }

    private static void Verify(Control root, params string[] needles)
    {
        var blob = WalkVisible(root);
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r037 missing: " + n);
        }
    }

    private static void VerifyNot(Control root, params string[] banned)
    {
        var blob = WalkVisible(root);
        foreach (var n in banned)
        {
            if (blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r037 unexpected: " + n);
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
print('R037Shots written')
