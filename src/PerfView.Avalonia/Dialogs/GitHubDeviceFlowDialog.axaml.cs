using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace PerfView.Dialogs
{
    /// <summary>
    /// Interaction logic for GitHubDeviceFlowDialog.axaml
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
            cancellationToken.Register(() => Dispatcher.UIThread.InvokeAsync(Close));
        }

        // TODO_AVALONIA: CommandBinding/ExecutedRoutedEventArgs replaced with direct Click handlers.
        private void NavigateTo_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is { } vm)
            {
                var uri = (Uri)vm.GetType().GetProperty("VerificationUri").GetValue(vm);
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is { } vm)
            {
                string userCode = vm.GetType().GetProperty("UserCode").GetValue(vm)?.ToString();
                // TODO_AVALONIA: Use TopLevel.Clipboard.SetTextAsync() instead
                // Clipboard.SetText(userCode);
                if (sender is Button button)
                {
                    button.Content = "Copied";
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
