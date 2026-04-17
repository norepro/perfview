namespace PerfView.Dialogs;

/// <summary>
/// Simple replacement for WPF's MessageBoxImage enum.
/// Used by MessageBoxWindow and XamlMessageBox for Avalonia builds.
/// </summary>
public enum MessageBoxImage
{
    None,
    Info,
    Warning,
    Error,
    Question,
    Success
}
