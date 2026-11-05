using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace RemoteAssist.Agent;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ConfirmPrompt.UiDispatcher = Dispatcher.UIThread;
            var home = new HomeWindow();
            AppNavigator.AttachHome(home);
            desktop.MainWindow = home;

            // 直达接受协助（联调）
            var args = Environment.GetCommandLineArgs();
            if (Program.ShotMode is null && args.Any(a => a is "--accept"))
            {
                AppNavigator.EnterAccept();
            }
            else if (Program.ShotMode is null && args.Any(a => a is "--control" or "--helper"))
            {
                AppNavigator.EnterHelper();
            }

            if (Program.ShotMode is { } shot)
            {
                home.Opened += async (_, _) =>
                {
                    try
                    {
                        await Task.Delay(200).ConfigureAwait(true);
                        await UiShotRunner.RunAsync(home, shot).ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("ui-shot failed: " + ex);
                        Environment.ExitCode = 2;
                    }
                    finally
                    {
                        desktop.Shutdown();
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
