using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace RemoteAssist.Agent;

/// <summary>关窗提示：first-run-mac-linux.md 第 2 节原文。</summary>
internal sealed class CloseWarnWindow : Window
{
    public CloseWarnWindow()
    {
        Title = "鹊桥远程协助";
        Width = 460;
        Height = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Background = Brushes.White;

        var root = new DockPanel { Margin = new Thickness(20) };

        var tip = new TextBlock
        {
            Text = "关闭窗口后，协助可能还在进行。要结束请点「结束这次协助」。",
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };
        DockPanel.SetDock(tip, Dock.Top);
        root.Children.Add(tip);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        var back = new Button { Content = "返回", Width = 96, Height = 36 };
        back.Click += (_, _) => { Close("back"); };

        var end = new Button
        {
            Content = "结束这次协助",
            Width = 140,
            Height = 36,
            FontWeight = FontWeight.Bold,
            Background = Brush.Parse("#1A73E8"),
            Foreground = Brushes.White
        };
        end.Click += (_, _) => { Close("end"); };

        buttons.Children.Add(back);
        buttons.Children.Add(end);
        root.Children.Add(buttons);

        Content = root;
    }
}
