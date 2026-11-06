using System.Text;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var mode = args.FirstOrDefault(a => a is "procs" or "logs" or "run" or "deny-probe" or "combo") ?? "procs";
        string? logPath = null;
        string? runCmd = null;
        var connectList = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "procs" or "combo") continue;
            if (a is "logs")
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-') && args[i + 1] is not ("procs" or "run" or "deny-probe" or "combo"))
                {
                    logPath = args[++i];
                }
                continue;
            }
            if (a is "run" or "deny-probe")
            {
                runCmd = string.Join(' ', args.Skip(i + 1));
                break;
            }
            connectList.Add(a);
        }
        var connectArgs = connectList.ToArray();

        if (!AssistConnectOptions.TryParse(connectArgs, Environment.GetEnvironmentVariable, out var opts, out var err))
        {
            if (err.Contains("证书指纹", StringComparison.Ordinal) &&
                connectArgs.Any(a => a is "--code") && connectArgs.Any(a => a is "--pwd"))
            {
                var withInsecure = connectArgs.Concat(new[] { "--insecure" }).ToArray();
                if (!AssistConnectOptions.TryParse(withInsecure, Environment.GetEnvironmentVariable, out opts, out err))
                {
                    Console.Error.WriteLine(err);
                    return 2;
                }
            }
            else
            {
                Console.Error.WriteLine(err);
                return 2;
            }
        }

        if (opts.ViaRelay && !opts.Insecure && string.IsNullOrWhiteSpace(opts.Fingerprint))
        {
            opts = new AssistConnectOptions
            {
                Host = opts.Host,
                Port = opts.Port,
                Code = opts.Code,
                Permission = opts.Permission,
                Fingerprint = null,
                Insecure = true,
                ViaRelay = true,
                RelayEndpoint = opts.RelayEndpoint,
                Password = opts.Password
            };
        }

        if (!opts.ViaRelay)
        {
            Console.Error.WriteLine("需要 --code --pwd（默认公网中继）");
            return 2;
        }

        Console.WriteLine($"配对中继 {opts.RelayEndpoint} code={opts.Code} insecure={opts.Insecure} mode={mode} ...");
        using var client = await AssistClient.PairAsync(opts, "r009-win-helper").ConfigureAwait(false);
        Console.WriteLine($"配对成功 perm={client.Permission}");

        async Task<int> DoProcs()
        {
            var resp = await client.InvokeToolAsync(ToolKind.ListProcesses).ConfigureAwait(false);
            Console.WriteLine($"PROCS ok={resp.Ok} denied={resp.DeniedByUser}");
            Console.WriteLine(Clip(resp.Result ?? resp.Error, 1500));
            return resp.Ok ? 0 : 1;
        }
        async Task<int> DoLogs()
        {
            var path = logPath ?? "/tmp/r009-demo.log";
            var resp = await client.InvokeToolAsync(ToolKind.ReadLogTail, new Dictionary<string, string?>
            {
                ["path"] = path,
                ["lines"] = "40"
            }).ConfigureAwait(false);
            Console.WriteLine($"LOGS ok={resp.Ok} denied={resp.DeniedByUser} path={path}");
            Console.WriteLine(Clip(resp.Result ?? resp.Error, 1500));
            return resp.Ok ? 0 : 1;
        }
        async Task<int> DoRun(bool expectDeny)
        {
            var cmd = string.IsNullOrWhiteSpace(runCmd)
                ? (expectDeny ? "echo r009-should-not-run >/tmp/r009-deny-marker" : "echo r009-ok")
                : runCmd!;
            var resp = await client.InvokeToolAsync(ToolKind.RunCommand, new Dictionary<string, string?>
            {
                ["command"] = cmd
            }).ConfigureAwait(false);
            Console.WriteLine($"RUN ok={resp.Ok} denied={resp.DeniedByUser} err={resp.Error}");
            Console.WriteLine(Clip(resp.Result ?? "", 800));
            if (expectDeny)
                return (!resp.Ok && (resp.DeniedByUser || (resp.Error ?? "").Contains("已拒绝", StringComparison.Ordinal))) ? 0 : 1;
            return resp.Ok ? 0 : 1;
        }

        return mode switch
        {
            "procs" => await DoProcs().ConfigureAwait(false),
            "logs" => await DoLogs().ConfigureAwait(false),
            "run" => await DoRun(false).ConfigureAwait(false),
            "deny-probe" => await DoRun(true).ConfigureAwait(false),
            "combo" => await Combo().ConfigureAwait(false),
            _ => 1
        };

        async Task<int> Combo()
        {
            var a = await DoProcs().ConfigureAwait(false);
            var b = await DoLogs().ConfigureAwait(false);
            var c = await DoRun(true).ConfigureAwait(false);
            Console.WriteLine($"COMBO procs={a} logs={b} deny={c}");
            return (a == 0 && b == 0 && c == 0) ? 0 : 1;
        }
    }

    private static string Clip(string? s, int max)
    {
        s ??= "";
        return s.Length <= max ? s : s[..max] + "…";
    }
}
