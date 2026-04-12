using System;
using System.Windows;

#if AVALONIA
using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MessageBoxImage = MsBox.Avalonia.Enums.Icon;
#endif

namespace PerfView.Dialogs;

/// <summary>
///  Themed replacement for <see cref="MessageBox"/> that uses a custom XAML window.
/// </summary>
public static class XamlMessageBox
{
    /// <inheritdoc cref="MessageBox.Show(string)"/>
    public static MessageBoxResult Show(string message)
        => Show(null, message, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);

    /// <inheritdoc cref="MessageBox.Show(string, string)"/>
    public static MessageBoxResult Show(string message, string caption)
        => Show(null, message, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);

    /// <inheritdoc cref="MessageBox.Show(string, string, MessageBoxButton)"/>
    public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons)
        => Show(null, message, caption, buttons, MessageBoxImage.None, MessageBoxResult.OK);

    /// <inheritdoc cref="MessageBox.Show(string, string, MessageBoxButton, MessageBoxImage)"/>
    public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon)
        => Show(null, message, caption, buttons, icon, MessageBoxResult.OK);

    /// <inheritdoc cref="MessageBox.Show(string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult)"/>
    public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult)
        => Show(null, message, caption, buttons, icon, defaultResult);

    /// <inheritdoc cref="MessageBox.Show(Window, string)"/>
    public static MessageBoxResult Show(Window owner, string message)
        => Show(owner, message, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);

    /// <inheritdoc cref="MessageBox.Show(Window, string, string)"/>
    public static MessageBoxResult Show(Window owner, string message, string caption)
        => Show(owner, message, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);

    /// <inheritdoc cref="MessageBox.Show(Window, string, string, MessageBoxButton)"/>
    public static MessageBoxResult Show(Window owner, string message, string caption, MessageBoxButton buttons)
        => Show(owner, message, caption, buttons, MessageBoxImage.None, MessageBoxResult.OK);

    /// <inheritdoc cref="MessageBox.Show(Window, string, string, MessageBoxButton, MessageBoxImage)"/>
    public static MessageBoxResult Show(Window owner, string message, string caption, MessageBoxButton buttons, MessageBoxImage icon)
        => Show(owner, message, caption, buttons, icon, MessageBoxResult.OK);

    /// <inheritdoc cref="MessageBox.Show(Window, string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult)"/>
    public static MessageBoxResult Show(Window owner, string message, string caption, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult)
    {
#if AVALONIA
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.Invoke(() => Show(owner, message, caption, buttons, icon));
        }

        var messageBoxCustomWindow = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ContentTitle = caption,
            ContentMessage = message,
            ButtonDefinitions = Map(buttons),
            Icon = icon,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        });

        var dialogTask = owner != null
            ? messageBoxCustomWindow.ShowWindowDialogAsync(owner)
            : messageBoxCustomWindow.ShowWindowAsync();

        return Map(dialogTask.GetAwaiter().GetResult());
#else
        // XamlMessageBox uses a WPF window that must be created and shown on the UI thread.
        // Auto-dispatch to match the old System.Windows.MessageBox behavior of working from
        // any thread. This fixes callers like the SecurityCheck delegate which is invoked from
        // background threads during symbol resolution (see issue #2300).
        var dispatcher = owner?.Dispatcher ?? Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            return dispatcher.Invoke(() => Show(owner, message, caption, buttons, icon, defaultResult));
        }

        MessageBoxWindow window = new(message, caption, buttons, icon, defaultResult);
        if (owner is not null)
        {
            window.Owner = owner;
        }

        window.ShowDialog();
        return window.Result;
#endif
    }

#if AVALONIA
    public static ButtonEnum Map(MessageBoxButton source) => source switch
    {
        MessageBoxButton.OK => ButtonEnum.Ok,
        MessageBoxButton.YesNo => ButtonEnum.YesNo,
        MessageBoxButton.OKCancel => ButtonEnum.OkCancel,
        MessageBoxButton.OKAbort => ButtonEnum.OkAbort,
        MessageBoxButton.YesNoCancel => ButtonEnum.YesNoCancel,
        MessageBoxButton.YesNoAbort => ButtonEnum.YesNoAbort,
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };

    public static MessageBoxResult Map(ButtonResult source) => source switch
    {
        ButtonResult.Ok => MessageBoxResult.OK,
        ButtonResult.Yes => MessageBoxResult.Yes,
        ButtonResult.No => MessageBoxResult.No,
        ButtonResult.Abort => MessageBoxResult.Abort,
        ButtonResult.Cancel => MessageBoxResult.Cancel,
        ButtonResult.None => MessageBoxResult.None,
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };
#endif
}
