using System;
using System.ComponentModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
// TODO_AVALONIA: WebView2 (Microsoft.Web.WebView2.Core / Microsoft.Web.WebView2.Wpf) not available in Avalonia
// using Microsoft.Web.WebView2.Core;
// using Microsoft.Web.WebView2.Wpf;
using Utilities;


namespace PerfView.GuiUtilities
{
    /// <summary>
    /// Interaction logic for WebBrowserWindow.axaml
    /// </summary>
    // TODO_AVALONIA: WindowBase not yet ported to Avalonia; using Window as base class
    public partial class WebBrowserWindow : Window
    {
        public WebBrowserWindow(Window parentWindow)
        {
            InitializeComponent();
        }

        public WebBrowserWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// If set simply hide the window rather than closing it when the user requests closing. 
        /// </summary>
        public bool HideOnClose;

        // TODO_AVALONIA: WebView2 Browser property not available
        // public bool CanGoForward { get { return _disposed ? false : Browser.CanGoForward; } }
        // public bool CanGoBack { get { return _disposed ? false : Browser.CanGoBack; } }
        // public WebView2 Browser { get { return _Browser; } }
        public bool CanGoForward { get { return false; } }
        public bool CanGoBack { get { return false; } }

        // TODO_AVALONIA: DependencyProperty → StyledProperty
        // public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        //     nameof(Source),
        //     typeof(Uri),
        //     typeof(WebBrowser),
        //     new PropertyMetadata(OnSourceChanged));

        public static readonly StyledProperty<Uri> SourceProperty =
            AvaloniaProperty.Register<WebBrowserWindow, Uri>(nameof(Source));

        public Uri Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        static WebBrowserWindow()
        {
            SourceProperty.Changed.AddClassHandler<WebBrowserWindow>((x, e) => x.Navigate());
        }

        /// <summary>
        /// If WebView2 has been initialized, navigate to current source.
        /// </summary>
        private void Navigate()
        {
            // TODO_AVALONIA: WebBrowser navigation not available
            // if (!_disposed && Source?.ToString() is { } source)
            // {
            //     Browser?.CoreWebView2.Navigate(source);
            // }
        }

        #region private
        private bool _disposed = false;
        private void BackClick(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: WebBrowser GoBack not available
            // if (CanGoBack)
            // {
            //     Browser.GoBack();
            // }
        }

        private void ForwardClick(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: WebBrowser GoForward not available
            // if (CanGoForward)
            // {
            //     Browser.GoForward();
            // }
        }

        /// <summary>
        /// We hide rather than close the editor.  
        /// </summary>
        private void Window_Closing(object sender, WindowClosingEventArgs e)
        {
            if (HideOnClose)
            {
                Hide();
                e.Cancel = true;
            }
            else
            {
                // TODO_AVALONIA: WebView2 dispose not available
                // if (!_disposed)
                // {
                //     Browser?.Dispose();
                //     _disposed = true;
                // }
            }
        }

        // TODO_AVALONIA: Browser_Loaded with CoreWebView2Environment not applicable without WebView2
        // private void Browser_Loaded(object sender, RoutedEventArgs e) { ... }

        #endregion
    }
}
