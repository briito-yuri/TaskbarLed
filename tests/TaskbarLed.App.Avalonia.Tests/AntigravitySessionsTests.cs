using System.Globalization;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia.Tests;

public class AntigravitySessionsTests
{
    [Fact]
    public void Active_children_are_visible_with_an_idle_parent_and_empty_titles()
    {
        var sessions = AntigravitySessions.Parse("""
            [
              {"id":"cc5911f9-02f3-49b0-b90f-7a0373ed9599","title":"German project",
               "status":"CASCADE_RUN_STATUS_IDLE","modified":"2026-11-27 00:55:23.537962+00:00"},
              {"id":"aa97e19d-7a2f-4ded-a816-1270782ea101","title":"","agent":"Vocabulary",
               "parent":"cc5911f9-02f3-49b0-b90f-7a0373ed9599","parent_title":"German project","status":"CASCADE_RUN_STATUS_RUNNING","modified":"2026-11-27 00:55:23.537962+00:00"},
              {"id":"aa97e19d-7a2f-4ded-a816-1270782ea102","agent":"Grammar",
               "parent":"cc5911f9-02f3-49b0-b90f-7a0373ed9599","parent_title":"German project","status":"CASCADE_RUN_STATUS_RUNNING","modified":"2026-11-27 00:55:23.537962+00:00"}
            ]
            """);
        Assert.Equal(3, sessions.Length);
        Assert.Equal(2, sessions.Count(s => s.Status == SessionStatus.Running));

        var feed = new LiveFeed(CultureInfo.GetCultureInfo("pt-BR"));
        var frame = feed.Compose(DateTimeOffset.UtcNow, sessions);
        Assert.True(frame.HasActivity);
        Assert.Collection(frame.SessionRows,
            header => { Assert.True(header.IsHeader); Assert.Equal(SessionStatus.Idle, header.Status); Assert.Equal("Antigravity → German project", header.Label); },
            child => { Assert.False(child.IsHeader); Assert.Equal("Subagente Vocabulary", child.Label); },
            child => { Assert.False(child.IsHeader); Assert.Equal("Subagente Grammar", child.Label); });
    }
}
