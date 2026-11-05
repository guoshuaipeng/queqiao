using System.Net;
using System.Net.Sockets;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

[CollectionDefinition("SerialConfirmConsole", DisableParallelization = true)]
public class SerialConfirmConsoleDefinition;

/// <summary>
/// R002 验收矩阵：ownerLink 免配对确认、写操作仍确认、访客仍要配对、中继配对≠写权限。
/// </summary>
[Collection("SerialConfirmConsole")]
public class OwnerLinkAcceptanceTests
{
    [Fact]
    public async Task OwnerLink_AutoPairs_WithoutConfirm_EvenWhenAutoYesFalse()
    {
        await using var fx = await Fixture.CreateAsync(autoYes: false, confirmTimeoutSeconds: 5);
        fx.Host.ArmOwnerLinkOnNextAccept();

        using var client = await AssistClient.PairAsync(new AssistConnectOptions
        {
            Host = "127.0.0.1",
            Port = fx.Port,
            Code = "OWNER",
            Permission = PermissionLevel.Assist,
            Insecure = true,
            ViaRelay = false,
            OwnerLink = true
        }, "owner-client");

        Assert.Equal(PermissionLevel.Assist, client.Permission);
        Assert.Single(fx.Host.ListPairedClients());
        Assert.True(fx.Host.ListPairedClients()[0].OwnerLink);

        var log = await File.ReadAllTextAsync(fx.AuditPath);
        Assert.Contains("pair_owner_auto", log);
        Assert.DoesNotContain("\tpair_ok\t", log);
        Assert.DoesNotContain("pair_timeout", log);
        Assert.DoesNotContain("pair_reject", log);
    }

    [Fact]
    public async Task OwnerLink_CapsRequestedAdmin_ToAssist()
    {
        await using var fx = await Fixture.CreateAsync(autoYes: false, confirmTimeoutSeconds: 5);
        fx.Host.ArmOwnerLinkOnNextAccept();

        using var client = await AssistClient.PairAsync(new AssistConnectOptions
        {
            Host = "127.0.0.1",
            Port = fx.Port,
            Code = "IGNORED",
            Permission = PermissionLevel.Admin,
            Insecure = true,
            ViaRelay = false,
            OwnerLink = true
        }, "owner-admin");

        Assert.Equal(PermissionLevel.Assist, client.Permission);
        var log = await File.ReadAllTextAsync(fx.AuditPath);
        Assert.Contains("pair_owner_auto", log);
        Assert.Contains("perm=Assist", log);
    }

    [Fact]
    public async Task OwnerLink_WriteStillRequiresConfirm_TimeoutDenies()
    {
        await using var fx = await Fixture.CreateAsync(autoYes: false, confirmTimeoutSeconds: 5);
        fx.Host.ArmOwnerLinkOnNextAccept();

        using var client = await AssistClient.PairAsync(new AssistConnectOptions
        {
            Host = "127.0.0.1",
            Port = fx.Port,
            Code = "OWNER",
            Permission = PermissionLevel.Assist,
            Insecure = true,
            ViaRelay = false,
            OwnerLink = true
        }, "owner-write");

        var procs = await client.InvokeToolAsync(ToolKind.ListProcesses);
        Assert.True(procs.Ok, procs.Error);

        var run = await client.InvokeToolAsync(
            ToolKind.RunCommand,
            new Dictionary<string, string?> { ["command"] = "echo should-not-run" });
        Assert.False(run.Ok);
        Assert.True(run.DeniedByUser);
        Assert.Contains("超时", run.Error ?? "");

        var log = await File.ReadAllTextAsync(fx.AuditPath);
        Assert.Contains("pair_owner_auto", log);
        Assert.Contains("tool_denied_user", log);
        Assert.DoesNotContain("tool_ok\ttool=RunCommand", log.Replace("\r", ""));
    }

    [Fact]
    public async Task Guest_StillRequiresPairConfirm_WhenAutoYesFalse()
    {
        await using var fx = await Fixture.CreateAsync(autoYes: false, confirmTimeoutSeconds: 5);

        var pairTask = AssistClient.PairAsync(new AssistConnectOptions
        {
            Host = "127.0.0.1",
            Port = fx.Port,
            Code = fx.Host.SessionCode,
            Permission = PermissionLevel.Assist,
            Insecure = true,
            ViaRelay = false,
            OwnerLink = false
        }, "guest-client");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await pairTask);
        Assert.Contains("配对", ex.Message);

