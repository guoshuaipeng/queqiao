using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

public class McpConfigSnippetTests
{
    [Fact]
    public void BuildCursorEnvPayload_ContainsExeAndPayload()
    {
        var payload = PairQrPayload.Build("192.168.1.10", 17890, "ABC123");
        var json = McpConfigSnippet.BuildCursorEnvPayload(
            @"E:\IdeaProjects\remote-assist\artifacts\mcp-win-x64-sc\RemoteAssist.Mcp.exe",
            payload);

        Assert.Contains("\"mcpServers\"", json);
        Assert.Contains("\"remote-assist\"", json);
        Assert.Contains("E:/IdeaProjects/remote-assist/artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe", json);
        Assert.Contains("REMOTE_ASSIST_PAYLOAD", json);
        Assert.Contains(payload, json);
        Assert.Contains("\"args\": []", json);
        Assert.DoesNotContain("\\", json.Split("REMOTE_ASSIST_PAYLOAD")[0]); // path side uses forward slashes
    }

    [Fact]
    public void BuildCursorArgsPayload_UsesPayloadArg()
    {
        var payload = PairQrPayload.Build("10.0.0.1", 19000, "ZZ9900");
        var json = McpConfigSnippet.BuildCursorArgsPayload(@"C:\tools\RemoteAssist.Mcp.exe", payload);
        Assert.Contains("\"--payload\"", json);
        Assert.Contains(payload, json);
        Assert.DoesNotContain("REMOTE_ASSIST_PAYLOAD", json);
        Assert.Contains("C:/tools/RemoteAssist.Mcp.exe", json);
    }

    [Fact]
    public void BuildClipboardBundle_IncludesBothStyles()
    {
        var payload = PairQrPayload.Build("127.0.0.1", 17890, "TEST01");
        var text = McpConfigSnippet.BuildClipboardBundle(
            McpConfigSnippet.DefaultRelativeExeHint.Replace('/', Path.DirectorySeparatorChar),
            payload);
        Assert.Contains("推荐", text);
        Assert.Contains("REMOTE_ASSIST_PAYLOAD", text);
        Assert.Contains("--payload", text);
        Assert.Contains(payload, text);
    }

    [Fact]
    public void EscapeJson_HandlesQuotesAndSlashes()
    {
        Assert.Equal(@"say \""hi\""", McpConfigSnippet.EscapeJson("say \"hi\""));
        Assert.Equal(@"a\\b", McpConfigSnippet.EscapeJson("a\\b"));
    }
}
