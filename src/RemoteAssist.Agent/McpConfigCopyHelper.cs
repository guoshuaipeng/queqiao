using System.Diagnostics;
using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 托盘与会话窗共用：复制 Cursor mcp.json 片段；缺 MCP exe 时引导发布。
/// 始终使用调用方传入的当前载荷（刷新会话码后应传入新 QrPayload）。
/// </summary>
internal static class McpConfigCopyHelper
{
    public enum OutcomeKind
    {
        CopiedSnippet,
        CopiedPublishCommand,
        StartedPublish,
        Cancelled,
        Failed
    }

    public sealed record Outcome(OutcomeKind Kind, bool ExeExists, string Message, string ExePath, string Payload);

    /// <summary>
    /// 复制当前会话的 MCP 配置。payload 必须是调用瞬间的 _host.QrPayload。
    /// </summary>
    public static Outcome Copy(string payload, IWin32Window? owner = null)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new Outcome(OutcomeKind.Failed, false, "当前没有可用的会话载荷。", "", payload ?? "");

        try
        {
            var exists = McpExeLocator.TryResolveExisting(out var exe);
            if (!exists)
                exe = McpExeLocator.Resolve();

            if (!exists)
            {
                var action = PromptMissingExe(owner, exe);
                switch (action)
                {
                    case MissingAction.CopySnippetAnyway:
                        break;
                    case MissingAction.CopyPublishCommand:
                        Clipboard.SetText(McpExeLocator.GetPublishCommandHint());
                        return new Outcome(
                            OutcomeKind.CopiedPublishCommand,
                            false,
                            "已复制发布命令。在仓库根目录的 PowerShell 中粘贴执行，完成后再点「复制 MCP 配置」。",
                            exe,
                            payload);
                    case MissingAction.RunPublishInBackground:
                        if (!TryStartPublishInBackground(out var startMsg))
                            return new Outcome(OutcomeKind.Failed, false, startMsg, exe, payload);
                        return new Outcome(
                            OutcomeKind.StartedPublish,
                            false,
                            startMsg,
                            exe,
                            payload);
                    default:
                        return new Outcome(OutcomeKind.Cancelled, false, "已取消复制。", exe, payload);
                }
            }

            var text = McpConfigSnippet.BuildClipboardBundle(exe, payload);
            Clipboard.SetText(text);

            var msg = exists
                ? "已复制 Cursor mcp.json 片段（含当前会话载荷）。粘贴到 MCP 配置即可。"
                : "已复制片段（占位路径）。请先运行 scripts\\publish-mcp.ps1，或把 command 改成实际 MCP exe。"
                  + Environment.NewLine + "当前路径：" + exe;

            return new Outcome(OutcomeKind.CopiedSnippet, exists, msg, exe, payload);
        }
        catch (Exception ex)
        {
            return new Outcome(OutcomeKind.Failed, false, "复制失败：" + ex.Message, "", payload);
        }
    }

    private enum MissingAction
    {
        Cancel,
        CopySnippetAnyway,
        CopyPublishCommand,
        RunPublishInBackground
    }

    private static MissingAction PromptMissingExe(IWin32Window? owner, string guessedExe)
    {
        var publishCmd = McpExeLocator.GetPublishCommandHint();
        var script = McpExeLocator.TryFindPublishScript();

        var page = new TaskDialogPage
        {
            Caption = "远端协助",
            Heading = "未找到 RemoteAssist.Mcp.exe",
            Text =
                "复制 MCP 配置前需要先发布 MCP 可执行文件。" + Environment.NewLine + Environment.NewLine +
                "猜测路径：" + guessedExe + Environment.NewLine + Environment.NewLine +
                "发布命令：" + Environment.NewLine + publishCmd,
            Icon = TaskDialogIcon.Warning,
            AllowCancel = true,
            DefaultButton = TaskDialogButton.Cancel
        };

        var copySnippet = new TaskDialogButton("仍复制配置（占位路径）");
        var copyCmd = new TaskDialogButton("复制发布命令");
        var runBg = new TaskDialogButton("后台运行发布脚本")
        {
            Enabled = !string.IsNullOrEmpty(script)
        };
        var cancel = TaskDialogButton.Cancel;

        page.Buttons.Add(copySnippet);
        page.Buttons.Add(copyCmd);
        page.Buttons.Add(runBg);
        page.Buttons.Add(cancel);

        var result = owner is null
            ? TaskDialog.ShowDialog(page)
            : TaskDialog.ShowDialog(owner, page);

        if (result == copySnippet) return MissingAction.CopySnippetAnyway;
        if (result == copyCmd) return MissingAction.CopyPublishCommand;
        if (result == runBg) return MissingAction.RunPublishInBackground;
        return MissingAction.Cancel;
    }

    internal static bool TryStartPublishInBackground(out string message)
    {
        var script = McpExeLocator.TryFindPublishScript();
        if (string.IsNullOrEmpty(script))
        {
            message = "找不到 scripts\\publish-mcp.ps1。已改为把发布命令放到剪贴板，请手动执行。";
            try { Clipboard.SetText(McpExeLocator.GetPublishCommandHint()); } catch { /* ignore */ }
            return false;
        }

        try
        {
            var root = McpExeLocator.TryFindRepoRoot()
                       ?? Path.GetDirectoryName(Path.GetDirectoryName(script)!)
                       ?? Environment.CurrentDirectory;

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
                WorkingDirectory = root,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                CreateNoWindow = false
            };
            Process.Start(psi);
            message = "已在后台启动 publish-mcp.ps1（新开 PowerShell 窗口）。完成后请再点「复制 MCP 配置」。";
            return true;
        }
        catch (Exception ex)
        {
            try { Clipboard.SetText(McpExeLocator.GetPublishCommandHint()); } catch { /* ignore */ }
            message = "无法启动发布脚本：" + ex.Message + "。发布命令已复制到剪贴板。";
            return false;
        }
    }
}

