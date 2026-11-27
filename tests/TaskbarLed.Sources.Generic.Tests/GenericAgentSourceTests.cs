using System.Text.Json;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.Generic.Tests;

public class GenericAgentSourceTests
{
    [Fact]
    public async Task A_worker_is_linked_to_its_parent_and_reported_running()
    {
        using var payload = JsonDocument.Parse("""
            {"agent":"webapp","session_id":"worker","parent_session_id":"main","status":"running"}
            """);
        var events = await new GenericAgentSource().TranslateAsync(payload.RootElement, DateTimeOffset.UtcNow,
            new Dictionary<string, string>(), CancellationToken.None);
        Assert.Equal("agent:6:webapp:main", Assert.Single(events.OfType<ParentIdentified>()).ParentId);
        Assert.Single(events.OfType<ActivityStarted>());
    }
}
