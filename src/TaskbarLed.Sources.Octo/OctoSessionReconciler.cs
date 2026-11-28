using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.Octo;

/// <summary>Combines holder sessions and native snapshots by confirmed conversation identity.</summary>
public static class OctoSessionReconciler
{
    public static IEnumerable<Session> Reconcile(IReadOnlyList<Session> octo, IReadOnlyList<Session> antigravity,
        IReadOnlyList<Session> codex)
    {
        var namedAntigravity = antigravity.Where(s => s.ConversationId.HasValue).ToDictionary(s => s.ConversationId!.Value);
        var codexById = codex.Where(s => s.ConversationId.HasValue).ToDictionary(s => s.ConversationId!.Value);
        var duplicates = new HashSet<Guid>();
        foreach (var session in octo)
        {
            if (session.ConversationId is { } id && session.EngineName == "Antigravity" && namedAntigravity.ContainsKey(id))
            {
                continue;
            }

            if (session.ConversationId is { } codexId && session.EngineName == "Codex" && codexById.TryGetValue(codexId, out var native))
            {
                if (duplicates.Add(codexId))
                {
                    yield return native with { Title = session.Title, EngineName = session.EngineName };
                }
            }
            else
            {
                yield return session;
            }
        }

        foreach (var session in antigravity.Concat(codex).Where(s => !s.ConversationId.HasValue || !duplicates.Contains(s.ConversationId.Value)))
        {
            yield return session;
        }
    }
}
