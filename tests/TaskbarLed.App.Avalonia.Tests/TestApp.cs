using Avalonia;
using Avalonia.Headless;
using TaskbarLed.App.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

namespace TaskbarLed.App.Avalonia.Tests;

/// <summary>Real app resources and styles, rendered with Skia so frames can be captured as images.</summary>
public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
