using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace System.Windows.Input;

/// <summary>
/// WPF compatibility shim for Mouse static class.
/// Avalonia does not have global mouse statics — callers should use event args instead:
///   - GetPosition: use e.GetPosition(relativeTo) from PointerEventArgs
///   - Capture:     use e.Pointer.Capture(control) from PointerEventArgs
///   - OverrideCursor: set the Cursor property on individual controls or the top-level Window
/// </summary>
public static class Mouse
{
    public static Point GetPosition(Visual relativeTo)
    {
        // Avalonia: callers should use e.GetPosition(relativeTo) from PointerEventArgs instead.
        return new Point(0, 0);
    }

    public static void Capture(Control element)
    {
        // Avalonia: callers should use e.Pointer.Capture(element) from PointerEventArgs instead.
    }

    public static Cursor OverrideCursor
    {
        get => null;
        set
        {
            // Avalonia has no global cursor override.
            // Set the Cursor property on individual controls or the top-level Window instead.
        }
    }
}

/// <summary>
/// WPF compatibility shim for MouseButtonState enum.
/// </summary>
public enum MouseButtonState
{
    Released,
    Pressed
}
