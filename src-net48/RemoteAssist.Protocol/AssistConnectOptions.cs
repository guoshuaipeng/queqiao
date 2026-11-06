using System;
using System.Collections.Generic;

namespace RemoteAssist.Protocol;

/// <summary>协助端连接参数（R043：默认经公网中继）。</summary>
public sealed class AssistConnectOptions
{
    public const int DefaultPort = 17890;

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Code { get; init; }
    public PermissionLevel Permission { get; init; } = PermissionLevel.Assist;
    public string? Fingerprint { get; init; }
    public bool Insecure { get; init; }

    public bool ViaRelay { get; init; }
    public string? RelayEndpoint { get; init; }
    public string? Password { get; init; }

    public bool OwnerLink { get; init; }
    public string? AccountToken { get; init; }
    public string? DeviceId { get; init; }
    public string? AccountUser { get; init; }
    public string? AccountPassword { get; init; }
}
