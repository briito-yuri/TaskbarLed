namespace TaskbarLed.Platform.Mac;

/// <summary>Where TaskbarLed keeps its files on macOS, following Apple's folder conventions.</summary>
/// <param name="Home">The user's home folder.</param>
public sealed record MacPaths(string Home)
{
    /// <summary>Paths for the signed-in user.</summary>
    public static MacPaths Current { get; } = new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    /// <summary>Settings and the hook spool: <c>~/Library/Application Support/TaskbarLed</c>.</summary>
    public string DataDirectory => Path.Combine(Home, "Library", "Application Support", "TaskbarLed");

    /// <summary>Log folder: <c>~/Library/Logs/TaskbarLed</c> (shown by Console.app).</summary>
    public string LogDirectory => Path.Combine(Home, "Library", "Logs", "TaskbarLed");

    /// <summary>Per-user launch agents, loaded by launchd at every sign-in.</summary>
    public string LaunchAgentsDirectory => Path.Combine(Home, "Library", "LaunchAgents");
}
