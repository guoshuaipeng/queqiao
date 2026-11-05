using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RemoteAssist.Protocol;

namespace RemoteAssist.Mcp;

/// <summary>
/// Minimal MCP server over stdio (JSON-RPC 2.0, Content-Length framing).
/// Env: REMOTE_ASSIST_HOST, REMOTE_ASSIST_PORT, REMOTE_ASSIST_CODE, REMOTE_ASSIST_PERM
/// Or first argv: --host --port --code --perm
/// </summary>
internal static class Program
{
    private static AssistClient? _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        var host = EnvOr("REMOTE_ASSIST_HOST", "127.0.0.1");
        var port = int.TryParse(EnvOr("REMOTE_ASSIST_PORT", "17890"), out var p) ? p : 17890;
        var code = EnvOr("REMOTE_ASSIST_CODE", "");
        var perm = Enum.TryParse(EnvOr("REMOTE_ASSIST_PERM", "Assist"), true, out PermissionLevel pl)
            ? pl
            : PermissionLevel.Assist;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--host" && i + 1 < args.Length) host = args[++i];
            else if (args[i] == "--port" && i + 1 < args.Length) port = int.Parse(args[++i]);
            else if (args[i] == "--code" && i + 1 < args.Length) code = args[++i];
            else if (args[i] == "--perm" && i + 1 < args.Length) perm = Enum.Parse<PermissionLevel>(args[++i], true);
        }

        // MCP uses stdout for protocol; log to stderr
        if (string.IsNullOrWhiteSpace(code))
        {
            await Console.Error.WriteLineAsync("REMOTE_ASSIST_CODE / --code 必填").ConfigureAwait(false);
            Environment.Exit(2);
        }

        try
        {
            _client = await AssistClient.PairAsync(host, port, code, perm, "mcp").ConfigureAwait(false);
            await Console.Error.WriteLineAsync($"[mcp] paired perm={_client.Permission}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync("[mcp] pair failed: " + ex.Message).ConfigureAwait(false);
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

            // notifications (no id) — ignore except initialized
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

    private static string EnvOr(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;

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
