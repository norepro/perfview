using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace System.Windows.Input;

/// <summary>
/// WPF compatibility shim for Keyboard static class.
/// </summary>
public static class Keyboard
{
    public static ModifierKeys Modifiers
    {
        get
        {
            // TODO_AVALONIA: Avalonia doesn't have a static Keyboard.Modifiers equivalent.
            // This returns None as a safe default; event handlers should use e.KeyModifiers instead.
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
            // TODO_AVALONIA: No direct equivalent for Keyboard.FocusedElement
            return null;
        }
    }
}
