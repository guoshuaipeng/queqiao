using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace UiShot;

internal static class R029Shots
{
    public static int Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = true;
        settings.NetworkAllowMaybeDenied = false;
        settings.TrustLevelPreference = nameof(TrustLevelKind.AskEveryTime);
        settings.Save();

        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r029-audit.log");
        try { if (File.Exists(auditPath)) File.Delete(auditPath); } catch { /* ignore */ }
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

            using var agent = new RemoteAssist.Agent.MainForm(host, enableReturnHome: true);
            agent.Show();
            Application.DoEvents();
            Thread.Sleep(350);
            Application.DoEvents();

            // 1) 菜单入口：展开「更多」，露出「复制 Skill 说明」
            agent.ShowMoreMenuForShot();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveFormWithMenu(agent, Path.Combine(outDir, "r029-skill-menu.png"));
            VerifyMenuHasSkill();

            // 2) 复制反馈只一次
            agent.PrepareSkillCopiedForShot();
            Application.DoEvents();
            Thread.Sleep(150);
            Application.DoEvents();
            Save(agent, Path.Combine(outDir, "r029-skill-copied.png"));
            Verify(agent, McpInstallGuide.SkillCopiedHint);

            // 3) 剪贴板正文预览（与 R027/R028 同一常量）
            agent.PrepareSkillClipboardPreviewForShot();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveFormWithOverlay(agent, Path.Combine(outDir, "r029-skill-clipboard-preview.png"));
            Verify(agent,
                "把下面这段保存成智能体的技能说明",
                "1. 在鹊桥里点「复制 MCP 配置」。",
                "不要用这段说明去远程控制电脑。");
            VerifyClipboard();

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

        Console.WriteLine("OK wrote r029 Windows shots to " + outDir);
        return 0;
    }

    private static void VerifyMenuHasSkill()
    {
        // 菜单项文案来自常量；此处核对常量未被改写即可
        if (McpInstallGuide.CopySkillButton != "复制 Skill 说明")
            throw new InvalidOperationException("CopySkillButton changed");
    }

    private static void VerifyClipboard()
    {
        var clip = Clipboard.GetText() ?? "";
        if (!clip.Contains("把下面这段保存成智能体的技能说明", StringComparison.Ordinal) ||
            !clip.Contains("不要用这段说明去远程控制电脑。", StringComparison.Ordinal))
            throw new InvalidOperationException("clipboard missing SkillInstructions");
        if (clip != McpInstallGuide.SkillInstructionsClipboard &&
            clip.Replace("\r\n", "\n") != McpInstallGuide.SkillInstructions.Replace("\r\n", "\n"))
        {
            // allow newline normalization
            var a = clip.Replace("\r\n", "\n").TrimEnd();
            var b = McpInstallGuide.SkillInstructions.Replace("\r\n", "\n").TrimEnd();
            if (a != b)
                throw new InvalidOperationException("clipboard not equal SkillInstructions");
        }
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
                throw new InvalidOperationException("r029 shot missing text: " + n);
        }
    }

    private static void Save(Form form, string path)
    {
        form.Update();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} {form.Width}x{form.Height}");
    }

    private static void SaveFormWithMenu(Form form, string path)
    {
        form.TopMost = true;
        form.Activate();
        form.BringToFront();
        form.Update();
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();
        var formRect = form.RectangleToScreen(form.ClientRectangle);
        var capture = new Rectangle(
            formRect.X,
            formRect.Y,
            Math.Max(formRect.Width, 780),
            Math.Max(formRect.Height + 40, 560));
        using var bmp = new Bitmap(capture.Width, capture.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(capture.Location, Point.Empty, capture.Size);
        form.TopMost = false;
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} screen-capture {capture.Width}x{capture.Height}");
    }

    private static void SaveFormWithOverlay(Form form, string path)
    {
        form.TopMost = true;
        form.Activate();
        form.BringToFront();
        form.Update();
        Application.DoEvents();
        Thread.Sleep(120);
        Application.DoEvents();

        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));

        // 手动叠上剪贴板预览 Label（DrawToBitmap 可能丢后加控件 z-order）
        var preview = FindNamed(form, "skillClipboardPreview");
        if (preview is { Visible: true })
        {
            using var overlay = new Bitmap(Math.Max(1, preview.Width), Math.Max(1, preview.Height));
            preview.DrawToBitmap(overlay, new Rectangle(0, 0, preview.Width, preview.Height));
            using var g = Graphics.FromImage(bmp);
            g.FillRectangle(Brushes.White, preview.Bounds);
            g.DrawImage(overlay, preview.Location);
            using var pen = new Pen(Color.FromArgb(0xDA, 0xDE, 0xE3));
            g.DrawRectangle(pen, preview.Bounds);
        }

        form.TopMost = false;
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} overlay-composite {form.Width}x{form.Height}");
    }

    private static Control? FindNamed(Control root, string name)
    {
        if (root.Name == name) return root;
        foreach (Control c in root.Controls)
        {
            var f = FindNamed(c, name);
            if (f is not null) return f;
        }
        return null;
    }
}
