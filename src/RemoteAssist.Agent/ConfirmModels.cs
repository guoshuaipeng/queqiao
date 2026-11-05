namespace RemoteAssist.Agent;

internal enum ConfirmChannel
{
    Ui,
    Console,
    AutoYes
}

internal enum ConfirmDecision
{
    Approved,
    Denied,
    TimedOut
}

internal sealed record ConfirmOutcome(
    bool Approved,
    ConfirmChannel Channel,
    ConfirmDecision Decision,
    DateTimeOffset AtUtc)
{
    public string AuditDetail(string tool, string summary, string source) =>
        $"tool={tool}; source={source}; via={Channel.ToString().ToLowerInvariant()}; result={Decision.ToString().ToLowerInvariant()}; at={AtUtc:o}; {summary}";
}

internal sealed record ConfirmRequestInfo(
    string Source,
    string Tool,
    string Summary,
    int TimeoutSeconds);