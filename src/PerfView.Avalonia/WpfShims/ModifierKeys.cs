// WPF compatibility: ModifierKeys is an alias for Avalonia's KeyModifiers.
// This allows WPF code using ModifierKeys.Control etc. to compile against Avalonia.
global using ModifierKeys = Avalonia.Input.KeyModifiers;
