using Controls;
using Diagnostics.Tracing.StackSources;
using Graphs;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Diagnostics.Tracing.Stacks.Formats;
using Microsoft.Diagnostics.Utilities;
using PerfView.Dialogs;
using PerfViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
// using System.Windows.Documents; // TODO_AVALONIA: Documents namespace not available
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree; // TODO_AVALONIA: for FindAncestorOfType<T>()
using System.Xml;
using Utilities;
using Address = System.UInt64;
using Path = System.IO.Path;

namespace PerfView
{
    /// <summary>
    /// Interaction logic for StackWindow.axaml
    /// </summary>
    public partial class StackWindow : Window // TODO_AVALONIA: was WindowBase
    {
        public StackWindow(Window parentWindow, PerfViewStackSource dataSource) // TODO_AVALONIA: was : base(parentWindow)
        {
            DataSource = dataSource;
            ParentWindow = parentWindow;
            m_history = new List<FilterParams>();

            InitializeComponent();
            MemoryStackPanel.IsVisible = false; // TODO_AVALONIA: was Visibility.Collapsed

            Title = DataSource.Title;
            FinishInit();
        }
        public StackWindow(Window parentWindow, StackWindow template) // TODO_AVALONIA: was : base(parentWindow)
        {
            ParentWindow = parentWindow;
            DataSource = template.DataSource;
            m_history = new List<FilterParams>();
            InitializeComponent();
            MemoryStackPanel.IsVisible = false; // TODO_AVALONIA: was Visibility.Collapsed

            FoldPercentTextBox.Text = GetDefaultFoldPercentage();
            GuiState = template.GuiState;

            Title = DataSource.Title;
            Filter = template.Filter;
            FindTextBox.Text = template.FindTextBox.Text;
            NotesPaneHidden = template.NotesPaneHidden;

            StartTextBox.CopyFrom(template.StartTextBox);
            EndTextBox.CopyFrom(template.EndTextBox);
            ScenarioTextBox.CopyFrom(template.ScenarioTextBox);
            FindTextBox.CopyFrom(template.FindTextBox);
            GroupRegExTextBox.CopyFrom(template.GroupRegExTextBox);
            FoldPercentTextBox.CopyFrom(template.FoldPercentTextBox);
            FoldRegExTextBox.CopyFrom(template.FoldRegExTextBox);
            IncludeRegExTextBox.CopyFrom(template.IncludeRegExTextBox);
            ExcludeRegExTextBox.CopyFrom(template.ExcludeRegExTextBox);
            PriorityTextBox.CopyFrom(template.PriorityTextBox);

            m_historyPos = template.m_historyPos;
            m_history.AddRange(m_history);

            FinishInit();

            // Clone the columns available.  
            var templateColumnNames = template.ByNameDataGrid.ColumnNames();
            foreach (var colName in ByNameDataGrid.ColumnNames())
            {
                if (!templateColumnNames.Contains(colName))
                {
                    RemoveColumn(colName);
                }
            }
        }
        public string GetDefaultFoldPercentage()
        {
            string defaultFoldPercentage = App.UserConfigData["DefaultFoldPercent"];
            if (defaultFoldPercentage == null)
            {
                defaultFoldPercentage = "";
            }

            return defaultFoldPercentage;
        }
        public string GetDefaultFoldPat()
        {
            string defaultFoldPat = App.UserConfigData["DefaultFoldPat"];
            if (defaultFoldPat == null)
            {
                defaultFoldPat = "ntoskrnl!%ServiceCopyEnd";
            }

            return defaultFoldPat;
        }
        public string GetDefaultGroupPat()
        {
            string defaultGroupPat = App.UserConfigData["DefaultGroupPat"];

            // By default, it is group module entries.
            if (defaultGroupPat == null)
            {
                defaultGroupPat = @"[group module entries]  {%}!=>module $1";
            }

            return defaultGroupPat;
        }
        public void RemoveColumn(string columnName)
        {
            // Remove View First or else GetColumnIndex will not work
            // Assumes ByNameDataGrid.Columns == CallTreeDataGrid.Columns == CalleesDataGrid.Columns == CallersDataGrid.Columns
            RemoveViewMenuColumn(ByNameDataGrid, ViewMenu, columnName);

            ByNameDataGrid.RemoveColumn(columnName);
            CallTreeDataGrid.RemoveColumn(columnName);
            CalleesDataGrid.RemoveColumn(columnName);
            CallersDataGrid.RemoveColumn(columnName);
            CallerCalleeView.RemoveCountColumn(columnName);
        }

        public void RemoveViewMenuColumn(PerfDataGrid perfDataGrid, MenuItem viewMenu, string columnName)
        {
            // First find the string displayed for the named column (e.g IncCount -> Inc Ct)
            int col = perfDataGrid.GetColumnIndex(columnName);
            if (col > -1)
            {
                string columnDisplayString = ((TextBlock)perfDataGrid.Grid.Columns[col].Header).Text;

                // Find that in the list of MenuItems, and delete it if present.  
                for (int i = 0; i < viewMenu.Items.Count; i++)
                {
                    MenuItem item = viewMenu.Items[i] as MenuItem;
                    if (item != null)
                    {
                        string name = (string)item.Header;
                        if (name == columnDisplayString)
                        {
                            viewMenu.Items.RemoveAt(i);
                            return;
                        }
                    }
                }
            }
        }

        public bool IsMemoryWindow
        {
            get { return m_IsMemoryWindow; }
            set
            {
                if (value != m_IsMemoryWindow)
                {
                    if (value == true)
                    {
                        ChangeHeaderText(CallersTab, "Referred-From");
                        ChangeHeaderText(CalleesTab, "Refs-To");
                        ChangeHeaderText(CallerCalleeTab, "RefFrom-RefTo");
                        ChangeHeaderText(CallTreeTab, "RefTree");
                        MemoryStackPanel.IsVisible = true; // TODO_AVALONIA: was Visibility.Visible
                        m_callersView.DisplayPrimaryOnly = false;
                        m_calleesView.DisplayPrimaryOnly = false;
                        m_callTreeView.DisplayPrimaryOnly = false;
                    }
                    else
                    {
                        ChangeHeaderText(CallersTab, "Callers");
                        ChangeHeaderText(CalleesTab, "Callees");
                        ChangeHeaderText(CallerCalleeTab, "Caller-Callee");
                        ChangeHeaderText(CallTreeTab, "CallTree");
                        MemoryStackPanel.IsVisible = false; // TODO_AVALONIA: was Visibility.Collapsed
                    }
                    m_IsMemoryWindow = value;
                }
            }
        }

        public bool IsScenarioWindow
        {
            get { return m_IsScenarioWindow; }
            set
            {
                if (value != m_IsScenarioWindow)
                {
                    if (value)
                    {
                        ScenarioStackPanel.IsVisible = false; // TODO_AVALONIA: was Visibility.Collapsed
                        ScenarioContextMenu.IsVisible = true; // TODO_AVALONIA: was Visibility.Visible
                    }
                    else
                    {
                        ScenarioStackPanel.IsVisible = false; // TODO_AVALONIA: was Visibility.Collapsed
                        ScenarioContextMenu.IsVisible = false; // TODO_AVALONIA: was Visibility.Collapsed
                    }
                }
                m_IsScenarioWindow = value;
            }
        }

        /// <summary>
        /// Changes the text of a header of 'tab' to 'newHeaderText' without losing the '?' hyperlinks.   
        /// </summary>
        private void ChangeHeaderText(TabItem tab, string newHeaderText)
        {
            // TODO_AVALONIA: Inlines/Run not available in Avalonia TextBlock the same way
            var textBlock = (TextBlock)tab.Header;
            textBlock.Text = newHeaderText + " ?";
        }

        private bool m_IsMemoryWindow;
        private bool m_IsScenarioWindow = true;

        public Window ParentWindow { get; private set; }
        public PerfViewStackSource DataSource { get; private set; }

        // TODO resolve the redundancy with DataSource.  
        public StackSource StackSource => m_stackSource;

