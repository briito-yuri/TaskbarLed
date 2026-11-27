using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.Octo.Tests;

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

        var rows = OctoSessionReconciler.Reconcile([octo], [], [codex, another]).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal("codex:1", rows[0].Id);
        Assert.Equal("Octo title", rows[0].Title);
        Assert.Equal(SessionStatus.Running, rows[0].Status);
        Assert.Equal(another, rows[1]);
    }

}
