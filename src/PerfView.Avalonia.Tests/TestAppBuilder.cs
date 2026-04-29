using Avalonia;
using Avalonia.Headless;

[assembly: Avalonia.Headless.XUnit.AvaloniaTestFramework]

namespace PerfViewTests
{
    public class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<TestApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }

    public class TestApp : Application
    {
        public override void Initialize()
        {
        }
    }
}
