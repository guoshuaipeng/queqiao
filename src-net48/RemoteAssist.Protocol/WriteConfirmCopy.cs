using System;

namespace RemoteAssist.Protocol;

/// <summary>
/// R021–R025 / R048：协助端「复制安装说明」亲友向正文（不含端口/放行入站；不暗示已安装或已连通）。
/// </summary>
public static class WriteConfirmCopy
{
    /// <summary>复制安装说明后的界面提示（剪贴板仍是说明原文）。</summary>
    public const string InstallCopiedHint = "说明里已带上官网下载地址，发给对方即可";

    /// <summary>协助端「复制安装说明」剪贴板正文。</summary>
    public const string InstallInstructions =
        "请打开官网下载「鹊桥远程协助」：http://121.196.45.81/\n" +
        "Windows 安装包：http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe\n" +
        "Linux 包：http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz（解开后运行，需要先装好 .NET 8 桌面方向运行时）\n" +
        "macOS 尚未提供正式下载。\n" +
        "如果你手头有安装包，也可以一起发出去。\n" +
        "装好后打开，选「接受协助」，再点「复制连接凭证」，把复制出来的发给我。\n" +
        "如果系统问起是否允许联网，选择允许。\n" +
        "若打开时提示缺少 .NET，先安装「.NET 8 运行时」（用来跑这个程序的，不是开发工具）。\n" +
        "打开 https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0 ，在「运行时」那一栏按系统下载，不要选 SDK。\n" +
        "如果浏览器提示通常不会下载，选择保留；如果打开时系统提示不认识这个程序，选择仍要运行。";

    public static string InstallInstructionsClipboard =>
        InstallInstructions.Replace("\n", Environment.NewLine);
}