        /// <summary>
        /// This sets the window to to the given stack source, this DOES triggers an update of the gridViews.  
        /// </summary>
        public void SetStackSource(StackSource newSource, Action onComplete = null)
        {
            Debug.Assert(newSource != null);
            Debug.Assert(m_callTree != null);

            if (!ValidateStartAndEnd(newSource))
            {
                return;
            }

            // Synchronize the sample rate if the source supports it.  
            // TODO - Currently nothing uses sampling.  USE OR REMOVE 
            if (newSource.SamplingRate == null)
            {
                SamplingStackPanel.IsVisible = false; // TODO_AVALONIA: was Visibility.Collapsed
            }
            else
            {
                if (SamplingTextBox.Text.Length == 0)
                {
                    SamplingTextBox.Text = newSource.SamplingRate.Value.ToString("f1");
                }
                else
                {
                    var sampleRate = 1.0F;
                    float.TryParse(SamplingTextBox.Text, out sampleRate);
                    if (sampleRate < 1)
                    {
                        sampleRate = 1;
                    }

                    newSource.SamplingRate = sampleRate;
                }
            }

            // Hide scenarios view if our source doesn't support scenarios.
            IsScenarioWindow = (newSource.ScenarioCount != 0);

            FixupJustMyCodeInGroupPats(newSource);

            FilterParams filterParams = Filter;
            if (!m_settingFromHistory && (m_history.Count == 0 || !filterParams.Equals(m_history[m_history.Count - 1])))
            {
                // Remove any 'forward' history
                if (m_historyPos + 1 < m_history.Count)
                {
                    m_history.RemoveRange(m_historyPos + 1, m_history.Count - m_historyPos - 1);
                }

                if (m_history.Count > 100)
                {
                    m_history.RemoveAt(0);
                }

                m_history.Add(new FilterParams(filterParams));
                m_historyPos = m_history.Count - 1;
            }

            var asMemoryGraphSource = newSource as Graphs.MemoryGraphStackSource;
            if (asMemoryGraphSource != null)
            {
                asMemoryGraphSource.PriorityRegExs = filterParams.TypePriority;
            }

            StatusBar.StartWork("Computing Stack Traces", delegate ()
            {
                CallTree newCallTree = new CallTree(ScalingPolicy);

                m_stackSource = newSource;
                if (m_stackSource == null)
                {
                    m_stackSource = new CopyStackSource();      // This is only needed for the degenerate case of no data.  
                }

                double histogramStart = 0;
                if (double.TryParse(filterParams.StartTimeRelativeMSec, out histogramStart))
                {
                    histogramStart -= .0006;
                }

                double histogramEnd = double.MaxValue;
                if (double.TryParse(filterParams.EndTimeRelativeMSec, out histogramEnd))
                {
                    histogramEnd += .0006;
                }

                if (histogramEnd > histogramStart)
                {
                    newCallTree.TimeHistogramController = new TimeHistogramController(newCallTree, histogramStart, histogramEnd);
                }

                if (m_stackSource.ScenarioCount > 0)
                {
                    if (filterParams.ScenarioList == null)
                    {
                        filterParams.ScenarioList = new int[m_stackSource.ScenarioCount];
                        for (int i = 0; i < m_stackSource.ScenarioCount; i++)
                        {
                            filterParams.ScenarioList[i] = i;
                        }
                    }
                    string[] names = null;
                    var aggregate = m_stackSource as AggregateStackSource;
                    if (aggregate != null)
                    {
                        names = aggregate.ScenarioNames;
                    }

                    if (filterParams.ScenarioList.Length > 1)
                    {
                        newCallTree.ScenarioHistogram = new ScenarioHistogramController(
                            newCallTree, filterParams.ScenarioList, m_stackSource.ScenarioCount, names);
                    }
                }

                if (App.CommandLineArgs.SafeMode)
                {
                    StatusBar.Log("SafeMode enable, turning off parallelism");
                    CallTree.DisableParallelism = true;
                }

                var filterStackSource = new FilterStackSource(filterParams, m_stackSource, ScalingPolicy);
                newCallTree.StackSource = filterStackSource;

                // TODO: do we want to expose useWholeTraceMetric = false too?
                // Fold away all small nodes.                     
                float minIncusiveTimePercent;
                if (float.TryParse(filterParams.MinInclusiveTimePercent, out minIncusiveTimePercent) && minIncusiveTimePercent > 0)
                {
                    newCallTree.FoldNodesUnder(minIncusiveTimePercent * newCallTree.Root.InclusiveMetric / 100, true);
                }

                // Compute the byName items sorted by exclusive time.  
                var byNameItems = newCallTree.ByIDSortedExclusiveMetric();

                StatusBar.EndWork(delegate ()
                {
                    var selectedNodeName = FocusName ?? "ROOT";
                    var oldCallTree = m_callTree;
                    m_callTree = newCallTree;

                    // TODO_AVALONIA: SortDescriptions not available in Avalonia DataGrid the same way
                    // Gather current sorting information
                    // var sortDescriptions = ByNameDataGrid.Grid.Items.SortDescriptions.ToArray();
                    // var sortDirections = ByNameDataGrid.Grid.Columns.Select(c => c.SortDirection).ToArray();

                    // SignalPropertyChange the ByName Tab 
                    m_byNameView = byNameItems;
                    ByNameDataGrid.Grid.ItemsSource = m_byNameView;

                    // TODO_AVALONIA: Reapply sort after setting ItemsSource

                    SetFocus(selectedNodeName);

                    ByNameDataGrid.Focus();

                    // SignalPropertyChange the CallTree Tab
                    m_callTreeView.SetRoot(m_callTree.Root);

                    // Update the threads stats
                    var stats = string.Format("Totals Metric: {0:n1}  Count: {1:n1}", CallTree.Root.InclusiveMetric, CallTree.Root.InclusiveCount);
                    if (CallTree.Root.LastTimeRelativeMSec != 0)
                    {
                        stats = string.Format("{0}  First: {1:n3} Last: {2:n3}  Last-First: {3:n3}  Metric/Interval: {4:n2}  TimeBucket: {5:n1}", stats,
                        CallTree.Root.FirstTimeRelativeMSec, CallTree.Root.LastTimeRelativeMSec, CallTree.Root.DurationMSec,
                        CallTree.Root.InclusiveMetric / CallTree.Root.DurationMSec,
                        CallTree.TimeHistogramController.BucketDuration);
                    }

                    if (ExtraTopStats != null)
                    {
                        stats = stats + " " + ExtraTopStats;
                    }

                    if (ComputeMaxInTopStats)
                    {
                        Histogram histogram = CallTree.Root.InclusiveMetricByTime;
                        TimeHistogramController controller = histogram.Controller as TimeHistogramController;

                        float cum = 0;
                        float cumMax = float.MinValue;
                        int cumMaxIdx = -1;
                        for (int i = 0; i < histogram.Count; i++)
                        {
                            var val = histogram[i];
                            cum += val;
                            if (cum > cumMax)
                            {
                                cumMax = cum;
                                cumMaxIdx = i + 1;
                            }
                        }

                        stats += string.Format(" MaxMetric: {0:n3}M at {1:n3}ms",
                            cumMax / 1000000, controller.GetStartTimeForBucket((HistogramCharacterIndex)cumMaxIdx));
                    }

                    RedrawFlameGraphIfVisible();

                    TopStats.Text = stats;

                    // TODO this is a bit of a hack, as it might replace other instances of the string.  
                    Title = Regex.Replace(Title, @" Stacks(\([^)]*\))? ", " Stacks(" + CallTree.Root.InclusiveMetric.ToString("n0") + " metric) ");
                    UpdateDiffMenus(StackWindows);
                    onComplete?.Invoke();
                });
            });
        }

        // The 'Just My App' pattern depends on the directory of the EXE and thus has to be fixed up to be the
        // correct pattern.  This code does this.
        private void FixupJustMyCodeInGroupPats(StackSource stackSource)
        {
            if (m_fixedUpJustMyCode)
            {
                return;
            }

            m_fixedUpJustMyCode = true;

            // Get tbe name and create the 'justMyApp pattern. 
            string justMyApp = null;
            string exeName = DataSource.DataFile.FindExeName(IncludeRegExTextBox.Text);
            if (exeName != null)
            {
                if (string.Compare(exeName, "w3wp", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    justMyApp = @"[ASP.NET Just My App] \Temporary ASP.NET Files\->;!dynamicClass.S->;!=>OTHER";
                }
                else if (!exeName.StartsWith("IISAspHost", StringComparison.OrdinalIgnoreCase) &&
                        string.Compare(exeName, "WWAHost", StringComparison.OrdinalIgnoreCase) != 0 &&
                        string.Compare(exeName, "iexplore", StringComparison.OrdinalIgnoreCase) != 0 &&
                        string.Compare(exeName, "dotnet", StringComparison.OrdinalIgnoreCase) != 0)
                {
                    string exePath = FindExePath(stackSource, exeName);
                    if (exePath != null)
                    {
                        var dirName = Path.GetDirectoryName(exePath);
                        if (!string.IsNullOrEmpty(dirName))
                        {
                            justMyApp = @"[Just My App]           \" + Path.GetFileName(dirName) + @"\%!->;!=>OTHER";
                        }
                    }

                    if (justMyApp == null)
                    {
                        StatusBar.Log("Could not determine EXE path, could not add 'Just my app' group");
                    }
                }
            }

            // If we asked for just my app in the TextBox, then set it to the specialized pattern
            if (GroupRegExTextBox.Text.StartsWith("[Just My App]"))
            {
                if (justMyApp == null)
                {
                    justMyApp = @"[group module entries]  {%}!=>module $1";
                }

                GroupRegExTextBox.Text = justMyApp;
            }

            // If we have a JustMyApp, add it to list of Group Pattern possibilities 
            if (justMyApp != null)
            {
                GroupRegExTextBox.Items.Insert(0, justMyApp);
            }
        }

        /// <summary>
        /// Update causes the gridview's to be recalculated based on the current stack source filter parameters. 
        /// </summary>
        public void Update()
        {
            if (m_stackSource != null)
            {
                SetStackSource(m_stackSource);  //  This forces a recomputation of the calltree.  
            }
        }

        public CallTree CallTree => m_callTree;

        public CallTreeView CallTreeView => m_callTreeView;

        /// <summary>
        /// Note that setting the filter does NOT trigger an update of the gridViews.  You have to call Update()
        /// </summary>
        public FilterParams Filter
        {
            get
            {
                var ret = new FilterParams();
                ret.StartTimeRelativeMSec = StartTextBox.Text;
                ret.EndTimeRelativeMSec = EndTextBox.Text;
                ret.Scenarios = ScenarioTextBox.Text;
                ret.MinInclusiveTimePercent = FoldPercentTextBox.Text;
                ret.FoldRegExs = FoldRegExTextBox.Text;
                ret.IncludeRegExs = IncludeRegExTextBox.Text;
                ret.ExcludeRegExs = ExcludeRegExTextBox.Text;
                ret.GroupRegExs = GroupRegExTextBox.Text;
                ret.TypePriority = PriorityTextBox.Text;
                return ret;
            }
            set
            {
                StartTextBox.Text = value.StartTimeRelativeMSec;
                EndTextBox.Text = value.EndTimeRelativeMSec;
                ScenarioTextBox.Text = value.Scenarios;
                FoldPercentTextBox.Text = value.MinInclusiveTimePercent;
                FoldRegExTextBox.Text = value.FoldRegExs;
                IncludeRegExTextBox.Text = value.IncludeRegExs;
                ExcludeRegExTextBox.Text = value.ExcludeRegExs;
                GroupRegExTextBox.Text = value.GroupRegExs;
                PriorityTextBox.Text = value.TypePriority;
            }
        }

        /// <summary>
        /// FilterGuiState is like 'Filter' in that it can set the filter paramters, but it goes further that it 
        /// can also set the history of each filter parameter (and other things that only the GUI cares about)
        /// </summary>
        public FilterGuiState FilterGuiState
        {
            get
            {
                var ret = new FilterGuiState();
                WriteFromTextBox(StartTextBox, ret.Start);
                WriteFromTextBox(EndTextBox, ret.End);
                WriteFromTextBox(ScenarioTextBox, ret.Scenarios);
                WriteFromTextBox(GroupRegExTextBox, ret.GroupRegEx);
                WriteFromTextBox(FoldPercentTextBox, ret.FoldPercent);
                WriteFromTextBox(FoldRegExTextBox, ret.FoldRegEx);
                WriteFromTextBox(IncludeRegExTextBox, ret.IncludeRegEx);
                WriteFromTextBox(ExcludeRegExTextBox, ret.ExcludeRegEx);
                WriteFromTextBox(PriorityTextBox, ret.TypePriority);
                return ret;
            }
            set
            {
                ReadIntoTextBox(StartTextBox, value.Start);
                ReadIntoTextBox(EndTextBox, value.End);
                ReadIntoTextBox(ScenarioTextBox, value.Scenarios);
                ReadIntoTextBox(GroupRegExTextBox, value.GroupRegEx);
                ReadIntoTextBox(FoldPercentTextBox, value.FoldPercent);
                ReadIntoTextBox(FoldRegExTextBox, value.FoldRegEx);
                ReadIntoTextBox(IncludeRegExTextBox, value.IncludeRegEx);
                ReadIntoTextBox(ExcludeRegExTextBox, value.ExcludeRegEx);
                ReadIntoTextBox(PriorityTextBox, value.TypePriority);
            }
        }

        private void ReadIntoTextBox(HistoryComboBox textBox, TextBoxGuiState guiState)
        {
            if (guiState == null)
            {
                return;
            }

            if (guiState.Value != null)
            {
                textBox.Text = guiState.Value;
            }

            if (guiState.History != null)
            {
                textBox.SetHistory(guiState.History);
            }
        }

        private void WriteFromTextBox(HistoryComboBox textBox, TextBoxGuiState guiState)
        {
            var val = textBox.Text;
            if (!string.IsNullOrWhiteSpace(val))
            {
                guiState.Value = val;
            }

            if (textBox.Items.Count > 0)
            {
                var itemList = new List<string>();
                foreach (string item in textBox.Items)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        itemList.Add(item);
                    }
                }

                if (itemList.Count > 0)
                {
                    guiState.History = itemList;
                }
            }
        }

