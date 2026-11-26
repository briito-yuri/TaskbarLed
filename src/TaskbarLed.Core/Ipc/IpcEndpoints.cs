namespace TaskbarLed.Core.Ipc;

/// <summary>Where the hook and the panel meet. Shared by both sides so they can never drift apart.</summary>
public static class IpcEndpoints
{
    public const int MaxPayloadBytes = 2 * 1024 * 1024;

    /// <summary>Named pipe for live events, scoped per Windows user.</summary>
    public static string PipeName { get; } = "TaskbarLed.Events." + Sanitize(Environment.UserName);

    /// <summary>Folder where the hook drops events when the panel is not running.</summary>
    public static string SpoolDirectory { get; } = Path.Combine(DataDirectory, "spool");

    /// <summary>Per-user data folder (%LOCALAPPDATA%\TaskbarLed).</summary>
    public static string DataDirectory =>
        OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "TaskbarLed")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskbarLed");

    /// <summary>
    /// Where the spool lands when the hook runs inside an MSIX package (Claude Code in the Claude desktop
    /// app): Windows redirects that package's %LOCALAPPDATA% writes to <c>&lt;package&gt;\LocalCache\Local</c>.
    /// </summary>
    /// <param name="packageDirectory">A folder under %LOCALAPPDATA%\Packages.</param>
    public static string PackagedSpoolDirectory(string packageDirectory) =>
        Path.Combine(packageDirectory, "LocalCache", "Local", "TaskbarLed", "spool");

    /// <summary>How long the hook waits for the panel before falling back to the spool.</summary>
    public static TimeSpan ConnectTimeout { get; } = TimeSpan.FromMilliseconds(150);

    private static string Sanitize(string value) =>
        new([.. value.Select(c => char.IsLetterOrDigit(c) ? c : '_')]);
}
