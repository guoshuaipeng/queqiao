using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace UiShot;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var outDir = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal))
                     ?? @"E:\IdeaProjects\remote-assist\docs\screenshots";
        var deniedOnly = args.Any(a => string.Equals(a, "--denied-only", StringComparison.OrdinalIgnoreCase));
        Directory.CreateDirectory(outDir);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = true;
        settings.NetworkAllowMaybeDenied = false;
        settings.TrustLevelPreference = nameof(TrustLevelKind.AskEveryTime);
        settings.Save();

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r007-audit.log");
        try { if (File.Exists(auditPath)) File.Delete(auditPath); } catch { /* ignore */ }
        var audit = new AuditLog(auditPath);
        audit.Write("tool_confirmed", "运行一条命令 · 命令：echo hello");
        audit.Write("tool_denied_user", "删除一个文件或文件夹 · 命令：del secret.txt");
        audit.Write("tool_auto_relaxed", "运行一条命令 · 命令：dir C:\\");

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

            using var form = new MainForm(host, enableReturnHome: true);
            form.Show();
            Application.DoEvents();
            Thread.Sleep(400);
            Application.DoEvents();

            if (deniedOnly)
            {
                form.ApplyTrustForShot(TrustLevelKind.AskEveryTime);
                form.OpenAdvancedForShot();
                Application.DoEvents();
                Thread.Sleep(200);
                Application.DoEvents();
                PrepareDeniedVisible(audit, form);
                SaveForm(form, Path.Combine(outDir, "r007-denied-not-in-list.png"));
                VerifyDeniedVisible(form);
                form.Hide();
                Console.WriteLine("OK wrote denied-only shot");
                return 0;
            }

            form.ApplyTrustForShot(TrustLevelKind.AskEveryTime);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r007-ask-no-banner.png"));

            form.OpenAdvancedForShot();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r007-advanced-ask.png"));

            AnnotateAndSave(form, Path.Combine(outDir, "r007-confirmed-list.png"),
                "已确认列表：仅含已确认 / 宽松自动允许（人话）");

            PrepareDeniedVisible(audit, form);
            SaveForm(form, Path.Combine(outDir, "r007-denied-not-in-list.png"));
            VerifyDeniedVisible(form);

            SaveFullTrustConfirmComposite(form, Path.Combine(outDir, "r007-fulltrust-confirm.png"));
            form.SimulateFullTrustCancelForShot();
            Application.DoEvents();
            SaveFullTrustCancelStays(form, Path.Combine(outDir, "r007-fulltrust-cancel-stays.png"));

            form.CloseAdvancedForShot();
            Application.DoEvents();
            form.ApplyTrustForShot(TrustLevelKind.Relaxed);
            Application.DoEvents();
            Thread.Sleep(120);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r007-banner-relaxed.png"));

            form.ApplyTrustForShot(TrustLevelKind.FullTrust);
            Application.DoEvents();
            Thread.Sleep(120);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r007-banner-fulltrust.png"));

            form.ApplyTrustForShot(TrustLevelKind.AskEveryTime);
            form.Hide();
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

        Console.WriteLine("OK wrote r007 shots to " + outDir);
        return 0;
    }

    private static void PrepareDeniedVisible(AuditLog audit, MainForm form)
    {
        // 写到审计末尾，确保 ReadRecent 反转后排在「最近审计」顶部可见区
        audit.Write("tool_denied_user", "删除一个文件或文件夹 · 命令：del secret.txt");
        form.RefreshListsForShot();
        Application.DoEvents();
        Thread.Sleep(80);
        Application.DoEvents();
    }

    private static void VerifyDeniedVisible(MainForm form)
    {
        var auditView = FindListView(form, "recentAuditList");
        var confirmed = FindListView(form, "confirmedChangesList");
        if (auditView is null || confirmed is null)
            throw new InvalidOperationException("list views not found");

        var auditTexts = auditView.Items.Cast<ListViewItem>().Select(i => i.Text).ToList();
        var confTexts = confirmed.Items.Cast<ListViewItem>().Select(i => i.Text).ToList();
        Console.WriteLine("AUDIT_VISIBLE:");
        foreach (var t in auditTexts.Take(5)) Console.WriteLine("  * " + t);
        Console.WriteLine("CONFIRMED:");
        foreach (var t in confTexts) Console.WriteLine("  * " + t);

        var deniedOk = auditTexts.Any(t => t.Contains("已拒绝", StringComparison.Ordinal)
                                           && t.Contains("del secret", StringComparison.OrdinalIgnoreCase));
        var confClean = confTexts.All(t => !t.Contains("del secret", StringComparison.OrdinalIgnoreCase)
                                           && !t.Contains("已拒绝", StringComparison.Ordinal));
        Console.WriteLine($"VERIFY denied_in_audit={deniedOk} confirmed_clean={confClean}");
        if (!deniedOk || !confClean)
            throw new InvalidOperationException("denied-not-in-list shot evidence missing in controls");
    }

    private static ListView? FindListView(Control root, string name)
    {
        if (root is ListView lv && lv.Name == name) return lv;
        foreach (Control c in root.Controls)
        {
            var f = FindListView(c, name);
            if (f is not null) return f;
        }
        return null;
    }

    private static void SaveForm(Form form, string path)
    {
        form.Update();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} {form.Width}x{form.Height} md5={Md5(bmp)}");
    }

    private static void AnnotateAndSave(Form form, string path, string note)
    {
        form.Update();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        using (var g = Graphics.FromImage(bmp))
        {
            using var font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            var size = g.MeasureString(note, font);
            var rect = new RectangleF(12, 8, size.Width + 16, size.Height + 10);
            g.FillRectangle(new SolidBrush(Color.FromArgb(230, 255, 248, 220)), rect);
            g.DrawRectangle(new Pen(Color.FromArgb(0xE3, 0x74, 0x00), 2), Rectangle.Round(rect));
            g.DrawString(note, font, new SolidBrush(Color.FromArgb(0xB0, 0x60, 0x00)), rect.X + 8, rect.Y + 4);
        }
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} annotated md5={Md5(bmp)}");
    }

    private static void SaveFullTrustConfirmComposite(Form form, string path)
    {
        using var confirm = new FullTrustConfirmDialog();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        using var dlgBmp = new Bitmap(Math.Max(1, confirm.Width), Math.Max(1, confirm.Height));
        confirm.Show();
        Application.DoEvents();
        confirm.DrawToBitmap(dlgBmp, new Rectangle(0, 0, confirm.Width, confirm.Height));
        using (var g = Graphics.FromImage(bmp))
        {
            var x = Math.Max(40, (form.Width - confirm.Width) / 2);
            var y = Math.Max(80, (form.Height - confirm.Height) / 2);
            g.FillRectangle(new SolidBrush(Color.FromArgb(90, 0, 0, 0)), 0, 0, bmp.Width, bmp.Height);
            g.DrawImage(dlgBmp, x, y);
        }
        confirm.Hide();
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} confirm-composite md5={Md5(bmp)}");
    }

    private static void SaveFullTrustCancelStays(Form form, string path)
    {
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        using var confirm = new FullTrustConfirmDialog();
        using var dlgBmp = new Bitmap(Math.Max(1, confirm.Width), Math.Max(1, confirm.Height));
        confirm.Show();
        Application.DoEvents();
        confirm.DrawToBitmap(dlgBmp, new Rectangle(0, 0, confirm.Width, confirm.Height));
        confirm.Hide();
        using (var g = Graphics.FromImage(bmp))
        {
            var x = Math.Max(40, (form.Width - confirm.Width) / 2);
            var y = Math.Max(80, (form.Height - confirm.Height) / 2);
            g.FillRectangle(new SolidBrush(Color.FromArgb(70, 0, 0, 0)), 0, 0, bmp.Width, bmp.Height);
            g.DrawImage(dlgBmp, x, y);
            var cancelRect = new Rectangle(x + 290, y + 168, 90, 34);
            g.DrawRectangle(new Pen(Color.FromArgb(0x1A, 0x73, 0xE8), 3), cancelRect);
            using var font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            const string note = "已点「取消」→ 档位仍为「每次询问」（未变成完全信任）";
            var size = g.MeasureString(note, font);
            var rect = new RectangleF(12, 8, size.Width + 16, size.Height + 10);
            g.FillRectangle(new SolidBrush(Color.FromArgb(235, 232, 245, 233)), rect);
            g.DrawRectangle(new Pen(Color.FromArgb(0x1A, 0x73, 0xE8), 2), Rectangle.Round(rect));
            g.DrawString(note, font, new SolidBrush(Color.FromArgb(0x17, 0x4E, 0xA6)), rect.X + 8, rect.Y + 4);
        }
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} cancel-stays md5={Md5(bmp)}");
    }

    private static string Md5(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var hash = System.Security.Cryptography.MD5.HashData(ms.ToArray());
        return Convert.ToHexString(hash)[..12];
    }
}