        public string FocusName { get { return CallerCalleeView.FocusName; } }

        public bool SetFocus(CallTreeNodeBase node)
        {
            CallerCalleeView.SetFocus(node.Name, m_callTree);

            m_calleesView.SetRoot(AggregateCallTreeNode.CalleeTree(node));
            if (IsMemoryWindow)
                CalleesTitle.Text = "Objects that are referred to by " + node.Name;
            else
                CalleesTitle.Text = "Methods that are called by " + node.Name;

            m_callersView.SetRoot(AggregateCallTreeNode.CallerTree(node));

            if (IsMemoryWindow)
                CallersTitle.Text = "Objects that refer to " + node.Name;
            else
                CallersTitle.Text = "Methods that call " + node.Name;
            DataContext = node;

            return true;
        }

        public bool SetFocus(string name)
        {
            if (name == null)
            {
                name = FocusName;       // Use the old focus name
                if (name == null)
                {
                    name = "ROOT";
                }
            }

            // TODO FIX NOW in case of duplicates
            // TODO FIX NOW make this a utility function 
            // Find the node in the ByName view.  
            CallTreeNodeBase node = null;
            foreach (var byName in m_callTree.ByID)
            {
                if (byName.Name == name)
                {
                    node = byName;
                    break;
                }
            }
            if (node == null)
            {
                // We want to aways succeed for root. 
                if (name != "ROOT")
                {
                    StatusBar.LogError("Could not find node named " + name + " (folded away?)");
                }

                node = m_callTree.Root;
                name = node.Name;
            }

            if (IsMemoryWindow)
            {
                CalleesTitle.Text = "Objects that are referred to by " + node.Name;
                CallersTitle.Text = "Objects that refer to " + node.Name;
            }
            else
            {
                CalleesTitle.Text = "Methods that are called by " + node.Name;
                CallersTitle.Text = "Methods that call " + node.Name;
            }

            CallerCalleeView.SetFocus(name, m_callTree);
            m_calleesView.SetRoot(AggregateCallTreeNode.CalleeTree(node));
            m_callersView.SetRoot(AggregateCallTreeNode.CallerTree(node));

            DataContext = node;
            return true;
        }

        /// <summary>
        /// Find a pattern in the appropriate window.  The pattern is a .NET regular expression (case insensitive). 
        /// </summary>
        public bool Find(string pat)
        {
            FindTextBox.Text = pat;
            FindNext(null);         // Restart the find operation. 
            return FindNext(pat);
        }

        public bool FindNext() => FindNext(FindTextBox.Text);

        /// <summary>
        /// If we save this view as a file, this is its name (may be null) 
        /// </summary>
        public string FileName { get { return m_fileName; } set { m_fileName = value; } }

