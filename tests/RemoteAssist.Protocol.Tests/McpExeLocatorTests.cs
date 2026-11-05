using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class McpExeLocatorTests
{
    [Fact]
    public void TryFindRepoRoot_FindsScriptsPublishMcp()
    {
        // 测试从仓库内运行时能定位到 scripts/publish-mcp.ps1
        var root = McpExeLocator.TryFindRepoRoot();
        Assert.False(string.IsNullOrEmpty(root));
        Assert.True(File.Exists(Path.Combine(root!, "scripts", "publish-mcp.ps1")));
        Assert.True(Directory.Exists(Path.Combine(root!, "src", "RemoteAssist.Mcp")));
    }

    [Fact]
    public void TryFindPublishScript_ReturnsExistingFile()
    {
        var script = McpExeLocator.TryFindPublishScript();
        Assert.False(string.IsNullOrEmpty(script));
        Assert.True(File.Exists(script!));
        Assert.EndsWith("publish-mcp.ps1", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPublishCommandHint_ContainsPowershellAndScript()
    {
        var cmd = McpExeLocator.GetPublishCommandHint();
        Assert.Contains("powershell", cmd, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("publish-mcp.ps1", cmd, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-ExecutionPolicy Bypass", cmd, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolveExisting_FindsPublishedMcpWhenPresent()
    {
        var root = McpExeLocator.TryFindRepoRoot();
        Assert.NotNull(root);
        var expected = Path.Combine(root!, "artifacts", "mcp-win-x64-sc", "RemoteAssist.Mcp.exe");
        if (!File.Exists(expected))
        {
            // 本机尚未 publish 时跳过存在性断言，但 Resolve 仍应返回提示路径
            var hint = McpExeLocator.Resolve();
            Assert.Contains("RemoteAssist.Mcp.exe", hint);
            return;
        }

        Assert.True(McpExeLocator.TryResolveExisting(out var path));
        Assert.True(File.Exists(path));
        Assert.Equal(Path.GetFullPath(expected), Path.GetFullPath(path), ignoreCase: true);
    }
}

public class McpConfigRefreshPayloadTests
{
    [Fact]
    public void AfterRefreshSessionCode_ClipboardBundleUsesNewPayload()
    {
        var port = GetFreePort();
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-mcp-refresh-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath);
        try
        {
            host.SetSelectedHost("127.0.0.1");
            var oldPayload = host.QrPayload;
            var oldCode = host.SessionCode;

            var oldBundle = McpConfigSnippet.BuildClipboardBundle(
                @"E:\IdeaProjects\remote-assist\artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe",
                oldPayload);
            Assert.Contains(oldPayload, oldBundle);
            Assert.Contains(oldCode, oldBundle);

            var newCode = host.RefreshSessionCode();
            Assert.NotEqual(oldCode, newCode);
            var newPayload = host.QrPayload;
            Assert.NotEqual(oldPayload, newPayload);
            Assert.Contains(newCode, newPayload);

            // 模拟「刷新后再复制」：必须用调用瞬间的新载荷
            var newBundle = McpConfigSnippet.BuildClipboardBundle(
                @"E:\IdeaProjects\remote-assist\artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe",
                newPayload);
            Assert.Contains(newPayload, newBundle);
            Assert.Contains(newCode, newBundle);
            Assert.DoesNotContain(oldPayload, newBundle);
        }
        finally
        {
            host.Stop();
            try { File.Delete(auditPath); } catch { /* ignore */ }
        }
    }

    private static int GetFreePort()
    {
        var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        l.Start();
        var p = ((System.Net.IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return p;
    }
}
