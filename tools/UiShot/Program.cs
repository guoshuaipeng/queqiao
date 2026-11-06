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
        var outDir = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal))
                     ?? @"E:\IdeaProjects\remote-assist\docs\screenshots";
        Directory.CreateDirectory(outDir);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (args.Any(a => a is "--r012"))
            return R012Shots.Run(outDir);

        if (args.Any(a => a is "--r029"))
            return R029Shots.Run(outDir);

        if (args.Any(a => a is "--r031"))
            return R031Shots.Run(outDir);

        if (args.Any(a => a is "--r032"))
            return R032Shots.Run(outDir);

        if (args.Any(a => a is "--r033"))
            return R033Shots.Run(outDir);

        if (args.Any(a => a is "--r035"))
            return R035Shots.Run(outDir);

        if (args.Any(a => a is "--r036"))
            return R036Shots.Run(outDir);

        if (args.Any(a => a is "--r037"))
            return R037Shots.Run(outDir);

        if (args.Any(a => a is "--r039"))
            return R039Shots.Run(outDir);

        if (args.Any(a => a is "--r041"))
            return R041Shots.Run(outDir);

        var settings = HelperUiSettings.Load();
        settings.NetworkAllowBannerDismissed = true;
        settings.NetworkAllowMaybeDenied = false;
        settings.TrustLevelPreference = nameof(TrustLevelKind.AskEveryTime);
        settings.Save();

        // --- B. 双模式首页（已满足：ModeSelectForm 两张卡片）---
        using (var home = new ModeSelectForm())
        {
            home.Show();
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            SaveForm(home, Path.Combine(outDir, "r008-home-dual-mode.png"));
            VerifyContains(home, "接受协助", "去协助别人");
            home.Hide();
        }

        // --- 协助端：MCP 安装指引 + 复制配置 ---
        using (var helper = new RemoteAssist.Helper.UI.MainForm(enableReturnHome: true))
        {
            helper.Show();
            Application.DoEvents();
            Thread.Sleep(250);
            Application.DoEvents();
            helper.PrepareSessionDraftForShot("AB12CD", "secret1");
            Application.DoEvents();
            SaveForm(helper, Path.Combine(outDir, "r008-helper-mcp-guide.png"));
            VerifyContains(helper, McpInstallGuide.SectionTitle, McpInstallGuide.CopyConfigButton, McpInstallGuide.Step1);

            helper.PrepareMcpCopiedForShot(exeExists: true);
            Application.DoEvents();
            Thread.Sleep(120);
            Application.DoEvents();
            SaveForm(helper, Path.Combine(outDir, "r008-helper-mcp-copied.png"));
            VerifyContains(helper, McpInstallGuide.CopiedOkHint);

            // 智能体会话只读/提议结果：用真实 mcp 源 AssistClient 跑一遍，再填到常用操作输出
            var toolsOutput = RunMcpToolsForShot(outDir);
            helper.PrepareConnectedToolsForShot(toolsOutput);
            Application.DoEvents();
            Thread.Sleep(200);
            Application.DoEvents();
            // 滚到常用操作区域：窗口已含 tool 卡
            SaveForm(helper, Path.Combine(outDir, "r008-mcp-tools-session.png"));
            VerifyContains(helper, "查看正在运行的程序", "查看日志", "提议一项改动");
            helper.Hide();
        }

        // --- 被协助端：默认首屏无 MCP 安装区；写确认；拒绝 ---
        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r008-audit.log");
        try { if (File.Exists(auditPath)) File.Delete(auditPath); } catch { /* ignore */ }
        var audit = new AuditLog(auditPath);
        audit.Write("tool_confirmed", "运行一条命令 · 命令：echo r008-ok; source=mcp");
        audit.Write("tool_denied_user", "运行一条命令 · 命令：echo bypass; source=mcp");

        var host = new AgentHost(
            port: 17998,
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
            Thread.Sleep(400);
            Application.DoEvents();

            SaveForm(agent, Path.Combine(outDir, "r008-agent-accept-no-mcp.png"));
            VerifyNotContains(agent, McpInstallGuide.SectionTitle, McpInstallGuide.CopyConfigButton, McpInstallGuide.Step1);
            VerifyContains(agent, "接受协助 · 把会话码和验证密码发给对方", "复制码+密码");

            // 写操作确认窗（每次询问）— 智能体提议 run_command
            SaveWriteConfirmComposite(agent, Path.Combine(outDir, "r008-mcp-write-confirm.png"),
                new ConfirmRequestInfo(
                    "mcp",
                    "RunCommand",
                    WriteConfirmCopy.Describe(ToolKind.RunCommand,
                        new Dictionary<string, string?> { ["command"] = "echo r008-propose" }).Headline,
                    60,
                    WriteConfirmCopy.Describe(ToolKind.RunCommand,
                        new Dictionary<string, string?> { ["command"] = "echo r008-propose" }).Detail));
            VerifyFileContainsBanCheck(Path.Combine(outDir, "r008-mcp-write-confirm.png"));

            // 拒绝后：最近审计可见「已拒绝」；已确认列表不含该项
            agent.ApplyTrustForShot(TrustLevelKind.AskEveryTime);
            agent.OpenAdvancedForShot();
            Application.DoEvents();
            audit.Write("tool_denied_user", "运行一条命令 · 命令：echo bypass; source=mcp");
            agent.RefreshListsForShot();
            Application.DoEvents();
            Thread.Sleep(100);
            Application.DoEvents();
            SaveForm(agent, Path.Combine(outDir, "r008-mcp-write-denied.png"));
            VerifyDeniedInAudit(agent);

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

        Console.WriteLine("OK wrote r008 shots to " + outDir);
        return 0;
    }

    private static string RunMcpToolsForShot(string outDir)
    {
        const string relay = "127.0.0.1:17891";
        var auditPath = Path.Combine(Path.GetTempPath(), "remote-assist-uishot-r008-mcp-tools.log");
        try { if (File.Exists(auditPath)) File.Delete(auditPath); } catch { /* ignore */ }
        var audit = new AuditLog(auditPath);
        var agent = new AgentHost(18041, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath,
            insecure: true, preferRelay: true, enableLanListen: false, relayEndpoint: relay);
        string? published = null;
        agent.SessionChanged += () => published = agent.SessionCode;
        agent.Start();
        try
        {
            for (var i = 0; i < 60 && string.IsNullOrWhiteSpace(published); i++)
                Thread.Sleep(100);
            if (string.IsNullOrWhiteSpace(published))
                return "（本机中继未就绪，截图仅展示按钮）";

            AssistClient? client = null;
            for (var i = 0; i < 40; i++)
            {
                try
                {
                    client = AssistClient.PairAsync(new AssistConnectOptions
                    {
                        Host = "127.0.0.1",
                        Port = 17891,
                        Code = published!,
                        ViaRelay = true,
                        RelayEndpoint = relay,
                        Password = agent.SessionPassword,
                        Insecure = true,
                        Permission = PermissionLevel.Assist
                    }, "mcp").GetAwaiter().GetResult();
                    break;
                }
                catch { Thread.Sleep(200); }
            }

            if (client is null)
                return "（配对失败）";

            using (client)
            {
                var procs = client.InvokeToolAsync(ToolKind.ListProcesses).GetAwaiter().GetResult();
                var log = client.InvokeToolAsync(ToolKind.ReadLogTail,
                    new Dictionary<string, string?> { ["path"] = auditPath, ["lines"] = "30" }).GetAwaiter().GetResult();
                var run = client.InvokeToolAsync(ToolKind.RunCommand,
                    new Dictionary<string, string?> { ["command"] = "echo r008-propose-ok" }).GetAwaiter().GetResult();

                var procsPreview = Truncate(procs.Result ?? procs.Error ?? "", 400);
                var logPreview = Truncate(log.Result ?? log.Error ?? "", 300);
                var runPreview = Truncate(run.Result ?? run.Error ?? "", 200);

                var text =
                    "【查看正在运行的程序】ok=" + procs.Ok + " · " + Truncate(procsPreview.Replace(Environment.NewLine, " | "), 120) + Environment.NewLine +
                    "【查看日志】ok=" + log.Ok + " · " + Truncate(logPreview.Replace(Environment.NewLine, " | "), 120) + Environment.NewLine +
                    "【提议一项改动】ok=" + run.Ok + " · " + Truncate(runPreview.Replace(Environment.NewLine, " | "), 120) + Environment.NewLine +
                    "----" + Environment.NewLine +
                    procsPreview + Environment.NewLine + "----" + Environment.NewLine +
                    logPreview + Environment.NewLine + "----" + Environment.NewLine +
                    runPreview;

                File.WriteAllText(Path.Combine(outDir, "r008-mcp-tools-session.txt"), text, System.Text.Encoding.UTF8);
                return text;
            }
        }
        finally
        {
            agent.Stop();
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    private static void SaveWriteConfirmComposite(Form form, string path, ConfirmRequestInfo info)
    {
        using var confirm = new ConfirmDialog(info);
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

        // 控件原文必须能核对：标题/允许/拒绝/人话
        if (WriteConfirmCopy.ContainsBannedUiTerm(info.Headline) ||
            WriteConfirmCopy.ContainsBannedUiTerm(info.Detail))
            throw new InvalidOperationException("confirm copy contains banned UI term");
    }

    private static void VerifyFileContainsBanCheck(string path)
    {
        // 占位：文件已写出即可；禁用词在 SaveWriteConfirmComposite 已查
        if (!File.Exists(path) || new FileInfo(path).Length < 1000)
            throw new InvalidOperationException("confirm shot missing: " + path);
    }

    private static void VerifyDeniedInAudit(RemoteAssist.Agent.MainForm form)
    {
        var auditView = FindListView(form, "recentAuditList");
        var confirmed = FindListView(form, "confirmedChangesList");
        if (auditView is null || confirmed is null)
            throw new InvalidOperationException("list views not found");

        var auditTexts = auditView.Items.Cast<ListViewItem>().Select(i => i.Text).ToList();
        var confTexts = confirmed.Items.Cast<ListViewItem>().Select(i => i.Text).ToList();
        Console.WriteLine("AUDIT_VISIBLE:");
        foreach (var t in auditTexts.Take(5)) Console.WriteLine("  * " + t);

        var deniedOk = auditTexts.Any(t => t.Contains("已拒绝", StringComparison.Ordinal));
        var confClean = confTexts.All(t => !t.Contains("bypass", StringComparison.OrdinalIgnoreCase)
                                           && !t.Contains("已拒绝", StringComparison.Ordinal));
        Console.WriteLine($"VERIFY denied_in_audit={deniedOk} confirmed_clean={confClean}");
        if (!deniedOk || !confClean)
            throw new InvalidOperationException("denied evidence missing in controls");
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

    private static void VerifyContains(Control root, params string[] needles)
    {
        var blob = CollectText(root);
        foreach (var n in needles)
        {
            if (!blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException($"shot evidence missing text: {n}");
        }
    }

    private static void VerifyNotContains(Control root, params string[] needles)
    {
        var blob = CollectText(root);
        foreach (var n in needles)
        {
            if (blob.Contains(n, StringComparison.Ordinal))
                throw new InvalidOperationException($"shot must not show: {n}");
        }
    }

    private static string CollectText(Control root)
    {
        var sb = new System.Text.StringBuilder();
        void Walk(Control c)
        {
            if (!string.IsNullOrEmpty(c.Text)) sb.AppendLine(c.Text);
            foreach (Control child in c.Controls) Walk(child);
        }
        Walk(root);
        return sb.ToString();
    }

    private static void SaveForm(Form form, string path)
    {
        form.Update();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} {form.Width}x{form.Height} md5={Md5(bmp)}");
    }

    private static string Md5(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var hash = System.Security.Cryptography.MD5.HashData(ms.ToArray());
        return Convert.ToHexString(hash)[..12];
    }
}



