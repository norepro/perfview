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
#if AVALONIA
            // The EULA body paragraphs (title and closing bold line are in AXAML)
            BodyText.Text =
                "Microsoft Corporation (or based on where you live, one of its affiliates) licenses this supplement to you. " +
                "If you are licensed to use or distribute Microsoft .Net Framework 2.0 and/or a successor version software " +
                "(the \u201csoftware\u201d), you may use or distribute, respectively, this supplement. You may not use or distribute " +
                "it if you do not have an applicable license for the software. You may use a copy of this supplement with " +
                "each validly licensed copy of the software.\n\n" +
                "The following license terms describe additional use terms for this supplement. These terms and the license " +
                "terms for the software apply to your use of the supplement. If there is a conflict, these supplemental " +
                "license terms apply.";
#else
            var eulaFile = System.IO.Path.Combine(SupportFiles.SupportFileDir, "EULA.rtf");
            ReadFromFile(eulaFile);
#endif
        }

        private void ReadFromFile(string eulaFile)
        {
#if !AVALONIA
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

        private void DeclineClick(object sender, RoutedEventArgs e)
        {
#if AVALONIA
            Close(false);
#else
            DialogResult = false;
            Close();
#endif
        }
    }
}
