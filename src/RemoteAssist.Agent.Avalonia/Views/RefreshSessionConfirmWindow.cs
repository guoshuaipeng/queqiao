using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>刷新会话码前确认（原文 FriendConnectGuide / quality.md §1）。</summary>
internal sealed class RefreshSessionConfirmWindow : Window
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    public bool Confirmed { get; private set; }
    public Task<bool> ResultTask => _tcs.Task;

    public RefreshSessionConfirmWindow()
    {
        Title = FriendConnectGuide.RefreshSessionConfirmOk;
        Width = 460;
        Height = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Topmost = true;
        Background = Brushes.White;

        var root = new DockPanel { Margin = new Thickness(20) };

        var title = new TextBlock
        {
            Name = "refreshConfirmTitle",
            Text = FriendConnectGuide.RefreshSessionConfirmOk,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        var body = new TextBlock
        {
            Name = "refreshConfirmBody",
            Text = FriendConnectGuide.RefreshSessionConfirm,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#3C4043"),
            Margin = new Thickness(0, 0, 0, 16)
        };
        DockPanel.SetDock(body, Dock.Top);
        root.Children.Add(body);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        var cancel = new Button
        {
            Name = "refreshConfirmCancel",
            Content = FriendConnectGuide.RefreshSessionConfirmCancel,
            Width = 96,
            Height = 36
        };
        cancel.Click += (_, _) => Finish(false);

        var ok = new Button
        {
            Name = "refreshConfirmOk",
            Content = FriendConnectGuide.RefreshSessionConfirmOk,
            Width = 120,
            Height = 36,
            FontWeight = FontWeight.Bold,
            Background = Brush.Parse("#1A73E8"),
            Foreground = Brushes.White
        };
        ok.Click += (_, _) => Finish(true);

        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        root.Children.Add(buttons);
        Content = root;

        Closing += (_, _) =>
        {
            if (!_tcs.Task.IsCompleted)
            {
                Confirmed = false;
                _tcs.TrySetResult(false);
            }
        };
    }

    private void Finish(bool ok)
    {
        Confirmed = ok;
        _tcs.TrySetResult(ok);
        try { Close(ok); } catch { /* ignore */ }
    }
}
