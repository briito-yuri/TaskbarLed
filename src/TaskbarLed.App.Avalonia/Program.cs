using Avalonia;

namespace TaskbarLed.App.Avalonia;

/// <summary>
/// Entry point of the cross-platform panel. For now it runs the demo feed (<c>--demo</c> is the default);
/// live Claude Code events come in the next step of the macOS port (docs/PROPOSTAS.md, P4).
/// </summary>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new MacOSPlatformOptions { ShowInDock = false })
        .LogToTrace();
}
