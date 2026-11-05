using System.Text.Json.Serialization;

namespace RemoteAssist.Protocol.Messages;

public sealed class WireEnvelope
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}

public sealed class PairRequest
{
    [JsonPropertyName("sessionCode")]
    public string SessionCode { get; set; } = "";

    [JsonPropertyName("requestedPermission")]
    public string RequestedPermission { get; set; } = "Assist";

    [JsonPropertyName("clientName")]
    public string ClientName { get; set; } = "helper";

    /// <summary>账号免密设备链路：被控端在中继 DEVICE ONLINE 路径上可自动批准。</summary>
    [JsonPropertyName("ownerLink")]
    public bool OwnerLink { get; set; }
}

public sealed class PairResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("permission")]
    public string? Permission { get; set; }

    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }
}

public sealed class ToolRequest
{
    [JsonPropertyName("tool")]
    public string Tool { get; set; } = "";

    [JsonPropertyName("arguments")]
    public Dictionary<string, string?> Arguments { get; set; } = new();
}

public sealed class ToolResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("needsConfirmation")]
    public bool NeedsConfirmation { get; set; }

    [JsonPropertyName("confirmationId")]
    public string? ConfirmationId { get; set; }

    [JsonPropertyName("deniedByUser")]
    public bool DeniedByUser { get; set; }
}

public sealed class ConfirmRequest
{
    [JsonPropertyName("confirmationId")]
    public string ConfirmationId { get; set; } = "";

    [JsonPropertyName("approve")]
    public bool Approve { get; set; }
}

public sealed class ErrorPayload
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

