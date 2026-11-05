using System.Windows.Forms;

namespace RemoteAssist.Agent;

internal static class ConfirmPrompt
{
    private static readonly object Gate = new();
    private static int _winFormsInit;

    public static Task<ConfirmOutcome> AskAsync(
        ConfirmRequestInfo info,
        bool autoYes,
        bool headless)
    {
        if (autoYes)
        {
            Console.WriteLine($"[自动确认操作] source={info.Source}; {info.Tool}: {info.Summary}");
            return Task.FromResult(new ConfirmOutcome(
                Approved: true,
                Channel: ConfirmChannel.AutoYes,
                Decision: ConfirmDecision.Approved,
                AtUtc: DateTimeOffset.UtcNow));
        }

        if (headless)
            return Task.Run(() => AskConsole(info));

        return Task.Run(() =>
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
        });
    }

    private static void EnsureWinFormsInit()
    {
        if (Interlocked.Exchange(ref _winFormsInit, 1) == 0)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
    }

    private static ConfirmOutcome AskUi(ConfirmRequestInfo info)
    {
        lock (Gate)
        {
            ConfirmOutcome? result = null;
            Exception? error = null;

            var thread = new Thread(() =>
            {
                try
                {
                    EnsureWinFormsInit();
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
    }

    private static ConfirmOutcome AskConsole(ConfirmRequestInfo info)
    {
        lock (Gate)
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
    }

    private static string? ReadLineWithTimeout(TimeSpan timeout)
    {
        var inputTask = Task.Run(Console.ReadLine);
        if (inputTask.Wait(timeout))
            return inputTask.Result;
        return null;
    }
}