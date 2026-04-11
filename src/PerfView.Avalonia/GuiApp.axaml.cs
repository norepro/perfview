using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PerfView.Avalonia.ViewModels;
using PerfView.Avalonia.Views;

namespace PerfView;

public partial class GuiApp : Application
{
    public static MainWindowAdapter MainWindow { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // 1. Create and show the splash screen
            var splash = new SplashScreen();
            desktop.MainWindow = splash;
            splash.Show();

            // 2. Start a background task to "load" your app
            Task.Run(async () =>
            {
                // Simulate work (e.g., loading database, configs, etc.)
                await Task.Delay(3000);

                // 3. Switch to the real MainWindow on the UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    var mainWin = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(),
                    };

                    desktop.MainWindow = mainWin;
                    GuiApp.MainWindow = new MainWindowAdapter(mainWin);
                    mainWin.Show();
                    splash.Close();
                });
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}