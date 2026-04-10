using Microsoft.Diagnostics.Tracing.Stacks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
// using System.Windows.Documents; // TODO_AVALONIA: Documents namespace not available in Avalonia
using Avalonia.Interactivity;
using Avalonia.VisualTree; // TODO_AVALONIA: for FindAncestorOfType<T>()
using Utilities;

namespace PerfView
{
    /// <summary>
    /// Interaction logic for PerfDataGrid.axaml
    /// </summary>
    public partial class PerfDataGrid : UserControl
    {
        public static bool NoPadOnCopyToClipboard = false;
        public static bool DoNotCompressStackFrames = false;

        public PerfDataGrid()
        {
            InitializeComponent();
            // TODO_AVALONIA: CopyingRowClipboardContent event not available in Avalonia DataGrid
            // The clipboard content formatting logic below needs to be reimplemented
            // using Avalonia's clipboard APIs.
            /*
            Grid.CopyingRowClipboardContent += delegate (object sender, DataGridRowClipboardEventArgs e)
            {
                // ... clipboard formatting logic ...
            };
            */

            // By default sort columns in descending order when they are clicked for the first time
            Grid.Sorting +=
                (sender, e) =>
                {
                    if (e.Column.SortDirection == null)
                    {
                        e.Column.SortDirection = ListSortDirection.Ascending;
                    }

                    e.Handled = false;
                };
        }

        public bool Find(string pat)
        {
            if (pat == null)
            {
                m_findPat = null;
                return true;
            }

            Grid.Focus();
            int curPos = SelectionStartIndex();
            var startingNewSearch = false;
            if (m_findPat == null || m_findPat.ToString() != pat)
            {
                startingNewSearch = true;
                m_FindEnd = curPos;
                try
                {
                    m_findPat = new Regex(pat, RegexOptions.IgnoreCase);    // TODO perf bad if you compile!
                }
                catch (ArgumentException e)
                {
                    throw new ApplicationException("Bad regular expression: " + e.Message);
                }
            }

            var list = Grid.ItemsSource as IList;
            if (list.Count == 0)
            {
                return false;
            }

            for (; ; )
            {
                if (startingNewSearch)
                {
                    startingNewSearch = false;
                }
                else
                {
                    curPos++;
                    if (curPos >= list.Count)
                    {
                        curPos = 0;
                    }

                    if (curPos == m_FindEnd)
                    {
                        m_findPat = null;
                        return false;
                    }
                }

                var item = list[curPos];
                if (m_findPat.IsMatch(GetName(item)))
                {
                    Select(item);
                    return true;
                }
            }
        }

