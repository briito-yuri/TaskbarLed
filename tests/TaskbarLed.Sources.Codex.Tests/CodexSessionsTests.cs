using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.Codex.Tests;

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
    [Fact]
    public void Process_links_only_one_distinct_rollout()
    {
        const string id = "01a2199c-59d0-71b1-bb79-58586fd61df3";
        const string path = "n/tmp/rollout-2026-11-26T17-46-46-" + id + ".jsonl";
        Assert.Equal(Guid.Parse(id), CodexProcessSessions.FindConversation(path + "\n" + path));
        Assert.Null(CodexProcessSessions.FindConversation(path + "\n" + path.Replace("fd61df3", "fd61df4", StringComparison.Ordinal)));
        var child = path.Replace("fd61df3", "fd61df4", StringComparison.Ordinal);
        Assert.Equal(Guid.Parse(id), CodexProcessSessions.FindConversation(path + "\n" + child,
            filename => filename == child[1..]));
    }

    [Fact]
    public void Reads_cumulative_token_usage_without_changing_the_turn_start()
    {
        const string header = """
            {"type":"session_meta","payload":{"id":"cc5911f9-02f3-49b0-b90f-7a0373ed9599"}}
            """;
        const string tail = """
            {"timestamp":"2026-01-01T12:00:00Z","type":"event_msg","payload":{"type":"task_started"}}
            {"timestamp":"2026-01-01T12:00:03Z","type":"event_msg","payload":{"type":"token_count","info":{"total_token_usage":{"total_tokens":12345},"last_token_usage":{"total_tokens":34}}}}
            {"type":"event_msg","payload":{"type":"token_count","info":{"total_token_usage":{"total_tokens":"bad"}}}}
            """;
        var session = Assert.IsType<Session>(CodexSessions.Parse(header, tail, DateTimeOffset.UtcNow));
        Assert.Equal(12345, session.TotalTokens);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), session.PromptStartedAt);
    }
}
