using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace UiShot;

internal static class R012Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = true;
        settings.NetworkAllowMaybeDenied = false;
        settings.TrustLevelPreference = nameof(TrustLevelKind.AskEveryTime);
        settings.Save();

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r012-audit.log");
        try { if (File.Exists(auditPath)) File.Delete(auditPath); } catch { /* ignore */ }
        var audit = new AuditLog(auditPath);

        var host = new AgentHost(
            port: 17997,
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

            using (var dlg = new FullTrustConfirmDialog())
            {
                dlg.Show();
                Application.DoEvents();
                Thread.Sleep(200);
                Application.DoEvents();
                Save(dlg, Path.Combine(outDir, "r012-win-fulltrust-confirm.png"));
                Verify(dlg,
                    TrustLevelCopy.FullTrustConfirmTitle,
                    "只对现在这位协助者、这一次有效",
                    "会回到你平时的选择",
                    TrustLevelCopy.FullTrustConfirmOk,
                    TrustLevelCopy.FullTrustConfirmCancel);
                dlg.Hide();
            }

            agent.ApplyTrustForShot(TrustLevelKind.FullTrust);
            agent.CloseAdvancedForShot();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r012-win-fulltrust-banner.png"));
            Verify(agent, TrustLevelCopy.FullTrustBanner, TrustLevelCopy.EndFullTrust);

            agent.ApplyTrustForShot(TrustLevelKind.Relaxed);
            Application.DoEvents();
            Thread.Sleep(120);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r012-win-relaxed-banner.png"));
            Verify(agent, TrustLevelCopy.RelaxedBanner, TrustLevelCopy.RevertToAskEveryTime);

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

        Console.WriteLine("OK wrote r012 Windows shots to " + outDir);
        return 0;
    }

    private static void Verify(Control root, params string[] needles)
    {
        var sb = new System.Text.StringBuilder();
        void Walk(Control c)
        {
            if (!string.IsNullOrEmpty(c.Text)) sb.AppendLine(c.Text);
            foreach (Control child in c.Controls) Walk(child);
        }
        Walk(root);
        var blob = sb.ToString();
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r012 shot missing text: " + n);
        }
    }

    private static void Save(Form form, string path)
    {
        form.Update();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(path, ImageFormat.Png);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var hash = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(ms.ToArray()))[..12];
        Console.WriteLine($"saved {path} {form.Width}x{form.Height} md5={hash}");
    }
}