        public void Select(object item)
        {
            // TODO_AVALONIA: DataGrid.SelectedCells not available in Avalonia
            // Grid.SelectedCells.Clear();
            Grid.SelectedItem = item;
            if (item == null)
            {
                Debug.Assert(false, "Null item selected:");
                return;
            }
            Grid.ScrollIntoView(item, null);

            // TODO_AVALONIA: ItemContainerGenerator not available in Avalonia
            // var row = (DataGridRow)Grid.ItemContainerGenerator.ContainerFromItem(item);
            // if (row != null)
            // {
            //     row.MoveFocus(
            //         new TraversalRequest(FocusNavigationDirection.Next));
            // }
        }
        public int SelectionStartIndex()
        {
            var ret = 0;
            // TODO_AVALONIA: DataGrid.SelectedCells not available in Avalonia
            // Using SelectedItem instead
            var selectedItem = Grid.SelectedItem;
            if (selectedItem != null)
            {
                var list = Grid.ItemsSource as IList;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == selectedItem)
                    {
                        return i;
                    }
                }
            }
            return ret;
        }
        public void RemoveColumn(string columnName)
        {
            int col = GetColumnIndex(columnName);
            if (0 <= col)
            {
                Grid.Columns.RemoveAt(col);
            }
        }

        public int GetColumnIndex(string columnName)
        {
            int i = 0;
            while (i < Grid.Columns.Count)
            {
                var name = ((TextBlock)Grid.Columns[i].Header).Name;
                if (name == columnName)
                {
                    return i;
                }
                else
                {
                    i++;
                }
            }
            return -1;
        }
        public List<string> ColumnNames()
        {
            var ret = new List<string>(Grid.Columns.Count);
            foreach (var column in Grid.Columns)
            {
                ret.Add(((TextBlock)column.Header).Name);
            }
            return ret;
        }

        /// <summary>
        /// Tries to make content smaller for cut and paste
        /// </summary>
        private string CompressContent(string content)
        {
            if (content.Length < 70)
            {
                return content;
            }

            // Check if the user option is set to not compress stack frames when copying
            if (DoNotCompressStackFrames)
            {
                return content;
            }

            // Trim method names !*.XXX.YYY(*) -> !XXX.YYY
            content = Regex.Replace(content, @"![\w\.]+\.(\w+\.\w+)\(.*\)", "!$1");
            if (content.Length < 70)
            {
                return content;
            }

            // Trim out generic parameters 
            for (; ; )
            {
                var result = Regex.Replace(content, @"(\w+)<[^>]+>", "$1");
                if (result == content)
                {
                    break;
                }

                content = result;
                if (content.Length < 70)
                {
                    return content;
                }
            }

            return content;
        }

        /// <summary>
        /// given the content string, and the columnIndex, return a string that is propertly padded
        /// so that when displayed the rows will line up by columns nicely  
        /// </summary>
        private string PadForColumn(string content, int columnIndex)
        {
            if (m_maxColumnInSelection == null)
            {
                m_maxColumnInSelection = new int[Grid.Columns.Count];
            }

            int maxString = m_maxColumnInSelection[columnIndex];
            if (maxString == 0)
            {
                for (int i = 0; i < m_maxColumnInSelection.Length; i++)
                {
                    m_maxColumnInSelection[i] = GetColumnHeaderText(Grid.Columns[i]).Length;
                }

                // TODO_AVALONIA: DataGrid.SelectedCells not available
                // foreach (var cellInfo in Grid.SelectedCells)
                // {
                //     var idx = cellInfo.Column.DisplayIndex;
                //     var contents = GetCellStringValue(cellInfo);
                //     contents = CompressContent(contents);
                //     m_maxColumnInSelection[idx] = Math.Max(m_maxColumnInSelection[idx], contents.Length + 1);
                // }
                maxString = m_maxColumnInSelection[columnIndex];
            }

            if (columnIndex == 0)
            {
                return content.PadRight(maxString);
            }
            else
            {
                return content.PadLeft(maxString);
            }
        }
        // TODO_AVALONIA: DataGridCellInfo not available in Avalonia
        // public static string GetCellStringValue(DataGridCellInfo cell)
        // {
        //     ...
        // }
        public static string GetCellStringValue(object item, string columnName)
        {
            CallTreeNodeBase model = item as CallTreeNodeBase;
            if (model != null)
            {
                switch (columnName)
                {
                    case "NameColumn": return model.DisplayName;
                    case "IncPercentColumn": return model.InclusiveMetricPercent.ToString("n1");
                    case "IncColumn": return model.InclusiveMetric.ToString("n1");
                    case "IncAvgColumn": return model.AverageInclusiveMetric.ToString("n1");
                    case "IncCountColumn": return model.InclusiveCount.ToString("n0");
                    case "ExcPercentColumn": return model.ExclusiveMetricPercent.ToString("n1");
                    case "ExcColumn": return model.ExclusiveMetric.ToString("n0");
                    case "ExcCountColumn": return model.ExclusiveCount.ToString("n0");
                    case "FoldColumn": return model.ExclusiveFoldedMetric.ToString("n0");
                    case "FoldCountColumn": return model.ExclusiveFoldedCount.ToString("n0");
                    case "TimeHistogramColumn": return model.InclusiveMetricByTimeString;
                    case "ScenarioHistogramColumn": return model.InclusiveMetricByScenarioString;
                    case "FirstColumn": return model.FirstTimeRelativeMSec.ToString("n3");
                    case "LastColumn": return model.LastTimeRelativeMSec.ToString("n3");
                }
            }
            // TODO_AVALONIA: FrameworkElement cell content retrieval not available
            return "";
        }
        public static string GetCellStringValue(Control contents) // TODO_AVALONIA: FrameworkElement → Control
        {
            string ret = Helpers.GetText(contents);
            return ret;
        }
        public static string GetColumnHeaderText(DataGridColumn column)
        {
            // TODO  I would like get the columnHeader text not from the column name but from what is displayed in the hyperlink
            var header = column.Header as TextBlock;
            string ret = header.Name;
            ret = ret.Replace("Column", "");
            ret = ret.Replace("Percent", " %");
            ret = ret.Replace("Count", " Ct");
            return ret;
        }

        #region private
        private void HistogramCell_CellSelectionChanged(object sender, RoutedEventArgs e, HistogramController controller, Histogram histogram)
        {
            var asTextBox = sender as TextBox;
            var window = this.FindAncestorOfType<StackWindow>(); // TODO_AVALONIA: Helpers.AncestorOfType → FindAncestorOfType

            if (asTextBox != null && window != null && 0 < asTextBox.SelectionEnd - asTextBox.SelectionStart) // TODO_AVALONIA: SelectionLength → compute from SelectionStart/SelectionEnd
            {
                window.StatusBar.Status = controller.GetInfoForCharacterRange(
                    (HistogramCharacterIndex)(asTextBox.SelectionStart),
                    (HistogramCharacterIndex)(asTextBox.SelectionEnd), histogram); // TODO_AVALONIA: SelectionStart + SelectionLength → SelectionEnd
            }
        }

        // TODO_AVALONIA: PreparingCellForEdit not available in Avalonia DataGrid
        // The Grid_PreparingCellForEdit method needs to be reimplemented
        // using Avalonia's DataGrid editing APIs.
        /*
        private void Grid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            ...
        }
        */

        // TODO FIX NOW.  This is an ugly hack.  
        public static TextBox EditingBox;

        internal static string GoodPrecision(double num, DataGridColumn column)
        {
            var format = "n3";

            string headerName = column.Header as string;
            if (headerName == null)
            {
                var header = column.Header as TextBlock;
                if (header != null)
                {
                    headerName = header.Name;
                }
            }

            if (headerName != null)
            {
                switch (headerName)
                {
                    case "ExcPercentColumn":
                    case "IncPercentColumn":
                        format = "n1";
                        break;
                    default:
                        if ((int)num == num)
                        {
                            format = "n0";
                        }

                        break;
                }
            }
            return num.ToString(format);
        }
        // TODO this is all a hack.
        internal static bool VeryClose(string val1, string val2)
        {
            if (val1 == val2)
            {
                return true;
            }

            double dval1, dval2;
            if (!double.TryParse(val1, out dval1))
            {
                return false;
            }

            if (!double.TryParse(val2, out dval2))
            {
                return false;
            }

            return (dval1 == dval2);
        }

        private static string GetName(object item)
        {
            var asCallTreeNodeBase = item as CallTreeNodeBase;
            if (asCallTreeNodeBase != null)
            {
                return asCallTreeNodeBase.DisplayName;
            }

            var asCallTreeViewNode = item as CallTreeViewNode;
            if (asCallTreeViewNode != null)
            {
                return asCallTreeViewNode.Name;
            }

            return "";
        }

        // TODO_AVALONIA: SelectedCellsChanged event not available in Avalonia DataGrid
        // private void SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) { ... }

        private void DoHyperlinkHelp(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: Hyperlink not available in Avalonia
            // var asHyperLink = sender as Hyperlink;
            // if (asHyperLink != null)
            // {
            //     MainWindow.DisplayUsersGuide((string)asHyperLink.Tag);
            // }
        }

        /// <summary>
        /// If we have only two cells selected, even if they are on differnet rows we want to morph them
        /// to a single row.  These variables are for detecting this situation.  
        /// </summary>
        private string m_clipboardRangeStart;
        private string m_clipboardRangeEnd;
        private int m_numSelectedCells;
        private int m_numSelectedColumns;
        private int m_numSelectedRows;
        private bool m_isFirstLastSelection;
        private int[] m_maxColumnInSelection;
        private int m_FindEnd;
        private Regex m_findPat;
        #endregion
    }
}
