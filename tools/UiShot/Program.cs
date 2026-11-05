using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.App;
using RemoteAssist.Helper.UI;
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

        // force banner for agent shot
        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = false;
        settings.NetworkAllowMaybeDenied = false;
        settings.Save();

        using (var home = new ModeSelectForm())
        {
            home.Show();
            Application.DoEvents();
            SaveForm(home, Path.Combine(outDir, "r003-home.png"));
            home.Hide();
        }

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r003-audit.log");
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
            using var form = new RemoteAssist.Agent.MainForm(host, enableReturnHome: true);
            form.Show();
            Application.DoEvents();
            Thread.Sleep(500);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r003-agent.png"));

            // open advanced for a secondary shot? keep default declutter as primary
            form.Hide();
        }
        finally
        {
            host.Stop();
        }

        // pair confirm dialog
        using (var pair = new PairConfirmDialog(new PairRequestInfo(
                   RemoteEndpoint: "relay-guest",
                   ClientName: "helper-ui",
                   RequestedPermission: PermissionLevel.Assist,
                   TimeoutSeconds: 30)))
        {
            pair.Show();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveForm(pair, Path.Combine(outDir, "r003-pair-confirm.png"));
            pair.Hide();
        }

        using (var helper = new HelperDesktopController(enableReturnHome: true))
        {
            helper.Show();
            Application.DoEvents();
            Thread.Sleep(300);
            Application.DoEvents();
            SaveForm(helper.Form, Path.Combine(outDir, "r003-helper.png"));
            helper.Form.Hide();
        }

        Console.WriteLine("OK wrote r003 shots to " + outDir);
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
