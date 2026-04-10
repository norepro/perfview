using PerfView;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;

namespace Controls
{
    /// <summary>
    /// Interaction logic for TextEditorWindow.axaml
    /// </summary>
    // TODO_AVALONIA: WindowBase not yet ported to Avalonia; using Window as base class
    public partial class TextEditorWindow : Window
    {
        public TextEditorWindow(string[] args = null) : this(null, args) { }

        // TODO_AVALONIA: WindowBase constructor took parentWindow; using Window base
        public TextEditorWindow(Window parentWindow, string[] args = null)
        {
            InitializeComponent();

            if (args != null && args.Length == 1)
            {
                TextEditor.OpenText(args[0]);
            }

            TextEditor.Body.Focus();
        }
        public TextEditorControl TextEditor { get { return m_TextEditor; } }

        /// <summary>
        /// If set simply hide the window rather than closing it when the user requests closing. 
        /// </summary>
        public bool HideOnClose;
        #region private
        // We hide rather than close the editor.  
        private void Window_Closing(object sender, WindowClosingEventArgs e)
        {
            if (HideOnClose)
            {
                Hide();
                e.Cancel = true;
            }
        }
        #endregion
    }
}
