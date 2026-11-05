using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.App;
using RemoteAssist.Helper.UI;

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
            SaveForm(home, Path.Combine(outDir, "r002-home.png"));
            home.Hide();
        }

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r002-audit.log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(
            port: 17992,
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
            using var form = new RemoteAssist.Agent.MainForm(host, enableReturnHome: true);
            form.Show();
            Application.DoEvents();
            // 模拟账号免密审计，便于截图展示状态栏提示与审计文案
            audit.Write("pair_owner_auto", "client=uishot-owner; remote=relay-device/dev-demo; perm=Assist");
            Application.DoEvents();
            Thread.Sleep(400);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r002-agent-ownerlink-hint.png"));
            form.Hide();
        }
        finally
        {
            host.Stop();
        }

        using (var helper = new HelperDesktopController(enableReturnHome: true))
        {
            helper.Show();
            Application.DoEvents();
            Thread.Sleep(300);
            Application.DoEvents();
            SaveForm(helper.Form, Path.Combine(outDir, "r002-helper.png"));
            helper.Form.Hide();
        }

        Console.WriteLine("OK wrote r002 shots to " + outDir);
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
