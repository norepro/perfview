using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;

namespace PerfView.Avalonia.Views;

public sealed class DispatcherAdapter
{
    public void BeginInvoke(Action callback) => Dispatcher.UIThread.Invoke(callback);

    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();
}