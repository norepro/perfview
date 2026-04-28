using System;
using System.Collections;
using System.Collections.ObjectModel;

#if !AVALONIA
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
#else
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using RoutedEventHandler = System.EventHandler<Avalonia.Interactivity.RoutedEventArgs>;
#endif

namespace Controls
{
    /// <summary>
    /// A trivial variation on a ComboBox that always remembers the last several entries entered into it.
    /// It is intended to be a drop-in replacement for TextBox.  
    /// </summary>
    public partial class HistoryComboBox : ComboBox
    {
        static HistoryComboBox()
        {
#if AVALONIA
            // OverrideDefaultValue<TOwner> is the correct Avalonia idiom; the WPF-style
            // OverrideMetadata(Type, ...) overload does not update the effective default value
            // in Avalonia, which would leave IsEditable=false at template-application time.
            IsEditableProperty.OverrideDefaultValue<HistoryComboBox>(true);
#else
            IsEditableProperty.OverrideMetadata(
                typeof(HistoryComboBox),
                new FrameworkPropertyMetadata(true));
#endif
        }

#if AVALONIA
        // Use the ComboBox ControlTheme so the Fluent template (including PART_EditableTextBox)
        // is applied to this subclass. Without this, Avalonia 12 fails to resolve a theme
        // for HistoryComboBox and no template is ever applied.
        protected override Type StyleKeyOverride => typeof(ComboBox);
#endif

#if AVALONIA
        private readonly ObservableCollection<string> m_items = new();
        /// <summary>
        /// Shadows the base ComboBox.Items to provide a mutable collection.
        /// Avalonia 12 removed the mutable Items property; this gives callers
        /// a compatible API without needing #if AVALONIA everywhere.
        /// </summary>
        public new ObservableCollection<string> Items => m_items;
#endif

        public HistoryComboBox()
        {
            HistoryLength = 10;
#if AVALONIA
            IsEditable = true;
            // The Fluent ComboBox theme defaults HorizontalAlignment to Left.
            // HistoryComboBox controls should stretch to fill their layout cell.
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
#endif
            KeyDown += DoKeyDown;
            GotFocus += DoGotFocus;
            LostFocus += DoLostFocus;
            SelectionChanged += DoComboSelectionChanged;
            DropDownClosed += DoDropDownClosed;
#if AVALONIA
            ItemsSource = m_items;
            Items.Add("");
#else
            Items.Add("");
#endif
        }
        public int HistoryLength { get; set; }

        public void SetHistory(IEnumerable values)
        {
#if AVALONIA
            Items.Clear();
#else
            Items.Clear();
#endif
            int count = 0;
            foreach (var value in values)
            {
                count++;
                if (count >= HistoryLength)
                {
                    break;
                }

#if AVALONIA
                Items.Add(value?.ToString() ?? "");
#else
                Items.Add(value);
#endif
            }
        }
        public void RemoveFromHistory(string value)
        {
            var text = Text;
#if AVALONIA
            for (int i = 0; i < Items.Count; i++)
            {
                if (m_items[i] == value)
                {
                    Items.RemoveAt(i);
                    Text = text;
                    break;
                }
            }
#else
            for (int i = 0; i < Items.Count; i++)
            {
                if ((string)Items[i] == value)
                {
                    Items.RemoveAt(i);
                    Text = text;
                    break;
                }
            }
#endif
        }
        public bool AddToHistory(string value)
        {
#if AVALONIA
            if (Items.Count > 0 && m_items[0] == value)
            {
                return false;
            }

            RemoveFromHistory(value);
            Items.Insert(0, value);
            Text = value;

            while (Items.Count > HistoryLength)
            {
                Items.RemoveAt(HistoryLength);
            }
#else
            if (Items.Count > 0 && ((string)Items[0]) == value)
            {
                return false;
            }

            RemoveFromHistory(value);
            Items.Insert(0, value);
            Text = value;

            while (Items.Count > HistoryLength)
            {
                Items.RemoveAt(HistoryLength);
            }
#endif

            return true;
        }

