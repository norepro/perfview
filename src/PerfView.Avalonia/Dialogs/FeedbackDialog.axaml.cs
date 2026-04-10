using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PerfView.Dialogs
{
    /// <summary>
    /// Interaction logic for FeedbackDialog.axaml
    /// </summary>
    public partial class FeedbackDialog : WindowBase
    {
        public FeedbackDialog(Window parentWindow, Action<string> action) : base(parentWindow)
        {
            m_action = action;
            InitializeComponent();
            TextBox.Focus();
        }

        #region private
        private void SubmitClicked(object sender, RoutedEventArgs e)
        {
            m_action(TextBox.Text);
            Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private Action<string> m_action;
        #endregion
    }
}
