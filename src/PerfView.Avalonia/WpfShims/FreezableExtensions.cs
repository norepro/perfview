using Avalonia.Media;

namespace PerfView;

/// <summary>
/// Extension methods providing WPF Freezable compatibility stubs for Avalonia.
/// In WPF, Freeze() makes objects immutable for cross-thread use. Avalonia doesn't
/// need this, so these are no-ops.
/// </summary>
internal static class FreezableExtensions
{
    public static void Freeze(this Brush _) { }
    public static void Freeze(this StreamGeometry _) { }
    public static void Freeze(this Pen _) { }
}
