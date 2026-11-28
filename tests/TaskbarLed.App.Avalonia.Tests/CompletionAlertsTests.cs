using System.Globalization;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia.Tests;

public class CompletionAlertsTests
{
    [Fact]
    public void Old_completions_do_not_signal_and_new_completions_expire_after_ten_seconds()
    {
        var at = DateTimeOffset.UtcNow;
        var alerts = 0;
        var feed = new LiveFeed(CultureInfo.GetCultureInfo("pt-BR"), () => alerts++);
        var old = Session.Create("old", "codex", at.AddMinutes(-2)) with
        {
            Status = SessionStatus.Completed,
            PromptEndedAt = at.AddMinutes(-2),
        };
        var fresh = Session.Create("fresh", "codex", at.AddSeconds(-1)) with
        {
            Status = SessionStatus.Completed,
            PromptEndedAt = at.AddSeconds(-1),
        };

        Assert.Empty(feed.Compose(at, [old]).SessionRows);
        Assert.Equal(0, alerts);
        var first = Assert.Single(feed.Compose(at, [old, fresh]).SessionRows);
        Assert.Equal("fresh", first.SessionId);
        Assert.Equal(10, first.SecondsRemaining);
        Assert.Equal(1, alerts);
        Assert.Equal(1, Assert.Single(feed.Compose(at.AddSeconds(9.5), []).SessionRows).SecondsRemaining);
        Assert.Empty(feed.Compose(at.AddSeconds(10), [old, fresh]).SessionRows);
        Assert.Empty(feed.Compose(at.AddSeconds(11), [fresh]).SessionRows);
        Assert.Equal(1, alerts);

    }

    [Fact]
    public void A_completed_parent_returns_to_idle_without_hiding_its_running_child()
    {
        var at = DateTimeOffset.UtcNow;
        var feed = new LiveFeed(CultureInfo.GetCultureInfo("pt-BR"));
        var parent = Session.Create("parent", "antigravity", at) with
        {
            Title = "Configurar aprendizado de alemão",
            Status = SessionStatus.Completed,
            PromptEndedAt = at,
        };
        var child = Session.Create("child", "antigravity", at) with { ParentId = parent.Id, Status = SessionStatus.Running };

        var flashed = feed.Compose(at, [parent, child]);
        Assert.Equal(SessionStatus.Completed, flashed.SessionRows[0].Status);
        Assert.Equal(10, flashed.SessionRows[0].SecondsRemaining);

        var stable = feed.Compose(at.AddSeconds(10), [parent, child]);
        Assert.Equal(SessionStatus.Idle, stable.SessionRows[0].Status);
        Assert.True(stable.SessionRows[0].HasStatus);
        Assert.Equal(SessionStatus.Running, stable.SessionRows[1].Status);
        Assert.False(stable.HasUnseenCompletion);
    }
}
