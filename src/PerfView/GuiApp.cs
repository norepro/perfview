using System;
using System.Globalization;
using System.IO;
using System.Windows;
#if !AVALONIA
using System.Windows.Markup;
#endif
using Utilities;

#if AVALONIA
using Application = Avalonia.Application;
#endif

namespace PerfView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class GuiApp : Application
    {
        /// <summary>
        /// The one and only main GUI window of the application.
        /// </summary>
        public static new MainWindow MainWindow;

        /// <summary>Design-time constructor required by Avalonia AXAML compiler.</summary>
        public GuiApp() : this(true) { }

        public GuiApp(bool installUnhandledExceptionHandlers = true)
        {
#if !AVALONIA
            Startup += delegate (object sender, StartupEventArgs e) { ApplicationStarted(); };

            InitializeComponent();
#else
            global::Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
#endif

            if (installUnhandledExceptionHandlers)
            {
                // Setup unhanded exception handlers
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
#if AVALONIA
                // Avalonia's equivalent of WPF's DispatcherUnhandledException
                global::Avalonia.Threading.Dispatcher.UIThread.UnhandledException += OnAvaloniaUnhandledException;
#else
                DispatcherUnhandledException += OnGuiUnhandledException;
#endif
            }
        }

#if AVALONIA
        private static PerfView.Avalonia.SplashScreen s_avaloniaSpashScreen;

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            // Show splash screen immediately while the main window loads
            s_avaloniaSpashScreen = new PerfView.Avalonia.SplashScreen();
            s_avaloniaSpashScreen.Show();

            ApplicationStarted();

            // Tell the lifetime which window is the main window
            if (ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = MainWindow;
            }
        }

        internal static void CloseAvaloniaSpashScreen()
        {
            if (s_avaloniaSpashScreen != null)
            {
                s_avaloniaSpashScreen.Close();
                s_avaloniaSpashScreen = null;
            }
        }
#endif

        /// <summary>
        /// Called when the application is started.
        /// </summary>
        private void ApplicationStarted()
        {
            if (!Enum.TryParse(App.UserConfigData["Theme"], out Theme theme))
            {
                theme = Theme.Light;
            }

            // initialize theme before creating any window
            ThemeViewModel.InitTheme(theme);
            MainWindow = new MainWindow(false);
            MainWindow.ThemeViewModel.SetTheme(theme);

            var logFile = File.CreateText(App.LogFileName);
            StatusBar.AttachWriterToLogStream(logFile);
            App.CommandProcessor.LogFile = MainWindow.StatusBar.LogWriter;

#if !AVALONIA
            // Work around for Non-English/US locale (e.g. French) where among other things the decimal point is a comma.
            // WPF never uses the CurrentCulture when it formats numbers (it always uses US)
            // This sets the default to the current culture.
            // see http://serialseb.blogspot.com/2007/04/wpf-tips-1-have-all-your-dates-times.html
            FrameworkElement.LanguageProperty.OverrideMetadata(
              typeof(FrameworkElement),
              new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
#endif

            if (App.CommandLineArgs.HelpRequested)
            {
                MainWindow.Show();
                MainWindow.DoCommandLineHelp(null, null);
                return;
            }

            MainWindow.StatusBar.LogWriter.WriteLine("Started with command line: {0}", Environment.CommandLine);
            MainWindow.StatusBar.LogWriter.WriteLine("PerfView Version: {0}  BuildDate: {1}", AppInfo.VersionNumber, AppInfo.BuildDate);
            MainWindow.StatusBar.LogWriter.WriteLine("PerfView Start Time {0}", DateTime.Now);

            if (App.NeedsEulaConfirmation(App.CommandLineArgs))
            {
                var eula = new PerfView.Dialogs.EULADialog(MainWindow);
#if AVALONIA
                MainWindow.Show();
                // ShowDialog is async in Avalonia — use ContinueWith to avoid blocking the UI thread
                eula.ShowDialog<bool?>(MainWindow).ContinueWith(t =>
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (!(t.Result ?? false))
                        {
                            Environment.Exit(-10);
                        }
                        App.AcceptEula();
                    });
                });
            }
