using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>写操作确认：对方想在这台电脑执行操作 / 允许 / 拒绝 / 60 秒。</summary>
internal sealed class WriteConfirmWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _countdown;
    private int _remaining;
    private readonly TaskCompletionSource<bool> _tcs = new();

    public ConfirmOutcome Outcome { get; private set; }
    public Task ResultTask => _tcs.Task;

    public WriteConfirmWindow(ConfirmRequestInfo info)
    {
        Title = WriteConfirmCopy.DialogTitle;
        Width = 520;
        Height = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Topmost = true;
        Background = Brushes.White;

        _remaining = Math.Max(1, info.TimeoutSeconds);
        Outcome = new ConfirmOutcome(false, ConfirmChannel.Ui, ConfirmDecision.Denied, DateTimeOffset.UtcNow);

        var root = new DockPanel { Margin = new Thickness(20) };

        var title = new TextBlock
        {
            Text = WriteConfirmCopy.DialogTitle,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        var headline = new TextBlock
        {
            Text = info.Headline,
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        DockPanel.SetDock(headline, Dock.Top);
        root.Children.Add(headline);

        if (!string.IsNullOrWhiteSpace(info.Detail))
        {
            var detail = new TextBlock
            {
                Text = info.Detail,
                FontFamily = new FontFamily("Consolas, monospace"),
                FontSize = 12,
                Foreground = Brush.Parse("#5F6368"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            DockPanel.SetDock(detail, Dock.Top);
            root.Children.Add(detail);
        }

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        var allow = new Button
        {
            Content = WriteConfirmCopy.AllowButton,
            Width = 100,
            Height = 36,
            FontWeight = FontWeight.Bold,
            Background = Brush.Parse("#1A73E8"),
            Foreground = Brushes.White
        };
        allow.Click += (_, _) => Finish(approved: true, timedOut: false);

        var deny = new Button
        {
            Content = WriteConfirmCopy.DenyButton,
            Width = 96,
            Height = 36
        };
        deny.Click += (_, _) => Finish(approved: false, timedOut: false);

        buttons.Children.Add(allow);
        buttons.Children.Add(deny);
        root.Children.Add(buttons);

        _countdown = new TextBlock
        {
            Text = WriteConfirmCopy.CountdownText(_remaining),
            Foreground = Brush.Parse("#5F6368"),
            Margin = new Thickness(0, 8, 0, 0)
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
            _countdown.Text = WriteConfirmCopy.CountdownText(_remaining);
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
            Outcome = new ConfirmOutcome(false, ConfirmChannel.Ui, ConfirmDecision.TimedOut, now);
        else if (approved)
            Outcome = new ConfirmOutcome(true, ConfirmChannel.Ui, ConfirmDecision.Approved, now);
        else
        {
            Outcome = new ConfirmOutcome(false, ConfirmChannel.Ui, ConfirmDecision.Denied, now);
            // 拒绝后短暂展示原文（截图/可见反馈）
            _countdown.Text = WriteConfirmCopy.DeniedMessage;
            _countdown.Foreground = Brush.Parse("#C5221F");
        }
        Close();
    }
}
