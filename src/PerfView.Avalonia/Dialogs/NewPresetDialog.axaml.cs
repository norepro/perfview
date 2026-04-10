using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace PerfView.Dialogs
{
    /// <summary>
    /// Interaction logic for NewPresetDialog.axaml
    /// </summary>
    public partial class NewPresetDialog : WindowBase
    {
        public string PresetName { get; private set; }

        public NewPresetDialog(Window parentWindow, string defaultValue, List<string> existingPresets) : base(parentWindow)
        {
            m_existingPresets = existingPresets;
            InitializeComponent();
            Title = "New Preset";
            PresetNameTextBox.Text = defaultValue;
            PresetNameTextBox.CaretIndex = defaultValue.Length;
            PresetName = defaultValue;
            PresetNameTextBox.Focus();
        }

        // TODO_AVALONIA: CommandBinding/ExecutedRoutedEventArgs not available in Avalonia.
        private void DoHyperlinkHelp(object sender, object e)
        {
            MainWindow.DisplayUsersGuide("Preset");
        }

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            // Check uniqueness of the name and ask if user wants to continue
            if (m_existingPresets.Exists(x => x == PresetNameTextBox.Text))
            {
                // TODO_AVALONIA: XamlMessageBox / MessageBoxButton / MessageBoxImage / MessageBoxResult
                // not available in Avalonia. Need to implement async dialog or use a different approach.
                // Original code:
                // if (XamlMessageBox.Show(
                //     $"Preset {PresetNameTextBox.Text} already exists. Overwrite?",
                //     "Preset Name",
                //     MessageBoxButton.OKCancel,
                //     MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                // {
                //     return;
                // }
            }
            PresetName = PresetNameTextBox.Text;
            // TODO_AVALONIA: DialogResult not available. Using Close(true) as workaround.
            Close(true);
        }

        private void DoKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }

            if (e.Key == Key.Enter)
            {
                OKClicked(null, null);
            }
        }

        private List<string> m_existingPresets;
    }
}
