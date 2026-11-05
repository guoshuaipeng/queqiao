using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Agent;

internal sealed class SessionState
{
    public required string Token { get; init; }
    public required PermissionLevel Permission { get; init; }
    public required DateTimeOffset ExpiresAtUtc { get; init; }
    public required string ClientName { get; init; }
    public required string RemoteEndpoint { get; init; }
    public DateTimeOffset PairedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public CancellationTokenSource? ConnectionCts { get; set; }
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
}

/// <summary>托盘/UI 展示用的已配对客户端摘要。</summary>
internal sealed record PairedClientInfo(
    string Token,
    string ClientName,
    string RemoteEndpoint,
    PermissionLevel Permission,
    DateTimeOffset PairedAtUtc,
    DateTimeOffset ExpiresAtUtc);

internal sealed class PendingConfirmation
{
    public required string Id { get; init; }
    public required ToolKind Tool { get; init; }
    public required Dictionary<string, string?> Arguments { get; init; }
    public required TaskCompletionSource<bool> Gate { get; init; }
    public required string Summary { get; init; }
}

internal sealed class AuditLog
{
    private readonly string _path;
    private readonly object _lock = new();

    public AuditLog(string path) => _path = path;

    public string FilePath => _path;

    /// <summary>新审计行写入后触发（UI 可订阅刷新）。</summary>
    public event Action? EntryWritten;

    public void Write(string eventType, string detail)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}\t{eventType}\t{detail}";
        lock (_lock)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
        }
        Console.WriteLine($"[AUDIT] {line}");
        try { EntryWritten?.Invoke(); } catch { /* ignore UI errors */ }
    }

    /// <summary>读取最近若干条审计行（文件尾部，按时间正序返回，旧→新）。</summary>
    public IReadOnlyList<AuditEntry> ReadRecent(int maxLines = 50)
    {
        maxLines = Math.Clamp(maxLines, 1, 500);
        string[] lines;
        lock (_lock)
        {
            if (!File.Exists(_path))
                return Array.Empty<AuditEntry>();
            // 小文件直接读；大文件仍可接受（审计日志通常不大）
            lines = File.ReadAllLines(_path, Encoding.UTF8);
        }

        var take = lines.Length <= maxLines ? lines : lines[^maxLines..];
        var list = new List<AuditEntry>(take.Length);
        foreach (var line in take)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            list.Add(AuditEntry.Parse(line));
        }
        return list;
    }
}

/// <summary>审计日志一行的可读解析。</summary>
internal sealed record AuditEntry(string Raw, string Timestamp, string EventType, string Detail)
{
    public static AuditEntry Parse(string line)
    {
        var parts = line.Split('\t', 3);
        if (parts.Length >= 3)
            return new AuditEntry(line, parts[0], parts[1], parts[2]);
        if (parts.Length == 2)
            return new AuditEntry(line, parts[0], parts[1], "");
        return new AuditEntry(line, "", "raw", line);
    }

    public string DisplaySummary
    {
        get
        {
            var label = EventType switch
            {
                "pair_ok" => "配对成功",
                "pair_reject" => "配对拒绝",
                "pair_timeout" => "配对超时",
                "tool_ok" => "工具成功",
                "tool_error" => "工具失败",
                "tool_confirmed" => "已确认执行",
                "tool_denied_user" => "用户拒绝执行",
                "tool_denied_perm" => "权限不足",
                "tool_admin_auto" => "管理员自动执行",
                "session_refresh" => "刷新会话码",
                "session_kick" => "断开客户端",
                "host_selected" => "切换主机 IP",
                "client_disconnect" => "客户端断开",
                "client_error" => "客户端错误",
                "listen_start" => "开始监听",
                "listen_stop" => "停止监听",
                _ => EventType
            };
            var ts = Timestamp.Length >= 19 ? Timestamp[..19] : Timestamp;
            var detail = Detail.Length > 80 ? Detail[..77] + "…" : Detail;
            return string.IsNullOrEmpty(detail) ? $"{ts}  {label}" : $"{ts}  {label}  ·  {detail}";
        }
    }
}

internal static class ToolExecutor
{
    private static readonly HashSet<string> BlockedPathPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.ssh",
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Credentials",
    };

    public static string ListProcesses()
    {
        var procs = System.Diagnostics.Process.GetProcesses()
            .OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
            .Select(p =>
            {
                try { return $"{p.Id}\t{p.ProcessName}"; }
                catch { return null; }
            })
            .Where(s => s is not null)
            .Take(200);
        return string.Join(Environment.NewLine, procs!);
    }

    public static string ReadLogTail(string path, int lines)
    {
        EnsurePathAllowed(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);
        lines = Math.Clamp(lines, 1, 500);
        var all = File.ReadLines(path).TakeLast(lines);
        return string.Join(Environment.NewLine, all);
    }

    public static string ListDir(string path)
    {
        EnsurePathAllowed(path);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);
        var entries = Directory.EnumerateFileSystemEntries(path)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .Select(p =>
            {
                var name = Path.GetFileName(p);
                var kind = Directory.Exists(p) ? "DIR" : "FILE";
                return $"{kind}\t{name}";
            });
        return string.Join(Environment.NewLine, entries);
    }

    public static string ReadFile(string path, int maxBytes = 64 * 1024)
    {
        EnsurePathAllowed(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);
        maxBytes = Math.Clamp(maxBytes, 1, 256 * 1024);
        var info = new FileInfo(path);
        if (info.Length > maxBytes)
        {
            using var fs = File.OpenRead(path);
            var buf = new byte[maxBytes];
            var read = fs.Read(buf, 0, maxBytes);
            return Encoding.UTF8.GetString(buf, 0, read) +
                   $"{Environment.NewLine}...[truncated, file={info.Length} bytes, shown={read}]";
        }
        return File.ReadAllText(path, Encoding.UTF8);
    }

    public static string RunCommand(string command, int timeoutMs = 15_000)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("command is empty");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c " + command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start process.");
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        if (!proc.WaitForExit(timeoutMs))
        {
            try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException($"Command timed out after {timeoutMs}ms");
        }
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        var sb = new StringBuilder();
        sb.AppendLine($"exit={proc.ExitCode}");
        if (!string.IsNullOrWhiteSpace(stdout)) sb.AppendLine("--- stdout ---").AppendLine(stdout.TrimEnd());
        if (!string.IsNullOrWhiteSpace(stderr)) sb.AppendLine("--- stderr ---").AppendLine(stderr.TrimEnd());
        return sb.ToString();
    }

    private static void EnsurePathAllowed(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path is empty");
        var full = Path.GetFullPath(path);
        foreach (var blocked in BlockedPathPrefixes)
        {
            if (!string.IsNullOrWhiteSpace(blocked) &&
                full.StartsWith(Path.GetFullPath(blocked), StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"Path blocked for safety: {full}");
        }
    }
}


