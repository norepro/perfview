using Avalonia.Controls;
using Avalonia.Interactivity;
// TODO_AVALONIA: MessageBoxButton, MessageBoxImage, MessageBoxResult are WPF types (System.Windows).
// These need to be defined as custom enums or replaced with an Avalonia-compatible message box abstraction.
// For now, referencing the WPF types as placeholders. They must be redefined for Avalonia.

namespace PerfView.Dialogs;

/// <summary>
///  Simple themed message box window.
///  TODO_AVALONIA: This class depends on WPF MessageBoxResult/MessageBoxButton/MessageBoxImage enums.
///  These must be redefined or replaced for Avalonia.
/// </summary>
internal partial class MessageBoxWindow : Window
{
    // TODO_AVALONIA: MessageBoxResult is a WPF type. Define a custom enum or use an Avalonia equivalent.
    public object Result { get; private set; }

    // TODO_AVALONIA: Constructor parameters use WPF MessageBoxButton/MessageBoxImage/MessageBoxResult.
    // These types need Avalonia equivalents.
    public MessageBoxWindow(string message, string caption, object buttons, object icon, object defaultResult)
    {
        InitializeComponent();
        Title = caption;
        MessageTextBlock.Text = message;
        // TODO_AVALONIA: ConfigureIcon and ConfigureButtons need rework for Avalonia types.
        // ConfigureIcon(icon);
        // ConfigureButtons(buttons, defaultResult);
    }

    // TODO_AVALONIA: Visibility.Collapsed → IsVisible = false
    // TODO_AVALONIA: ImageHelpers.ToImageSource uses WPF imaging. Needs Avalonia equivalent.
    // private void ConfigureIcon(MessageBoxImage icon)
    // {
    //     switch (icon)
    //     {
    //         case MessageBoxImage.None:
    //             IconImage.IsVisible = false;
    //             break;
    //         default:
    //             IconImage.Source = ImageHelpers.ToImageSource(icon);
    //             break;
    //     }
    // }

    // TODO_AVALONIA: Button configuration needs rework. WPF MessageBoxButton/MessageBoxResult
    // enums and DialogResult are not available in Avalonia.
    // private void ConfigureButtons(MessageBoxButton buttons, MessageBoxResult defaultResult)
    // {
    //     ButtonsPanel.Children.Clear();
    //     foreach ((string Text, MessageBoxResult Result) in Get(buttons))
    //     {
    //         Button button = new()
    //         {
    //             Content = Text,
    //             Tag = Result,
    //             IsDefault = Result == defaultResult,
    //         };
    //         button.Click += Button_Click;
    //         ButtonsPanel.Children.Add(button);
    //     }
    // }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is not null)
        {
            Result = button.Tag;
            // TODO_AVALONIA: DialogResult not available. Using Close(true) as workaround.
            Close(true);
        }
    }
}
