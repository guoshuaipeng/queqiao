#if !AGENT_CLI
using System.Windows.Forms;
#endif
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 配对确认与写操作确认共用串行队列：一次只弹出一个 UI，请求不丢。
/// AGENT_CLI（net8.0 跨平台被控）仅控制台确认，无 WinForms。
/// </summary>
internal static class ConfirmPrompt
{
    private static readonly SemaphoreSlim Queue = new(1, 1);
#if !AGENT_CLI
    private static int _winFormsInit;
#endif

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

#if AGENT_CLI
            _ = headless;
            return await Task.Run(() => AskConsole(info)).ConfigureAwait(false);
#else
            if (headless)
                return await Task.Run(() => AskConsole(info)).ConfigureAwait(false);

            return await Task.Run(() =>
            {
                try
                {
                    return AskUi(info);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[确认窗口失败，回退控制台] {ex.Message}");
                    return AskConsole(info);
                }
            }).ConfigureAwait(false);
#endif
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
                Console.WriteLine($"[自动确认配对] {info.RemoteEndpoint} client={info.ClientName} => {info.RequestedPermission}");
                return new PairOutcome(
                    Approved: true,
                    GrantedPermission: info.RequestedPermission,
                    Channel: ConfirmChannel.AutoYes,
                    Decision: ConfirmDecision.Approved,
                    AtUtc: DateTimeOffset.UtcNow);
            }

#if AGENT_CLI
            _ = headless;
            return await Task.Run(() => AskPairConsole(info)).ConfigureAwait(false);
#else
            if (headless)
                return await Task.Run(() => AskPairConsole(info)).ConfigureAwait(false);

            return await Task.Run(() =>
            {
                try
                {
                    return AskPairUi(info);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[配对窗口失败，回退控制台] {ex.Message}");
                    return AskPairConsole(info);
                }
            }).ConfigureAwait(false);
#endif
        }
        finally
        {
            Queue.Release();
        }
    }

