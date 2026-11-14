using System.Security;

namespace TaskbarLed.Platform.Mac;

/// <summary>
/// Starts the panel at sign-in with a per-user LaunchAgent (the macOS counterpart of the Windows Run key).
/// launchd reads <c>~/Library/LaunchAgents</c> at every sign-in, so writing the file is enough; no admin rights.
/// </summary>
public sealed class LaunchAgent
{
    /// <summary>Reverse-DNS label, also the file name.</summary>
    public const string Label = "io.github.llorenzojorge.taskbarled";

    private readonly string _plistPath;
    private readonly string _contents;

    /// <param name="launchAgentsDirectory">Usually <see cref="MacPaths.LaunchAgentsDirectory"/>.</param>
    /// <param name="executablePath">The app executable (inside the .app bundle once packaged).</param>
    /// <param name="arguments">Arguments that mark an automatic start.</param>
    public LaunchAgent(string launchAgentsDirectory, string executablePath, string arguments = "--autostart")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(launchAgentsDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        _plistPath = Path.Combine(launchAgentsDirectory, Label + ".plist");
        _contents = BuildPlist(executablePath, arguments);
    }

    /// <summary>Full path of the agent file.</summary>
    public string PlistPath => _plistPath;

    /// <summary>True when launchd will start this exact executable at sign-in.</summary>
    public bool IsEnabled => File.Exists(_plistPath) && File.ReadAllText(_plistPath) == _contents;

    /// <summary>Writes (or repairs, after the app moved) the agent.</summary>
    public void Enable()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_plistPath)!);
        File.WriteAllText(_plistPath, _contents);
    }

    public void Disable()
    {
        if (File.Exists(_plistPath))
        {
            File.Delete(_plistPath);
        }
    }

    internal static string BuildPlist(string executablePath, string arguments)
    {
        var programArguments = new[] { executablePath }
            .Concat(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(a => $"    <string>{SecurityElement.Escape(a)}</string>");

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
              <key>Label</key>
              <string>{Label}</string>
              <key>ProgramArguments</key>
              <array>
            {string.Join('\n', programArguments)}
              </array>
              <key>RunAtLoad</key>
              <true/>
              <key>ProcessType</key>
              <string>Interactive</string>
            </dict>
            </plist>

            """;
    }
}
