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
            if (ChangeHooks(args[0] == "--install-hooks") is { } error)
            {
                Console.Error.WriteLine(error);
                return 1;
            }

            return 0;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    internal static string? ChangeHooks(bool install)
    {
        var hook = Path.Combine(AppContext.BaseDirectory, "TaskbarLed.Hook");
        var installer = new ClaudeCodeHookInstaller(ClaudeCodeHookInstaller.DefaultSettingsPath, hook,
            [.. ClaudeCodeHookInstaller.Events, "PermissionRequest", "PostToolUseFailure", "SubagentStart", "SubagentStop"]);
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

            return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return ex.Message;
        }
    }

    // Also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new MacOSPlatformOptions { ShowInDock = false })
        .LogToTrace();
}
