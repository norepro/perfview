// PerfView.Avalonia/ExecutedRoutedEventArgs.cs
namespace System.Windows.Input;

public class ExecutedRoutedEventArgs : EventArgs
{
    public bool Handled { get; set; }
    public ICommand Command { get; set; }
    public object Parameter { get; set; }
}