        /// <summary>
        /// This event fires when focus is lost or and Enter is typed in the box.  
        /// </summary>
        public event RoutedEventHandler TextEntered;
        /// <summary>
        /// This fires only when the enter character is typed or a combo box item is selected.  
        /// </summary>
        public event RoutedEventHandler Enter;
        public void CopyFrom(HistoryComboBox other)
        {
#if AVALONIA
            foreach (var item in other.Items)
            {
                Items.Add(item);
            }
#else
            foreach (var item in other.Items)
            {
                Items.Add(item);
            }
#endif
        }

        #region private

        private void DoComboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_selectedItem = null;
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                m_selectedItem = e.AddedItems[0].ToString();
            }
        }

        private void DoGotFocus(object sender, RoutedEventArgs e)
        {
            m_hasFocus = true;
#if !AVALONIA
            m_origBackground = Background;
            Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE5, 0xEB));
#endif
        }
        private void DoLostFocus(object sender, RoutedEventArgs e)
        {
            bool prevFocus = m_hasFocus;
            m_hasFocus = false;
#if !AVALONIA
            if (m_origBackground != null)
            {
                Background = m_origBackground;
            }
#endif

            if (prevFocus)
            {
                TextEntered?.Invoke(sender, e);
                ValueUpdate();
            }
        }
        private void DoKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                FireEnter(sender, e);
            }
        }
        private void DoDropDownClosed(object sender, EventArgs e)
        {
            if (m_selectedItem != null)
            {
                Text = m_selectedItem;
            }

            FireEnter(sender, e as RoutedEventArgs);
        }

        /// <summary>
        /// Force an callback as if you hit the Enter Key.  
        /// </summary>
        private void FireEnter(object sender, RoutedEventArgs e)
        {
            var text = GetTextBox().Text;

            if (text.Length > 0)
            {
                AddToHistory(Text);
            }

            // Logically we have lost focus.  (Any way of giving it up for real?)
            m_hasFocus = false;

            var enter = Enter;
            if (enter != null)
            {
                Enter(sender, e);
            }

            TextEntered?.Invoke(sender, e);

            ValueUpdate();
        }
        /// <summary>
        /// If someone is data-bound to me, update them.
        /// </summary>
        private void ValueUpdate()
        {
#if AVALONIA
            var binding = BindingOperations.GetBindingExpressionBase(this, ComboBox.TextProperty);
#else
            var binding = GetBindingExpression(ComboBox.TextProperty);
#endif
            if (binding != null)
            {
                binding.UpdateSource();
            }
        }

#if AVALONIA
        internal TextBox GetTextBox() => m_textBox;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            m_textBox = e.NameScope.Find<TextBox>("PART_EditableTextBox");
            // Sync any Text value that was set before the template was applied.
            if (m_textBox != null && Text != null && m_textBox.Text != Text)
                m_textBox.Text = Text;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == SelectedItemProperty)
            {
                // When items are cleared/modified, the base ComboBox resets Text via
                // UpdateInputTextFromSelection(null). Preserve Text because in an
                // editable combo box, Text is the authoritative value.
                // DoDropDownClosed explicitly sets Text when the user picks from the dropdown.
                string savedText = Text;
                base.OnPropertyChanged(change);
                if (Text != savedText)
                    SetCurrentValue(TextProperty, savedText);
                return;
            }

            base.OnPropertyChanged(change);

            // Avalonia 12 doesn't reliably propagate ComboBox.Text to PART_EditableTextBox
            // when set programmatically. Force-update the inner TextBox directly.
            if (change.Property == ComboBox.TextProperty && m_textBox != null)
            {
                var newText = change.GetNewValue<string>() ?? "";
                if (m_textBox.Text != newText)
                    m_textBox.Text = newText;
            }
        }
#else
        internal TextBox GetTextBox()
        {
            if (m_textBox == null)
            {
                m_textBox = (TextBox)GetTemplateChild("PART_EditableTextBox");
            }
            return m_textBox;
        }
#endif

#if AVALONIA
        private IBrush m_origBackground;
#else
        private Brush m_origBackground;
#endif
        private bool m_hasFocus;
        private string m_selectedItem;
        private TextBox m_textBox;
        #endregion
    }
}
