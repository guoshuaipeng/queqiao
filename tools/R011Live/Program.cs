using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

var code = args.ElementAtOrDefault(0) ?? throw new Exception("code required");
var pwd = args.ElementAtOrDefault(1) ?? throw new Exception("pwd required");
var opts = new AssistConnectOptions
{
    Host = "121.196.45.81",
    Port = 17891,
    Code = code,
    Password = pwd,
    ViaRelay = true,
    RelayEndpoint = RelayEndpoint.DefaultPublic,
    Insecure = true,
    Permission = PermissionLevel.Assist
};
Console.WriteLine($"connecting code={code} via {opts.RelayEndpoint} insecure");
using var client = await AssistClient.PairAsync(opts, "r011-live");
Console.WriteLine("PAIR_OK");

var procs = await client.InvokeToolAsync(ToolKind.ListProcesses);
Console.WriteLine($"PROCS ok={procs.Ok} denied={procs.DeniedByUser}");
if (procs.Ok)
{
    foreach (var line in (procs.Result ?? "").Split('\n').Take(8))
        Console.WriteLine(line);
}
else Console.WriteLine(procs.Error);

var logs = await client.InvokeToolAsync(ToolKind.ReadLogTail, new Dictionary<string, string?>
{
    ["path"] = "/tmp/r011-demo.log",
    ["lines"] = "80"
});
Console.WriteLine($"LOGS ok={logs.Ok} denied={logs.DeniedByUser}");
Console.WriteLine(logs.Ok ? logs.Result : logs.Error);

var run = await client.InvokeToolAsync(ToolKind.RunCommand, new Dictionary<string, string?>
{
    ["command"] = "echo r011-should-not-run >/tmp/r011-deny-marker"
});
Console.WriteLine($"RUN ok={run.Ok} denied={run.DeniedByUser}");
var msg = run.DeniedByUser ? WriteConfirmCopy.DeniedMessage : (run.Error ?? run.Result ?? "");
Console.WriteLine("RUN_MSG=" + msg);
