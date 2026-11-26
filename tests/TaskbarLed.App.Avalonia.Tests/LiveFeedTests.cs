using System.Globalization;
using System.Text;
using TaskbarLed.Core.Ipc;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia.Tests;

public class LiveFeedTests
{
    [Fact]
    public async Task Spool_combines_Claude_and_orchestrator_sessions_and_prioritizes_attention()
    {
        var spool = Directory.CreateTempSubdirectory();
        try
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var at = DateTimeOffset.UtcNow;
            var feed = new LiveFeed(CultureInfo.GetCultureInfo("en-US"));
            await EmitAsync("01", "claude-code", """{"session_id":"same","hook_event_name":"UserPromptSubmit","cwd":"/tmp/webapp"}""");
            await EmitAsync("02", "agent", """{"session_id":"same","agent":"pipeline","status":"started","title":"Build"}""");
            await EmitAsync("03", "claude-code", """{"session_id":"same","hook_event_name":"PermissionRequest","tool_name":"Edit"}""");
            await EmitAsync("04", "agent", """{"session_id":"same","agent":"pipeline","status":"done","title":"Build","message":"Tests passed"}""");
            await File.WriteAllTextAsync(Path.Combine(spool.FullName, "05.json"), "broken", cancellationToken);

            await feed.DrainAsync(spool.FullName, cancellationToken);
            var frame = feed.Compose(at);
            Assert.Equal(SessionStatus.AwaitingInput, frame.SelectedStatus);
            Assert.Equal(2, frame.Channels.Count);
            Assert.True(frame.HasUnseenCompletion);
            Assert.Empty(spool.EnumerateFiles());

            var completed = Assert.Single(frame.Channels, c => c.Status == SessionStatus.Completed);
            feed.Acknowledge(completed.SessionId);
            Assert.False(feed.Compose(at).HasUnseenCompletion);

            await EmitAsync("06", "claude-code", """{"session_id":"same","hook_event_name":"PostToolUseFailure"}""");
            await feed.DrainAsync(spool.FullName, cancellationToken);
            Assert.Equal(SessionStatus.Running, feed.Compose(at).SelectedStatus);

            Task EmitAsync(string order, string source, string payload) =>
                File.WriteAllBytesAsync(Path.Combine(spool.FullName, order + ".json"),
                    HookEnvelope.Encode(source, at, Encoding.UTF8.GetBytes(payload)), cancellationToken);
        }
        finally
        {
            spool.Delete(recursive: true);
        }
    }
}
