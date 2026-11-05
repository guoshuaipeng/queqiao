using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
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

        // Helper: MCP install guide visible
        using (var helper = new HelperDesktopController(enableReturnHome: true))
        {
            helper.Show();
            Application.DoEvents();
            Thread.Sleep(250);
            Application.DoEvents();
            // scroll/size already shows MCP card near bottom — capture full form
            SaveForm(helper.Form, Path.Combine(outDir, "r005-helper-mcp-guide.png"));
            helper.Form.Hide();
        }

        // Agent: network ready => banner hidden; friend path no MCP
        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = false;
        settings.NetworkAllowMaybeDenied = false;
        settings.Save();

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r005-audit.log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(
            port: 17995,
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
            // wait until relay phase ready if possible
            for (var i = 0; i < 40; i++)
            {
                if (host.RelayPhase is "ready" or "waiting" or "paired") break;
                Thread.Sleep(100);
            }

            using var form = new RemoteAssist.Agent.MainForm(host, enableReturnHome: true);
            form.Show();
            Application.DoEvents();
            Thread.Sleep(600);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r005-agent-ready-no-banner.png"));

            // Force denied recovery banner
            typeof(RemoteAssist.Agent.MainForm)
                .GetMethod("ShowDeniedBanner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(form, null);
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r005-agent-denied-banner.png"));
            // OpenFirewallButtonText check
            foreach (Control c in form.Controls)
            {
                if (c.Name != "banner") continue;
                foreach (Control b in c.Controls)
                {
                    if (b is Button btn && btn.Text.Contains("防火墙", StringComparison.Ordinal))
                        Console.WriteLine("firewall-btn=[" + btn.Text + "]");
                }
            }
            form.Hide();
        }
        finally
        {
            host.Stop();
        }

        Console.WriteLine("OK wrote r005 shots to " + outDir);
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
