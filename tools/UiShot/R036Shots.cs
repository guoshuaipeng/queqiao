using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace UiShot;

/// <summary>R036：会话码主按钮连上后改为不可点「已连接」；结束后恢复实心「连接」。</summary>
internal static class R036Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        using var helper = new RemoteAssist.Helper.UI.MainForm(enableReturnHome: true);
        helper.Show();
        Application.DoEvents();
        Thread.Sleep(300);
        Application.DoEvents();

        // 1) 已连上：主按钮「已连接」不可点、非实心绿；主状态 R035 句仍在
        helper.PreparePeerConnectedForShot();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r036-helper-connected-btn.png"));
        VerifyBtn(helper, connected: true);
        Verify(helper, HelperSessionCopy.StatusConnected, HelperSessionCopy.ConnectedButtonText);
        VerifyNot(helper, HelperSessionCopy.StatusPeerEndedAssist);

        // 2) 对方结束后：主状态 R035 + 主按钮回到实心「连接」
        helper.PreparePeerEndedAssistForShot();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r036-helper-peer-ended-reconnect.png"));
        VerifyBtn(helper, connected: false);
        Verify(helper, HelperSessionCopy.StatusPeerEndedAssist, HelperSessionCopy.ConnectButtonText);

        // 3) 自己断开：状态未连接 + 实心「连接」
        helper.PrepareSelfDisconnectedForShot();
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r036-helper-self-disconnect-reconnect.png"));
        VerifyBtn(helper, connected: false);
        Verify(helper, HelperSessionCopy.StatusDisconnectedSelf, HelperSessionCopy.ConnectButtonText);
        VerifyNot(helper, HelperSessionCopy.StatusPeerEndedAssist);

        helper.Hide();
        Console.WriteLine("OK wrote r036 Windows helper shots (ui-shot) to " + outDir);
        return 0;
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
                b.Width >= 100) // session primary wider than small account bits sometimes
            {
                // Prefer the pair-card primary (larger / bold-ish)
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
        var blob = Walk(root);
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r036 missing: " + n);
        }
    }

    private static void VerifyNot(Control root, params string[] banned)
    {
        var blob = Walk(root);
        foreach (var n in banned)
        {
            if (blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r036 unexpected: " + n);
        }
    }

    private static string Walk(Control root)
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
