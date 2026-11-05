using RemoteAssist.Agent;

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