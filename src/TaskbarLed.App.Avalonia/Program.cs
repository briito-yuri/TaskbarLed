using Avalonia;
using TaskbarLed.Sources.ClaudeCode;

namespace TaskbarLed.App.Avalonia;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (OperatingSystem.IsMacOS() && args is ["--install-hooks" or "--uninstall-hooks"])
        {
            return ChangeHooks(args[0] == "--install-hooks");
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static int ChangeHooks(bool install)
    {
        var hook = Path.Combine(AppContext.BaseDirectory, "TaskbarLed.Hook");
        var installer = new ClaudeCodeHookInstaller(ClaudeCodeHookInstaller.DefaultSettingsPath, hook,
            [.. ClaudeCodeHookInstaller.Events, "PermissionRequest", "PostToolUseFailure"]);
        try
        {
            if (install)
            {
                if (!File.Exists(hook))
                {
                    throw new InvalidOperationException("TaskbarLed.Hook is missing from the app bundle.");
                }

                installer.Install();
            }
            else
            {
                installer.Uninstall();
            }

            return 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    // Also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new MacOSPlatformOptions { ShowInDock = false })
        .LogToTrace();
}
