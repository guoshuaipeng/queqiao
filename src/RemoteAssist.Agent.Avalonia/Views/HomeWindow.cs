using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace RemoteAssist.Agent;

/// <summary>启动首页：接受协助（蓝）/ 去协助别人（绿）；整卡可点（R011 / quality.md §4）。</summary>
internal sealed class HomeWindow : Window
{
    public HomeWindow()
    {
        Title = "鹊桥";
        Width = 560;
        Height = 460;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brush.Parse("#F7F8FA");

        var root = new DockPanel();

        var header = new Border
        {
            Background = Brush.Parse("#1A73E8"),
            Height = 88,
            Padding = new Thickness(24, 18, 24, 12),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = "鹊桥远程协助 · 智能体",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.White
                    },
                    new TextBlock
                    {
                        Text = "一个应用，两种角色 — 先选你要做什么",
                        FontSize = 13,
                        Foreground = Brush.Parse("#E3F0FF")
                    }
                }
            }
        };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var footer = new DockPanel { Margin = new Thickness(24, 0, 24, 16), Height = 28 };
        DockPanel.SetDock(footer, Dock.Bottom);
        footer.Children.Add(new TextBlock
        {
            Text = "选上面一项即可。",
            Foreground = Brush.Parse("#5F6368"),
            VerticalAlignment = VerticalAlignment.Center
        });
        root.Children.Add(footer);

        var body = new StackPanel
        {
            Margin = new Thickness(24, 20, 24, 12),
            Spacing = 18
        };
        body.Children.Add(MakeModeCard(
            title: "接受协助",
            subtitle: "生成本机会话码和验证密码，让亲友或同事连过来帮你排查。危险操作需你本机确认。",
            accent: "#1A73E8",
            hover: "#E8F0FE",
            onClick: () => AppNavigator.EnterAccept()));
        body.Children.Add(MakeModeCard(
            title: "去协助别人",
            subtitle: "粘贴对方发来的会话码和验证密码，连接后查看进程 / 日志 / 文件，或提议执行命令。",
            accent: "#0F9D58",
            hover: "#E6F4EA",
            onClick: () => AppNavigator.EnterHelper()));
        root.Children.Add(body);

        Content = root;
    }

    private static Border MakeModeCard(string title, string subtitle, string accent, string hover, Action onClick)
    {
        var accentBrush = Brush.Parse(accent);
        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#DADEE3"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Height = 124,
            Cursor = new Cursor(StandardCursorType.Hand),
            ClipToBounds = true
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(8)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(80)));

        var bar = new Border { Background = accentBrush };
        Grid.SetColumn(bar, 0);
        grid.Children.Add(bar);

        var textCol = new StackPanel
        {
            Margin = new Thickness(22, 20, 8, 16),
            Spacing = 8
        };
        textCol.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeight.Bold,
            Foreground = accentBrush
        });
        textCol.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 12,
            Foreground = Brush.Parse("#5F6368"),
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetColumn(textCol, 1);
        grid.Children.Add(textCol);

        var enter = new TextBlock
        {
            Text = "进入 ›",
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = accentBrush,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 18, 0)
        };
        Grid.SetColumn(enter, 2);
        grid.Children.Add(enter);

        card.Child = grid;

        card.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(card).Properties.IsLeftButtonPressed)
                onClick();
        };
        // 整卡可点：子控件不拦截
        foreach (var child in new Control[] { textCol, enter, bar })
        {
            child.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(card).Properties.IsLeftButtonPressed)
                    onClick();
            };
        }

        card.PointerEntered += (_, _) => card.Background = Brush.Parse(hover);
        card.PointerExited += (_, _) => card.Background = Brushes.White;
        return card;
    }
}

