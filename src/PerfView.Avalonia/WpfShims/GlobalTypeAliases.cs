// WPF compatibility: global type aliases so that shared code-behind files
// can reference WPF types (DependencyObject, FrameworkElement, etc.) without
// per-file #if AVALONIA using aliases.
global using DependencyObject = Avalonia.AvaloniaObject;
global using FrameworkElement = Avalonia.Controls.Control;
global using UIElement = Avalonia.Controls.Control;
global using RoutedEventHandler = System.EventHandler<Avalonia.Interactivity.RoutedEventArgs>;
