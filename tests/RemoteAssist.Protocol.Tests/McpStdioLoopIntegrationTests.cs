using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;

namespace RemoteAssist.Protocol.Tests;

/// <summary>
/// R008：MCP stdio Content-Length 闭环（列进程 / 读日志 / 提议命令走确认），智能体不可绕过写确认。
/// </summary>
public class McpStdioLoopIntegrationTests
{
    private static bool RelayUp()
    {
        try
        {
            using var c = new TcpClient();
            c.Connect("127.0.0.1", 17891);
            return true;
        }
        catch { return false; }
    }

    private static string FindMcpExe()
    {
        var fromEnv = Environment.GetEnvironmentVariable("REMOTE_ASSIST_MCP_EXE");
        if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
            return Path.GetFullPath(fromEnv);

        if (McpExeLocator.TryResolveExisting(out var found))
            return found;

        var root = FindRepoRoot();
        var candidate = Path.Combine(root, "artifacts", "mcp-win-x64-sc", "RemoteAssist.Mcp.exe");
        Assert.True(File.Exists(candidate), "需要 artifacts/mcp-win-x64-sc/RemoteAssist.Mcp.exe（scripts/publish-mcp.ps1）");
        return candidate;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RemoteAssist.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "scripts", "publish-mcp.ps1")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found");
    }

    [Fact]
    public async Task McpStdio_ListProcesses_ReadLog_RunCommand_ViaRelay()
    {
        Assert.True(RelayUp(), "需要本机 Go relay 监听 127.0.0.1:17891");

        const string relay = "127.0.0.1:17891";
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-mcp-stdio-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var agent = new AgentHost(18021, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath,
            insecure: true, preferRelay: true, enableLanListen: false, relayEndpoint: relay);
        string? published = null;
        agent.SessionChanged += () => published = agent.SessionCode;
        agent.Start();

        try
        {
            for (var i = 0; i < 60 && string.IsNullOrWhiteSpace(published); i++)
                await Task.Delay(100);
            Assert.False(string.IsNullOrWhiteSpace(published), "agent session code not published");

            var mcpExe = FindMcpExe();
            var payload = PairQrPayload.BuildRelay(relay, published!, agent.SessionPassword, fingerprint: null, useTls: false);
            var psi = new ProcessStartInfo
            {
                FileName = mcpExe,
                Arguments = "--payload \"" + payload + "\" --insecure",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("MCP process failed to start");
            // Grab raw pipes once; never touch StreamReader/Writer APIs (they buffer and break Content-Length).
            var stdin = proc.StandardInput.BaseStream;
            var stdout = proc.StandardOutput.BaseStream;
            try
            {
                var paired = false;
                var errBuf = new StringBuilder();
                proc.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data is null) return;
                    errBuf.AppendLine(e.Data);
                    if (e.Data.Contains("[mcp] paired", StringComparison.Ordinal))
                        paired = true;
                };
                proc.BeginErrorReadLine();

                for (var i = 0; i < 80 && !paired && !proc.HasExited; i++)
                    await Task.Delay(100);
                Assert.True(paired, "MCP did not pair (exited=" + proc.HasExited + "): " + errBuf);

                await SendRpcAsync(stdin, 1, "initialize", new JsonObject
                {
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"] = new JsonObject(),
                    ["clientInfo"] = new JsonObject { ["name"] = "r008-test", ["version"] = "1" }
                });
                var init = await ReadRpcAsync(stdout);
                Assert.True(init is not null, "initialize response null; stderr=" + errBuf);
                Assert.NotNull(init!["result"]);

                await SendRpcAsync(stdin, 2, "tools/list", new JsonObject());
                var listed = await ReadRpcAsync(stdout);
                Assert.True(listed is not null, "tools/list response null");
                var toolsJson = listed!["result"]!.ToJsonString();
                Assert.Contains("list_processes", toolsJson, StringComparison.Ordinal);
                Assert.Contains("read_log_tail", toolsJson, StringComparison.Ordinal);
                Assert.Contains("run_command", toolsJson, StringComparison.Ordinal);

                await SendRpcAsync(stdin, 3, "tools/call", new JsonObject
                {
                    ["name"] = "list_processes",
                    ["arguments"] = new JsonObject()
                });
                var procs = await ReadRpcAsync(stdout);
                Assert.NotNull(procs);
                var procsText = ToolText(procs!);
                Assert.False(string.IsNullOrWhiteSpace(procsText));
                Assert.DoesNotContain("\"isError\": true", procs!.ToJsonString().Replace(" ", ""), StringComparison.OrdinalIgnoreCase);

                await SendRpcAsync(stdin, 4, "tools/call", new JsonObject
                {
                    ["name"] = "read_log_tail",
                    ["arguments"] = new JsonObject
                    {
                        ["path"] = auditPath.Replace('\\', '/'),
                        ["lines"] = "40"
                    }
                });
                var log = await ReadRpcAsync(stdout);
                Assert.NotNull(log);
                var logText = ToolText(log!);
                Assert.False(string.IsNullOrWhiteSpace(logText));
                Assert.Contains("tool_ok", logText, StringComparison.Ordinal);

                await SendRpcAsync(stdin, 5, "tools/call", new JsonObject
                {
                    ["name"] = "run_command",
                    ["arguments"] = new JsonObject { ["command"] = "echo r008-mcp-ok" }
                });
                var run = await ReadRpcAsync(stdout);
                Assert.NotNull(run);
                var runText = ToolText(run!);
                Assert.Contains("r008-mcp-ok", runText, StringComparison.OrdinalIgnoreCase);

                var auditBody = await File.ReadAllTextAsync(auditPath);
                Assert.Contains("tool_confirmed", auditBody, StringComparison.Ordinal);
                Assert.Contains("source=mcp", auditBody, StringComparison.Ordinal);
            }
            finally
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(3000);
                    }
                }
                catch { /* ignore */ }
            }
        }
        finally
        {
            agent.Stop();
            try { File.Delete(auditPath); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task McpClientName_WriteDenied_WhenAskEveryTime_CannotBypass()
    {
        var port = GetFreePort();
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-mcp-deny-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var agent = new AgentHost(port, autoYes: false, headless: true, confirmTimeoutSeconds: 3, audit, auditPath,
            insecure: true, preferRelay: false);
        agent.Start();
        agent.ArmOwnerLinkOnNextAccept();
        await Task.Delay(50);

        try
        {
            using var client = await AssistClient.PairAsync(new AssistConnectOptions
            {
                Host = "127.0.0.1",
                Port = port,
                Code = "OWNER",
                Permission = PermissionLevel.Assist,
                Insecure = true,
                ViaRelay = false,
                OwnerLink = true
            }, "mcp");

            Assert.Equal(TrustLevelKind.AskEveryTime, agent.TrustLevelEffective);
            Assert.True(TrustLevelPolicy.NeedsLocalConfirm(TrustLevelKind.AskEveryTime, ToolKind.RunCommand,
                new Dictionary<string, string?> { ["command"] = "echo no" }));

            var procs = await client.InvokeToolAsync(ToolKind.ListProcesses);
            Assert.True(procs.Ok, procs.Error);

            var run = await client.InvokeToolAsync(ToolKind.RunCommand,
                new Dictionary<string, string?> { ["command"] = "echo should-not-run-r008" });
            Assert.False(run.Ok);
            Assert.True(run.DeniedByUser);
            Assert.Contains("未做任何改动", run.Error ?? "", StringComparison.Ordinal);

            var log = await File.ReadAllTextAsync(auditPath);
            Assert.Contains("tool_denied_user", log, StringComparison.Ordinal);
            Assert.Contains("source=mcp", log, StringComparison.Ordinal);
            Assert.DoesNotContain("tool_ok\ttool=RunCommand", log.Replace("\r", ""), StringComparison.Ordinal);
        }
        finally
        {
            agent.Stop();
            try { File.Delete(auditPath); } catch { /* ignore */ }
        }
    }

    private static string ToolText(JsonNode rpc)
    {
        var content = rpc["result"]?["content"] as JsonArray;
        if (content is null || content.Count == 0) return rpc.ToJsonString();
        return content[0]?["text"]?.GetValue<string>() ?? rpc.ToJsonString();
    }

    private static async Task SendRpcAsync(Stream stdin, int id, string method, JsonNode? paramsNode)
    {
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = paramsNode ?? new JsonObject()
        };
        var body = msg.ToJsonString();
        var bytes = Encoding.UTF8.GetBytes(body);
        var header = Encoding.ASCII.GetBytes($"Content-Length: {bytes.Length}\r\n\r\n");
        await stdin.WriteAsync(header);
        await stdin.WriteAsync(bytes);
        await stdin.FlushAsync();
    }

    private static async Task<JsonNode?> ReadRpcAsync(Stream stdout, int timeoutMs = 20000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var line = await ReadLineAsync(stdout, cts.Token);
            if (line is null) return null;
            if (line.Length == 0) break;
            var idx = line.IndexOf(':');
            if (idx > 0)
                headers[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }

        if (!headers.TryGetValue("Content-Length", out var lenStr) || !int.TryParse(lenStr, out var len))
            return null;

        var buffer = new byte[len];
        var offset = 0;
        while (offset < len)
        {
            var n = await stdout.ReadAsync(buffer.AsMemory(offset, len - offset), cts.Token);
            if (n == 0) return null;
            offset += n;
        }

        return JsonNode.Parse(Encoding.UTF8.GetString(buffer));
    }

    private static async Task<string?> ReadLineAsync(Stream input, CancellationToken ct)
    {
        var buf = new List<byte>();
        var one = new byte[1];
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var n = await input.ReadAsync(one.AsMemory(0, 1), ct);
            if (n == 0)
                return buf.Count == 0 ? null : Encoding.UTF8.GetString(buf.ToArray());
            if (one[0] == (byte)'\n')
            {
                if (buf.Count > 0 && buf[^1] == (byte)'\r')
                    buf.RemoveAt(buf.Count - 1);
                return Encoding.UTF8.GetString(buf.ToArray());
            }
            buf.Add(one[0]);
        }
    }

    private static int GetFreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var p = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return p;
    }
}
