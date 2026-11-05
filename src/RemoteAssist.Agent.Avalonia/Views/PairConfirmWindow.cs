using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>访客配对确认：有人想协助这台电脑 / 允许协助 / 拒绝 / 30 秒。</summary>
internal sealed class PairConfirmWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _countdown;
    private int _remaining;
    private readonly TaskCompletionSource<bool> _tcs = new();
    private readonly PermissionLevel _requested;

    public PairOutcome Outcome { get; private set; }
    public Task ResultTask => _tcs.Task;

    public PairConfirmWindow(PairRequestInfo info)
    {
        _requested = info.RequestedPermission;
        Title = "有人想协助这台电脑";
        Width = 480;
        Height = 260;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Topmost = true;
        Background = Brushes.White;

        _remaining = Math.Max(1, info.TimeoutSeconds);
        Outcome = new PairOutcome(false, null, ConfirmChannel.Ui, ConfirmDecision.Denied, DateTimeOffset.UtcNow);

        var root = new DockPanel { Margin = new Thickness(20) };

        var title = new TextBlock
        {
            Text = "有人想协助这台电脑",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        var body = new TextBlock
        {
            Text = "对方可以查看进程、日志和文件，不能直接改你的电脑。真正要改动时，还会再问你。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#3C4043"),
            Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(body, Dock.Top);
        root.Children.Add(body);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        var allow = new Button
        {
            Content = "允许协助",
            Width = 110,
            Height = 36,
            FontWeight = FontWeight.Bold,
            Background = Brush.Parse("#1A73E8"),
            Foreground = Brushes.White
        };
        allow.Click += (_, _) => Finish(approved: true, timedOut: false);

        var deny = new Button { Content = "拒绝", Width = 96, Height = 36 };
        deny.Click += (_, _) => Finish(approved: false, timedOut: false);

        buttons.Children.Add(allow);
        buttons.Children.Add(deny);
        root.Children.Add(buttons);

        _countdown = new TextBlock
        {
            Text = $"若不操作，将在 {_remaining} 秒后自动拒绝",
            Foreground = Brush.Parse("#5F6368"),
            VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(_countdown);

        Content = root;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            _remaining--;
            if (_remaining <= 0)
            {
                Finish(approved: false, timedOut: true);
                return;
            }
            _countdown.Text = $"若不操作，将在 {_remaining} 秒后自动拒绝";
        };
        Opened += (_, _) => _timer.Start();
        Closed += (_, _) =>
        {
            _timer.Stop();
            _tcs.TrySetResult(true);
        };
    }

    private void Finish(bool approved, bool timedOut)
    {
        _timer.Stop();
        var now = DateTimeOffset.UtcNow;
        if (timedOut)
            Outcome = new PairOutcome(false, null, ConfirmChannel.Ui, ConfirmDecision.TimedOut, now);
        else if (approved)
            Outcome = new PairOutcome(true, _requested, ConfirmChannel.Ui, ConfirmDecision.Approved, now);
        else
            Outcome = new PairOutcome(false, null, ConfirmChannel.Ui, ConfirmDecision.Denied, now);
        Close();
    }
}
