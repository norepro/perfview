using System;
using Avalonia.Threading;

namespace PerfView;

public sealed class DispatcherAdapter
{
    public void BeginInvoke(Action callback) => Dispatcher.UIThread.Invoke(callback);

    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public DispatcherOperation InvokeAsync(Action callback) => Dispatcher.UIThread.InvokeAsync(callback);
}