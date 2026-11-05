using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RemoteAssist.Protocol;

namespace RemoteAssist.Mcp;

/// <summary>
/// Minimal MCP server over stdio (JSON-RPC 2.0, Content-Length framing).
/// 启动时自动配对；支持整段 remoteassist:pair 载荷或 host+code+port。
/// 详见 AssistConnectOptions / README「MCP」一节。
/// </summary>
internal static class Program
{
    private static AssistClient? _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Any(a => a is "-h" or "--help" or "help"))
        {
            await Console.Error.WriteLineAsync("RemoteAssist MCP (stdio)").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(AssistConnectOptions.UsageText).ConfigureAwait(false);
            Environment.Exit(0);
        }

        if (!AssistConnectOptions.TryParse(args, Environment.GetEnvironmentVariable, out var opts, out var parseError))
        {
            await Console.Error.WriteLineAsync("[mcp] 启动参数错误:").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(parseError).ConfigureAwait(false);
            Environment.Exit(2);
        }

        try
        {
            _client = await AssistClient.PairAsync(opts, "mcp").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(
                    $"[mcp] paired {opts.Host}:{opts.Port} perm={_client.Permission} tls={(!opts.Insecure).ToString().ToLowerInvariant()}")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync("[mcp] 配对失败: " + ex.Message).ConfigureAwait(false);
            await Console.Error.WriteLineAsync(
                    $"  目标 {opts.Host}:{opts.Port} code={opts.Code} perm={opts.Permission} tls={(!opts.Insecure).ToString().ToLowerInvariant()} fp={(opts.Fingerprint is null ? "-" : opts.Fingerprint[..Math.Min(16, opts.Fingerprint.Length)] + "…")}").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(
                    "  请确认 Agent 已在局域网启动，会话码未过期，且本机已在 Agent 确认配对。")
                .ConfigureAwait(false);
            Environment.Exit(3);
        }

        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();

        while (true)
        {
            var msg = await ReadLspMessageAsync(stdin).ConfigureAwait(false);
            if (msg is null)
                break;

            JsonNode? node;
            try { node = JsonNode.Parse(msg); }
            catch { continue; }
            if (node is null) continue;

            var id = node["id"];
            var method = node["method"]?.GetValue<string>();
            if (method is null)
                continue;

            // notifications (no id)
            if (id is null)
                continue;

            object result;
            object? error = null;
            try
            {
                result = method switch
                {
                    "initialize" => HandleInitialize(),
                    "tools/list" => HandleToolsList(),
                    "tools/call" => await HandleToolsCallAsync(node["params"]).ConfigureAwait(false),
                    "ping" => new { },
                    _ => throw new InvalidOperationException($"Method not found: {method}")
                };
            }
            catch (Exception ex)
            {
                result = null!;
                error = new { code = -32000, message = ex.Message };
            }

            var response = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id is JsonValue v ? v.DeepClone() : id.DeepClone()
            };
            if (error is null)
                response["result"] = JsonSerializer.SerializeToNode(result, JsonOpts);
            else
                response["error"] = JsonSerializer.SerializeToNode(error, JsonOpts);

            await WriteLspMessageAsync(stdout, response.ToJsonString(JsonOpts)).ConfigureAwait(false);
        }
    }

    private static object HandleInitialize() => new
    {
        protocolVersion = "2024-11-05",
        capabilities = new { tools = new { } },
        serverInfo = new { name = "remote-assist", version = "0.1.0" }
    };

    private static object HandleToolsList() => new
    {
        tools = new object[]
        {
            ToolDef("list_processes", "列出远端 Windows 进程", new { type = "object", properties = new { } }),
            ToolDef("read_log_tail", "读取日志文件尾部", new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string" },
                    lines = new { type = "integer" }
                },
                required = new[] { "path" }
            }),
            ToolDef("list_dir", "列出目录", new
            {
                type = "object",
                properties = new { path = new { type = "string" } },
                required = new[] { "path" }
            }),
            ToolDef("read_file", "读取文本文件", new
            {
                type = "object",
                properties = new { path = new { type = "string" } },
                required = new[] { "path" }
            }),
            ToolDef("run_command", "提议在远端执行命令（Assist 模式需对方本机确认，不可绕过）", new
            {
                type = "object",
                properties = new { command = new { type = "string" } },
                required = new[] { "command" }
            }),
        }
    };

    private static object ToolDef(string name, string description, object inputSchema) => new
    {
        name,
        description,
        inputSchema
    };

    private static async Task<object> HandleToolsCallAsync(JsonNode? paramsNode)
    {
        if (_client is null)
            throw new InvalidOperationException("not paired");
        var name = paramsNode?["name"]?.GetValue<string>()
            ?? throw new ArgumentException("missing tool name");
        var argsNode = paramsNode?["arguments"] as JsonObject;

        string? Arg(string key) => argsNode?[key]?.ToString()?.Trim('"');

        var tool = name switch
        {
            "list_processes" => ToolKind.ListProcesses,
            "read_log_tail" => ToolKind.ReadLogTail,
            "list_dir" => ToolKind.ListDir,
            "read_file" => ToolKind.ReadFile,
            "run_command" => ToolKind.RunCommand,
            _ => throw new InvalidOperationException("unknown tool: " + name)
        };

        var args = new Dictionary<string, string?>();
        if (tool == ToolKind.ReadLogTail)
        {
            args["path"] = Arg("path");
            args["lines"] = Arg("lines") ?? "80";
        }
        else if (tool is ToolKind.ListDir or ToolKind.ReadFile)
        {
            args["path"] = Arg("path");
        }
        else if (tool == ToolKind.RunCommand)
        {
            args["command"] = Arg("command");
        }

        var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(false);
        if (!resp.Ok)
        {
            var err = resp.Error ?? "failed";
            if (resp.DeniedByUser) err += " (denied by local user confirmation)";
            return new
            {
                content = new[] { new { type = "text", text = err } },
                isError = true
            };
        }

        return new
        {
            content = new[] { new { type = "text", text = resp.Result ?? "" } },
            isError = false
        };
    }

    private static async Task<string?> ReadLspMessageAsync(Stream input)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var line = await ReadLineAsync(input).ConfigureAwait(false);
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
            var read = await input.ReadAsync(buffer.AsMemory(offset, len - offset)).ConfigureAwait(false);
            if (read == 0) return null;
            offset += read;
        }
        return Encoding.UTF8.GetString(buffer);
    }

    private static async Task WriteLspMessageAsync(Stream output, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var header = Encoding.ASCII.GetBytes($"Content-Length: {bytes.Length}\r\n\r\n");
        await output.WriteAsync(header).ConfigureAwait(false);
        await output.WriteAsync(bytes).ConfigureAwait(false);
        await output.FlushAsync().ConfigureAwait(false);
    }

    private static async Task<string?> ReadLineAsync(Stream input)
    {
        var buf = new List<byte>();
        var one = new byte[1];
        while (true)
        {
            var n = await input.ReadAsync(one).ConfigureAwait(false);
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
}
