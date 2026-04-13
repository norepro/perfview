// PerfView.Avalonia/CanExecuteRoutedEventArgs.cs
namespace System.Windows.Input;

public class CanExecuteRoutedEventArgs : EventArgs
{
    public bool CanExecute { get; set; }
}