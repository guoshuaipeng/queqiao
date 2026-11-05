using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Helper;

internal static class Program
{
    private const int DefaultPort = 17890;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp();
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "pair" => await CmdPairAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                "oneshot" => await CmdOneshotAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                _ => Fail($"未知命令: {args[0]}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("错误: " + ex.Message);
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            RemoteAssist Helper — 协助端 CLI（局域网）

            用法:
              pair <host> <sessionCode> [--port 17890] [--perm Assist|Guest|Admin]

            配对成功后进入交互:
              procs                     列出进程
              logs <path> [lines]       读日志尾部
              ls <path>                 列目录
              cat <path>                读文件
              run <command...>          提议执行命令（Assist 需对方确认）
              perm                      显示当前权限
              quit                      退出
            """);
    }

    private static async Task<int> CmdPairAsync(string[] args)
    {
        if (args.Length < 2)
            return Fail("用法: pair <host> <sessionCode> [--port N] [--perm Assist]");

        var host = args[0];
        var code = args[1];
        var port = DefaultPort;
        var perm = PermissionLevel.Assist;

        for (var i = 2; i < args.Length; i++)
        {
            if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[++i], out var p))
                port = p;
            else if (args[i] is "--perm" && i + 1 < args.Length && Enum.TryParse(args[++i], true, out PermissionLevel pl))
                perm = pl;
        }

        Console.WriteLine($"正在配对 {host}:{port} 会话码={code} 权限={perm} ...");
        using var client = await AssistClient.PairAsync(host, port, code, perm, "helper-cli").ConfigureAwait(false);
        Console.WriteLine($"配对成功。权限={client.Permission} token={client.Token![..8]}…");
        Console.WriteLine("输入 help 查看命令，quit 退出。");

        while (true)
        {
            Console.Write("helper> ");
            var line = Console.ReadLine();
            if (line is null)
                break;
            line = line.Trim();
            if (line.Length == 0)
                continue;

            var parts = SplitArgs(line);
            var cmd = parts[0].ToLowerInvariant();
            if (cmd is "quit" or "exit" or "q")
                break;
            if (cmd is "help" or "?")
            {
                PrintHelp();
                continue;
            }
            if (cmd is "perm")
            {
                Console.WriteLine(client.Permission);
                continue;
            }

            try
            {
                ToolResponse resp = cmd switch
                {
                    "procs" or "ps" => await client.InvokeToolAsync(ToolKind.ListProcesses).ConfigureAwait(false),
                    "logs" or "log" => await client.InvokeToolAsync(ToolKind.ReadLogTail, new()
                    {
                        ["path"] = parts.ElementAtOrDefault(1) ?? throw new ArgumentException("需要 path"),
                        ["lines"] = parts.ElementAtOrDefault(2) ?? "80"
                    }).ConfigureAwait(false),
                    "ls" or "dir" => await client.InvokeToolAsync(ToolKind.ListDir, new()
                    {
                        ["path"] = parts.ElementAtOrDefault(1) ?? throw new ArgumentException("需要 path")
                    }).ConfigureAwait(false),
                    "cat" or "read" => await client.InvokeToolAsync(ToolKind.ReadFile, new()
                    {
                        ["path"] = parts.ElementAtOrDefault(1) ?? throw new ArgumentException("需要 path")
                    }).ConfigureAwait(false),
                    "run" or "exec" => await client.InvokeToolAsync(ToolKind.RunCommand, new()
                    {
                        ["command"] = parts.Count > 1
                            ? string.Join(' ', parts.Skip(1))
                            : throw new ArgumentException("需要 command")
                    }).ConfigureAwait(false),
                    _ => throw new ArgumentException($"未知命令: {cmd}")
                };

                PrintToolResponse(resp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("失败: " + ex.Message);
            }
        }

        return 0;
    }

    private static void PrintToolResponse(ToolResponse resp)
    {
        if (!resp.Ok)
        {
            Console.WriteLine("失败: " + (resp.Error ?? "unknown"));
            if (resp.DeniedByUser)
                Console.WriteLine("(对方在 Agent 端拒绝了该操作)");
            return;
        }
        Console.WriteLine(resp.Result ?? "(empty)");
    }

    private static List<string> SplitArgs(string line)
    {
        var list = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    list.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }
            current.Append(ch);
        }
        if (current.Length > 0)
            list.Add(current.ToString());
        return list;
    }

    
    private static async Task<int> CmdOneshotAsync(string[] args)
    {
        if (args.Length < 2)
            return Fail("用法: oneshot <host> <sessionCode> [--port N]");

        var host = args[0];
        var code = args[1];
        var port = DefaultPort;
        for (var i = 2; i < args.Length; i++)
        {
            if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[++i], out var p))
                port = p;
        }

        using var client = await AssistClient.PairAsync(host, port, code, PermissionLevel.Assist, "helper-oneshot").ConfigureAwait(false);
        Console.WriteLine($"PAIR_OK perm={client.Permission}");

        var procs = await client.InvokeToolAsync(ToolKind.ListProcesses).ConfigureAwait(false);
        if (!procs.Ok) return Fail("procs failed: " + procs.Error);
        var procLines = (procs.Result ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Console.WriteLine($"PROCS_OK count={procLines.Length}");

        var run = await client.InvokeToolAsync(ToolKind.RunCommand, new Dictionary<string, string?>
        {
            ["command"] = "echo remote-assist-smoke-ok"
        }).ConfigureAwait(false);
        if (!run.Ok) return Fail("run failed: " + run.Error);
        Console.WriteLine("RUN_OK");
        Console.WriteLine(run.Result);
        Console.WriteLine("SMOKE_PASS");
        return 0;
    }
    private static int Fail(string msg)
    {
        Console.Error.WriteLine(msg);
        return 1;
    }
}

