using System.Text.Json;
using TaskbarLed.Core.Sessions;
using TaskbarLed.Core.Sources;

namespace TaskbarLed.Sources.Generic;

/// <summary>Translates provider-neutral orchestrator events into the shared session model.</summary>
public sealed class GenericAgentSource : ISessionSource
{
    public const string Id = "agent";
    public string SourceId => Id;

    public ValueTask<IReadOnlyList<SessionEvent>> TranslateAsync(JsonElement payload, DateTimeOffset receivedAt,
        IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (payload.ValueKind != JsonValueKind.Object || Text(payload, "session_id") is not { Length: > 0 and <= 256 } id
            || Text(payload, "agent") is not { Length: > 0 and <= 80 } agent)
        {
            return ValueTask.FromResult<IReadOnlyList<SessionEvent>>([]);
        }

        var session = $"agent:{agent.Length}:{agent}:{id}";
        var message = Text(payload, "message");
        var title = Text(payload, "title") ?? agent;
        var evt = Text(payload, "status") switch
        {
            "started" => (SessionEvent)new PromptSubmitted(session, agent, receivedAt, title, null),
            "running" => new ActivityStarted(session, agent, receivedAt, message ?? agent),
            "waiting" => new AttentionRequested(session, agent, receivedAt, message),
            "done" => new ResponseCompleted(session, agent, receivedAt, message),
            "ended" => new SessionEnded(session, agent, receivedAt),
            _ => null,
        };

        var events = new List<SessionEvent>();
        if (evt is not null)
        {
            if (evt is not (SessionEnded or PromptSubmitted))
            {
                events.Add(new TitleIdentified(session, agent, receivedAt, title));
            }

            if (evt is not SessionEnded && Text(payload, "parent_session_id") is { Length: > 0 and <= 256 } parent && parent != id)
            {
                events.Add(new ParentIdentified(session, agent, receivedAt, $"agent:{agent.Length}:{agent}:{parent}"));
            }

            events.Add(evt);
        }

        return ValueTask.FromResult<IReadOnlyList<SessionEvent>>(events);
    }

    private static string? Text(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
