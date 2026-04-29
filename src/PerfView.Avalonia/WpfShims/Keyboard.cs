using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using PerfView;

namespace System.Windows.Input;

/// <summary>
/// WPF compatibility shim for Keyboard static class.
/// Avalonia does not have a global Keyboard static — callers should use event args
/// (e.KeyModifiers) for modifier key state when possible.
/// </summary>
public static class Keyboard
{
    public static ModifierKeys Modifiers
    {
        get
        {
            // Avalonia has no static equivalent for current modifier keys.
            // Callers should use e.KeyModifiers from KeyEventArgs instead.
            return ModifierKeys.None;
        }
    }

    public static void Focus(Control element)
    {
        element?.Focus();
    }

    public static Control FocusedElement
    {
        get
        {
            // Use Avalonia's FocusManager to find the currently focused element.
            var topLevel = TopLevel.GetTopLevel(GuiApp.MainWindow);
            return topLevel?.FocusManager?.GetFocusedElement() as Control;
        }
    }
}
