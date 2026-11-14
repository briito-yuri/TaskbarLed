namespace TaskbarLed.Platform.Mac.Tests;

public sealed class LaunchAgentTests : IDisposable
{
    private readonly string _home = Path.Combine(Path.GetTempPath(), "taskbarled-mac-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_home))
        {
            Directory.Delete(_home, recursive: true);
        }
    }

    [Fact]
    public void Paths_follow_the_macos_library_folders()
    {
        var paths = new MacPaths("/Users/me");

        Assert.Equal(Path.Combine("/Users/me", "Library", "Application Support", "TaskbarLed"), paths.DataDirectory);
        Assert.Equal(Path.Combine("/Users/me", "Library", "Logs", "TaskbarLed"), paths.LogDirectory);
        Assert.Equal(Path.Combine("/Users/me", "Library", "LaunchAgents"), paths.LaunchAgentsDirectory);
    }

    [Fact]
    public void Enable_writes_an_agent_that_runs_the_app_at_sign_in_and_disable_removes_it()
    {
        var agents = new MacPaths(_home).LaunchAgentsDirectory;
        var agent = new LaunchAgent(agents, "/Applications/TaskbarLed.app/Contents/MacOS/TaskbarLed");

        Assert.False(agent.IsEnabled);
        agent.Enable();

        Assert.True(agent.IsEnabled);
        Assert.Equal(Path.Combine(agents, "io.github.llorenzojorge.taskbarled.plist"), agent.PlistPath);
        var plist = File.ReadAllText(agent.PlistPath);
        Assert.Contains("<string>/Applications/TaskbarLed.app/Contents/MacOS/TaskbarLed</string>", plist, StringComparison.Ordinal);
        Assert.Contains("<string>--autostart</string>", plist, StringComparison.Ordinal);
        Assert.Contains("<key>RunAtLoad</key>\n  <true/>", plist.ReplaceLineEndings("\n"), StringComparison.Ordinal);

        agent.Disable();
        Assert.False(File.Exists(agent.PlistPath));
    }

    [Fact]
    public void An_agent_pointing_to_an_old_location_is_not_enabled_so_it_gets_repaired()
    {
        var agents = new MacPaths(_home).LaunchAgentsDirectory;
        new LaunchAgent(agents, "/Users/me/Downloads/TaskbarLed.app/Contents/MacOS/TaskbarLed").Enable();

        var moved = new LaunchAgent(agents, "/Applications/TaskbarLed.app/Contents/MacOS/TaskbarLed");

        Assert.False(moved.IsEnabled);
    }

    [Fact]
    public void Paths_with_xml_characters_are_escaped()
    {
        var plist = LaunchAgent.BuildPlist("/Users/a&b/TaskbarLed", "--autostart");

        Assert.Contains("<string>/Users/a&amp;b/TaskbarLed</string>", plist, StringComparison.Ordinal);
    }
}
