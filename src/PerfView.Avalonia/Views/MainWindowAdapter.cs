using System;
using Avalonia.Controls;

namespace PerfView.Avalonia.Views;

public sealed class MainWindowAdapter
{
    private readonly MainWindow mainWindow;

    public MainWindowAdapter(MainWindow mainWindow)
    {
        ArgumentNullException.ThrowIfNull(mainWindow);
        this.mainWindow = mainWindow;
    }

    public DispatcherAdapter Dispatcher { get; } = new();
}