using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace RemoteAssist.Agent;

/// <summary>Avalonia 双模式导航：首页 ↔ 被控 ↔ 协助端（R011）。</summary>
internal static class AppNavigator
{
    public static HomeWindow? Home { get; private set; }
    public static MainWindow? AcceptWindow { get; private set; }
    public static HelperWindow? HelperWindow { get; private set; }
    private static bool _navigating;

    public static void AttachHome(HomeWindow home) => Home = home;

    public static void ShowHome()
    {
        if (_navigating) return;
        _navigating = true;
        try
        {
            LeaveAccept(closeWindow: true);
            LeaveHelper(closeWindow: true);
            if (Home is null) return;
            Home.Show();
            Home.Activate();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = Home;
        }
        finally
        {
            _navigating = false;
        }
    }

    public static void EnterAccept()
    {
        if (_navigating) return;
        _navigating = true;
        try
        {
            LeaveHelper(closeWindow: true);
            Home?.Hide();

            Program.EnsureHostStarted();
            AcceptWindow = new MainWindow(Program.SharedHost!, Program.ShotMode, enableReturnHome: true);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = AcceptWindow;
            AcceptWindow.Show();
            AcceptWindow.Activate();
        }
        finally
        {
            _navigating = false;
        }
    }

    public static void EnterHelper()
    {
        if (_navigating) return;
        _navigating = true;
        try
        {
            LeaveAccept(closeWindow: true);
            Home?.Hide();

            HelperWindow = new HelperWindow(Program.ShotMode, enableReturnHome: true);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = HelperWindow;
            HelperWindow.Show();
            HelperWindow.Activate();
        }
        finally
        {
            _navigating = false;
        }
    }

    public static void LeaveAccept(bool closeWindow)
    {
        var w = AcceptWindow;
        AcceptWindow = null;
        if (closeWindow && w is not null)
        {
            try { w.AllowCloseWithoutPrompt(); } catch { /* ignore */ }
            try { w.Close(); } catch { /* ignore */ }
        }
        Program.StopHost();
    }

    public static void LeaveHelper(bool closeWindow)
    {
        var w = HelperWindow;
        HelperWindow = null;
        if (closeWindow && w is not null)
        {
            try { w.PrepareClose(); } catch { /* ignore */ }
            try { w.Close(); } catch { /* ignore */ }
        }
    }
}
