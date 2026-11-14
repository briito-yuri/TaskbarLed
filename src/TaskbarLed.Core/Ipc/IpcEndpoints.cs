namespace TaskbarLed.Core.Ipc;

/// <summary>Where the hook and the panel meet. Shared by both sides so they can never drift apart.</summary>
public static class IpcEndpoints
{
    /// <summary>Named pipe for live events, scoped per Windows user.</summary>
    public static string PipeName { get; } = "TaskbarLed.Events." + Sanitize(Environment.UserName);

    /// <summary>Folder where the hook drops events when the panel is not running.</summary>
    public static string SpoolDirectory { get; } = Path.Combine(DataDirectory, "spool");

    /// <summary>Per-user data folder (%LOCALAPPDATA%\TaskbarLed).</summary>
    public static string DataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskbarLed");

    /// <summary>How long the hook waits for the panel before falling back to the spool.</summary>
    public static TimeSpan ConnectTimeout { get; } = TimeSpan.FromMilliseconds(150);

    private static string Sanitize(string value) =>
        new([.. value.Select(c => char.IsLetterOrDigit(c) ? c : '_')]);
}
