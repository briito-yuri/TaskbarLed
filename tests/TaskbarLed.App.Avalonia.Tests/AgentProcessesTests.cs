namespace TaskbarLed.App.Avalonia.Tests;

public class AgentProcessesTests
{
    [Fact]
    public void Matches_cli_and_desktop_executables_without_matching_helpers_or_arguments()
    {
        var commands = """
            /Applications/Claude.app/Contents/MacOS/Claude
            /Applications/Claude.app/Contents/MacOS/Claude Helper
            /usr/local/bin/claude
            /opt/homebrew/bin/ollama
            /opt/homebrew/bin/opencode
            /Applications/Octo-2.app/Contents/MacOS/octo
            /Users/test/.local/bin/agy
            /usr/bin/echo codex
            /tmp/custom-runner
            """;
        var custom = new Dictionary<string, string> { ["custom-runner"] = "Local AI" };

        Assert.Equal(["Antigravity", "Claude", "Local AI", "Octo", "Ollama", "OpenCode"], AgentProcesses.Match(commands, custom));
        Assert.Empty(AgentProcesses.Match(string.Empty));
    }
}
