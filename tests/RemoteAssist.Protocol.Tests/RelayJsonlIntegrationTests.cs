using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

/// <summary>需要本机 127.0.0.1:17891 已启动 Go relay。</summary>
public class RelayJsonlIntegrationTests
{
    private static bool RelayUp()
    {
        try
        {
            using var c = new System.Net.Sockets.TcpClient();
            c.Connect("127.0.0.1", 17891);
            return true;
        }
        catch { return false; }
    }

    [Fact]
    public async Task Guest_HostJoin_ListProcesses_AndConfirmCommand()
    {
        Assert.True(RelayUp(), "需要本机 Go relay 监听 127.0.0.1:17891");

        const string relay = "127.0.0.1:17891";
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-itest-guest-audit.log");
        var audit = new AuditLog(auditPath);
        var agent = new AgentHost(18011, true, true, 30, audit, auditPath, insecure: true,
            preferRelay: true, enableLanListen: false, relayEndpoint: relay);
        string? published = null;
        agent.SessionChanged += () => published = agent.SessionCode;
        agent.Start();

        AssistClient? helper = null;
        for (var i = 0; i < 60; i++)
        {
            await Task.Delay(200);
            if (string.IsNullOrWhiteSpace(published)) continue;
            try
            {
                helper = await AssistClient.PairAsync(new AssistConnectOptions
                {
                    Host = "127.0.0.1",
                    Port = 17891,
                    Code = published!,
                    ViaRelay = true,
                    RelayEndpoint = relay,
                    Password = agent.SessionPassword,
                    Insecure = true,
                    Permission = PermissionLevel.Assist
                }, "itest-helper");
                break;
            }
            catch { /* retry */ }
        }

        Assert.NotNull(helper);
        var procs = await helper!.InvokeToolAsync(ToolKind.ListProcesses);
        Assert.True(procs.Ok, procs.Error);
        Assert.False(string.IsNullOrWhiteSpace(procs.Result));

        var run = await helper.InvokeToolAsync(ToolKind.RunCommand,
            new Dictionary<string, string?> { ["command"] = "echo smoke-ok" });
        Assert.True(run.Ok, run.Error ?? "run failed");
        Assert.Contains("smoke-ok", run.Result ?? "", StringComparison.OrdinalIgnoreCase);

        helper.Dispose();
        agent.Stop();
    }

    [Fact]
    public async Task Account_Login_List_Connect_ListProcesses()
    {
        Assert.True(RelayUp(), "需要本机 Go relay 监听 127.0.0.1:17891");

        const string relay = "127.0.0.1:17891";
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-itest-acct-audit.log");
        var audit = new AuditLog(auditPath);

        var user = "it_" + Guid.NewGuid().ToString("N")[..8];
        var pass = "pass1234";
        var token = await RelayClient.SignupOrLoginAsync("127.0.0.1", 17891, signup: true, user, pass);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var deviceId = "dev-" + Guid.NewGuid().ToString("N")[..12];
        var agent = new AgentHost(18012, true, true, 30, audit, auditPath, insecure: true,
            preferRelay: true, enableLanListen: false, relayEndpoint: relay,
            accountToken: token, deviceId: deviceId, deviceDisplayName: "itest-pc");
        agent.Start();
        await Task.Delay(700);

        var list = await RelayClient.ListDevicesAsync("127.0.0.1", 17891, token);
        Assert.Contains(list, d => d.Id == deviceId && d.Online);

        AssistClient? owner = null;
        for (var i = 0; i < 40; i++)
        {
            try
            {
                owner = await AssistClient.PairAsync(new AssistConnectOptions
                {
                    Host = "127.0.0.1",
                    Port = 17891,
                    Code = "OWNER",
                    ViaRelay = true,
                    RelayEndpoint = relay,
                    OwnerLink = true,
                    AccountToken = token,
                    DeviceId = deviceId,
                    Insecure = true,
                    Permission = PermissionLevel.Assist
                }, "itest-owner");
                break;
            }
            catch { await Task.Delay(250); }
        }

        Assert.NotNull(owner);
        var procs = await owner!.InvokeToolAsync(ToolKind.ListProcesses);
        Assert.True(procs.Ok, procs.Error);
        owner.Dispose();
        agent.Stop();
    }

    [Fact]
    public void McpOptions_GuestAndOwner_Parse()
    {
        Assert.True(AssistConnectOptions.TryParse(
            new[] { "--relay", "127.0.0.1:17891", "--code", "AB12CD", "--pwd", "secret1", "--insecure" },
            _ => null, out var guest, out var err), err);
        Assert.True(guest.ViaRelay);
        Assert.False(guest.OwnerLink);

        Assert.True(AssistConnectOptions.TryParse(
            new[] { "--relay", "127.0.0.1:17891", "--token", "abc", "--device-id", "d1", "--insecure" },
            _ => null, out var owner, out var err2), err2);
        Assert.True(owner.OwnerLink);
        Assert.Equal("d1", owner.DeviceId);
    }
}
