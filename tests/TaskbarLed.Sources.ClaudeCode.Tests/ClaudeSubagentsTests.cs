using System.Globalization;
using System.Text.Json;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.ClaudeCode.Tests;

public class ClaudeSubagentsTests
{
    [Fact]
    public async Task Subagent_hooks_are_opt_in_and_link_worker_to_parent()
    {
        var culture = CultureInfo.InvariantCulture;
        var formatter = new ToolActivityFormatter(ActivityStrings.For(culture), culture);
        using var start = JsonDocument.Parse("""
            {"session_id":"webapp","hook_event_name":"SubagentStart","agent_id":"api","agent_type":"reviewer"}
            """);
        var environment = new Dictionary<string, string>();
        var now = DateTimeOffset.UtcNow;
        Assert.Empty(await new ClaudeCodeSource(formatter).TranslateAsync(start.RootElement, now, environment, CancellationToken.None));
        var source = new ClaudeCodeSource(formatter, includeSubagents: true);
        var events = await source.TranslateAsync(start.RootElement, now, environment, CancellationToken.None);
        Assert.Equal("webapp", Assert.Single(events.OfType<ParentIdentified>()).ParentId);
        using var stop = JsonDocument.Parse("""
            {"session_id":"webapp","hook_event_name":"SubagentStop","agent_id":"api"}
            """);
        Assert.Single((await source.TranslateAsync(stop.RootElement, now, environment, CancellationToken.None))
            .OfType<ResponseCompleted>());
    }
}
