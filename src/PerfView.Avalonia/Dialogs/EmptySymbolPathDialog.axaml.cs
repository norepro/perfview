using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PerfView.Dialogs
{
    /// <summary>
    /// Dialog poped when the symbol path is empty.  This gives the user control over whether PerfView will perform network operations
    ///
    /// It is intended that this be a modal dialog (ShowDialog)
    /// </summary>
    public partial class EmptySymbolPathDialog : Window
    {
        public EmptySymbolPathDialog()
        {
            InitializeComponent();
        }

        public bool UseMSSymbols;

        #region private
        private void UseEmptyPathClicked(object sender, RoutedEventArgs e)
        {
            UseMSSymbols = false;
            Close();
        }

        private void UseMSSymbolsClicked(object sender, RoutedEventArgs e)
        {
            UseMSSymbols = true;
            Close();
        }
        #endregion
    }
}
