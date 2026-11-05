using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// Avalonia 被控确认：配对 / 写操作弹窗。文案见 first-run-mac-linux.md 第 2 节。
/// </summary>
internal static class ConfirmPrompt
{
    private static readonly SemaphoreSlim Queue = new(1, 1);

    public static Dispatcher? UiDispatcher { get; set; }

    /// <summary>联调钩子：自动允许访客配对（非正式产品路径）。</summary>
    public static bool ForceAllowPair { get; set; }

    /// <summary>联调钩子：自动拒绝写操作并返回 DeniedMessage（非正式产品路径）。</summary>
    public static bool ForceDenyWrites { get; set; }

    public static async Task<ConfirmOutcome> AskAsync(
        ConfirmRequestInfo info,
        bool autoYes,
        bool headless)
    {
        await Queue.WaitAsync().ConfigureAwait(false);
        try
        {
            if (autoYes)
            {
                Console.WriteLine($"[自动确认] {info.Headline}");
                return new ConfirmOutcome(true, ConfirmChannel.AutoYes, ConfirmDecision.Approved, DateTimeOffset.UtcNow);
            }

            if (ForceDenyWrites)
            {
                Console.WriteLine(WriteConfirmCopy.DeniedMessage);
                return new ConfirmOutcome(false, ConfirmChannel.Ui, ConfirmDecision.Denied, DateTimeOffset.UtcNow);
            }

            if (headless || UiDispatcher is null)
                return AskConsoleWrite(info);

            var tcs = new TaskCompletionSource<ConfirmOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
            await UiDispatcher.InvokeAsync(() =>
            {
                _ = ShowWriteAsync(info, tcs);
            });
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            Queue.Release();
        }
    }

    public static async Task<PairOutcome> AskPairAsync(
        PairRequestInfo info,
        bool autoYes,
        bool headless)
    {
        await Queue.WaitAsync().ConfigureAwait(false);
        try
        {
            if (autoYes)
            {
                Console.WriteLine($"[自动确认配对] {info.ClientName}");
                return new PairOutcome(
                    true, info.RequestedPermission, ConfirmChannel.AutoYes, ConfirmDecision.Approved, DateTimeOffset.UtcNow);
            }

            if (ForceAllowPair)
            {
                Console.WriteLine("[联调] 自动允许协助");
                return new PairOutcome(
                    true, info.RequestedPermission, ConfirmChannel.Ui, ConfirmDecision.Approved, DateTimeOffset.UtcNow);
            }

            if (headless || UiDispatcher is null)
                return AskConsolePair(info);

            var tcs = new TaskCompletionSource<PairOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
            await UiDispatcher.InvokeAsync(() =>
            {
                _ = ShowPairAsync(info, tcs);
            });
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            Queue.Release();
        }
    }

    internal static void EnsureWinFormsInitialized() { }

    private static async Task ShowWriteAsync(ConfirmRequestInfo info, TaskCompletionSource<ConfirmOutcome> tcs)
    {
        try
        {
            var win = new WriteConfirmWindow(info);
            var owner = GetOwner();
            if (owner is not null)
                await win.ShowDialog(owner).ConfigureAwait(true);
            else
            {
                win.Show();
                await win.ResultTask.ConfigureAwait(true);
            }
            tcs.TrySetResult(win.Outcome);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }

    private static async Task ShowPairAsync(PairRequestInfo info, TaskCompletionSource<PairOutcome> tcs)
    {
        try
        {
            var win = new PairConfirmWindow(info);
            var owner = GetOwner();
            if (owner is not null)
                await win.ShowDialog(owner).ConfigureAwait(true);
            else
            {
                win.Show();
                await win.ResultTask.ConfigureAwait(true);
            }
            tcs.TrySetResult(win.Outcome);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }

    private static Window? GetOwner()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    private static ConfirmOutcome AskConsoleWrite(ConfirmRequestInfo info)
    {
        Console.WriteLine();
        Console.WriteLine(WriteConfirmCopy.DialogTitle);
        Console.WriteLine(info.Headline);
        if (!string.IsNullOrWhiteSpace(info.Detail))
            Console.WriteLine(info.Detail);
        Console.WriteLine(WriteConfirmCopy.CountdownText(info.TimeoutSeconds));
        Console.Write("> ");
        var answer = Console.ReadLine();
        var now = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(answer))
            return new ConfirmOutcome(false, ConfirmChannel.Console, ConfirmDecision.TimedOut, now);
        if (answer.Trim().Equals("允许", StringComparison.Ordinal) ||
            answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
            return new ConfirmOutcome(true, ConfirmChannel.Console, ConfirmDecision.Approved, now);
        Console.WriteLine(WriteConfirmCopy.DeniedMessage);
        return new ConfirmOutcome(false, ConfirmChannel.Console, ConfirmDecision.Denied, now);
    }

    private static PairOutcome AskConsolePair(PairRequestInfo info)
    {
        Console.WriteLine();
        Console.WriteLine("有人想协助这台电脑");
        Console.WriteLine($"若不操作，将在 {info.TimeoutSeconds} 秒后自动拒绝");
        Console.Write("> ");
        var answer = Console.ReadLine();
        var now = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(answer))
            return new PairOutcome(false, null, ConfirmChannel.Console, ConfirmDecision.TimedOut, now);
        if (answer.Trim() is "允许" or "允许协助" ||
            answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
            return new PairOutcome(true, info.RequestedPermission, ConfirmChannel.Console, ConfirmDecision.Approved, now);
        return new PairOutcome(false, null, ConfirmChannel.Console, ConfirmDecision.Denied, now);
    }
}

