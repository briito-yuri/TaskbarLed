using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia.Tests;

public class SessionDeduplicationTests
{
    [Fact]
    public void Octo_and_native_codex_share_one_row_only_when_conversation_ids_match()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var octo = Session.Create("octo:1", "octo", now) with { ConversationId = id, Title = "Octo title", EngineName = "Codex" };
        var codex = Session.Create("codex:1", "codex", now) with { ConversationId = id, Status = SessionStatus.Running };
        var another = Session.Create("codex:2", "codex", now) with { ConversationId = Guid.NewGuid(), Status = SessionStatus.Running };

        var rows = LiveFeed.MergeExternal([octo], [], [codex, another]).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal("codex:1", rows[0].Id);
        Assert.Equal("Octo title", rows[0].Title);
        Assert.Equal(SessionStatus.Running, rows[0].Status);
        Assert.Equal(another, rows[1]);
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
}
