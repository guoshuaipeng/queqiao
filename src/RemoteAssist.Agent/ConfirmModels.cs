using RemoteAssist.Protocol;

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
    int TimeoutSeconds,
    string? Detail = null)
{
    public string Headline => string.IsNullOrWhiteSpace(Summary) ? "执行一项电脑操作" : Summary;
}

internal sealed record PairRequestInfo(
    string RemoteEndpoint,
    string ClientName,
    PermissionLevel RequestedPermission,
    int TimeoutSeconds);

internal sealed record PairOutcome(
    bool Approved,
    PermissionLevel? GrantedPermission,
    ConfirmChannel Channel,
    ConfirmDecision Decision,
    DateTimeOffset AtUtc)
{
    public string AuditDetail(string remote, string clientName, PermissionLevel requested) =>
        $"client={clientName}; remote={remote}; requested={requested}; granted={(GrantedPermission?.ToString() ?? "none")}; via={Channel.ToString().ToLowerInvariant()}; result={Decision.ToString().ToLowerInvariant()}; at={AtUtc:o}";
}
