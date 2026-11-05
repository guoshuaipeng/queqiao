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
        var outDir = args.Length > 0 ? args[0] : @"E:\IdeaProjects\remote-assist\docs\screenshots";
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
        // 人话种子（对齐 WriteConfirmCopy Headline）；拒绝项不得入已确认列表
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
            Thread.Sleep(500);
            Application.DoEvents();

            // —— 主界面：每次询问无提示 ——
            form.ApplyTrustForShot(TrustLevelKind.AskEveryTime);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r007-ask-no-banner.png"));

            // —— 高级展开：三档完整，默认每次询问 ——
            form.OpenAdvancedForShot();
            Application.DoEvents();
            Thread.Sleep(250);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r007-advanced-ask.png"));

            // —— 已确认列表（人话条目）—— 与 advanced-ask 区分：加截图角标并确保列表可见
            AnnotateAndSave(form, Path.Combine(outDir, "r007-confirmed-list.png"),
                "已确认列表：仅含已确认 / 宽松自动允许（人话）");

            // —— 拒绝不入列：角标点明拒绝命令只在「最近审计」——
            AnnotateAndSave(form, Path.Combine(outDir, "r007-denied-not-in-list.png"),
                "对照：最近审计有「已拒绝…del secret」，上方已确认列表没有");

            // —— 完全信任二次确认原文 ——
            SaveFullTrustConfirmComposite(form, Path.Combine(outDir, "r007-fulltrust-confirm.png"));

            // —— 取消后仍每次询问：先模拟取消，再叠对话框并标「取消」——
            form.SimulateFullTrustCancelForShot();
            Application.DoEvents();
            SaveFullTrustCancelStays(form, Path.Combine(outDir, "r007-fulltrust-cancel-stays.png"));

            // —— 主界面提示 ——
            form.CloseAdvancedForShot();
            Application.DoEvents();

            form.ApplyTrustForShot(TrustLevelKind.Relaxed);
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r007-banner-relaxed.png"));

            form.ApplyTrustForShot(TrustLevelKind.FullTrust);
            Application.DoEvents();
            Thread.Sleep(150);
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
        // 背景：取消后仍为「每次询问」（高级展开）
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

            // 强调「取消」按钮区域（对话框右下）
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
