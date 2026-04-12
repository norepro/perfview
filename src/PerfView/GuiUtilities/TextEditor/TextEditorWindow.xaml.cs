using PerfView;
using System.ComponentModel;
#if !AVALONIA
using System.Windows;
#else
using Avalonia.Controls;
using WindowBase = Avalonia.Controls.Window;
#endif

namespace Controls
{
    /// <summary>
    /// Interaction logic for TextEditorWindow.xaml
    /// </summary>
    public partial class TextEditorWindow : WindowBase
    {
#if AVALONIA
        public TextEditorWindow(string[] args = null)
#else
        public TextEditorWindow(string[] args = null) : this(null, args) { }

        public TextEditorWindow(Window parentWindow, string[] args = null) : base(parentWindow)
#endif
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
        private void Window_Closing(object sender, CancelEventArgs e)
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
