using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia.Tests;

public class CodexSessionsTests
{
    [Fact]
    public void Child_rollout_reports_its_name_parent_and_completion()
    {
        const string header = """
            {"type":"session_meta","payload":{"id":"cc5911f9-02f3-49b0-b90f-7a0373ed9599","cwd":"/tmp/german","source":{"subagent":{"thread_spawn":{"parent_thread_id":"aa97e19d-7a2f-4ded-a816-1270782ea101","agent_nickname":"Ada"}}}}}
            """;
        const string start = """
            {"type":"event_msg","payload":{"type":"task_started"}}
            """;
        const string end = """
            {"type":"event_msg","payload":{"type":"task_complete"}}
            """;
        var now = DateTimeOffset.UtcNow;
        var running = CodexSessions.Parse(header, start, now);
        var completed = CodexSessions.Parse(header, start + "\n" + end, now);
        Assert.NotNull(running);
        Assert.Equal("codex:aa97e19d-7a2f-4ded-a816-1270782ea101", running.ParentId);
        Assert.Equal("Ada", running.DisplayName);
        Assert.Equal(SessionStatus.Running, running.Status);
        Assert.Equal(SessionStatus.Completed, completed?.Status);
    }

    [Fact]
    public void Long_turn_without_a_boundary_in_the_tail_stays_running()
    {
        var now = DateTimeOffset.Now;
        var home = Directory.CreateTempSubdirectory();
        try
        {
            var directory = home.CreateSubdirectory($"sessions/{now:yyyy/MM/dd}");
            var rollout = Path.Combine(directory.FullName, "rollout-long.jsonl");
            File.WriteAllText(rollout, """
                {"type":"session_meta","payload":{"id":"cc5911f9-02f3-49b0-b90f-7a0373ed9599"}}
                {"type":"event_msg","payload":{"type":"task_started"}}
                """ + "\n" + new string(' ', 240 * 1024));
            Assert.Equal(SessionStatus.Running, Assert.Single(new CodexSessions().Scan(home.FullName, now)).Status);
        }
        finally
        {
            home.Delete(recursive: true);
        }
    }
}