        private void DoBack(object sender, RoutedEventArgs e)
        {
            // TODO FIX NOW, clone this for Forward too. 
            if (m_historyPos > 0)
            {
                --m_historyPos;
                m_settingFromHistory = true;        // TODO, can we pass as a parameter?
                bool success = false;
                var origFilter = Filter;
                try
                {
                    Filter = m_history[m_historyPos];
                    Update();
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        Filter = origFilter;
                    }
                }

                m_settingFromHistory = false;
            }
        }
        // TODO_AVALONIA: CanExecuteRoutedEventArgs not available
        // private void CanDoBack(object sender, CanExecuteRoutedEventArgs e) { ... }
        private void DoForward(object sender, RoutedEventArgs e)
        {
            if (m_historyPos + 1 < m_history.Count)
            {
                m_historyPos++;
                m_settingFromHistory = true;
                Filter = m_history[m_historyPos];
                Update();
                m_settingFromHistory = false;
            }
        }
        // TODO_AVALONIA: CanExecuteRoutedEventArgs not available
        // private void CanDoForward(object sender, CanExecuteRoutedEventArgs e) { ... }

        private void DoClose(object sender, RoutedEventArgs e) => Close();

        private void DoOpenParent(object sender, RoutedEventArgs e)
        {
            for (; ; )
            {
                try
                {
                    if (ParentWindow != null)
                    {
                        ParentWindow.IsVisible = true; // TODO_AVALONIA: was Visibility = Visibility.Visible
                        ParentWindow.Focus();
                    }
                    return;
                }
                catch (InvalidOperationException)
                {
                    // This means the window was closed, fix our parent to skip it.  
                    var asStackWindow = ParentWindow as PerfView.StackWindow;
                    if (asStackWindow != null)
                    {
                        ParentWindow = asStackWindow.ParentWindow;
                        continue;
                    }
                    var asEventWindow = ParentWindow as EventWindow;
                    if (asEventWindow != null)
                    {
                        ParentWindow = asEventWindow.ParentWindow;
                        continue;
                    }
                    break;
                }
            }
        }
        private void DoSetSymbolPath(object sender, RoutedEventArgs e) => GuiApp.MainWindow.DoSetSymbolPath(sender, e);

        private void DoSetSourcePath(object sender, RoutedEventArgs e)
        {
            var symPathDialog = new SymbolPathDialog(this, App.SourcePath, "Source", delegate (string newPath)
            {
                App.SourcePath = newPath;
            });
            symPathDialog.Show();
        }
        private void DoSetStartupPreset(object sender, RoutedEventArgs e)
        {
            App.UserConfigData["DefaultFoldPercent"] = FoldPercentTextBox.Text;
            App.UserConfigData["DefaultFoldPat"] = FoldRegExTextBox.Text;

            var defaultGroupPat = GroupRegExTextBox.Text;
            if (defaultGroupPat.StartsWith("[Just My App]"))
            {
                defaultGroupPat = defaultGroupPat.Substring(0, 13);
            }

            App.UserConfigData["DefaultGroupPat"] = defaultGroupPat;
        }
        private void DoSaveAs(object sender, RoutedEventArgs e)
        {
            m_fileName = null;
            DoSave(sender, e);
        }
        internal void DoSave(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: SaveFileDialog API differs in Avalonia
            // Microsoft.Win32.SaveFileDialog → Avalonia SaveFileDialog
            if (m_fileName == null)
            {
                // TODO_AVALONIA: Implement Avalonia file save dialog
                StatusBar.LogError("Save dialog not yet implemented for Avalonia");
                return;
            }

            if (m_fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                if (!ByNameTab.IsSelected)
                {
                    throw new ApplicationException("Saving as a CSV is only supported in the ByName tab");
                }

                string listSeparator = Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator;

                bool hasWhichColumn = (DataSource.DataFile as ScenarioSetPerfViewFile != null);
                string whichColumnHeader = hasWhichColumn ? listSeparator + "Which" : "";
                string whichValue = "";

                using (var csvFile = File.CreateText(m_fileName))
                {
                    csvFile.WriteLine("Name{0}Exc %{0}Exc{0}Exc Ct{1}", listSeparator, whichColumnHeader);

                    List<CallTreeNodeBase> items = CallTree.ByIDSortedExclusiveMetric();
                    foreach (var item in items)
                    {
                        if (hasWhichColumn)
                        {
                            whichValue = item.InclusiveMetricByScenario.ToString();
                        }

                        csvFile.WriteLine("{0}{1}{2:f1}{1}{3:f0}{1}{4}{5}", EventWindow.EscapeForCsv(item.Name, listSeparator), listSeparator,
                            item.ExclusiveMetricPercent, item.ExclusiveMetric, item.ExclusiveCount, whichValue);
                    }
                }
            }
            else if(m_fileName.EndsWith(".speedscope.json", StringComparison.OrdinalIgnoreCase))
            {
                SpeedScopeStackSourceWriter.WriteStackViewAsJson(CallTree.StackSource, m_fileName);
            }
            else if (m_fileName.EndsWith(".chromium.json", StringComparison.OrdinalIgnoreCase))
            {
                ChromiumStackSourceWriter.WriteStackViewAsJson(CallTree.StackSource, m_fileName, false);
            }
            else
            {
                if (m_fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    m_fileName = m_fileName + ".zip";
                }

                if (!m_fileName.EndsWith(".xml.zip"))
                {
                    throw new ApplicationException("File names for views must end in .xml.zip");
                }

                var filteredSource = new FilterStackSource(Filter, StackSource, ScalingPolicy);
                InternStackSource source = new InternStackSource(filteredSource, StackSource);

                XmlStackSourceWriter.WriteStackViewAsZippedXml(source, m_fileName, delegate (XmlWriter writer)
                {
                    GuiState.WriteToXml("StackWindowGuiState", writer);
                });
            }

            StatusBar.Log("Wrote stack view as " + Path.GetFullPath(m_fileName));
            if (m_ViewsShouldBeSaved)
            {
                m_ViewsShouldBeSaved = false;
                --GuiApp.MainWindow.NumWindowsNeedingSaving;
            }
        }
        private void DoOpenRegressionItem(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var baselineWindow = menuItem.Tag as StackWindow;

            var reportName = "Regression Report between " + Name + " and " + baselineWindow.Name;
            StatusBar.StartWork("Computing: " + reportName, delegate ()
            {
                var htmlReport = Path.Combine(CacheFiles.CacheDir, "OverweightAnalysis." + DateTime.Now.ToString("MM-dd.HH.mm.ss.fff") + ".html");
                OverWeigthReport.GenerateOverweightReport(htmlReport, this, baselineWindow);
                StatusBar.EndWork(delegate ()
                {
                    OverWeigthReport.ViewOverweightReport(htmlReport, reportName);
                });
            });
        }
        private void DoOpenDiffItem(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var baselineWindow = menuItem.Tag as StackWindow;

            var dataFile = new DiffPerfViewData(DataSource, baselineWindow.DataSource);
            var testFilter = new FilterParams(Filter);
            testFilter.MinInclusiveTimePercent = "";
            var baselineFilter = new FilterParams(baselineWindow.Filter);
            baselineFilter.MinInclusiveTimePercent = "";

            var stackWindow = new StackWindow(this, dataFile);
            if (DataSource.DataFile.SupportsProcesses)
            {
                stackWindow.GroupRegExTextBox.Items.Insert(0, "[group module entries]  ^Process% {%}->$1;^Thread->Thread;{%}!=>module $1");
                stackWindow.GroupRegExTextBox.Items.Insert(0, "[group modules]           ^Process% {%}->$1;^Thread->Thread;{%}!->module $1");
                stackWindow.GroupRegExTextBox.Items.Insert(0, "[Ignore PID/TID]          ^Process% {%}->$1;^Thread->Thread;");
                stackWindow.GroupRegExTextBox.Items.Insert(0, "[Ignore Paths]               ^Process% {%}->$1;^Thread->Thread;{%}!{*}->$1!$2");

                var osGroupings = @"^Process% {%}->$1;^Thread->Thread;\Temporary ASP.NET Files\->;v4.0.30319\%!=>CLR;v2.0.50727\%!=>CLR;mscoree=>CLR;\mscorlib.*!=>LIB;\System.Xaml.*!=>WPF;\System.*!=>LIB;Presentation%=>WPF;WindowsBase%=>WPF;system32\*!=>OS;syswow64\*!=>OS;{%}!=> module $1";
                stackWindow.GroupRegExTextBox.Items.Insert(0, "[group CLR/OS ignore paths] " + osGroupings + ";{%}!{*}->$1!$2");

                var defaultGroup = "[group CLR/OS entries] " + osGroupings;
                stackWindow.GroupRegExTextBox.Items.Insert(0, defaultGroup);
                stackWindow.GroupRegExTextBox.Text = defaultGroup;
                stackWindow.PriorityTextBox.Text = MemoryGraphStackSource.DefaultPriorities;
            }
            else
            {
                DataSource.DataFile.ConfigureStackWindow("", stackWindow);
            }

            stackWindow.GuiState = GuiState;

            stackWindow.Show();

            stackWindow.StatusBar.StartWork("Computing " + dataFile.Name, delegate ()
            {
                var source = InternStackSource.Diff(
                    new FilterStackSource(testFilter, StackSource, ScalingPolicy), StackSource,
                    new FilterStackSource(baselineFilter, baselineWindow.StackSource, ScalingPolicy), baselineWindow.StackSource);
                stackWindow.StatusBar.EndWork(delegate ()
                {
                    stackWindow.SetStackSource(source);
                });
            });
        }

        private void DoUpdate(object sender, RoutedEventArgs e) => Update();

        private void DoFindNext(object sender, RoutedEventArgs e) => FindNext();

        private void DoFindEnter(object sender, RoutedEventArgs e) => Find(FindTextBox.Text);

        // TODO_AVALONIA: ExecutedRoutedEventArgs not available
        private void DoCancel(object sender, RoutedEventArgs e) => StatusBar.AbortWork();

        private void DoToggleNoPadOnCopy(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            PerfDataGrid.NoPadOnCopyToClipboard = !PerfDataGrid.NoPadOnCopyToClipboard;
            StatusBar.Status = "No Pad On Copy is now " + PerfDataGrid.NoPadOnCopyToClipboard;
        }

        private bool GetSamplesForSelection(bool exclusiveSamples, out bool[] sampleSet, out string name)
        {
            name = "";
            sampleSet = null;

            var addedDots = false;
            Debug.Assert(CallTree.StackSource.BaseStackSource == m_stackSource);
            var localSampleSet = new bool[m_stackSource.SampleIndexLimit];

            var selectedNodes = GetSelectedNodes();
            foreach (var asCallTreeNodeBase in selectedNodes)
            {
                asCallTreeNodeBase.GetSamples(exclusiveSamples, delegate (StackSourceSampleIndex sampleIdx)
                {
                    Debug.Assert((int)sampleIdx < localSampleSet.Length);
                    Debug.Assert(!localSampleSet[(int)sampleIdx] || selectedNodes.Count > 1);
                    localSampleSet[(int)sampleIdx] = true;
                    return true;
                });
                if (name.Length == 0)
                {
                    name = (exclusiveSamples ? "Exc" : "Inc") + " of " + asCallTreeNodeBase.Name;
                }
                else if (!addedDots)
                {
                    name = name + "...";
                    addedDots = true;
                }
            }
            sampleSet = localSampleSet;
            return true;
        }

        // TODO_AVALONIA: CanExecuteRoutedEventArgs not available
        // private void CanExecuteMemoryOperation(...) { ... }
        // private void CanExecuteSamplesBasedOperation(...) { ... }

        // TODO_AVALONIA: ExecutedRoutedEventArgs → RoutedEventArgs for all command handlers below
        private void DoDrillInto(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: e.Parameter not available; need to determine exclusiveSamples differently
            bool exclusiveSamples = false;

            bool[] sampleSet;
            string sampleSetName;
            if (!GetSamplesForSelection(exclusiveSamples, out sampleSet, out sampleSetName))
            {
                return;
            }

            var drillIntoSamples = new CopyStackSource(m_stackSource);
            for (int i = 0; i < sampleSet.Length; i++)
            {
                if (sampleSet[i])
                {
                    drillIntoSamples.AddSample(m_stackSource.GetSampleByIndex((StackSourceSampleIndex)i));
                }
            }
            var newStackWindow = new StackWindow(this, this);
            newStackWindow.ExcludeRegExTextBox.Text = "";
            newStackWindow.IncludeRegExTextBox.Text = "";
            newStackWindow.Show();
            newStackWindow.SetStackSource(drillIntoSamples);
        }

        private void DoNewWindow(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            var newStackWindow = new StackWindow(this, this);

            newStackWindow.Show();
            newStackWindow.SetStackSource(StackSource);
        }
        private void DoFind(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            FindTextBox.Focus();
        }

        private void DoFindInByName(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            var displayName = GetSelectedNodes().Single().DisplayName;

            for (int i = 0; i < m_byNameView.Count; i++)
            {
                var item = m_byNameView[i];
                if (displayName == item.DisplayName)
                {
                    ByNameDataGrid.Grid.SelectedIndex = i;
                    try
                    {
                        ByNameDataGrid.Grid.ScrollIntoView(item, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Caught Exception while scrolling " + ex.ToString());
                    }
                    ByNameTab.IsSelected = true;
                    return;
                }
            }
        }

        private void DoFindInCallTreeName(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            var displayName = Regex.Escape(GetSelectedNodes().Single().DisplayName);

            CallTreeTab.IsSelected = true;
            FindTextBox.Text = displayName;
            CallTreeView.Find(displayName);
        }

        private void DoViewInCallerCallee(object sender, RoutedEventArgs e)
        {
            SetFocus(GetSelectedNodes().Single().Name);

            CallerCalleeTab.IsSelected = true;
        }
        private void DoViewInCallers(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            SetFocus(GetSelectedNodes().Single().Name);

            CallersTab.IsSelected = true;
        }
        private void DoViewInCallees(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            SetFocus(GetSelectedNodes().Single().Name);

            CalleesTab.IsSelected = true;
        }

        private void DoGroupModuleHelper(string op)
        {
            var str = GroupRegExTextBox.Text;
            var badStrs = "";
            foreach (var node in GetSelectedNodes())
            {
                Match moduleNameMatch = Regex.Match(node.DisplayName, @"\b([\w.]*?)!");
                if (moduleNameMatch.Success)
                {
                    var groupPat = FilterParams.EscapeRegEx(moduleNameMatch.Groups[1].Value) + "!" + op + moduleNameMatch.Groups[1].Value;
                    str = AddSet(groupPat, str);
                }
                else
                {
                    if (badStrs.Length > 0)
                    {
                        badStrs += " ";
                    }
                    badStrs += node.DisplayName;
                }
            }
            if (badStrs.Length > 0)
            {
                StatusBar.LogError("Could not find a module pattern in text " + badStrs + ".");
            }

            if (GroupRegExTextBox.Text != str)
            {
                GroupRegExTextBox.Text = str;
                Update();
            }
        }

        private void DoCopyTimeRange(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            // TODO_AVALONIA: Use TopLevel.Clipboard.SetTextAsync()
            // Clipboard.SetText(RangeUtilities.ToString(StartTextBox.Text, EndTextBox.Text));
        }

        private void DoSetTimeRange(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            // TODO_AVALONIA: Keyboard.FocusedElement not available in Avalonia
            // The text box focus detection and histogram selection logic needs Avalonia-specific implementation
            var callTreeNodes = GetSelectedNodes();
            if (callTreeNodes.Any())
            {
                StartTextBox.Text = callTreeNodes.Min(node => node.FirstTimeRelativeMSec).ToString("n3");
                EndTextBox.Text = callTreeNodes.Max(node => node.LastTimeRelativeMSec).ToString("n3");
                Update();
            }
        }

        private CallTreeNodeBase ToCallTreeNodeBase(object viewOrDataObject)
        {
            var asViewNode = viewOrDataObject as CallTreeViewNode;
            if (asViewNode != null)
            {
                return asViewNode.Data;
            }

            return viewOrDataObject as CallTreeNodeBase;
        }

        private void DoIncludeItem(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            var incPat = "";
            foreach (var node in GetSelectedNodes())
            {
                if (incPat.Length != 0)
                {
                    incPat += "|";
                }

                var pat = node.DisplayName;
                if (pat.IndexOf('!') < 0)
                {
                    pat = "^" + pat;
                }

                incPat += FilterParams.EscapeRegEx(pat);
            }

            IncludeRegExTextBox.Text = AddSet(IncludeRegExTextBox.Text, incPat);
            Update();
        }
        private void DoExcludeItem(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            var str = ExcludeRegExTextBox.Text;
            foreach (var node in GetSelectedNodes())
            {
                var pat = node.DisplayName;
                if (pat.IndexOf('!') < 0)
                {
                    pat = "^" + pat;
                }

                str = AddSet(str, FilterParams.EscapeRegEx(pat));
            }
            ExcludeRegExTextBox.Text = str;
            Update();
        }
        private void DoCopyFilterParams(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings() { Indent = true, NewLineOnAttributes = true }))
            {
                FilterGuiState.WriteToXml("FilterGuiState", writer);
            }

            // TODO_AVALONIA: Use TopLevel.Clipboard.SetTextAsync()
            // Clipboard.SetText(sb.ToString());
        }
        private void DoMergeFilterParams(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            // TODO_AVALONIA: Use TopLevel.Clipboard.GetTextAsync()
            // string text = Clipboard.GetText();
            string text = ""; // placeholder

            XmlReaderSettings settings = new XmlReaderSettings() { IgnoreWhitespace = true, IgnoreComments = true };
            XmlReader reader = XmlReader.Create(new StringReader(text), settings);
            var filterGuiState = new FilterGuiState();
            filterGuiState.ReadFromXml(reader);

            FilterGuiState = filterGuiState;
            Update();
        }

        private void DoPri1Only(object sender, RoutedEventArgs e)
        {
            var isChecked = (Pri1OnlyCheckBox.IsChecked ?? false);
            m_callTreeView.DisplayPrimaryOnly = isChecked;
            m_callersView.DisplayPrimaryOnly = isChecked;
            m_calleesView.DisplayPrimaryOnly = isChecked;
            Update();
        }

        private void DoHyperlinkHelp(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            // TODO_AVALONIA: e.Parameter not available
            var param = "StackViewerQuickStart";

            if (DataSource.DataFile is ClrProfilerHeapPerfViewFile ||
                DataSource.DataFile is HeapDumpPerfViewFile)
            {
                if (param == "StartingAnAnalysis" || param == "UnderstandingPerfData" || param == "StackViewerQuickStart" || param == "Tutorial")
                {
                    param += "GCHeap";
                }
            }

            StatusBar.Log("Displaying Users Guide in Web Browser.");
            MainWindow.DisplayUsersGuide(param);
        }

        private void ByName_MouseDoubleClick(object sender, TappedEventArgs e) // TODO_AVALONIA: was MouseButtonEventArgs
        {
            e.Handled = true;

            if (GetSelectedNodes().Count == 1)
            {
                DoViewInCallers(sender, null);
            }
        }

        internal void DataGrid_MouseDoubleClick(object sender, TappedEventArgs e) // TODO_AVALONIA: was MouseButtonEventArgs
        {
            // TODO_AVALONIA: HitTest/VisualTreeHelper not available the same way
            // Need to use Avalonia's visual tree APIs to find the "Name" TextBlock
            // var uiElement = sender as Control;
            // For now, try to get selected item name
            var dataGrid = sender as PerfDataGrid;
            if (dataGrid != null)
            {
                var selectedItem = dataGrid.Grid.SelectedItem;
                var node = ToCallTreeNodeBase(selectedItem);
                if (node != null)
                {
                    SetFocus(node.Name);
                }
            }
        }

        private void Notes_GotFocus(object sender, RoutedEventArgs e)
        {
            HelpMessage.IsVisible = false; // TODO_AVALONIA: was Visibility.Hidden
        }
        public bool NotesPaneHidden
        {
            get { return m_NotesPaneHidden; }
            set
            {
                if (value == m_NotesPaneHidden)
                {
                    return;
                }

                if (value)
                {
                    App.UserConfigData["NotesPaneHidden"] = "true";
                    m_NotesPaneHidden = true;
                    NodePaneRowDef.MaxHeight = 0;
                }
                else
                {
                    App.UserConfigData["NotesPaneHidden"] = "false";
                    m_NotesPaneHidden = false;
                    NodePaneRowDef.MaxHeight = Double.PositiveInfinity;
                }
            }
        }

        private bool m_NotesPaneHidden;

        /// <summary>
        /// This is whether the sample being shown represent time and thus should be divided up or not.  
        /// </summary>
        public ScalingPolicyKind ScalingPolicy;

        private void DoToggleNotesPane(object sender, RoutedEventArgs e) // TODO_AVALONIA: was ExecutedRoutedEventArgs
        {
            NotesPaneHidden = !NotesPaneHidden;
        }
        private bool m_ViewsShouldBeSaved;
        private void Notes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_IgnoreNotesChange)
            {
                return;
            }

            if (!m_ViewsShouldBeSaved)
            {
                m_ViewsShouldBeSaved = true;
                GuiApp.MainWindow.NumWindowsNeedingSaving++;
            }
        }

        private bool m_NotesTabActive;
        private bool m_IgnoreNotesChange;
        private void NotesTab_GotFocus(object sender, RoutedEventArgs e)
        {
            m_IgnoreNotesChange = true;
            NotesTabBody.Text = Notes.Text;
            m_IgnoreNotesChange = false;

            m_NotesTabActive = true;
            NodePaneRowDef.MaxHeight = 0;
        }

        private void NotesTab_LostFocus(object sender, RoutedEventArgs e)
        {
            m_IgnoreNotesChange = true;
            Notes.Text = NotesTabBody.Text;
            m_IgnoreNotesChange = false;

            if (!m_NotesPaneHidden)
            {
                NodePaneRowDef.MaxHeight = Double.PositiveInfinity;
            }

            m_NotesTabActive = false;
        }

        private bool m_RedrawFlameGraphWhenItBecomesVisible = false;

        private void FlameGraphTab_GotFocus(object sender, RoutedEventArgs e)
        {
            if (FlameGraphCanvas.IsEmpty || m_RedrawFlameGraphWhenItBecomesVisible)
            {
                RedrawFlameGraph();
            }
        }

        private void FlameGraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => RedrawFlameGraphIfVisible();

        private void RedrawFlameGraphIfVisible()
        {
            if (FlameGraphTab.IsSelected)
            {
                RedrawFlameGraph();
            }
            else
            {
                m_RedrawFlameGraphWhenItBecomesVisible = true;
            }
        }

        private void RedrawFlameGraph()
        {
            FlameGraphCanvas.Draw(
                  CallTree.Root.HasChildren
                      ? FlameGraph.Calculate(CallTree, FlameGraphCanvas.Bounds.Width, FlameGraphCanvas.Bounds.Height) // TODO_AVALONIA: ActualWidth/Height → Bounds.Width/Height
                      : Enumerable.Empty<FlameGraph.FlameBox>());

            m_RedrawFlameGraphWhenItBecomesVisible = false;
        }

        private void FlameGraphCanvas_CurrentFlameBoxChanged(object sender, string toolTipText)
        {
            if (StatusBar.LoggedError)
            {
                return;
            }

            StatusBar.Status = toolTipText;
        }

        private void DoSaveFlameGraph(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: SaveFileDialog API differs in Avalonia
            StatusBar.LogError("Save flame graph not yet implemented for Avalonia");
        }

        private TabItem SelectedTab
        {
            get
            {
                if (ByNameTab.IsSelected)
                {
                    return ByNameTab;
                }
                else if (CallerCalleeTab.IsSelected)
                {
                    return CallerCalleeTab;
                }
                else if (CallTreeTab.IsSelected)
                {
                    return CallTreeTab;
                }
                else if (CallersTab.IsSelected)
                {
                    return CallersTab;
                }
                else if (CalleesTab.IsSelected)
                {
                    return CalleesTab;
                }
                else if (FlameGraphTab.IsSelected)
                {
                    return FlameGraphTab;
                }
                else if (NotesTab.IsSelected)
                {
                    return NotesTab;
                }

                Debug.Assert(false, "No tab selected!");
                return null;
            }
        }

        public StackWindowGuiState GuiState
        {
            get
            {
                var ret = new StackWindowGuiState();

                ret.FilterGuiState = FilterGuiState;

                ret.Notes = m_NotesTabActive ? NotesTabBody.Text : Notes.Text;

                var logText = Regex.Replace(StatusBar.LogWindow.TextEditor.Text, @"[^ -~\s]", "");
                ret.Log = logText;

                var selectedTab = SelectedTab;
                if (selectedTab != null)
                {
                    ret.TabSelected = SelectedTab.Name;
                }

                ret.FocusName = FocusName;

                var columns = new List<string>(ByNameDataGrid.Grid.Columns.Count);
                foreach (var column in ByNameDataGrid.Grid.Columns)
                {
                    var name = ((TextBlock)column.Header).Name;
                    columns.Add(name);
                }
                ret.Columns = columns;
                ret.NotesPaneHidden = NotesPaneHidden;
                ret.ScalingPolicy = ScalingPolicy;

                return ret;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value.Notes))
                {
                    m_IgnoreNotesChange = true;
                    Notes.Text = value.Notes;
                }

                if (!string.IsNullOrWhiteSpace(value.Log))
                {
                    StatusBar.Log("********** The following is the log file that was caputured when the stack view was saved *************");
                    StatusBar.Log(value.Log);
                    StatusBar.Log("********** End of the saved log *************");
                }

                if (value.TabSelected != null)
                {
                    switch (value.TabSelected)
                    {
                        case nameof(ByNameTab):
                            ByNameTab.IsSelected = true;
                            break;
                        case nameof(CallerCalleeTab):
                            CallerCalleeTab.IsSelected = true;
                            break;
                        case nameof(CallTreeTab):
                            CallTreeTab.IsSelected = true;
                            break;
                        case nameof(CalleesTab):
                            CalleesTab.IsSelected = true;
                            break;
                        case nameof(CallersTab):
                            CallersTab.IsSelected = true;
                            break;
                        case nameof(FlameGraphTab):
                            FlameGraphTab.IsSelected = true;
                            break;
                        case nameof(NotesTab):
                            NotesTab.IsSelected = true;
                            break;
                    }
                }

                if (m_callTree != null)
                {
                    if (value.FocusName != null)
                    {
                        SetFocus(value.FocusName);
                    }
                }

                if (value.Columns != null)
                {
                    foreach (var columnName in ByNameDataGrid.ColumnNames())
                    {
                        if (!value.Columns.Contains(columnName) && columnName != "NameColumn")
                        {
                            RemoveColumn(columnName);
                        }
                    }
                }

                NotesPaneHidden = value.NotesPaneHidden;
                ScalingPolicy = value.ScalingPolicy;
            }
        }

        /// <summary>
        /// Intended to be called from ConfigureStackWindow
        /// </summary>
        public string ExtraTopStats { get; set; }

        public bool ComputeMaxInTopStats;

        #region commandDefintions
        // TODO_AVALONIA: RoutedUICommand not available in Avalonia. All command definitions below need to be
        // converted to ICommand implementations (e.g. RelayCommand or ReactiveCommand).
        // The InputGestureCollection/KeyGesture bindings should be converted to KeyBindings in XAML or code.
        /*
        public static RoutedUICommand UsersGuideCommand = ...;
        public static RoutedUICommand SaveCommand = ...;
        public static RoutedUICommand SaveAsCommand = ...;
        public static RoutedUICommand SaveFlameGraphCommand = ...;
        public static RoutedUICommand CancelCommand = ...;
        public static RoutedUICommand UpdateCommand = ...;
        public static RoutedUICommand NewWindowCommand = ...;
        public static RoutedUICommand DrillIntoInclusiveCommand = ...;
        public static RoutedUICommand DrillIntoExclusiveCommand = ...;
        public static RoutedUICommand FlattenCommand = ...;
        public static RoutedUICommand FindCommand = ...;
        public static RoutedUICommand FindNextCommand = ...;
        public static RoutedUICommand ViewInCallerCalleeCommand = ...;
        public static RoutedUICommand ViewInCallersCommand = ...;
        public static RoutedUICommand ViewInCalleesCommand = ...;
        public static RoutedUICommand FindInCallTreeCommand = ...;
        public static RoutedUICommand FindInByNameCommand = ...;
        public static RoutedUICommand OpenEventsCommand = ...;
        public static RoutedUICommand GroupModuleCommand = ...;
        public static RoutedUICommand EntryGroupModuleCommand = ...;
        public static RoutedUICommand UngroupCommand = ...;
        public static RoutedUICommand UngroupModuleCommand = ...;
        public static RoutedUICommand FoldModuleCommand = ...;
        public static RoutedUICommand FoldItemCommand = ...;
        public static RoutedUICommand RemoveAllFoldingCommand = ...;
        public static RoutedUICommand RaiseItemPriorityCommand = ...;
        public static RoutedUICommand LowerItemPriorityCommand = ...;
        public static RoutedUICommand RaiseModulePriorityCommand = ...;
        public static RoutedUICommand LowerModulePriorityCommand = ...;
        public static RoutedUICommand LookupSymbolsCommand = ...;
        public static RoutedUICommand LookupWarmSymbolsCommand = ...;
        public static RoutedUICommand GotoSourceCommand = ...;
        public static RoutedUICommand SetTimeRangeCommand = ...;
        public static RoutedUICommand CopyTimeRangeCommand = ...;
        public static RoutedUICommand SetScenarioListCommand = ...;
        public static RoutedUICommand CopyScenarioListCommand = ...;
        public static RoutedUICommand CopyScenarioListNamesCommand = ...;
        public static RoutedUICommand SortScenariosByDefaultCommand = ...;
        public static RoutedUICommand SortScenariosByRootNodeCommand = ...;
        public static RoutedUICommand SortScenariosByThisNodeCommand = ...;
        public static RoutedUICommand IncludeItemCommand = ...;
        public static RoutedUICommand ExcludeItemCommand = ...;
        public static RoutedUICommand ToggleNoPadOnCopyCommand = ...;
        public static RoutedUICommand ViewObjectsInclusiveCommand = ...;
        public static RoutedUICommand ViewObjectsExclusiveCommand = ...;
        public static RoutedUICommand DumpObjectCommand = ...;
        public static RoutedUICommand ToggleNotesPaneCommand = ...;
        public static RoutedUICommand CopyFilterParamsCommand = ...;
        public static RoutedUICommand MergeFilterParamsCommand = ...;
        public static RoutedUICommand ExpandAllCommand = ...;
        public static RoutedUICommand ExpandCommand = ...;
        public static RoutedUICommand CollapseCommand = ...;
        public static RoutedUICommand SetBackgroundColorCommand = ...;
        public static RoutedUICommand FoldPercentCommand = ...;
        public static RoutedUICommand IncreaseFoldPercentCommand = ...;
        public static RoutedUICommand DecreaseFoldPercentCommand = ...;
        */
        #endregion
        #region private
        private void FinishInit()
        {
            DataContext = this;

            // Customize the control
            ByNameDataGrid.Grid.CanUserSortColumns = true;
            var columns = ByNameDataGrid.Grid.Columns;

            // Put the exclusive columns first if they are not already there.  
            var col = ByNameDataGrid.GetColumnIndex("ExcPercentColumn");
            if (0 <= col && col != 1)
            {
                ByNameDataGrid.Grid.Columns.Move(col, 1);
            }

            col = ByNameDataGrid.GetColumnIndex("ExcColumn");
            if (0 <= col && col != 2)
            {
                ByNameDataGrid.Grid.Columns.Move(col, 2);
            }

            col = ByNameDataGrid.GetColumnIndex("ExcCountColumn");
            if (0 <= col && col != 3)
            {
                ByNameDataGrid.Grid.Columns.Move(col, 3);
            }

            // Initialize the CallTree, Callers, and Callees tabs
            var template = (DataTemplate)Resources["TreeControlCell"];
            m_callTreeView = new CallTreeView(CallTreeDataGrid, template);
            m_callersView = new CallTreeView(CallersDataGrid, template);
            m_calleesView = new CallTreeView(CalleesDataGrid, template);

            List<PerfDataGrid> perfDataGrids = new List<PerfDataGrid>(4)
            {
                ByNameDataGrid,
                CallTreeDataGrid,
                CallersDataGrid,
                CalleesDataGrid
            };

            // Populate ViewMenu items for showing/hiding columns
            PopulateViewMenuWithPerfDataGridItems(perfDataGrids);

            // Make up a trivial call tree (so that the rest of the code works).  
            m_callTree = new CallTree(ScalingPolicy);

            // Configure the Preset menu (add standard commands and known presets)
            ConfigurePresetMenu();

            StackWindows.Add(this);

            // TODO_AVALONIA: IsVisibleChanged → use PropertyChanged or override OnPropertyChanged
            // IsVisibleChanged += delegate { UpdateDiffMenus(StackWindows); };
            Closing += delegate (object sender, CancelEventArgs e)
            {
                if (StatusBar.IsWorking)
                {
                    StatusBar.LogError("Cancel work before closing window.");
                    e.Cancel = true;
                    return;
                }

                if (m_ViewsShouldBeSaved)
                {
                    // TODO_AVALONIA: MessageBox not available — use Avalonia dialog
                    // var result = XamlMessageBox.Show(...);
                }

                // TODO_AVALONIA: WindowState not directly the same in Avalonia
                // Save window position
                if (StackWindows.Count > 0 && StackWindows[0] == this)
                {
                    App.UserConfigData["StackWindowTop"] = Bounds.Top.ToString("f0", CultureInfo.InvariantCulture);
                    App.UserConfigData["StackWindowLeft"] = Bounds.Left.ToString("f0", CultureInfo.InvariantCulture);
                    App.UserConfigData["StackWindowWidth"] = Bounds.Width.ToString("f0", CultureInfo.InvariantCulture);
                    App.UserConfigData["StackWindowHeight"] = Bounds.Height.ToString("f0", CultureInfo.InvariantCulture);
                }

                StackWindows.Remove(this);
                UpdateDiffMenus(StackWindows);
                if (DataSource != null)
                {
                    DataSource.ViewClosing(this);
                }
                DoOpenParent(null, null);

                if (m_callTree != null)
                {
                    m_callTree.FreeMemory();
                }

                if (m_callTreeView != null)
                {
                    m_calleesView.Dispose();
                }
            };
            // TODO_AVALONIA: PreviewMouseDoubleClick → DoubleTapped
            TopStats.DoubleTapped += delegate (object sender, TappedEventArgs e)
            {
                e.Handled = StatusBar.ExpandSelectionByANumber(TopStats);
                return;
            };

            if (m_ViewsShouldBeSaved)
            {
                m_ViewsShouldBeSaved = false;
                --GuiApp.MainWindow.NumWindowsNeedingSaving;
            }

            NotesPaneHidden = (App.UserConfigData["NotesPaneHidden"] == "true");

            if (StackWindows.Count == 1)
            {
                var top = App.UserConfigData.GetDouble("StackWindowTop", Bounds.Top);
                // TODO_AVALONIA: SystemParameters not available — use Screens API
                // Top = Math.Min(Math.Max(top, 0), SystemParameters.PrimaryScreenHeight - 200);

                Height = App.UserConfigData.GetDouble("StackWindowHeight", Height);
                Width = App.UserConfigData.GetDouble("StackWindowWidth", Width);
            }
        }

        /// <summary>
        /// Populate the View MenuItem
        /// </summary>
        private void PopulateViewMenuWithPerfDataGridItems(List<PerfDataGrid> perfDataGrids)
        {
            List<Tuple<string, MenuItem>> perfDataGridMenuItems = new List<Tuple<string, MenuItem>>();

            foreach (PerfDataGrid perfDataGrid in perfDataGrids)
            {
                foreach (DataGridColumn col in perfDataGrid.Grid.Columns)
                {
                    MenuItem menuItem = null;

                    IEnumerable<Tuple<string, MenuItem>> temp = perfDataGridMenuItems.Where(x => x.Item1 == ((TextBlock)col.Header).Text);

                    if (temp.Count() == 0)
                    {
                        menuItem = new MenuItem()
                        {
                            // TODO_AVALONIA: IsCheckable not available on MenuItem in Avalonia the same way
                            // IsCheckable = true
                        };

                        string header = ((TextBlock)col.Header).Text;
                        menuItem.Header = header;

                        string configValue = App.UserConfigData[XmlConvert.EncodeName(header + "ColumnView")];
                        if (configValue == null || configValue == "1")
                        {
                            // menuItem.IsChecked = true; // TODO_AVALONIA
                            col.IsVisible = true; // TODO_AVALONIA: was Visibility = Visibility.Visible
                        }
                        else
                        {
                            // menuItem.IsChecked = false; // TODO_AVALONIA
                            col.IsVisible = false; // TODO_AVALONIA: was Visibility = Visibility.Collapsed
                        }

                        perfDataGridMenuItems.Add(new Tuple<string, MenuItem>(header, menuItem));
                        ViewMenu.Items.Add(menuItem);
                    }
                    else
                    {
                        menuItem = temp.First().Item2;
                    }

                    menuItem.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        // TODO_AVALONIA: Toggle column visibility
                        col.IsVisible = !col.IsVisible;
                    };
                }
            }

            ViewMenu.Items.Add(new Separator());

            string saveViewSettingsStr = "Save View Settings";
            MenuItem saveSettings = new MenuItem()
            {
                Header = saveViewSettingsStr,
            };

            saveSettings.Click += delegate (object sender, RoutedEventArgs e)
            {
                for (int i = 0; i < ViewMenu.Items.Count; i++)
                {
                    if (ViewMenu.Items[i] is MenuItem)
                    {
                        MenuItem mItem = ViewMenu.Items[i] as MenuItem;
                        string header = mItem.Header.ToString();
                        if (header == saveViewSettingsStr)
                        {
                            continue;
                        }

                        string name = XmlConvert.EncodeName(header + "ColumnView");
                        // TODO_AVALONIA: IsChecked not available
                        // App.UserConfigData[name] = mItem.IsChecked ? "1" : "0";
                    }
                }
            };

            ViewMenu.Items.Add(saveSettings);
        }

        private bool DoForSelectedModules(Action<string> moduleAction)
        {
            var badStrs = "";
            foreach (var node in GetSelectedNodes())
            {
                Match m = Regex.Match(node.DisplayName, @"([ \w.-]+)!");
                if (m.Success)
                {
                    moduleAction(m.Groups[1].Value);
                }
                else
                {
                    m = Regex.Match(node.DisplayName, @"^module ([ \w.-]+)");
                    if (m.Success)
                    {
                        moduleAction(m.Groups[1].Value);
                    }
                    else
                    {
                        if (badStrs.Length > 0)
                        {
                            badStrs += " ";
                        }

                        badStrs += node.DisplayName;
                    }
                }
            }
            if (badStrs.Length > 0)
            {
                StatusBar.LogError("Could not find a module pattern in text " + badStrs + ".");
                return false;
            }
            return true;
        }

        private static string FindExePath(StackSource stackSource, string procName)
        {
            if (procName == null)
            {
                return null;
            }

            while (stackSource.BaseStackSource != stackSource)
            {
                stackSource = stackSource.BaseStackSource;
            }

            for (int frameidx = (int)StackSourceFrameIndex.Start; frameidx < stackSource.CallFrameIndexLimit; frameidx++)
            {
                var frameName = stackSource.GetFrameName((StackSourceFrameIndex)frameidx, true);
                var match = Regex.Match(frameName, @"^([^!]*\\(.*?))!");
                if (match.Success)
                {
                    if (string.Compare(match.Groups[2].Value, procName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return null;
        }

        private bool ValidateStartAndEnd(StackSource newSource)
        {
            double start = 0, end = double.PositiveInfinity;
            if (string.IsNullOrWhiteSpace(EndTextBox.Text))
            {
                double limit = newSource.SampleTimeRelativeMSecLimit;
                if (limit != 0)
                {
                    EndTextBox.Text = (newSource.SampleTimeRelativeMSecLimit).ToString("n3");
                }
            }
            else if (!double.TryParse(EndTextBox.Text, out end))
            {
                StatusBar.LogError("Invalid number " + EndTextBox.Text);
                EndTextBox.Text = "Infinity";
                return false;
            }
            else
            {
                EndTextBox.Text = end.ToString("n3");
            }

            if (double.TryParse(StartTextBox.Text, out start))
            {
                StartTextBox.Text = start.ToString("n3");
            }
            else if (string.IsNullOrWhiteSpace(StartTextBox.Text))
            {
                StartTextBox.Text = "0";
            }
            else
            {
                if (RangeUtilities.TryParse(StartTextBox.Text, out start, out end))
                {
                    StartTextBox.Text = start.ToString("n3");
                    EndTextBox.Text = end.ToString("n3");
                }
                else
                {
                    StatusBar.LogError("Invalid number " + StartTextBox.Text);
                    StartTextBox.Text = "0";
                    return false;
                }
            }

            if (end < start)
            {
                var str = StartTextBox.Text;
                StartTextBox.Text = EndTextBox.Text;
                EndTextBox.Text = str;
            }
            return true;
        }

        private bool FindNext(string pat)
        {
            StatusBar.Status = "";
            bool ret = true;

            if (ByNameTab.IsSelected)
            {
                ret = ByNameDataGrid.Find(pat);
            }
            else if (CallTreeTab.IsSelected)
            {
                ret = CallTreeView.Find(pat);
            }
            else if (CallerCalleeTab.IsSelected)
            {
                ret = CallerCalleeView.Find(pat);
            }
            else if (CallersTab.IsSelected)
            {
                ret = m_callersView.Find(pat);
            }
            else if (CalleesTab.IsSelected)
            {
                ret = m_calleesView.Find(pat);
            }
            else
            {
                StatusBar.LogError("Find not support on this tab.");
                return false;
            }

            if (!ret)
            {
                StatusBar.LogError("Could not find " + pat + ".");
            }

            return ret;
        }

        private readonly static CallTreeNodeBase[] _emptyNodes = new CallTreeNodeBase[0];

        private IReadOnlyList<CallTreeNodeBase> GetSelectedNodes()
        {
            if (FlameGraphTab.IsSelected)
            {
                if (FlameGraphCanvas.SelectedNode != null)
                    return new[] { FlameGraphCanvas.SelectedNode };

                return _emptyNodes;
            }

            var dataGrid = GetDataGrid();
            if (dataGrid != null)
            {
                // TODO_AVALONIA: DataGrid.SelectedCells not available
                // Using SelectedItem instead
                var selectedItem = dataGrid.SelectedItem;
                if (selectedItem != null)
                {
                    if (selectedItem is CallTreeNodeBase nodeBase)
                        return new[] { nodeBase };
                    else if (selectedItem is CallTreeViewNode callTreeNode)
                        return new[] { callTreeNode.Data };
                }

                return _emptyNodes;
            }

            return _emptyNodes;
        }

        private DataGrid GetDataGrid()
        {
            if (ByNameTab.IsSelected)
                return ByNameDataGrid.Grid;
            else if (CallerCalleeTab.IsSelected)
            {
                // TODO_AVALONIA: FocusManager.GetFocusedElement differs in Avalonia
                // For now return null — needs Avalonia-specific focus detection
                return null;
            }
            else if (CallTreeTab.IsSelected)
                return CallTreeDataGrid.Grid;
            else if (CallersTab.IsSelected)
                return CallersDataGrid.Grid;
            else if (CalleesTab.IsSelected)
                return CalleesDataGrid.Grid;

            return null;
        }

        // We keep a list of stack windows for use with the 'Diff' feature.  
        public static List<StackWindow> StackWindows = new List<StackWindow>();

        private static void UpdateDiffMenus(List<StackWindow> stackWindows)
        {
            foreach (var stackWindow in stackWindows)
            {
                UpdateDiffMenu("Diff", stackWindow.DiffMenu, stackWindow.DoOpenDiffItem, stackWindow, stackWindows);
                UpdateDiffMenu("Regression", stackWindow.RegressionMenu, stackWindow.DoOpenRegressionItem, stackWindow, stackWindows);
            }
        }

        private static void UpdateDiffMenu(string diffName, MenuItem diffMenuItem, Action<object, RoutedEventArgs> onSelectAction, StackWindow stackWindow, List<StackWindow> stackWindows)
        {
            diffMenuItem.Items.Clear();
            foreach (var menuEntry in stackWindows)
            {
                if (menuEntry == stackWindow)
                {
                    continue;
                }

                var childMenuItem = new MenuItem();
                childMenuItem.Header = "With Baseline: " + menuEntry.Title;
                childMenuItem.Tag = menuEntry;
                childMenuItem.Click += new EventHandler<RoutedEventArgs>(onSelectAction);
                diffMenuItem.Items.Add(childMenuItem);
            }

            var helpMenuItem = new MenuItem();
            helpMenuItem.Header = "Help for " + diffName;
            helpMenuItem.Click += delegate (object sender, RoutedEventArgs e) { MainWindow.DisplayUsersGuide(diffName); };
            diffMenuItem.Items.Add(helpMenuItem);
        }

        private void ConfigurePresetMenu()
        {
            var presets = App.UserConfigData["Presets"];
            m_presets = Preset.ParseCollection(presets);

            foreach (var preset in m_presets)
            {
                var presetMenuItem = new MenuItem();
                presetMenuItem.Header = preset.Name;
                presetMenuItem.Tag = preset.Name;
                presetMenuItem.Click += DoSelectPreset;
                PresetMenu.Items.Add(presetMenuItem);
            }

            PresetMenu.Items.Add(new Separator());

            var setDefaultPresetMenuItem = new MenuItem();
            setDefaultPresetMenuItem.Header = "S_et As Startup Preset";
            setDefaultPresetMenuItem.Click += DoSetStartupPreset;
            setDefaultPresetMenuItem.ToolTip = // TODO_AVALONIA: should be ToolTip.Tip or use a property setter
                "Sets the default values of Group Patterns and Fold Patterns and % to the current values.";
            PresetMenu.Items.Add(setDefaultPresetMenuItem);

            var newPresetMenuItem = new MenuItem();
            newPresetMenuItem.Header = "_Save As Preset";
            newPresetMenuItem.Click += DoSaveAsPreset;
            PresetMenu.Items.Add(newPresetMenuItem);

            var managePresetsMenuItem = new MenuItem();
            managePresetsMenuItem.Header = "_Manage Presets";
            managePresetsMenuItem.Click += DoManagePresets;
            PresetMenu.Items.Add(managePresetsMenuItem);

            var helpMenuItem = new MenuItem();
            helpMenuItem.Header = "_Help for Preset";
            helpMenuItem.Click += delegate { MainWindow.DisplayUsersGuide("Preset"); };
            PresetMenu.Items.Add(helpMenuItem);
        }

        private void DoUpdatePresetMenu()
        {
            while (!(PresetMenu.Items[0] is Separator))
            {
                PresetMenu.Items.RemoveAt(0);
            }
            m_presets.Sort((x, y) => Comparer<string>.Default.Compare(y.Name, x.Name));
            foreach (var preset in m_presets)
            {
                var presetMenuItem = new MenuItem();
                presetMenuItem.Header = preset.Name;
                presetMenuItem.Tag = preset.Name;
                presetMenuItem.Click += DoSelectPreset;
                PresetMenu.Items.Insert(0, presetMenuItem);
            }
        }

        private void DoSaveAsPreset(object sender, RoutedEventArgs e)
        {
            string groupPat = GroupRegExTextBox.Text.Trim();
            string nameCandidate = "Preset " + (m_presets.Count + 1).ToString();
            if (groupPat.Length > 0 && groupPat[0] == '[')
            {
                int closingBracketIndex = groupPat.IndexOf(']');
                if (closingBracketIndex > 0)
                {
                    nameCandidate = groupPat.Substring(1, closingBracketIndex - 1);
                    groupPat = groupPat.Substring(closingBracketIndex + 1).Trim();
                }
            }

            // TODO_AVALONIA: NewPresetDialog needs Avalonia conversion
            var newPresetDialog = new NewPresetDialog(this, nameCandidate, m_presets.Select(x => x.Name).ToList());
            // TODO_AVALONIA: Owner property and ShowDialog differ in Avalonia
            // newPresetDialog.Owner = this;
            // if (!(newPresetDialog.ShowDialog() ?? false)) { return; }

            // Placeholder — dialog result handling needs Avalonia async pattern
            return;
        }

        private void DoManagePresets(object sender, RoutedEventArgs e)
        {
            // TODO_AVALONIA: ManagePresetsDialog needs Avalonia conversion
            // var managePresetsDialog = new ManagePresetsDialog(this, m_presets, ...);
            // managePresetsDialog.ShowDialog();
        }

        private void DoSelectPreset(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            string presetName = menuItem.Tag as string;

            var preset = m_presets.Find(x => x.Name == presetName);
            GroupRegExTextBox.AddToHistory($"[{preset.Name}] {preset.GroupPat}");
            FoldPercentTextBox.AddToHistory(preset.FoldPercentage);
            FoldRegExTextBox.AddToHistory(preset.FoldPat);
            Update();
        }

        private static string AddSet(string target, string addend)
        {
            if (target.Length == 0)
            {
                return addend;
            }

            if (addend.Length == 0)
            {
                return target;
            }

            var match = Regex.Match(target, @"(^\s*(\[.*?\])?\s*)(.*?)\s*$");
            var comment = match.Groups[1].Value;
            target = match.Groups[3].Value;
            int pos = 0;
            for (; ; )
            {
                int index = target.IndexOf(addend, pos);
                if (index < 0)
                {
                    break;
                }

                int next = index + addend.Length;
                if ((index == 0 || target[index] == ';') &&
                    (next == target.Length || target[next] == ';'))
                {
                    return target;
                }

                pos = next + 1;
                if (pos >= target.Length)
                {
                    break;
                }
            }
            return comment + target + ";" + addend;
        }

        // TODO_AVALONIA: SelectedCellsChangedEventArgs not available
        internal int SelectedCellsChanged(object sender, EventArgs e) // TODO_AVALONIA: was SelectedCellsChangedEventArgs
        {
            var dataGrid = sender as DataGrid;

            // TODO_AVALONIA: DataGrid.SelectedCells not available
            // Returning 0 as placeholder
            StatusBar.Status = "";
            return 0;
        }

        internal void RestoreWindow(StackWindowGuiState guiState, string fileName)
        {
            if (fileName != null)
            {
                m_fileName = fileName;
            }

            if (guiState != null)
            {
                FilterGuiState = guiState.FilterGuiState;
            }

            if (m_ViewsShouldBeSaved)
            {
                m_ViewsShouldBeSaved = false;
                --GuiApp.MainWindow.NumWindowsNeedingSaving;
            }
        }

        private StackSource m_stackSource;
        internal CallTree m_callTree;

        private List<FilterParams> m_history;
        private int m_historyPos;
        private bool m_settingFromHistory;
        private bool m_fixedUpJustMyCode;

        internal List<CallTreeNodeBase> m_byNameView;

        internal CallTreeView m_callTreeView;
        internal CallTreeView m_calleesView;
        internal CallTreeView m_callersView;

        private string m_fileName;

        private List<Preset> m_presets;

        #endregion
    }
}