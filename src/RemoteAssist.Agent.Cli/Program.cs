using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 跨平台被控 CLI（R009）：文案见 docs/first-run-mac-linux.md 第 3、4 节。
/// </summary>
internal static class Program
{
    private const int DefaultPort = 17890;
    private const int DefaultConfirmTimeoutSeconds = 60;

    public static async Task Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* ignore */ }
        try { Console.InputEncoding = Encoding.UTF8; } catch { /* ignore */ }

        if (args.Any(a => a is "-h" or "--help" or "help"))
        {
            PrintHelp();
            return;
        }

        var port = DefaultPort;
        var autoYes = args.Any(a => a is "--yes" or "--auto-yes");
        var insecure = args.Any(a => a is "--insecure");
        var enableLan = args.Any(a => a is "--lan");
        var confirmTimeoutSeconds = DefaultConfirmTimeoutSeconds;
        string? relayEndpoint = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                port = p;
            if (args[i] is "--confirm-timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t))
                confirmTimeoutSeconds = Math.Clamp(t, 5, 600);
            if (args[i] is "--relay" && i + 1 < args.Length)
                relayEndpoint = args[++i];
        }

        var auditDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteAssist");
        Directory.CreateDirectory(auditDir);
        var auditPath = Path.Combine(auditDir, "audit-cli.log");
        var audit = new AuditLog(auditPath);

        var host = new AgentHost(
            port,
            autoYes: autoYes,
            headless: true,
            confirmTimeoutSeconds: confirmTimeoutSeconds,
            audit: audit,
            auditPath: auditPath,
            insecure: insecure,
            preferRelay: true,
            enableLanListen: enableLan,
            relayEndpoint: relayEndpoint);

        var lastPrintedCode = "";
        var announcedPaired = false;

        void PrintWaitingCredentials(bool afterRefresh)
        {
            var code = host.SessionCode;
            var pwd = host.SessionPassword;
            if (afterRefresh)
            {
                Console.WriteLine("已换新的会话码和验证密码。之前发出去的码不能再用。");
            }
            else
            {
                Console.WriteLine("鹊桥远程协助已启动，正在等待对方连接。");
                Console.WriteLine("把下面两行发给要帮你的人：");
            }

            var share = FriendConnectGuide.FormatShareClipboard(code, pwd);
            Console.WriteLine(share);
            TryCopyShare(share);
            lastPrintedCode = code;
        }

        void OnSessionOrRelay()
        {
            try
            {
                var phase = host.RelayPhase;
                if (phase is "ready" or "waiting")
                {
                    if (!string.Equals(lastPrintedCode, host.SessionCode, StringComparison.Ordinal))
                    {
                        var refresh = lastPrintedCode.Length > 0;
                        PrintWaitingCredentials(afterRefresh: refresh);
                    }
                }
                else if (phase is "paired" && !announcedPaired)
                {
                    announcedPaired = true;
                    Console.WriteLine("已有人连上。对方可以查看进程、日志和文件；要改动时会再问你。");
                }
            }
            catch { /* ignore */ }
        }

        host.SessionChanged += OnSessionOrRelay;
        host.RelayStatusChanged += OnSessionOrRelay;

        host.Start();

        for (var i = 0; i < 75 && host.RelayPhase is "starting" or "dialing"; i++)
            await Task.Delay(200).ConfigureAwait(false);

        if (lastPrintedCode.Length == 0 && host.RelayPhase is "ready" or "waiting" or "paired")
            PrintWaitingCredentials(afterRefresh: false);
        else if (lastPrintedCode.Length == 0)
            PrintWaitingCredentials(afterRefresh: false);

        var exit = new TaskCompletionSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            host.Stop();
            exit.TrySetResult();
        };
        await exit.Task.ConfigureAwait(false);
    }

    private static void TryCopyShare(string share)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // CLI 在无 WinForms 程序集时用 clip.exe
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c clip",
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p is not null)
                {
                    p.StandardInput.Write(share);
                    p.StandardInput.Close();
                    p.WaitForExit(2000);
                    if (p.ExitCode == 0)
                    {
                        Console.WriteLine("已复制，直接发给对方即可。");
                        return;
                    }
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "pbcopy",
                    RedirectStandardInput = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                if (p is not null)
                {
                    p.StandardInput.Write(share);
                    p.StandardInput.Close();
                    p.WaitForExit(2000);
                    if (p.ExitCode == 0)
                    {
                        Console.WriteLine("已复制，直接发给对方即可。");
                        return;
                    }
                }
            }
            else
            {
                foreach (var tool in new[] { "wl-copy", "xclip", "xsel" })
                {
                    try
                    {
                        var args = tool switch
                        {
                            "xclip" => "-selection clipboard",
                            "xsel" => "--clipboard --input",
                            _ => ""
                        };
                        var psi = new ProcessStartInfo
                        {
                            FileName = tool,
                            Arguments = args,
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var p = Process.Start(psi);
                        if (p is null) continue;
                        p.StandardInput.Write(share);
                        p.StandardInput.Close();
                        p.WaitForExit(2000);
                        if (p.ExitCode == 0)
                        {
                            Console.WriteLine("已复制，直接发给对方即可。");
                            return;
                        }
                    }
                    catch { /* try next */ }
                }
            }
        }
        catch { /* fall through */ }

        Console.WriteLine("用鼠标选中上面两行，复制后发给对方。");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            鹊桥被控 CLI — 接受协助（终端确认写操作）
            文案见 docs/first-run-mac-linux.md 第 3、4 节。

            用法:
              RemoteAssist.Agent.Cli [--relay host:port] [--port 17890] [--confirm-timeout 60] [--lan] [--insecure]

            这个程序比较小，但需要先安装「.NET 8 运行时」。装一次就可以，它不是开发工具。
            """);
    }
}
