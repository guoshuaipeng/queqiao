using System;
using System.Collections.Generic;

namespace RemoteAssist.Protocol;

/// <summary>
/// 写操作确认窗白话（R004 / PRODUCT）+ 协助端「复制安装说明」（R021–R025 / R048）。
/// 界面禁止出现 Assist / MCP / ownerLink / TLS。
/// Windows 主路径安装前提为 .NET Framework 4.8（非 .NET 8）；Linux 包仍依赖 .NET 8 运行时。
/// </summary>
public sealed class WriteConfirmDescription
{
    public WriteConfirmDescription(string headline, string? detail = null)
    {
        Headline = headline;
        Detail = detail;
    }

    public string Headline { get; }
    public string? Detail { get; }

    /// <summary>审计/单行摘要：人话为主，细节附后。</summary>
    public string ForAudit =>
        string.IsNullOrWhiteSpace(Detail) ? Headline : Headline + " · " + Detail;
}

public static class WriteConfirmCopy
{
    public const string DialogTitle = "对方想在这台电脑执行操作";

    public const string DeniedMessage = "已拒绝，未做任何改动";

    public const string TimedOutMessage = "确认超时，未做任何改动";

    public const string AllowButton = "允许";

    public const string DenyButton = "拒绝";

    /// <summary>复制安装说明后的界面提示（剪贴板仍是说明原文）。</summary>
    public const string InstallCopiedHint = "说明里已带上官网下载地址，发给对方即可";

    /// <summary>
    /// 协助端「复制安装说明」剪贴板正文。
    /// Windows：.NET Framework 4.8；Linux：.NET 8 桌面运行时；含官网下载与不受信任打开指引。
    /// </summary>
    public const string InstallInstructions =
        "请打开官网下载「鹊桥远程协助」：http://121.196.45.81/\n" +
        "Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Net48-Setup-0.1.0.exe（需要本机已安装 .NET Framework 4.8；勿再主推 net8 自包含 RemoteAssist-Setup）\n" +
        "Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）\n" +
        "macOS 尚未提供正式下载。\n" +
        "如果你手头有安装包，也可以一起发出去。\n" +
        "装好后打开，选「接受协助」，再点「复制连接凭证」，把复制出来的发给我。\n" +
        "如果系统问起是否允许联网，选择允许。\n" +
        "若 Windows 打开时提示缺少 .NET Framework，先安装「.NET Framework 4.8」（用来跑这个程序的，不是开发工具）。\n" +
        "打开 https://dotnet.microsoft.com/zh-cn/download/dotnet-framework/net48 下载并安装。\n" +
        "Linux 若提示缺少 .NET，先安装「.NET 8 运行时」（桌面方向，不是 SDK）：https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0\n" +
        "如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。";

    public static string InstallInstructionsClipboard =>
        InstallInstructions.Replace("\n", Environment.NewLine);

    public static WriteConfirmDescription Describe(ToolKind tool, IReadOnlyDictionary<string, string?>? args = null)
    {
        args ??= new Dictionary<string, string?>();
        switch (tool)
        {
            case ToolKind.RunCommand:
                return DescribeRun(GetArgRaw(args, "command"));
            case ToolKind.ReadFile:
                return new WriteConfirmDescription("读取一个文件", DetailPath(GetArgRaw(args, "path")));
            case ToolKind.ReadLogTail:
                return new WriteConfirmDescription("查看一段日志", DetailPath(GetArgRaw(args, "path")));
            case ToolKind.ListDir:
                return new WriteConfirmDescription("查看一个文件夹", DetailPath(GetArgRaw(args, "path")));
            case ToolKind.ListProcesses:
                return new WriteConfirmDescription("查看正在运行的程序列表");
            default:
                return new WriteConfirmDescription("执行一项电脑操作");
        }
    }

    public static string DescribeText(ToolKind tool, IReadOnlyDictionary<string, string?>? args = null) =>
        Describe(tool, args).ForAudit;

    public static WriteConfirmDescription DescribeFromToolName(string? toolName, IReadOnlyDictionary<string, string?>? args = null)
    {
        if (Enum.TryParse(toolName, true, out ToolKind tool))
            return Describe(tool, args);
        return new WriteConfirmDescription("执行一项电脑操作");
    }

    public static string CountdownText(int seconds) =>
        "若不操作，将在 " + Math.Max(0, seconds) + " 秒后自动拒绝";

    public static bool ContainsBannedUiTerm(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return text.IndexOf("Assist", StringComparison.Ordinal) >= 0 ||
               text.IndexOf("MCP", StringComparison.Ordinal) >= 0 ||
               text.IndexOf("ownerLink", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("TLS", StringComparison.Ordinal) >= 0;
    }

    private static WriteConfirmDescription DescribeRun(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new WriteConfirmDescription("运行一条命令");

        var trimmed = command.Trim();
        if (LooksLikeDelete(trimmed))
        {
            return new WriteConfirmDescription(
                "删除一个文件或文件夹",
                "命令：" + Truncate(trimmed, 160));
        }

        if (LooksLikeListProcesses(trimmed))
        {
            return new WriteConfirmDescription(
                "运行一条命令，列出当前进程",
                "命令：" + Truncate(trimmed, 160));
        }

        return new WriteConfirmDescription(
            "运行一条命令",
            "命令：" + Truncate(trimmed, 160));
    }

    private static bool LooksLikeDelete(string command)
    {
        var lower = command.ToLowerInvariant();
        return lower.IndexOf("del ", StringComparison.Ordinal) >= 0 ||
               lower.StartsWith("del ", StringComparison.Ordinal) ||
               lower.IndexOf("rmdir", StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("remove-item", StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("rm -", StringComparison.Ordinal) >= 0 ||
               lower.IndexOf(" erase ", StringComparison.Ordinal) >= 0 ||
               lower.StartsWith("erase ", StringComparison.Ordinal);
    }

    private static bool LooksLikeListProcesses(string command)
    {
        var lower = command.ToLowerInvariant();
        return lower.IndexOf("tasklist", StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("get-process", StringComparison.Ordinal) >= 0 ||
               lower.IndexOf("ps aux", StringComparison.Ordinal) >= 0 ||
               lower == "ps";
    }

    private static string? DetailPath(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : "路径：" + Truncate(path.Trim(), 160);

    private static string Truncate(string s, int max)
    {
        if (s.Length <= max) return s;
        return s.Substring(0, max - 1) + "…";
    }

    private static string? GetArgRaw(IReadOnlyDictionary<string, string?> args, string key) =>
        args.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;
}