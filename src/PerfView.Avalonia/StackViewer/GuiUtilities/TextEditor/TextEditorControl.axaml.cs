using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
// TODO_AVALONIA: System.Windows.Documents (FlowDocument, TextPointer, TextRange, Block, Paragraph, etc.) not available in Avalonia
// using System.Windows.Documents;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Interactivity;

namespace Controls
{
    /// <summary>
    /// A TextEditorControl is a richTextBox with search, open, and save (basically notepad)
    /// TODO_AVALONIA: RichTextBox is not available in Avalonia. This is converted to use a plain TextBox.
    /// FlowDocument, TextPointer, TextRange, Block, Paragraph APIs are not available.
    /// For full rich text support, consider using AvaloniaEdit.
    /// </summary>
    public partial class TextEditorControl : UserControl
    {
        public TextEditorControl()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Is the user allowed to modify the text. 
        /// </summary>
        public bool IsReadOnly { get { return Body.IsReadOnly; } set { Body.IsReadOnly = value; } }

        // TODO_AVALONIA: FlowDocument not available in Avalonia; removed Document property
        // public FlowDocument Document { get { return Body.Document; } }

        /// <summary>
        /// The body of the editor as text.
        /// </summary>
        public string Text
        {
            get { return Body.Text ?? string.Empty; }
            set { Body.Text = value; }
        }
        /// <summary>
        /// Appends text to the editor's text buffer. 
        /// </summary>
        public void AppendText(string textData)
        {
            Body.Text = (Body.Text ?? string.Empty) + textData;
        }
        /// <summary>
        /// Opens a text file and loads its content into the editor.
        /// </summary>
        /// <param name="fileName"></param>
        public void OpenText(string fileName)
        {
            try
            {
                Body.Text = File.ReadAllText(fileName);
                m_fileName = fileName;
            }
            catch (Exception)
            {
                SystemSounds.Beep.Play();
            }
        }
        public void SaveText(string fileName)
        {
            try
            {
                File.WriteAllText(fileName, Body.Text ?? string.Empty);
                m_fileName = fileName;
            }
            catch (Exception)
            {
                SystemSounds.Beep.Play();
            }
        }

