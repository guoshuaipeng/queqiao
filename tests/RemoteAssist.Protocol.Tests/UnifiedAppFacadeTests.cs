using RemoteAssist.Agent;
using RemoteAssist.Helper.UI;

namespace RemoteAssist.Protocol.Tests;

public class UnifiedAppFacadeTests
{
    [Fact]
    public void PublicFacades_AreAvailableForUnifiedApp()
    {
        Assert.True(typeof(AgentDesktopController).IsPublic);
        Assert.True(typeof(HelperDesktopController).IsPublic);
        Assert.NotNull(typeof(AgentDesktopController).GetMethod("Start"));
        Assert.NotNull(typeof(HelperDesktopController).GetMethod("Show"));
    }
}
