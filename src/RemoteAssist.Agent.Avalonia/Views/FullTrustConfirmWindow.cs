using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RemoteAssist.Protocol;

namespace RemoteAssist.Agent;

/// <summary>打开「完全信任」前的本机二次确认（原文 TrustLevelCopy / trust-levels.md）。</summary>
internal sealed class FullTrustConfirmWindow : Window
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    public bool Confirmed { get; private set; }
    public Task<bool> ResultTask => _tcs.Task;

    public FullTrustConfirmWindow()
    {
        Title = TrustLevelCopy.FullTrustConfirmTitle;
        Width = 520;
        Height = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Topmost = true;
        Background = Brushes.White;

        var root = new DockPanel { Margin = new Thickness(20) };

        var title = new TextBlock
        {
            Name = "fullTrustConfirmTitle",
            Text = TrustLevelCopy.FullTrustConfirmTitle,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        var body = new TextBlock
        {
            Name = "fullTrustConfirmBody",
            Text = TrustLevelCopy.FullTrustConfirmBody,
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
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        var ok = new Button
        {
            Name = "fullTrustConfirmOk",
            Content = TrustLevelCopy.FullTrustConfirmOk,
            Width = 180,
            Height = 36,
            FontWeight = FontWeight.Bold,
            Background = Brush.Parse("#E37400"),
            Foreground = Brushes.White
        };
        ok.Click += (_, _) => Finish(true);

        var cancel = new Button
        {
            Name = "fullTrustConfirmCancel",
            Content = TrustLevelCopy.FullTrustConfirmCancel,
            Width = 96,
            Height = 36
        };
        cancel.Click += (_, _) => Finish(false);

        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        root.Children.Add(buttons);

        Content = root;
        Closing += (_, e) =>
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