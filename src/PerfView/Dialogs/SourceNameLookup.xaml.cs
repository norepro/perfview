using System.Windows.Input;

#if AVALONIA
using Avalonia.Controls;
using Avalonia.Interactivity;
#endif

namespace PerfView.Dialogs
{
    /// <summary>
    /// TODO FIX NOW use or remove
    /// </summary>
    public partial class SourceNameLookup : WindowBase
    {
        /// <summary>Design-time constructor required by Avalonia AXAML compiler.</summary>
        public SourceNameLookup() { InitializeComponent(); }

        public SourceNameLookup(Window parentWindow) : base(parentWindow)
        {
            InitializeComponent();
        }

        private void DoHyperlinkHelp(object sender, ExecutedRoutedEventArgs e)
        {
            MainWindow.DisplayUsersGuide(e.Parameter as string);
        }
    }
}
