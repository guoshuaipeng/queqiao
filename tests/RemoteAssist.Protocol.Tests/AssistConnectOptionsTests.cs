using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class AssistConnectOptionsTests
{
    private static Func<string, string?> Env(Dictionary<string, string> map) =>
        key => map.TryGetValue(key, out var v) ? v : null;

    private const string SampleFp = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void Parse_PayloadEnv_WithFp()
    {
        var payload = PairQrPayload.Build("10.0.0.8", 19000, "AA11BB", SampleFp);
        Assert.True(AssistConnectOptions.TryParse(
            Array.Empty<string>(),
            Env(new() { ["REMOTE_ASSIST_PAYLOAD"] = payload }),
            out var opts, out _));
        Assert.Equal("10.0.0.8", opts.Host);
        Assert.Equal(19000, opts.Port);
        Assert.Equal("AA11BB", opts.Code);
        Assert.Equal(PermissionLevel.Assist, opts.Permission);
        Assert.Equal(SampleFp, opts.Fingerprint);
        Assert.False(opts.Insecure);
    }

    [Fact]
    public void Parse_PayloadArg_OverridesEnvHostCode()
    {
        var payload = PairQrPayload.Build("192.168.1.9", 17890, "ZZ9900", SampleFp);
        Assert.True(AssistConnectOptions.TryParse(
            new[] { "--payload", payload, "--perm", "Guest" },
            Env(new()
            {
                ["REMOTE_ASSIST_HOST"] = "1.2.3.4",
                ["REMOTE_ASSIST_CODE"] = "IGNORE"
            }),
            out var opts, out _));
        Assert.Equal("192.168.1.9", opts.Host);
        Assert.Equal("ZZ9900", opts.Code);
        Assert.Equal(PermissionLevel.Guest, opts.Permission);
        Assert.Equal(SampleFp, opts.Fingerprint);
    }

    [Fact]
    public void Parse_PositionalPayload()
    {
        var payload = PairQrPayload.Build("172.16.0.2", 20000, "POS001", SampleFp);
        Assert.True(AssistConnectOptions.TryParse(
            new[] { payload },
            Env(new()),
            out var opts, out _));
        Assert.Equal("172.16.0.2", opts.Host);
        Assert.Equal(20000, opts.Port);
        Assert.Equal("POS001", opts.Code);
    }

    [Fact]
    public void Parse_HostCodeEnv_RequiresFp()
    {
        Assert.False(AssistConnectOptions.TryParse(
            Array.Empty<string>(),
            Env(new()
            {
                ["REMOTE_ASSIST_HOST"] = "127.0.0.1",
                ["REMOTE_ASSIST_CODE"] = "ab-c123",
                ["REMOTE_ASSIST_PORT"] = "17891",
                ["REMOTE_ASSIST_PERM"] = "Admin"
            }),
            out _, out var err));
        Assert.Contains("指纹", err);
    }

    [Fact]
    public void Parse_HostCodeEnv_WithFp()
    {
        Assert.True(AssistConnectOptions.TryParse(
            Array.Empty<string>(),
            Env(new()
            {
                ["REMOTE_ASSIST_HOST"] = "127.0.0.1",
                ["REMOTE_ASSIST_CODE"] = "ab-c123",
                ["REMOTE_ASSIST_PORT"] = "17891",
                ["REMOTE_ASSIST_PERM"] = "Admin",
                ["REMOTE_ASSIST_FP"] = SampleFp
            }),
            out var opts, out _));
        Assert.Equal("127.0.0.1", opts.Host);
        Assert.Equal(17891, opts.Port);
        Assert.Equal("ABC123", opts.Code);
        Assert.Equal(PermissionLevel.Admin, opts.Permission);
        Assert.Equal(SampleFp, opts.Fingerprint);
        Assert.False(opts.Insecure);
    }

    [Fact]
    public void Parse_Insecure_SkipsFpRequirement()
    {
        Assert.True(AssistConnectOptions.TryParse(
            new[] { "--host", "127.0.0.1", "--code", "ABC123", "--insecure" },
            Env(new()),
            out var opts, out _));
        Assert.True(opts.Insecure);
        Assert.Null(opts.Fingerprint);
    }

    [Fact]
    public void Parse_InsecurePayload_Tls0()
    {
        var payload = PairQrPayload.Build("10.0.0.1", 17890, "PLAIN1", useTls: false);
        Assert.True(AssistConnectOptions.TryParse(
            new[] { "--payload", payload },
            Env(new()),
            out var opts, out _));
        Assert.True(opts.Insecure);
    }

    [Fact]
    public void Parse_ArgHostCode_OverridesEnv()
    {
        Assert.True(AssistConnectOptions.TryParse(
            new[] { "--host", "10.1.1.1", "--code", "NEWCODE", "--port", "18000", "--fp", SampleFp },
            Env(new()
            {
                ["REMOTE_ASSIST_HOST"] = "9.9.9.9",
                ["REMOTE_ASSIST_CODE"] = "OLDCODE",
                ["REMOTE_ASSIST_PORT"] = "17890"
            }),
            out var opts, out _));
        Assert.Equal("10.1.1.1", opts.Host);
        Assert.Equal(18000, opts.Port);
        Assert.Equal("NEWCODE", opts.Code);
        Assert.Equal(SampleFp, opts.Fingerprint);
    }

    [Fact]
    public void Parse_ArgPort_OverridesPayloadPort()
    {
        var payload = PairQrPayload.Build("10.0.0.1", 17890, "OVPORT", SampleFp);
        Assert.True(AssistConnectOptions.TryParse(
            new[] { "--payload", payload, "--port", "19999" },
            Env(new()),
            out var opts, out _));
        Assert.Equal(19999, opts.Port);
        Assert.Equal("10.0.0.1", opts.Host);
    }

    [Fact]
    public void Parse_MissingTarget_ClearError()
    {
        Assert.False(AssistConnectOptions.TryParse(
            Array.Empty<string>(),
            Env(new()),
            out _, out var err));
        Assert.Contains("REMOTE_ASSIST_PAYLOAD", err);
        Assert.Contains("REMOTE_ASSIST_HOST", err);
    }

    [Fact]
    public void Parse_BadPayload_ClearError()
    {
        Assert.False(AssistConnectOptions.TryParse(
            new[] { "--payload", "not-a-payload" },
            Env(new()),
            out _, out var err));
        Assert.Contains("无法解析", err);
    }

    [Fact]
    public void Parse_BadPortEnv()
    {
        Assert.False(AssistConnectOptions.TryParse(
            Array.Empty<string>(),
            Env(new()
            {
                ["REMOTE_ASSIST_HOST"] = "127.0.0.1",
                ["REMOTE_ASSIST_CODE"] = "ABC123",
                ["REMOTE_ASSIST_PORT"] = "99999",
                ["REMOTE_ASSIST_FP"] = SampleFp
            }),
            out _, out var err));
        Assert.Contains("REMOTE_ASSIST_PORT", err);
    }

    [Fact]
    public void Parse_UnknownOption()
    {
        Assert.False(AssistConnectOptions.TryParse(
            new[] { "--foo", "bar" },
            Env(new()),
            out _, out var err));
        Assert.Contains("未知选项", err);
    }
}
