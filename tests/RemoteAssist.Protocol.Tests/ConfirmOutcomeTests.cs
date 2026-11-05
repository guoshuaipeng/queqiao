using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class ConfirmOutcomeTests
{
    [Fact]
    public void AuditDetail_IncludesViaResultSourceAndTool()
    {
        var at = DateTimeOffset.Parse("2026-11-05T06:00:00Z");
        var outcome = new ConfirmOutcome(true, ConfirmChannel.Ui, ConfirmDecision.Approved, at);
        var detail = outcome.AuditDetail("RunCommand", "RunCommand: echo hi", "helper-cli");
        Assert.Contains("tool=RunCommand", detail);
        Assert.Contains("source=helper-cli", detail);
        Assert.Contains("via=ui", detail);
        Assert.Contains("result=approved", detail);
        Assert.Contains("at=2026-11-05T06:00:00.0000000+00:00", detail);
        Assert.Contains("RunCommand: echo hi", detail);
    }

    [Fact]
    public void AuditDetail_ConsoleChannel_AndTimedOut()
    {
        var outcome = new ConfirmOutcome(false, ConfirmChannel.Console, ConfirmDecision.TimedOut, DateTimeOffset.UtcNow);
        var detail = outcome.AuditDetail("RunCommand", "x", "src");
        Assert.Contains("via=console", detail);
        Assert.Contains("result=timedout", detail);
    }

    [Fact]
    public void AuditDetail_AutoYesChannel()
    {
        var outcome = new ConfirmOutcome(true, ConfirmChannel.AutoYes, ConfirmDecision.Approved, DateTimeOffset.UtcNow);
        Assert.Contains("via=autoyes", outcome.AuditDetail("RunCommand", "x", "src"));
    }
}

public class PairOutcomeTests
{
    [Fact]
    public void AuditDetail_IncludesRemoteRequestedGrantedVia()
    {
        var at = DateTimeOffset.Parse("2026-11-05T07:00:00Z");
        var outcome = new PairOutcome(true, PermissionLevel.Guest, ConfirmChannel.Ui, ConfirmDecision.Approved, at);
        var detail = outcome.AuditDetail("127.0.0.1:54321", "helper-cli", PermissionLevel.Assist);
        Assert.Contains("client=helper-cli", detail);
        Assert.Contains("remote=127.0.0.1:54321", detail);
        Assert.Contains("requested=Assist", detail);
        Assert.Contains("granted=Guest", detail);
        Assert.Contains("via=ui", detail);
        Assert.Contains("result=approved", detail);
        Assert.Contains("at=2026-11-05T07:00:00.0000000+00:00", detail);
    }

    [Fact]
    public void AuditDetail_Denied_HasGrantedNone()
    {
        var outcome = new PairOutcome(false, null, ConfirmChannel.Console, ConfirmDecision.Denied, DateTimeOffset.UtcNow);
        var detail = outcome.AuditDetail("10.0.0.2:1", "c", PermissionLevel.Admin);
        Assert.Contains("granted=none", detail);
        Assert.Contains("result=denied", detail);
        Assert.Contains("via=console", detail);
    }
}

public class ConfirmQueueTests
{
    [Fact]
    public async Task Concurrent_AutoYes_PairAndWrite_DoNotDrop()
    {
        var pairTasks = Enumerable.Range(0, 8).Select(i => ConfirmPrompt.AskPairAsync(
            new PairRequestInfo($"127.0.0.1:{1000 + i}", $"client-{i}", PermissionLevel.Assist, 30),
            autoYes: true,
            headless: true));

        var writeTasks = Enumerable.Range(0, 8).Select(i => ConfirmPrompt.AskAsync(
            new ConfirmRequestInfo($"src-{i}", "RunCommand", $"echo {i}", 30),
            autoYes: true,
            headless: true));

        var pairs = await Task.WhenAll(pairTasks);
        var writes = await Task.WhenAll(writeTasks);

        Assert.Equal(8, pairs.Length);
        Assert.Equal(8, writes.Length);
        Assert.All(pairs, p =>
        {
            Assert.True(p.Approved);
            Assert.Equal(PermissionLevel.Assist, p.GrantedPermission);
            Assert.Equal(ConfirmChannel.AutoYes, p.Channel);
        });
        Assert.All(writes, w =>
        {
            Assert.True(w.Approved);
            Assert.Equal(ConfirmChannel.AutoYes, w.Channel);
        });
    }
}
