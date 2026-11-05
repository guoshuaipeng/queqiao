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
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
}

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

    public void Write(string eventType, string detail)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}\t{eventType}\t{detail}";
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
        }
        Console.WriteLine($"[AUDIT] {line}");
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
