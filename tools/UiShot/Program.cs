using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
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

        using (var home = new ModeSelectForm())
        {
            home.Show();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveForm(home, Path.Combine(outDir, "r006-home.png"));
            home.Hide();
        }

        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = true;
        settings.NetworkAllowMaybeDenied = false;
        settings.Save();

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r006-audit.log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(
            port: 17996,
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

            using var form = new RemoteAssist.Agent.MainForm(host, enableReturnHome: true);
            form.Show();
            Application.DoEvents();
            Thread.Sleep(700);
            Application.DoEvents();
            SaveForm(form, Path.Combine(outDir, "r006-agent.png"));

            using var confirm = BuildRefreshConfirmDialog(form.Font);
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
            var confirmPath = Path.Combine(outDir, "r006-agent-refresh-confirm.png");
            bmp.Save(confirmPath, ImageFormat.Png);
            Console.WriteLine("saved " + confirmPath + " composite");
            confirm.Hide();
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
            SaveForm(helper.Form, Path.Combine(outDir, "r006-helper-disconnected.png"));

            var main = helper.Form;
            var deviceList = FindControl(main, c => c is ListBox) as ListBox;
            if (deviceList is not null)
            {
                deviceList.Items.Clear();
                deviceList.Items.Add("dev-living\t客厅电脑\t在线");
                deviceList.Items.Add("dev-study\t书房电脑\t离线");
                Application.DoEvents();
                SaveForm(main, Path.Combine(outDir, "r006-helper-account-unselected.png"));

                deviceList.SelectedIndex = 0;
                Application.DoEvents();
                Thread.Sleep(120);
                Application.DoEvents();
                SaveForm(main, Path.Combine(outDir, "r006-helper-account-selected.png"));
                var own = FindControl(main, c => c is Button b && (b.Text.Contains("连接：", StringComparison.Ordinal) || b.Text.Contains("连接这台", StringComparison.Ordinal))) as Button;
                Console.WriteLine("own-btn=[" + (own?.Text ?? "") + "]");
            }

            // 与真连接路径一致：先展开常用操作，再同步「已连接对方电脑」状态文案
            var setUi = main.GetType().GetMethod("SetConnectedUi", BindingFlags.Instance | BindingFlags.NonPublic);
            setUi?.Invoke(main, new object[] { true });
            var setStatus = main.GetType().GetMethod("SetStatus", BindingFlags.Instance | BindingFlags.NonPublic);
            setStatus?.Invoke(main, new object[] { "已连接对方电脑" });
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveForm(main, Path.Combine(outDir, "r006-helper-connected.png"));
            Console.WriteLine("connected-status=[" + FindStatusText(main) + "]");
            helper.Form.Hide();
        }

        Console.WriteLine("OK wrote r006 shots to " + outDir);
        return 0;
    }

    private static Form BuildRefreshConfirmDialog(Font font)
    {
        var dlg = new Form
        {
            Text = "刷新会话码",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.Manual,
            ClientSize = new Size(420, 160),
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = font
        };
        dlg.Controls.Add(new Label
        {
            AutoSize = false,
            Location = new Point(20, 20),
            Size = new Size(380, 60),
            Text = FriendConnectGuide.RefreshSessionConfirm
        });
        var ok = new Button
        {
            Text = FriendConnectGuide.RefreshSessionConfirmOk,
            Location = new Point(120, 100),
            Size = new Size(120, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0x1A, 0x73, 0xE8),
            ForeColor = Color.White
        };
        ok.FlatAppearance.BorderSize = 0;
        var cancel = new Button
        {
            Text = FriendConnectGuide.RefreshSessionConfirmCancel,
            Location = new Point(250, 100),
            Size = new Size(90, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)
        };
        cancel.FlatAppearance.BorderColor = Color.FromArgb(0xDA, 0xDE, 0xE3);
        cancel.FlatAppearance.BorderSize = 1;
        dlg.Controls.Add(ok);
        dlg.Controls.Add(cancel);
        return dlg;
    }

    private static Control? FindControl(Control root, Func<Control, bool> pred)
    {
        if (pred(root)) return root;
        foreach (Control c in root.Controls)
        {
            var f = FindControl(c, pred);
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
        Console.WriteLine($"saved {path} {form.Width}x{form.Height} title={form.Text}");
    }
}
