using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
// TODO_AVALONIA: System.Windows.Documents not available in Avalonia
using Utilities;

namespace PerfView.Dialogs
{
    /// <summary>
    /// Interaction logic for EULADialog.axaml
    /// </summary>
    public partial class EULADialog : WindowBase
    {
        public EULADialog(Window parentWindow) : base(parentWindow)
        {
            InitializeComponent();
            var eulaFile = System.IO.Path.Combine(SupportFiles.SupportFileDir, "EULA.rtf");
            ReadFromFile(eulaFile);
        }

        private void ReadFromFile(string eulaFile)
        {
            // TODO_AVALONIA: RichTextBox with RTF/FlowDocument not available in Avalonia.
            // TextRange, DataFormats.Rtf not supported. Loading as plain text as placeholder.
            Body.Text = File.ReadAllText(eulaFile);
        }

        private void AcceptClick(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: DialogResult not available. Using Close(true) as workaround.
            Close(true);
        }
    }
}
