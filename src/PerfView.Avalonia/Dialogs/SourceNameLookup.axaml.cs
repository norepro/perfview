using Avalonia.Controls;

namespace PerfView.Dialogs
{
    /// <summary>
    /// TODO FIX NOW use or remove
    /// </summary>
    public partial class SourceNameLookup : WindowBase
    {
        public SourceNameLookup(Window parentWindow) : base(parentWindow)
        {
            InitializeComponent();
        }

        // TODO_AVALONIA: CommandBinding/ExecutedRoutedEventArgs not available in Avalonia.
        // Rework Help command routing for Avalonia.
        private void DoHyperlinkHelp(object sender, object e)
        {
            // MainWindow.DisplayUsersGuide(e.Parameter as string);
        }
    }
}
