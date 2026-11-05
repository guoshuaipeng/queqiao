using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace UiShot;

/// <summary>R032：底栏不再常驻托盘说明；关窗后一次性反馈。</summary>
internal static class R032Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = true;
        settings.NetworkAllowMaybeDenied = false;
        settings.TrustLevelPreference = nameof(TrustLevelKind.AskEveryTime);
        settings.Save();

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r032-audit.log");
        try { if (File.Exists(auditPath)) File.Delete(auditPath); } catch { /* ignore */ }
        var audit = new AuditLog(auditPath);

        var host = new AgentHost(
            port: 17994,
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

            // 1) 默认底栏空白，不含常驻托盘句；复制码+密码仍是主按钮
            agent.PrepareTrayStayIdleForShot();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r032-accept-statusbar-empty.png"));
            Verify(agent, "复制码+密码", FriendConnectGuide.StatusReadyToShare);
            VerifyNot(agent, TrayStayCopy.RemovedDefaultStatusBar);
            VerifyNot(agent, TrayStayCopy.HiddenToTrayFeedback);

            // 2) 关窗藏托盘后的一次性反馈（窗仍显示便于截图）
            agent.PrepareHiddenToTrayFeedbackForShot();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r032-accept-hidden-to-tray-feedback.png"));
            Verify(agent, TrayStayCopy.HiddenToTrayFeedback, "复制码+密码");
            VerifyNot(agent, TrayStayCopy.RemovedDefaultStatusBar);

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

        // 气泡文案证明：常量即 AgentTrayContext 所用；写出旁注文本供文档引用
        var proof = Path.Combine(outDir, "r032-balloon-copy-proof.txt");
        File.WriteAllText(proof,
            "ui-shot 难以真拍 NotifyIcon 气泡。启动气泡文案未改，由 TrayStayCopy 常量与 AgentTrayContext 共用：\n" +
            "BalloonDefault=" + TrayStayCopy.BalloonDefault + "\n" +
            "BalloonWithReturnHome=" + TrayStayCopy.BalloonWithReturnHome + "\n");
        Console.WriteLine("OK wrote r032 Windows shots (ui-shot) to " + outDir);
        return 0;
    }

    private static void Verify(Control root, params string[] needles)
    {
        var blob = Walk(root);
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r032 shot missing text: " + n);
        }
    }

    private static void VerifyNot(Control root, params string[] banned)
    {
        var blob = Walk(root);
        foreach (var n in banned)
        {
            if (blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r032 shot unexpected text: " + n);
        }
    }

    private static string Walk(Control root)
    {
        var sb = new System.Text.StringBuilder();
        void Rec(Control c)
        {
            if (!string.IsNullOrEmpty(c.Text)) sb.AppendLine(c.Text);
            if (c is ListView lv)
            {
                foreach (ListViewItem it in lv.Items)
                    sb.AppendLine(it.Text);
            }
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
