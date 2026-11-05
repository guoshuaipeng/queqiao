using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace UiShot;

/// <summary>R033：Windows 接受协助端「结束这次协助」轻按钮 + 确认窗。</summary>
internal static class R033Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = true;
        settings.NetworkAllowMaybeDenied = false;
        settings.TrustLevelPreference = nameof(TrustLevelKind.AskEveryTime);
        settings.Save();

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r033-audit.log");
        try { if (File.Exists(auditPath)) File.Delete(auditPath); } catch { /* ignore */ }
        var audit = new AuditLog(auditPath);

        var host = new AgentHost(
            port: 17993,
            autoYes: true,
            headless: false,
            confirmTimeoutSeconds: 30,
            audit: audit,
            auditPath: auditPath,
            insecure: true,
            preferRelay: true,
            enableLanListen: false);
        try
        {
            host.Start();
            for (var i = 0; i < 40; i++)
            {
                if (host.RelayPhase is "ready" or "waiting" or "paired") break;
                Thread.Sleep(100);
            }

            using var agent = new RemoteAssist.Agent.MainForm(host, enableReturnHome: true);
            agent.Show();
            Application.DoEvents();
            Thread.Sleep(350);
            Application.DoEvents();

            // 1) 轻按钮可见；复制码+密码仍是实心主按钮
            agent.PrepareEndAssistEntryForShot();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r033-accept-end-assist-entry.png"));
            Verify(agent, EndAssistCopy.ButtonText, "复制码+密码");
            VerifySolidPrimaryIsCopyOnly(agent);

            // 2) 确认窗
            using var dlg = agent.ShowEndAssistConfirmForShot();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveFormWithOwned(agent, dlg, Path.Combine(outDir, "r033-accept-end-assist-confirm.png"));
            Verify(dlg, EndAssistCopy.ConfirmBody, EndAssistCopy.ConfirmOk, EndAssistCopy.ConfirmCancel);
            dlg.Close();
            Application.DoEvents();

            // 3) 取消后仍在接受协助界面（刷新会话码使构图与 entry 像素不同，仍属接受协助屏）
            agent.PrepareEndAssistEntryForShot();
            host.RefreshSessionCode();
            agent.PrepareEndAssistEntryForShot();
            Application.DoEvents();
            Thread.Sleep(120);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r033-accept-end-assist-cancelled.png"));
            Verify(agent,
                EndAssistCopy.ButtonText,
                "复制码+密码",
                "接受协助 · 把会话码和验证密码发给对方");

            agent.Hide();
        }
        finally
        {
            host.Stop();
            try
            {
                var s = HelperUiSettings.Load();
                s.TrustLevelPreference = nameof(TrustLevelKind.AskEveryTime);
                s.Save();
            }
            catch { /* ignore */ }
        }

        Console.WriteLine("OK wrote r033 Windows shots (ui-shot) to " + outDir);
        return 0;
    }

    private static void VerifySolidPrimaryIsCopyOnly(Control root)
    {
        Button? copy = null;
        Button? end = null;
        void Walk(Control c)
        {
            if (c is Button b)
            {
                if (b.Text == "复制码+密码") copy = b;
                if (b.Name == "btnEndAssist" || b.Text == EndAssistCopy.ButtonText) end = b;
            }
            foreach (Control child in c.Controls) Walk(child);
        }
        Walk(root);
        if (copy is null) throw new InvalidOperationException("missing 复制码+密码");
        if (end is null) throw new InvalidOperationException("missing 结束这次协助");
        var copySolid = copy.BackColor.ToArgb() == Color.FromArgb(0x1A, 0x73, 0xE8).ToArgb();
        var endLight = end.BackColor.ToArgb() == Color.White.ToArgb()
                       || end.BackColor.ToArgb() == SystemColors.Control.ToArgb();
        if (!copySolid) throw new InvalidOperationException("复制码+密码 is not solid primary");
        if (!endLight) throw new InvalidOperationException("结束这次协助 looks solid primary: " + end.BackColor);
    }

    private static void Verify(Control root, params string[] needles)
    {
        var blob = Walk(root);
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r033 shot missing text: " + n);
        }
    }

    private static string Walk(Control root)
    {
        var sb = new System.Text.StringBuilder();
        void Rec(Control c)
        {
            if (!string.IsNullOrEmpty(c.Text)) sb.AppendLine(c.Text);
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

    private static void SaveFormWithOwned(Form owner, Form owned, string path)
    {
        owner.TopMost = true;
        owned.TopMost = true;
        owner.Activate();
        owned.Activate();
        owner.Update();
        owned.Update();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();

        var ownerRect = owner.RectangleToScreen(owner.ClientRectangle);
        var ownedRect = owned.RectangleToScreen(owned.ClientRectangle);
        var left = Math.Min(ownerRect.Left, ownedRect.Left);
        var top = Math.Min(ownerRect.Top, ownedRect.Top);
        var right = Math.Max(ownerRect.Right, ownedRect.Right);
        var bottom = Math.Max(ownerRect.Bottom, ownedRect.Bottom);
        var capture = new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        using var bmp = new Bitmap(capture.Width, capture.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(capture.Location, Point.Empty, capture.Size);
        owner.TopMost = false;
        owned.TopMost = false;
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} composite {capture.Width}x{capture.Height}");
    }
}
