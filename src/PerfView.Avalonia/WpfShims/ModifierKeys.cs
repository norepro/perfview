using Avalonia.Input;
using KeyModifiers = Avalonia.Input.KeyModifiers;

namespace System.Windows.Input;

/// <summary>
/// WPF compatibility shim that maps ModifierKeys to Avalonia's KeyModifiers.
/// </summary>
[Flags]
public enum ModifierKeys
{
    None = (int)KeyModifiers.None,
    Alt = (int)KeyModifiers.Alt,
    Control = (int)KeyModifiers.Control,
    Shift = (int)KeyModifiers.Shift,
}
