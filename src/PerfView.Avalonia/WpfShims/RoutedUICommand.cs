// PerfView.Avalonia/RoutedUICommand.cs
using System.Collections.Generic;

namespace System.Windows.Input;

public class RoutedUICommand : ICommand
{
    private readonly List<Action<object, CanExecuteRoutedEventArgs>> _canExecuteHandlers = new();
    private readonly List<Action<object, ExecutedRoutedEventArgs>> _executeHandlers = new();

    public string Text { get; }
    public string Name { get; }

    public RoutedUICommand(
        string text,
        string name,
        Type ownerType,
        InputGestureCollection gestures = null)
    {
        Text = text;
        Name = name;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        var args = new CanExecuteRoutedEventArgs();
        foreach (var handler in _canExecuteHandlers)
            handler(null, args);
        return args.CanExecute || _canExecuteHandlers.Count == 0;
    }

    public void Execute(object parameter)
    {
        var args = new ExecutedRoutedEventArgs { Command = this, Parameter = parameter };
        foreach (var handler in _executeHandlers)
            handler(null, args);
    }

    // Called during initialization to wire the CanExecute/Executed handlers
    public void RegisterCanExecute(Action<object, CanExecuteRoutedEventArgs> handler)
        => _canExecuteHandlers.Add(handler);

    public void RegisterExecuted(Action<object, ExecutedRoutedEventArgs> handler)
        => _executeHandlers.Add(handler);
}