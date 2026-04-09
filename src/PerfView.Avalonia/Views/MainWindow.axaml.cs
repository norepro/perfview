using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PerfView.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    internal void DoRun(object sender, RoutedEventArgs e)
    {
        ChangeCurrentDirectoryIfNeeded();
        // CollectWindow = new RunCommandDialog(App.CommandLineArgs, this, false, TryOpenDataFile);
        // CollectWindow.Show();
    }

    internal void DoCollect(object sender, RoutedEventArgs e)
    {
        ChangeCurrentDirectoryIfNeeded();
        // CollectWindow = new RunCommandDialog(App.CommandLineArgs, this, true, TryOpenDataFile);
        // CollectWindow.Show();
    }

    /// <summary>
    /// If we can't write to the directory as a normal user, change the directory to your home directory.
    /// This is useful if PerfVIew is launch from embeded E-mail to avoid writing in \Program Files
    /// </summary>
    private void ChangeCurrentDirectoryIfNeeded()
    {
        // See if the current directory is writable

        bool changDir = false;
        var curDir = Environment.CurrentDirectory;
        if (string.Compare(curDir, 1, @":\windows\System32", 0, 18, StringComparison.OrdinalIgnoreCase) == 0)
        {
            // ETW will refuse to write files int system32 and if people put PerfView there it will end up trying to do so.
            changDir = true;
        }
        else
        {
            try
            {
                var testFile = Path.Combine(curDir, "PerfViewData.testfile");
                File.Open(testFile, FileMode.Create, FileAccess.Write).Close();
                File.Delete(testFile);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                changDir = true;
            }
            catch (Exception) { }
        }
        if (changDir)
        {
            // No then change directory to my documents directory
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // TODO_AVALONIA: Replace StatusBar
            // StatusBar.Log("Current Directory " + Environment.CurrentDirectory + " is not writable, changing to " + docs);
            Environment.CurrentDirectory = docs;
        }
    }
}