using Microsoft.Diagnostics.Symbols;
using System;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace PerfView.Dialogs
{
    /// <summary>
    /// Interaction logic for SymbolPathDialog.axaml
    /// </summary>
    public partial class SymbolPathDialog : WindowBase
    {
        /// <summary>
        /// Kind should be either 'symbol' or 'source' depending on which path variable to set.  
        /// </summary>
        public SymbolPathDialog(Window parentWindow, string defaultValue, string kind, Action<string> action) : base(parentWindow)
        {
            InitializeComponent();
            m_kind = kind;
            if (kind != "Symbol")
            {
                // TODO_AVALONIA: Visibility not available. Using IsVisible instead.
                AddMSSymbols.IsVisible = false;
            }

            m_action = action;
            Title = "Setting " + kind + " Path";
            // TODO_AVALONIA: TitleHyperLink was a Hyperlink element. CommandParameter not applicable on TextBlock.
            TitleHyperLinkText.Text = kind + " Path";
            SymbolPathTextBox.Text = defaultValue.Replace(";", ";\r\n") + "\r\n";
            GetValue();
            SymbolPathTextBox.SelectionStart = 0;
            SymbolPathTextBox.SelectionEnd = 0;
            SymbolPathTextBox.Focus();
        }

        // TODO_AVALONIA: CommandBinding/ExecutedRoutedEventArgs not available in Avalonia.
        private void DoHyperlinkHelp(object sender, object e)
        {
            // MainWindow.DisplayUsersGuide(e.Parameter as string);
        }

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            m_action(GetValue());
            Close();
        }

        private void AddMSSymbolsClicked(object sender, RoutedEventArgs e)
        {
            var symPath = new SymbolPath(GetValue());
            symPath.Add("SRV*https://msdl.microsoft.com/download/symbols");
            SymbolPathTextBox.Text = symPath.InsureHasCache(symPath.DefaultSymbolCache()).CacheFirst().ToString();
            GetValue();
        }

        private string GetValue()
        {
            var ret = Regex.Replace(SymbolPathTextBox.Text.Trim(), @"(\s*(;|(\r\n))\s*)+", ";");
            EnvVarTextBox.Text = "set _NT_" + m_kind.ToUpper() + "_PATH=" + ret;
            return ret;
        }

        // TODO_AVALONIA: TextChangedEventArgs is in Avalonia.Controls namespace
        private void DoTextChanged(object sender, TextChangedEventArgs e)
        {
            GetValue();
        }

        private void DoKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private Action<string> m_action;
        private string m_kind;
    }
}