#if !AGENT_CLI
    internal static void EnsureWinFormsInitialized()
    {
        if (Interlocked.Exchange(ref _winFormsInit, 1) == 0)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            catch (InvalidOperationException)
            {
                // 已由宿主 Program 初始化过 WinForms。
            }
        }
    }

    private static ConfirmOutcome AskUi(ConfirmRequestInfo info)
    {
        ConfirmOutcome? result = null;
        Exception? error = null;

        var thread = new Thread(() =>
        {
            try
            {
                EnsureWinFormsInitialized();
                using var dlg = new ConfirmDialog(info);
                var dr = dlg.ShowDialog();
                var now = DateTimeOffset.UtcNow;
                if (dlg.TimedOut)
                {
                    result = new ConfirmOutcome(false, ConfirmChannel.Ui, ConfirmDecision.TimedOut, now);
                }
                else if (dr == DialogResult.Yes)
                {
                    result = new ConfirmOutcome(true, ConfirmChannel.Ui, ConfirmDecision.Approved, now);
                }
                else
                {
                    result = new ConfirmOutcome(false, ConfirmChannel.Ui, ConfirmDecision.Denied, now);
                    MessageBox.Show(
                        WriteConfirmCopy.DeniedMessage,
                        WriteConfirmCopy.DialogTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (error is not null)
            throw error;
        return result ?? new ConfirmOutcome(false, ConfirmChannel.Ui, ConfirmDecision.Denied, DateTimeOffset.UtcNow);
    }

    private static PairOutcome AskPairUi(PairRequestInfo info)
    {
        PairOutcome? result = null;
        Exception? error = null;

        var thread = new Thread(() =>
        {
            try
            {
                EnsureWinFormsInitialized();
                using var dlg = new PairConfirmDialog(info);
                dlg.ShowDialog();
                var now = DateTimeOffset.UtcNow;
                if (dlg.TimedOut)
                {
                    result = new PairOutcome(false, null, ConfirmChannel.Ui, ConfirmDecision.TimedOut, now);
                }
                else if (dlg.GrantedPermission is { } granted)
                {
                    result = new PairOutcome(true, granted, ConfirmChannel.Ui, ConfirmDecision.Approved, now);
                }
                else
                {
                    result = new PairOutcome(false, null, ConfirmChannel.Ui, ConfirmDecision.Denied, now);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (error is not null)
            throw error;
        return result ?? new PairOutcome(false, null, ConfirmChannel.Ui, ConfirmDecision.Denied, DateTimeOffset.UtcNow);
    }
#else
    internal static void EnsureWinFormsInitialized() { }
#endif

    private static ConfirmOutcome AskConsole(ConfirmRequestInfo info)
    {
        // docs/first-run-mac-linux.md §4
        Console.WriteLine();
        Console.WriteLine(WriteConfirmCopy.DialogTitle);
        Console.WriteLine(info.Headline);
        if (!string.IsNullOrWhiteSpace(info.Detail))
            Console.WriteLine(info.Detail);
        Console.WriteLine("请输入 允许 或 拒绝（也可以输入 y 或 n）。");
        Console.WriteLine(WriteConfirmCopy.CountdownText(info.TimeoutSeconds));
        Console.Write("> ");

        var answer = ReadLineWithTimeout(TimeSpan.FromSeconds(info.TimeoutSeconds));
        var now = DateTimeOffset.UtcNow;
        if (answer is null)
        {
            Console.WriteLine(WriteConfirmCopy.TimedOutMessage);
            return new ConfirmOutcome(false, ConfirmChannel.Console, ConfirmDecision.TimedOut, now);
        }

        if (IsAllowAnswer(answer))
            return new ConfirmOutcome(true, ConfirmChannel.Console, ConfirmDecision.Approved, now);
        Console.WriteLine(WriteConfirmCopy.DeniedMessage);
        return new ConfirmOutcome(false, ConfirmChannel.Console, ConfirmDecision.Denied, now);
    }

    private static PairOutcome AskPairConsole(PairRequestInfo info)
    {
        Console.WriteLine();
        Console.WriteLine(WriteConfirmCopy.DialogTitle);
        Console.WriteLine($"对方请求连接本机（{info.ClientName}）");
        Console.WriteLine($"请求权限: {info.RequestedPermission}");
        Console.WriteLine(WriteConfirmCopy.CountdownText(info.TimeoutSeconds));
        Console.Write($"[{WriteConfirmCopy.AllowButton}]=按请求权限 / [G]=仅访客只读 / [{WriteConfirmCopy.DenyButton}] > ");

        var answer = ReadLineWithTimeout(TimeSpan.FromSeconds(info.TimeoutSeconds));
        var now = DateTimeOffset.UtcNow;
        if (answer is null)
        {
            Console.WriteLine($"（{WriteConfirmCopy.TimedOutMessage}）");
            return new PairOutcome(false, null, ConfirmChannel.Console, ConfirmDecision.TimedOut, now);
        }

        var ans = answer.Trim();
        if (IsDenyAnswer(ans))
            return new PairOutcome(false, null, ConfirmChannel.Console, ConfirmDecision.Denied, now);
        if (ans.Equals("G", StringComparison.OrdinalIgnoreCase) ||
            ans.Equals("GUEST", StringComparison.OrdinalIgnoreCase) ||
            ans.Equals("访客", StringComparison.Ordinal))
            return new PairOutcome(true, PermissionLevel.Guest, ConfirmChannel.Console, ConfirmDecision.Approved, now);
        if (IsAllowAnswer(ans))
            return new PairOutcome(true, info.RequestedPermission, ConfirmChannel.Console, ConfirmDecision.Approved, now);
        Console.WriteLine(WriteConfirmCopy.DeniedMessage);
        return new PairOutcome(false, null, ConfirmChannel.Console, ConfirmDecision.Denied, now);
    }

    private static bool IsAllowAnswer(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return false;
        var a = answer.Trim();
        // first-run-mac-linux.md §4：允许 / y / Y
        return a.Equals(WriteConfirmCopy.AllowButton, StringComparison.Ordinal) ||
               a.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDenyAnswer(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return false;
        var a = answer.Trim();
        // first-run-mac-linux.md §4：拒绝 / n / N
        return a.Equals(WriteConfirmCopy.DenyButton, StringComparison.Ordinal) ||
               a.Equals("n", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadLineWithTimeout(TimeSpan timeout)
    {
        var inputTask = Task.Run(Console.ReadLine);
        if (inputTask.Wait(timeout))
            return inputTask.Result;
        return null;
    }
}
