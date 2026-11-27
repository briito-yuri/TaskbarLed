using System.Text.Json;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.Octo.Tests;

public class OctoSessionsTests
{
    [Theory]
    [InlineData("working", SessionStatus.Running)]
    [InlineData("paused", SessionStatus.AwaitingInput)]
    [InlineData("done", SessionStatus.Completed)]
    [InlineData("idle", SessionStatus.Idle)]
    [InlineData("exited", null)]
    public void Octo_reports_real_turn_state_and_duration(string state, SessionStatus? expected)
    {
        var agent = JsonSerializer.SerializeToElement(new { id = 73, kind = "terminal", name = "Research" });
        var status = JsonSerializer.SerializeToElement(new
        {
            state,
            state_since_ms = 9000,
            last_turn_ms = 7500,
            nested = new { kind = "antigravity" },
        });
        var session = OctoSessions.Parse(agent, status);
        Assert.Equal(expected, session?.Status);
        if (session is not null)
        {
            Assert.Equal("octo:73", session.Id);
            Assert.Equal("Antigravity · Research", session.DisplayName);
            Assert.Equal(TimeSpan.FromSeconds(7.5), session.Elapsed(DateTimeOffset.FromUnixTimeMilliseconds(9000)));
        }
    }
}
