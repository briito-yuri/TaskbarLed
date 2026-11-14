using System.Globalization;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Core.Presentation;

/// <summary>A one-off notice worth interrupting full-screen content for.</summary>
/// <param name="SessionId">The session it is about.</param>
/// <param name="Status">Completed or AwaitingInput.</param>
/// <param name="Text">Display text, e.g. <c>2 API  ›  CONCLUÍDO EM 12M47S</c>.</param>
public sealed record PanelAlert(string SessionId, SessionStatus Status, string Text);

/// <summary>
/// Emits an alert when a session it already knew <em>becomes</em> completed or starts waiting for the user.
/// Sessions seen for the first time never alert, so replaying the spool at startup stays quiet.
/// </summary>
public sealed class AlertDetector
{
    private readonly Dictionary<string, SessionStatus> _last = new(StringComparer.Ordinal);
    private readonly TickerStrings _strings;
    private readonly CultureInfo _culture;

    public AlertDetector(TickerStrings strings, CultureInfo culture)
    {
        _strings = strings ?? throw new ArgumentNullException(nameof(strings));
        _culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    public IReadOnlyList<PanelAlert> Detect(IReadOnlyList<Session> sessions, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var alerts = new List<PanelAlert>();
        for (var i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var known = _last.TryGetValue(session.Id, out var previous);
            _last[session.Id] = session.Status;

            if (!known || previous == session.Status)
            {
                continue;
            }

            var name = string.Create(CultureInfo.InvariantCulture, $"{i + 1} {PanelComposer.Clean(session.DisplayName, 16)}");
            var text = session.Status switch
            {
                SessionStatus.Completed => session.Elapsed(now) is { } e
                    ? string.Format(_culture, _strings.DoneIn, DurationFormat.Human(e))
                    : _strings.Done,
                SessionStatus.AwaitingInput => _strings.WaitingForYou,
                _ => null,
            };

            if (text is not null)
            {
                alerts.Add(new PanelAlert(session.Id, session.Status, $"{name}  ›  {text}"));
            }
        }

        // Forget sessions that left the store.
        if (_last.Count > sessions.Count)
        {
            var alive = sessions.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var id in _last.Keys.Where(id => !alive.Contains(id)).ToList())
            {
                _last.Remove(id);
            }
        }

        return alerts;
    }
}
