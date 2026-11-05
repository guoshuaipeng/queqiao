using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace UiShot;

/// <summary>R031：接受协助端对方连上 / 断开后人话主状态 + 协助方列表。</summary>
internal static class R031Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = true;
        settings.NetworkAllowMaybeDenied = false;
        settings.TrustLevelPreference = nameof(TrustLevelKind.AskEveryTime);
        settings.Save();

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r031-audit.log");
        try { if (File.Exists(auditPath)) File.Delete(auditPath); } catch { /* ignore */ }
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

            // 1) 还没人连上
            agent.PreparePeerIdleForShot();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r031-accept-idle.png"));
            Verify(agent,
                FriendConnectGuide.StatusReadyToShare,
                FriendConnectGuide.PairedClientsEmpty,
                "复制码+密码");

            // 2) 对方已连上（人话称呼，非内部编号）
            agent.PreparePeerConnectedForShot("小明");
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r031-accept-peer-connected.png"));
            Verify(agent,
                FriendConnectGuide.StatusPeerConnected,
                "小明",
                "复制码+密码");
            VerifyNot(agent, "（暂无）");

            // 3) 对方断开后
            agent.PreparePeerDisconnectedForShot();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r031-accept-peer-disconnected.png"));
            Verify(agent,
                FriendConnectGuide.StatusReadyToShare,
                FriendConnectGuide.PairedClientsEmpty,
                "复制码+密码");

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

        Console.WriteLine("OK wrote r031 Windows shots (ui-shot) to " + outDir);
        return 0;
    }

    private static void Verify(Control root, params string[] needles)
    {
        var blob = Walk(root);
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r031 shot missing text: " + n);
        }
    }

    private static void VerifyNot(Control root, params string[] banned)
    {
        var blob = Walk(root);
        foreach (var n in banned)
        {
            if (blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException("r031 shot unexpected text: " + n);
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
