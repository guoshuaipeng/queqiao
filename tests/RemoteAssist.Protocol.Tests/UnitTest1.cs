using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class SessionCodeGeneratorTests
{
    [Fact]
    public void Generate_DefaultLength_IsSix()
    {
        var code = SessionCodeGenerator.Generate();
        Assert.Equal(6, code.Length);
    }

    [Fact]
    public void Generate_UsesSafeAlphabet()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        for (var i = 0; i < 20; i++)
        {
            var code = SessionCodeGenerator.Generate(8);
            Assert.All(code, ch => Assert.Contains(ch, alphabet));
        }
    }

    [Fact]
    public void Normalize_StripsSpacesAndDashes()
    {
        Assert.Equal("ABC123", SessionCodeGenerator.Normalize(" ab-c1 23 "));
    }
}

public class ToolCapabilitiesTests
{
    [Theory]
    [InlineData(ToolKind.ListProcesses, true)]
    [InlineData(ToolKind.ReadLogTail, true)]
    [InlineData(ToolKind.ListDir, true)]
    [InlineData(ToolKind.ReadFile, true)]
    [InlineData(ToolKind.RunCommand, false)]
    public void IsReadOnly_MatchesExpectation(ToolKind tool, bool expected)
    {
        Assert.Equal(expected, ToolCapabilities.IsReadOnly(tool));
    }

    [Fact]
    public void Assist_RunCommand_RequiresConfirmation()
    {
        Assert.True(ToolCapabilities.RequiresConfirmation(ToolKind.RunCommand, PermissionLevel.Assist));
    }

    [Fact]
    public void Admin_RunCommand_DoesNotRequireConfirmation()
    {
        Assert.False(ToolCapabilities.RequiresConfirmation(ToolKind.RunCommand, PermissionLevel.Admin));
    }

    [Fact]
    public void Guest_CannotRunCommand()
    {
        Assert.False(ToolCapabilities.IsAllowed(ToolKind.RunCommand, PermissionLevel.Guest));
        Assert.True(ToolCapabilities.IsAllowed(ToolKind.ListProcesses, PermissionLevel.Guest));
    }

    [Fact]
    public void Assist_CannotBypassConfirmationForWrites()
    {
        // Product rule: Assist mode write ops always need local confirmation — never auto-approve.
        Assert.True(ToolCapabilities.RequiresConfirmation(ToolKind.RunCommand, PermissionLevel.Assist));
        Assert.False(ToolCapabilities.IsReadOnly(ToolKind.RunCommand));
    }
}

