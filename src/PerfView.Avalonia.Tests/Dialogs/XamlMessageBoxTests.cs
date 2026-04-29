using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using PerfView.Dialogs;
using Xunit;

namespace PerfViewTests.Dialogs
{
    /// <summary>
    /// Avalonia equivalent of the WPF XamlMessageBoxTests.
    /// Uses Avalonia.Headless for headless UI testing.
    /// </summary>
    public class XamlMessageBoxTests
    {
        /// <summary>
        /// Verifies that Dispatcher.UIThread.CheckAccess returns false
        /// on a dedicated background thread.
        /// </summary>
        [AvaloniaFact]
        public void CheckAccess_ReturnsFalseOnBackgroundThread()
        {
            bool? result = null;
            var thread = new Thread(() =>
            {
                result = Dispatcher.UIThread.CheckAccess();
            });
            thread.IsBackground = true;
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(5));

            Assert.NotNull(result);
            Assert.False(result.Value, "CheckAccess should return false on a non-UI thread");
        }

        /// <summary>
        /// Verifies that CheckAccess returns true on the UI thread (headless).
        /// </summary>
        [AvaloniaFact]
        public void CheckAccess_ReturnsTrueOnUIThread()
        {
            Assert.True(Dispatcher.UIThread.CheckAccess());
        }

        /// <summary>
        /// Verifies that MessageBoxWindow can be constructed with various
        /// button/icon combinations without throwing.
        /// </summary>
        [AvaloniaTheory]
        [InlineData(MessageBoxButton.OK, MessageBoxImage.None)]
        [InlineData(MessageBoxButton.YesNo, MessageBoxImage.Question)]
        [InlineData(MessageBoxButton.OKCancel, MessageBoxImage.Warning)]
        [InlineData(MessageBoxButton.YesNoCancel, MessageBoxImage.Error)]
        public void MessageBoxWindow_CanBeConstructed(MessageBoxButton buttons, MessageBoxImage icon)
        {
            var window = new MessageBoxWindow("Test message", "Test Caption", buttons, icon, MessageBoxResult.OK);
            Assert.NotNull(window);
            Assert.Equal(MessageBoxResult.OK, window.Result);
        }
    }
}
