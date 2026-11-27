using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.Antigravity.Tests;

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

        Assert.All(sessions.Where(session => session.Status == SessionStatus.Running),
            session => Assert.Equal(sessions[0].Id, session.ParentId));
    }

    [Fact]
    public void Suppresses_uninitialized_entries_and_keeps_short_worker_completions()
    {
        var entries = AntigravitySessions.Parse("""
            [{"id":"d1520043-f43e-4de0-8607-1f73401fa94c","modified":"0001-01-01 00:00:00+00:00","status":"CASCADE_RUN_STATUS_RUNNING"}]
            """);
        Assert.Empty(entries);

        var now = DateTimeOffset.UtcNow;
        var worker = Session.Create("worker", "antigravity", now) with
        {
            ParentId = "parent",
            Title = "polidor",
            Status = SessionStatus.Idle,
        };
        var tracker = new AntigravityActivity();
        Assert.Empty(tracker.Update([], now.AddMilliseconds(-500), now.AddMilliseconds(-490)));
        var completed = Assert.Single(tracker.Update([worker], now, now));
        Assert.Equal(SessionStatus.Completed, completed.Status);
        Assert.Equal(now, completed.PromptEndedAt);
        Assert.Equal(SessionStatus.Idle, Assert.Single(tracker.Update([worker], now.AddMilliseconds(500), now.AddMilliseconds(500))).Status);
    }

    [Fact]
    public void Worker_stream_stays_open_until_its_completion_signal()
    {
        var id = Guid.NewGuid();
        var streams = new Dictionary<Guid, bool>();
        AntigravityStreams.Record($"Starting conversation update stream for {id:D}", streams);
        Assert.True(streams[id]);
        AntigravityStreams.Record($"Stream goroutine exited for {id:D}, sending completion signal", streams);
        Assert.False(streams[id]);
    }

    [Fact]
    public async Task Reads_the_checkpointed_index_when_no_wal_sidecars_exist()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var home = Directory.CreateTempSubdirectory("antigravity test ");
        try
        {
            var db = Path.Combine(home.CreateSubdirectory("antigravity-cli").FullName, "conversation_summaries.db");
            var options = new System.Diagnostics.ProcessStartInfo("/usr/bin/sqlite3")
            {
                ArgumentList = { "-batch", "-init", "/dev/null", db, """
                    PRAGMA journal_mode=WAL;
                    CREATE TABLE conversation_summaries (conversation_id TEXT, title TEXT, agent_name TEXT,
                        parent_conversation_id TEXT, last_modified_time TEXT, status TEXT, killed INTEGER);
                    INSERT INTO conversation_summaries VALUES ('cc5911f9-02f3-49b0-b90f-7a0373ed9599',
                        'Alemão', '', '', datetime('now'), 'CASCADE_RUN_STATUS_RUNNING', 0);
                    PRAGMA wal_checkpoint(TRUNCATE);
                    """ },
                RedirectStandardOutput = true,
            };
            using var sqlite = System.Diagnostics.Process.Start(options)!;
            await sqlite.WaitForExitAsync(CancellationToken.None);
            Assert.Equal(0, sqlite.ExitCode);
            File.Delete(db + "-wal");
            File.Delete(db + "-shm");
            var session = Assert.Single(await new AntigravitySessions(home.FullName, "/usr/bin/sqlite3").ScanAsync(CancellationToken.None));
            Assert.Equal("Alemão", session.Title);
            Assert.Equal(SessionStatus.Running, session.Status);
            Assert.False(File.Exists(db + "-wal"));
        }
        finally
        {
            home.Delete(recursive: true);
        }
    }
}
