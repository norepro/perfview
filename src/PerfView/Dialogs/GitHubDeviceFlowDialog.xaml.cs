using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;

#if !AVALONIA
using System.Windows;
using System.Windows.Controls;
#else
using Avalonia.Controls;
using Avalonia.Input.Platform;
#endif

namespace PerfView.Dialogs
{
    /// <summary>
    /// Interaction logic for GitHubDeviceFlowDialog.xaml
    /// </summary>
    public partial class GitHubDeviceFlowDialog : WindowBase
    {
        /// <summary>
        /// Construct a new instance.
        /// </summary>
        public GitHubDeviceFlowDialog(Window parentWindow, Uri verificationUri, string userCode, CancellationToken cancellationToken) : base(parentWindow)
        {
            var viewModel = new
            {
                VerificationUri = verificationUri,
                UserCode = userCode,
            };

            DataContext = viewModel;
            InitializeComponent();

            // Automatically Close the dialog when the cancellation token is canceled.
            cancellationToken.Register(() => Dispatcher.InvokeAsync(Close));
        }

        private void NavigateTo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Uri uri = (Uri)e.Parameter;
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string userCode = e.Parameter.ToString();
#if AVALONIA
            Clipboard.SetTextAsync(userCode).Wait();
#else
            Clipboard.SetText(userCode);
#endif
#if !AVALONIA
            ((Button)e.Source).Content = "Copied";
#else
            ((Button)sender).Content = "Copied";
#endif
            e.Handled = true;
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }
    }
}
