using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace UiShot;

/// <summary>R035：去协助端对方结束协助后人话；自己断开 / 未连上失败仍用原文。</summary>
internal static class R035Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        using var helper = new RemoteAssist.Helper.UI.MainForm(enableReturnHome: true);
        helper.Show();
        Application.DoEvents();
        Thread.Sleep(300);
        Application.DoEvents();

        // 1) 已连上：主状态 + 常用操作可见
        helper.PreparePeerConnectedForShot();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r035-helper-connected.png"));
        Verify(helper, HelperSessionCopy.StatusConnected, "常用操作", "查看正在运行的程序", "复制码+密码".Length >= 0 ? "提议一项改动" : "");
        VerifyNot(helper, HelperSessionCopy.StatusPeerEndedAssist);

        // 2) 对方结束后
        helper.PreparePeerEndedAssistForShot();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r035-helper-peer-ended.png"));
        Verify(helper, HelperSessionCopy.StatusPeerEndedAssist);
        VerifyNot(helper, "查看正在运行的程序"); // tools card hidden
        VerifyNot(helper, HelperSessionCopy.StatusConnectFailed);

        // 3) 自己断开
        helper.PrepareSelfDisconnectedForShot();
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r035-helper-self-disconnect.png"));
        Verify(helper, HelperSessionCopy.StatusDisconnectedSelf);
        VerifyNot(helper, HelperSessionCopy.StatusPeerEndedAssist);

        // 4) 尚未连上失败（R003）
        helper.PrepareConnectFailedForShot(FriendConnectGuide.FailPeerNotAccepting);
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();
        Save(helper, Path.Combine(outDir, "r035-helper-connect-failed.png"));
        Verify(helper, HelperSessionCopy.StatusConnectFailed, FriendConnectGuide.FailPeerNotAccepting);
        VerifyNot(helper, HelperSessionCopy.StatusPeerEndedAssist);

        helper.Hide();
        Console.WriteLine("OK wrote r035 Windows helper shots (ui-shot) to " + outDir);
        return 0;
    }

    private static void Verify(Control root, params string[] needles)
    {
        var blob = Walk(root);
        foreach (var n in needles)
        {
            if (string.IsNullOrEmpty(n)) continue;
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r035 missing: " + n);
        }
    }

    private static void VerifyNot(Control root, params string[] banned)
    {
        var blob = Walk(root);
        foreach (var n in banned)
        {
            if (blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r035 unexpected: " + n);
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
