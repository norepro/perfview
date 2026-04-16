using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace System.Windows.Input;

/// <summary>
/// WPF compatibility shim for Mouse static class.
/// </summary>
public static class Mouse
{
    public static Point GetPosition(Visual relativeTo)
    {
        // TODO_AVALONIA: This returns (0,0) as a fallback. Callers should use e.GetPosition() instead.
        return new Point(0, 0);
    }

    public static void Capture(Control element)
    {
        // TODO_AVALONIA: Avalonia doesn't have Mouse.Capture; pointer capture is per-pointer.
    }

    public static Cursor OverrideCursor
    {
        get => null;
        set
        {
            // TODO_AVALONIA: Avalonia doesn't have Mouse.OverrideCursor.
            // Set Cursor on the control directly instead.
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
