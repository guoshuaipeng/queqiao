using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.App;

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

        using (var home = new ModeSelectForm())
        {
            home.Show();
            Application.DoEvents();
            SaveForm(home, Path.Combine(outDir, "r001-after-home.png"));
            home.Hide();
        }

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-audit.log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(
            port: 17991,
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
            using var form = new MainForm(host, enableReturnHome: true);
            form.Show();
            Application.DoEvents();
            Thread.Sleep(400);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r001-after-agent.png"));
            host.RefreshSessionCode();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r001-after-agent-refreshed.png"));
            form.Hide();
        }
        finally
        {
            host.Stop();
        }

        Console.WriteLine("OK wrote shots to " + outDir);
        return 0;
    }

    private static void SaveForm(Form form, string path)
    {
        form.Update();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} {form.Width}x{form.Height} title={form.Text}");
    }
}
