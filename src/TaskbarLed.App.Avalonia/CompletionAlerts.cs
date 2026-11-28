using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia;

/// <summary>Keeps a new completion visible for ten seconds, regardless of how often its source is polled.</summary>
internal sealed class CompletionAlerts
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Freshness = TimeSpan.FromSeconds(5);
    private sealed record Notice(Session Session, DateTimeOffset Expires);
    private readonly Dictionary<string, Notice> _notices = new(StringComparer.Ordinal);

    public (Session[] Sessions, bool PlaySound) Update(IEnumerable<Session> sessions, DateTimeOffset now)
    {
        var current = sessions.DistinctBy(s => s.Id).ToArray();
        var visible = new List<Session>(current.Length);
        var playSound = false;
        foreach (var session in current)
        {
            if (session.Status != SessionStatus.Completed)
            {
                if (session.Status is SessionStatus.Running or SessionStatus.AwaitingInput)
                {
                    _notices.Remove(session.Id);
                }

                if (session.Status != SessionStatus.Idle
                    || !_notices.TryGetValue(session.Id, out var prior) || prior.Expires <= now)
                {
                    visible.Add(session);
                }
                continue;
            }

            var completed = session.PromptEndedAt ?? session.LastUpdated;
            if (!_notices.ContainsKey(session.Id) && completed > now - Freshness && completed <= now)
            {
                _notices.Add(session.Id, new Notice(session, now + Duration));
                playSound = true;
            }

            if (!_notices.TryGetValue(session.Id, out var notice) || notice.Expires <= now)
            {
                visible.Add(session with { Status = SessionStatus.Idle });
            }
        }

        visible.AddRange(_notices.Values.Where(notice => notice.Expires > now).Select(notice => notice.Session));
        foreach (var key in _notices.Where(entry => entry.Value.Expires <= now
            && (entry.Value.Session.PromptEndedAt ?? entry.Value.Session.LastUpdated) <= now - Freshness).Select(entry => entry.Key).ToArray())
        {
            _notices.Remove(key);
        }

        return ([.. visible], playSound);
    }

    public int SecondsRemaining(string? id, DateTimeOffset now) =>
        id is not null && _notices.TryGetValue(id, out var notice) && notice.Expires > now
            ? Math.Clamp((int)Math.Ceiling((notice.Expires - now).TotalSeconds), 0, (int)Duration.TotalSeconds) : 0;

    public void Dismiss(string id)
    {
        if (_notices.TryGetValue(id, out var notice))
        {
            _notices[id] = notice with { Expires = DateTimeOffset.MinValue };
        }
    }
}
