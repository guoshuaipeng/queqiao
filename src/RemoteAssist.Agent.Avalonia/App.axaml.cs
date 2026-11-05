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
            var main = new MainWindow(Program.SharedHost!, Program.ShotMode);
            desktop.MainWindow = main;
            if (Program.ShotMode is { } shot)
            {
                main.Opened += async (_, _) =>
                {
                    try
                    {
                        await Task.Delay(200).ConfigureAwait(true);
                        await UiShotRunner.RunAsync(main, shot).ConfigureAwait(true);
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
