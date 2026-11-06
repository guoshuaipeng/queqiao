using System.Windows.Forms;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>
/// 配对确认与写操作确认共用串行队列：一次只弹出一个 UI，请求不丢。
/// </summary>
internal static class ConfirmPrompt
{
    private static readonly SemaphoreSlim Queue = new(1, 1);
    private static int _winFormsInit;

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
                Console.WriteLine($"[自动确认操作] source={info.Source}; {info.Tool}: {info.Summary}");
                return new ConfirmOutcome(
                    Approved: true,
                    Channel: ConfirmChannel.AutoYes,
                    Decision: ConfirmDecision.Approved,
                    AtUtc: DateTimeOffset.UtcNow);
            }

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
                    Console.WriteLine("[确认窗口失败，回退控制台] " + FriendConnectGuide.RedactSecureMaterial(ex.Message));
                    return AskConsole(info);
                }
            }).ConfigureAwait(false);
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
                    Console.WriteLine("[配对窗口失败，回退控制台] " + FriendConnectGuide.RedactSecureMaterial(ex.Message));
                    return AskPairConsole(info);
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            Queue.Release();
        }
    }

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
                // 已由宿主 Program / 首页 Form 初始化过 WinForms（R043 实测根因）。
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

    private static ConfirmOutcome AskConsole(ConfirmRequestInfo info)
    {
        Console.WriteLine();
        Console.WriteLine("!!!!!!!! 需要确认的操作 !!!!!!!!");
        Console.WriteLine($"来源: {info.Source}");
        Console.WriteLine($"工具: {info.Tool}");
        Console.WriteLine($"详情: {info.Summary}");
        Console.WriteLine($"超时: {info.TimeoutSeconds}s（无输入则自动拒绝）");
        Console.Write("是否允许执行? [Y/N] > ");

        var answer = ReadLineWithTimeout(TimeSpan.FromSeconds(info.TimeoutSeconds));
        var now = DateTimeOffset.UtcNow;
        if (answer is null)
        {
            Console.WriteLine("（超时，自动拒绝）");
            return new ConfirmOutcome(false, ConfirmChannel.Console, ConfirmDecision.TimedOut, now);
        }

        var ans = answer.Trim().ToUpperInvariant();
        if (ans is "Y" or "YES")
            return new ConfirmOutcome(true, ConfirmChannel.Console, ConfirmDecision.Approved, now);
        return new ConfirmOutcome(false, ConfirmChannel.Console, ConfirmDecision.Denied, now);
    }

    private static PairOutcome AskPairConsole(PairRequestInfo info)
    {
        Console.WriteLine();
        Console.WriteLine("!!!!!!!! 配对请求 !!!!!!!!");
        Console.WriteLine($"来源 IP : {info.RemoteEndpoint}");
        Console.WriteLine($"客户端  : {info.ClientName}");
        Console.WriteLine($"请求权限: {info.RequestedPermission}");
        Console.WriteLine($"超时: {info.TimeoutSeconds}s（无输入则自动拒绝）");
        Console.Write("允许吗? [Y]=按请求权限 / [G]=仅访客只读 / [N]=拒绝 > ");

        var answer = ReadLineWithTimeout(TimeSpan.FromSeconds(info.TimeoutSeconds));
        var now = DateTimeOffset.UtcNow;
        if (answer is null)
        {
            Console.WriteLine("（超时，自动拒绝）");
            return new PairOutcome(false, null, ConfirmChannel.Console, ConfirmDecision.TimedOut, now);
        }

        var ans = answer.Trim().ToUpperInvariant();
        if (ans is "N" or "NO")
            return new PairOutcome(false, null, ConfirmChannel.Console, ConfirmDecision.Denied, now);
        if (ans is "G" or "GUEST")
            return new PairOutcome(true, PermissionLevel.Guest, ConfirmChannel.Console, ConfirmDecision.Approved, now);
        return new PairOutcome(true, info.RequestedPermission, ConfirmChannel.Console, ConfirmDecision.Approved, now);
    }

    private static string? ReadLineWithTimeout(TimeSpan timeout)
    {
        var inputTask = Task.Run(Console.ReadLine);
        if (inputTask.Wait(timeout))
            return inputTask.Result;
        return null;
    }
}

