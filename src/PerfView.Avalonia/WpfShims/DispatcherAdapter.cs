using System;
using Avalonia.Threading;

namespace PerfView;

public sealed class DispatcherAdapter
{
    public static readonly DispatcherAdapter Instance = new();

    public void BeginInvoke(Action callback) => Dispatcher.UIThread.InvokeAsync(callback);

    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public DispatcherOperation InvokeAsync(Action callback) => Dispatcher.UIThread.InvokeAsync(callback);

    public void Invoke(Action callback) => Dispatcher.UIThread.Invoke(callback);

    public T Invoke<T>(Func<T> callback) => Dispatcher.UIThread.Invoke(callback);
}