#else
                bool? accepted = eula.ShowDialog();
                if (!(accepted ?? false))
                {
                    Environment.Exit(-10);
                }

                App.AcceptEula();       // Remember that we have accepted the EULA for next time.
            }
#endif

#if !AVALONIA
            MainWindow.Loaded += delegate (object sender, RoutedEventArgs ev)
#else
            MainWindow.Loaded += delegate (object sender, global::Avalonia.Interactivity.RoutedEventArgs ev)
#endif
            {
#if AVALONIA
                CloseAvaloniaSpashScreen();
#endif
                string[] providers = App.CommandLineArgs.Providers;

                if (App.CommandLineArgs.CommandLineFailure != null)
                {
                    var message = App.CommandLineArgs.CommandLineFailure.Message;
                    if (message.Contains("\n"))
                    {
                        MainWindow.StatusBar.LogError("Command Line Error, see log file for details.");
                        MainWindow.StatusBar.Log(message);
                    }
                    else
                    {
                        MainWindow.StatusBar.LogError("Command Line Error: " + message);
                    }

                    return;
                }

                if (App.CommandLineArgs.DoCommand == null)
                {
                    App.CommandLineArgs.DoCommand = App.CommandProcessor.View;
                }

                string commandName = "View";
                Action continuation = delegate
                {
                    if (App.CommandLineArgs.DataFile != null)
                    {
                        MainWindow.OpenPath(App.CommandLineArgs.DataFile);
                    }
                };
                if (App.CommandLineArgs.DoCommand != App.CommandProcessor.View)
                {
                    commandName = App.CommandLineArgs.DoCommand.Method.Name;
                    continuation = null;
                }

                // Run commands in the PerfViewExtensions\PerfViewStartup file.
                PerfViewExtensibility.Extensions.RunUserStartupCommands(MainWindow.StatusBar);
                MainWindow.OpenPreviouslyOpened();
#if AVALONIA
                MainWindow.m_suppressDirectorySelectionChanged = false;
#endif
                MainWindow.ExecuteCommand(commandName, App.CommandLineArgs.DoCommand, null, continuation);
            };
            MainWindow.Show();
        }

#if !AVALONIA
        /// <summary>
        /// Called when exception happens in a GUI routine
        /// </summary>
        private void OnGuiUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            bool userLevel;
            string message = ExceptionMessage.GetUserMessage(e.Exception, out userLevel);
            if (userLevel)
            {
                // TODO FIX NOW would really like to find the window with focus, and not always use the main window...
                MainWindow.Focus();
                MainWindow.StatusBar.LogError(message);
                e.Handled = true;
            }
            else
            {
                var dialog = new PerfView.Dialogs.UnhandledExceptionDialog(MainWindow, e.Exception);
                var ret = dialog.ShowDialog();
                // If it returns, it means that the user has opted to continue.
                e.Handled = true;
            }
        }
#endif

#if AVALONIA
        /// <summary>
        /// Called when an unhandled exception occurs on the Avalonia UI thread.
        /// </summary>
        private void OnAvaloniaUnhandledException(object sender, global::Avalonia.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var message = e.Exception?.ToString() ?? "Unknown error";
            App.AvaloniaLog($"Unhandled UI exception: {message}");

            // Try to show in status bar if available
            try
            {
                if (MainWindow?.StatusBar != null)
                {
                    MainWindow.StatusBar.LogError("Error: " + e.Exception?.Message);
                    e.Handled = true;
                    return;
                }
            }
            catch { }

            e.Handled = true; // Prevent crash, but the error is logged
        }
#endif

        /// <summary>
        /// Fallback if we happen to take an exception in a non-gui routine (shouldn't happen!)
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO discriminate between the GUI and Non_GUI case.
            MainWindow.Dispatcher.BeginInvoke((Action)delegate ()
            {
                var dialog = new PerfView.Dialogs.UnhandledExceptionDialog(MainWindow, e.ExceptionObject);
#if !AVALONIA
                var ret = dialog.ShowDialog();
#else
                // Use Show() instead of ShowDialog to avoid deadlocking the UI thread
                dialog.Show(MainWindow);
#endif
            });
        }
    }
}

