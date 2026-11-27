using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.Antigravity;

/// <summary>Tracks observed turns and catches workers that finish between two index reads.</summary>
internal sealed class AntigravityActivity
{
    private readonly HashSet<string> _running = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _lastSeen = new(StringComparer.Ordinal);
    private DateTimeOffset? _previousPoll;

    public Session[] Update(Session[] sessions, DateTimeOffset started, DateTimeOffset now)
    {
        var rows = new Session[sessions.Length];
        for (var i = 0; i < sessions.Length; i++)
        {
            var session = sessions[i];
            if (session.Status == SessionStatus.Running)
            {
                _running.Add(session.Id);
            }
            else if (_running.Remove(session.Id) ||
                (session.ParentId is not null && _previousPoll is { } previous
                 && session.LastUpdated > previous && session.LastUpdated <= now
                 && !_lastSeen.ContainsKey(session.Id)))
            {
                session = session with { Status = SessionStatus.Completed, PromptEndedAt = now };
            }

            rows[i] = session;
            _lastSeen[session.Id] = now;
        }

        foreach (var id in _lastSeen.Where(entry => entry.Value < now.AddSeconds(-15)).Select(entry => entry.Key).ToArray())
        {
            _running.Remove(id);
            _lastSeen.Remove(id);
        }

        _previousPoll = started;
        return rows;
    }
}
