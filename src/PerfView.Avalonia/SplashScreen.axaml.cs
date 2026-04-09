using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace PerfView.Avalonia;

public partial class SplashScreen : Window
{
    public SplashScreen()
    {
        InitializeComponent();
    }
}