        Assert.Empty(fx.Host.ListPairedClients());
        var log = await File.ReadAllTextAsync(fx.AuditPath);
        Assert.True(log.Contains("pair_timeout") || log.Contains("pair_reject"), log);
        Assert.DoesNotContain("pair_owner_auto", log);
    }

    [Fact]
    public async Task SpoofedOwnerLinkFlag_OnGuestChannel_StillRequiresConfirm()
    {
        await using var fx = await Fixture.CreateAsync(autoYes: false, confirmTimeoutSeconds: 5);

        var pairTask = AssistClient.PairAsync(new AssistConnectOptions
        {
            Host = "127.0.0.1",
            Port = fx.Port,
            Code = fx.Host.SessionCode,
            Permission = PermissionLevel.Assist,
            Insecure = true,
            ViaRelay = false,
            OwnerLink = true
        }, "spoof-owner");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await pairTask);
        Assert.Contains("配对", ex.Message);

        var log = await File.ReadAllTextAsync(fx.AuditPath);
        Assert.DoesNotContain("pair_owner_auto", log);
        Assert.True(log.Contains("pair_timeout") || log.Contains("pair_reject"), log);
        Assert.Empty(fx.Host.ListPairedClients());
    }

    [Fact]
    public async Task Guest_PairSuccess_DoesNotGrantWriteWithoutConfirmCapability()
    {
        await using var fx = await Fixture.CreateAsync(autoYes: true, confirmTimeoutSeconds: 5);
        using var client = await AssistClient.PairAsync(new AssistConnectOptions
        {
            Host = "127.0.0.1",
            Port = fx.Port,
            Code = fx.Host.SessionCode,
            Permission = PermissionLevel.Assist,
            Insecure = true,
            ViaRelay = false
        }, "guest-ok");

        Assert.Equal(PermissionLevel.Assist, client.Permission);
        Assert.True(ToolCapabilities.RequiresConfirmation(ToolKind.RunCommand, client.Permission));
        Assert.False(ToolCapabilities.IsReadOnly(ToolKind.RunCommand));

        var log = await File.ReadAllTextAsync(fx.AuditPath);
        Assert.Contains("pair_ok", log);
        Assert.DoesNotContain("pair_owner_auto", log);
    }

    [Fact]
    public void Brand_UserVisibleNames_UseMagpieBridge()
    {
        var root = FindRepoRoot();
        var iss = File.ReadAllText(Path.Combine(root, "installer", "RemoteAssist.iss"));
        Assert.Contains("MyAppName \"鹊桥远程协助\"", iss);
        Assert.DoesNotContain("远端协助", iss);

        AssertNoProductBrand(Path.Combine(root, "src", "RemoteAssist.Helper.UI", "RemoteAssist.Helper.UI.csproj"));
        AssertNoProductBrand(Path.Combine(root, "src", "RemoteAssist.App", "RemoteAssist.App.csproj"));
        AssertNoProductBrand(Path.Combine(root, "src", "RemoteAssist.App", "ModeSelectForm.cs"));
        AssertNoProductBrand(Path.Combine(root, "src", "RemoteAssist.Helper.UI", "MainForm.cs"));
        AssertNoProductBrand(Path.Combine(root, "src", "RemoteAssist.Agent", "MainForm.cs"));

        var helperCsproj = File.ReadAllText(Path.Combine(root, "src", "RemoteAssist.Helper.UI", "RemoteAssist.Helper.UI.csproj"));
        Assert.Contains("<Product>鹊桥", helperCsproj);
        var appCsproj = File.ReadAllText(Path.Combine(root, "src", "RemoteAssist.App", "RemoteAssist.App.csproj"));
        Assert.Contains("<Product>鹊桥远程协助</Product>", appCsproj);
    }

    private static void AssertNoProductBrand(string path)
    {
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("远端协助", text);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RemoteAssist.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "installer", "RemoteAssist.iss")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found from " + AppContext.BaseDirectory);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required AgentHost Host { get; init; }
        public required int Port { get; init; }
        public required string AuditPath { get; init; }

        public static async Task<Fixture> CreateAsync(bool autoYes, int confirmTimeoutSeconds)
        {
            var port = GetFreePort();
            var auditPath = Path.Combine(Path.GetTempPath(), "ra-owner-" + Guid.NewGuid().ToString("N") + ".log");
            var audit = new AuditLog(auditPath);
            var host = new AgentHost(
                port,
                autoYes: autoYes,
                headless: true,
                confirmTimeoutSeconds: confirmTimeoutSeconds,
                audit: audit,
                auditPath: auditPath,
                insecure: true,
                preferRelay: false);
            host.Start();
            await Task.Delay(50);
            return new Fixture { Host = host, Port = port, AuditPath = auditPath };
        }

        public async ValueTask DisposeAsync()
        {
            Host.Stop();
            await Task.Delay(20);
            try { File.Delete(AuditPath); } catch { /* ignore */ }
        }
    }

    private static int GetFreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var p = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return p;
    }
}