        /// <summary>
        /// Selects the line at 'lineNum' (the first line is 1).   Will select the last line
        /// if lineNum is greater than the number of lines.
        /// </summary>
        public void GotoLine(int lineNum)
        {
            // TODO_AVALONIA: Simplified GotoLine for plain TextBox (no FlowDocument/Block)
            var text = Body.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Line numbers start at 1
            if (lineNum > 0)
            {
                --lineNum;
            }

            var lines = text.Split('\n');
            int offset = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == lineNum || i == lines.Length - 1)
                {
                    Body.SelectionStart = offset;
                    Body.SelectionEnd = offset + lines[i].TrimEnd('\r').Length;
                    // TODO_AVALONIA: BringIntoView / ScrollToLine for selected text
                    Body.Focus();
                    return;
                }
                offset += lines[i].Length + 1; // +1 for the '\n'
            }
        }

        /// <summary>
        /// Finds 'pattern' in the text starting at the current selection point and wrapping around
        /// until all the text is searched.  Returns the match index or -1 if not found.
        /// </summary>
        /// <param name="pattern">The .NET regular expression to match.</param>
        /// <returns>Returns the index of the match or -1 if not matched.</returns>
        public int Find(string pattern)
        {
            // TODO_AVALONIA: Simplified Find for plain TextBox (no TextPointer/TextRange)
            try
            {
                var pat = new Regex(pattern, RegexOptions.IgnoreCase);
                var text = Body.Text ?? string.Empty;
                if (text.Length == 0)
                {
                    return -1;
                }

                var startPos = Body.SelectionEnd;
                if (startPos < 0)
                {
                    startPos = 0;
                }

                // Search from current position to end
                var match = pat.Match(text, startPos);
                if (!match.Success)
                {
                    // Wrap around to beginning
                    match = pat.Match(text, 0);
                }

                if (match.Success)
                {
                    Body.SelectionStart = match.Index;
                    Body.SelectionEnd = match.Index + match.Length;
                    Body.Focus();
                    return match.Index;
                }

                SystemSounds.Beep.Play();
                return -1;
            }
            catch (Exception)
            {
                SystemSounds.Beep.Play();
                return -1;
            }
        }

        /// <summary>
        /// Scrolls to the end of the text content.
        /// </summary>
        public void ScrollToEnd()
        {
            // TODO_AVALONIA: Proper scroll-to-end for TextBox
            var text = Body.Text;
            if (!string.IsNullOrEmpty(text))
            {
                Body.CaretIndex = text.Length;
            }
        }

        #region private
        // TODO_AVALONIA: WPF RoutedUICommand not available in Avalonia. Commands handled via Click events or KeyBindings.
        // public static RoutedUICommand FindNextCommand = ...
        // public static RoutedUICommand DeleteLineCommand = ...
        // public static RoutedUICommand ClearCommand = ...

        // Command Callbacks - converted from ExecutedRoutedEventArgs to RoutedEventArgs
        private void DoFindNext(object sender, RoutedEventArgs e)
        {
            Find(FindTextBox.Text);
        }
        private void DoFind(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: TextBox Selection.IsEmpty check
            if (!string.IsNullOrEmpty(Body.SelectedText))
            {
                FindTextBox.Text = Body.SelectedText;
            }

            if (string.IsNullOrEmpty(FindTextBox.Text))
            {
                FindTextBox.Text = "Enter Search Text";
            }

            FindTextBox.Focus();
        }
        private void DoDeleteLine(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: EditingCommands.MoveToLineStart/SelectDownByLine/Cut not available
            // Simplified: delete the current line based on caret position
            var text = Body.Text ?? string.Empty;
            var caretIndex = Body.CaretIndex;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var lineStart = text.LastIndexOf('\n', Math.Max(0, caretIndex - 1)) + 1;
            var lineEnd = text.IndexOf('\n', caretIndex);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }
            else
            {
                lineEnd++; // include the newline
            }

            Body.Text = text.Substring(0, lineStart) + text.Substring(lineEnd);
            Body.CaretIndex = Math.Min(lineStart, (Body.Text ?? string.Empty).Length);
        }
        private void DoClear(object sender, RoutedEventArgs e)
        {
            Body.Text = "";
        }
        private void DoClose(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: Window.GetWindow not available; using Parent traversal
            var asWindow = this.FindAncestorOfType<Window>();
            if (asWindow != null)
            {
                asWindow.Close();
            }
        }
        private void DoSaveAs(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: SaveFileDialog API differs in Avalonia; this is a simplified async version
            DoSaveAsAsync();
        }

        private async void DoSaveAsAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "File Save",
                DefaultExtension = ".txt",
                SuggestedFileName = m_fileName != null ? Path.GetFileName(m_fileName) : null,
            });

            if (file != null)
            {
                m_fileName = file.Path.LocalPath;
                var window = this.FindAncestorOfType<Window>();
                if (window != null)
                {
                    window.Title = "Editing: " + m_fileName;
                }

                SaveText(m_fileName);
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }

        private void DoSave(object sender, RoutedEventArgs e)
        {
            if (m_fileName == null)
            {
                DoSaveAs(sender, e);
            }
            else
            {
                SaveText(m_fileName);
            }
        }
        private void DoOpen(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: OpenFileDialog API differs in Avalonia; this is a simplified async version
            DoOpenAsync();
        }

        private async void DoOpenAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "File Open",
                AllowMultiple = false,
            });

            if (files != null && files.Count > 0)
            {
                m_fileName = files[0].Path.LocalPath;
                var window = this.FindAncestorOfType<Window>();
                if (window != null)
                {
                    window.Title = "Editing: " + m_fileName;
                }

                OpenText(m_fileName);
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }

        // GUI callbacks
        private void FindTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Find(FindTextBox.Text);
            }
        }
        private void IncreaseFont_Click(object sender, RoutedEventArgs e)
        {
            Body.FontSize = Math.Min(Body.FontSize + 2, 36);
        }
        private void DecreaseFont_Click(object sender, RoutedEventArgs e)
        {
            Body.FontSize = Math.Max(Body.FontSize - 2, 6);
        }

        // TODO_AVALONIA: GetTextPointerFromTextOffset removed; TextPointer not available in Avalonia

        private string m_fileName;
        #endregion
    }

    /// <summary>
    /// TextEditorWriter creates a TextWriter that sends it TextEditor.  
    /// </summary>
    internal class TextEditorWriter : TextWriter
    {
        public TextEditorWriter(TextEditorControl textEditorControl)
        {
            m_textEditorControl = textEditorControl;
            m_sb = new StringBuilder();
            m_timer = new DispatcherTimer();
            m_timer.Tick += delegate { Flush(); };
            m_timer.Interval = new TimeSpan(100000 * 300);     // 300 msec 
            m_textEditorControl.PropertyChanged += delegate (object sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property == Visual.IsVisibleProperty)
                {
                    Flush();
                }
            };
        }
        public override void Write(char value)
        {
            Write(new String(value, 1));
        }
        public override void Write(char[] buffer, int index, int count)
        {
            Write(new String(buffer, index, count));
        }
        public override void Flush()
        {
            m_timer.Stop();

            if (m_textEditorControl.IsVisible && m_sb.Length > 0)
            {
                Dispatcher.UIThread.InvokeAsync((Action)delegate ()
                {
                    lock (this)
                    {
                        // Flushing is expensive, do it no more frequently than once every 200 msec
                        if ((DateTime.UtcNow - m_LastTimeFlushed).TotalMilliseconds > 200)
                        {
                            // The Text control gets unresponsive if it is too big.  Scroll the data, but also put it in a file
                            const int maxLengthInViewer = 50000;        // TODO make this a parameter
                            string logData = m_sb.ToString();
                            m_sb.Clear();
                            var newTotalLen = logData.Length + m_charsWritten;
                            if (newTotalLen < maxLengthInViewer)
                            {
                                m_textEditorControl.AppendText(logData);
                            }
                            else
                            {
                                if (logData.Length < maxLengthInViewer)
                                {
                                    logData = m_textEditorControl.Text + logData;
                                }

                                var newLogData = @"***** See " + PerfView.App.LogFileName + " for complete log. ******\r\n";
                                var dataLen = Math.Min(maxLengthInViewer, logData.Length);
                                newLogData += logData.Substring(logData.Length - dataLen, dataLen);
                                m_textEditorControl.Text = newLogData;
                            }
                            m_textEditorControl.ScrollToEnd();
                            m_charsWritten = newTotalLen;
                            m_LastTimeFlushed = DateTime.UtcNow;
                        }
                    }
                });
            }
            base.Flush();
        }
        public override void Write(string value)
        {
            if (value.Length == 0)
            {
                return;
            }

            lock (this)
            {
                if (!m_timer.IsEnabled)
                {
                    m_timer.Start();
                }

                m_sb.Append(value);

                if (value.EndsWith("\r\n"))
                {
                    value = value.Substring(0, value.Length - 2);
                }

                PerfViewLogger.Log.PerfViewLog(value);
            }
        }
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
        public string GetText()
        {
            lock (this)
            {
                return m_sb.ToString() + m_textEditorControl.Text;
            }
        }

        public override string ToString()
        {
            lock (this)
            {
                return m_textEditorControl.Text + " PENDING " + m_sb.ToString();
            }
        }
        #region private
        private const int BuffSize = 10240;
        protected TextEditorControl m_textEditorControl;
        private StringBuilder m_sb;
        private DispatcherTimer m_timer;
        private DateTime m_LastTimeFlushed;
        private int m_charsWritten;
        #endregion
    }
}
