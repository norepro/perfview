using System;
using System.ComponentModel;
using System.IO;
using Utilities;

#if AVALONIA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DependencyObject = Avalonia.AvaloniaObject;
#else
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
#endif


namespace PerfView.GuiUtilities
{
    /// <summary>
    /// Interaction logic for WebBrowserWindow.xaml
    /// </summary>
    public partial class WebBrowserWindow : WindowBase
    {
        /// <summary>Design-time constructor required by Avalonia AXAML compiler.</summary>
        public WebBrowserWindow() { InitializeComponent(); }

        public WebBrowserWindow(Window parentWindow) : base(parentWindow)
        {
            InitializeComponent();
        }

#if AVALONIA
        static WebBrowserWindow()
        {
            SourceProperty.Changed.AddClassHandler<WebBrowserWindow>((x, _) => x.Navigate());
        }
#endif

        /// <summary>
        /// If set simply hide the window rather than closing it when the user requests closing. 
        /// </summary>
        public bool HideOnClose;

#if AVALONIA
        // TODO_AVALONIA: NativeWebView requires a commercial Avalonia license.
        // Stubbed out until licensing is resolved.
        public bool CanGoForward { get { return false; } }
        public bool CanGoBack { get { return false; } }
#else
        public bool CanGoForward { get { return _disposed ? false : Browser.CanGoForward; } }
        public bool CanGoBack { get { return _disposed ? false : Browser.CanGoBack; } }
        public WebView2 Browser { get { return _Browser; } }
#endif

#if AVALONIA
        public static readonly StyledProperty<Uri> SourceProperty =
            AvaloniaProperty.Register<WebBrowserWindow, Uri>(nameof(Source));
#else
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(Uri),
            typeof(WebBrowser),
            new PropertyMetadata(OnSourceChanged));
#endif

        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

#if !AVALONIA
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as WebBrowserWindow)?.Navigate();
        }
#endif

        /// <summary>
        /// If WebView2 has been initialized, navigate to current source. If WebView2 is not initialized yet, it will 
        /// be navigated to once initialization has completed.
        /// </summary>
        private void Navigate()
        {
#if !AVALONIA
            if (!_disposed && Source is { } uri)
            {
                Browser?.CoreWebView2.Navigate(uri.ToString());
            }
#endif
        }

        #region private
        private bool _disposed = false;
        private void BackClick(object sender, RoutedEventArgs e)
        {
#if !AVALONIA
            if (CanGoBack)
            {
                Browser.GoBack();
            }
#endif
        }

        private void ForwardClick(object sender, RoutedEventArgs e)
        {
#if !AVALONIA
            if (CanGoForward)
            {
                Browser.GoForward();
            }
#endif
        }

        /// <summary>
        /// We hide rather than close the editor.  
        /// </summary>
#if AVALONIA
        private void Window_Closing(object sender, WindowClosingEventArgs e)
#else
        private void Window_Closing(object sender, CancelEventArgs e)
#endif
        {
            if (HideOnClose)
            {
                Hide();
                e.Cancel = true;
            }
            else
            {
                // Dispose the browser control to prevent resource leaks
                if (!_disposed)
                {
#if !AVALONIA
                    Browser?.Dispose();
#endif
                    _disposed = true;
                }
            }
        }

#if AVALONIA
        /// <summary>
        /// Navigate to the current source once the browser control is loaded.
        /// </summary>
        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            Navigate();
        }
#else
        /// <summary>
        /// Ensure that we configure the WebView2 environment to specify where the user data is stored.
        /// </summary>
        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            var userDataFolder = Path.Combine(SupportFiles.SupportFileDir, "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var environmentAwaiter = CoreWebView2Environment
                .CreateAsync(userDataFolder: userDataFolder)
                .ConfigureAwait(true)
                .GetAwaiter();

            environmentAwaiter.OnCompleted(async () =>
            {
                if (_disposed)
                {
                    return;
                }

                var environment = environmentAwaiter.GetResult();
                await Browser.EnsureCoreWebView2Async(environment).ConfigureAwait(true);

                // Set the preferred color scheme directly on the profile
                Browser.CoreWebView2.Profile.PreferredColorScheme = GuiApp.MainWindow.ThemeViewModel.IsLightTheme
                    ? CoreWebView2PreferredColorScheme.Light
                    : CoreWebView2PreferredColorScheme.Dark;

                // Navigate to the current specified source
                Navigate();
            });
        }
#endif

        #endregion
    }
}
