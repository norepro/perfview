using System.IO;
using Utilities;

#if !AVALONIA
using System.Windows;
using System.Windows.Documents;
#else
using Avalonia.Controls;
using Avalonia.Interactivity;
#endif

namespace PerfView.Dialogs
{
    /// <summary>
    /// Interaction logic for EULADialog.xaml
    /// </summary>
    public partial class EULADialog : WindowBase
    {
        /// <summary>Design-time constructor required by Avalonia AXAML compiler.</summary>
        public EULADialog() { InitializeComponent(); }

        public EULADialog(Window parentWindow) : base(parentWindow)
        {
            InitializeComponent();
            var eulaFile = System.IO.Path.Combine(SupportFiles.SupportFileDir, "EULA.rtf");
            ReadFromFile(eulaFile);
        }

        private void ReadFromFile(string eulaFile)
        {
#if AVALONIA
            // TODO_AVALONIA: Convert eulaFile to Markdown so we don't lose formatting?
            Body.Text = File.ReadAllText(eulaFile);
#else
            var bodyRange = new TextRange(Body.Document.ContentStart, Body.Document.ContentEnd);
            using (var stream = File.OpenRead(eulaFile))
            {
                bodyRange.Load(stream, DataFormats.Rtf);
            }
#endif
        }

        private void AcceptClick(object sender, RoutedEventArgs e)
        {
#if AVALONIA
            Close(true);
#else
            DialogResult = true;
#endif
        }
    }
}
