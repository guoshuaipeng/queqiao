using System.Diagnostics;
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
              pair <remoteassist:pair?v=1&host=...&port=...&code=...> [--perm Assist]
              pair --payload <载荷> [--perm Assist]
              pair --clipboard [--perm Assist]

              oneshot 同上（冒烟：配对 → procs → run）

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
        if (!TryParseConnectArgs(args, out var host, out var port, out var code, out var perm, out var err))
            return Fail(err);

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

    private static async Task<int> CmdOneshotAsync(string[] args)
    {
        if (!TryParseConnectArgs(args, out var host, out var port, out var code, out _, out var err))
            return Fail(err);

        using var client = await AssistClient.PairAsync(host, port, code, PermissionLevel.Assist, "helper-oneshot").ConfigureAwait(false);
        Console.WriteLine($"PAIR_OK perm={client.Permission} host={host}:{port}");

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

    /// <summary>
    /// 解析 pair/oneshot 连接参数。支持 host+code、整段载荷、--payload、--clipboard。
    /// </summary>
    internal static bool TryParseConnectArgs(
        string[] args,
        out string host,
        out int port,
        out string code,
        out PermissionLevel perm,
        out string error)
    {
        host = "";
        port = DefaultPort;
        code = "";
        perm = PermissionLevel.Assist;
        error = "";

        var positional = new List<string>();
        string? explicitPayload = null;
        var fromClipboard = false;
        var portOverride = false;
        var portValue = DefaultPort;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "--payload" or "-P")
            {
                if (i + 1 >= args.Length)
                {
                    error = "--payload 需要参数";
                    return false;
                }
                explicitPayload = args[++i];
            }
            else if (a is "--clipboard" or "-c")
            {
                fromClipboard = true;
            }
            else if (a is "--port" or "-p")
            {
                if (i + 1 >= args.Length || !int.TryParse(args[++i], out portValue) || portValue is < 1 or > 65535)
                {
                    error = "--port 需要 1–65535";
                    return false;
                }
                portOverride = true;
            }
            else if (a is "--perm")
            {
                if (i + 1 >= args.Length || !Enum.TryParse(args[++i], true, out perm))
                {
                    error = "--perm 需要 Assist|Guest|Admin";
                    return false;
                }
            }
            else if (a.StartsWith('-'))
            {
                error = $"未知选项: {a}";
                return false;
            }
            else
            {
                positional.Add(a);
            }
        }

        if (fromClipboard)
        {
            var clip = TryReadClipboardText();
            if (string.IsNullOrWhiteSpace(clip))
            {
                error = "剪贴板为空或无法读取（--clipboard 仅支持 Windows）";
                return false;
            }
            explicitPayload = clip.Trim();
        }

        if (!PairTargetResolver.TryResolve(positional, explicitPayload, portOverride ? portValue : DefaultPort, out var target, out error))
            return false;

        host = target.Host;
        code = target.Code;
        // 载荷自带 port；若用户同时给了 --port，以 --port 覆盖（便于本机 127.0.0.1 转发调试）
        port = portOverride ? portValue : target.Port;
        return true;
    }

    private static string? TryReadClipboardText()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command Get-Clipboard -Raw",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            var text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch
        {
            return null;
        }
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

    private static int Fail(string msg)
    {
        Console.Error.WriteLine(msg);
        return 1;
    }
}
