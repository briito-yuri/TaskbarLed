using System.Globalization;
using System.Text.Json;
using TaskbarLed.Core.Ipc;
using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Sessions;
using TaskbarLed.Sources.ClaudeCode;

namespace TaskbarLed.App.Avalonia;

internal sealed class LiveFeed(CultureInfo culture)
{
    private readonly SessionStore _store = new();
    private readonly ClaudeCodeSource _claude = new(new ToolActivityFormatter(ActivityStrings.For(culture), culture));
    private readonly PanelComposer _composer = new(new SessionRotation(TimeSpan.FromSeconds(8)), TickerStrings.For(culture), culture);

    private readonly string _detectedLabel = culture.TwoLetterISOLanguageName == "pt" ? "Processos detectados" : "Detected processes";
    private string[] _detected = [];
    private Session[] _octo = [];
    private Session[] _antigravity = [];
    private Session[] _codex = [];
    private readonly CodexSessions _codexReader = new();
    private readonly bool _pt = culture.TwoLetterISOLanguageName == "pt";
    private readonly Dictionary<string, DateTimeOffset> _seenExternal = new(StringComparer.Ordinal);

    public PanelFrame Compose(DateTimeOffset now) =>
        Compose(now, Volatile.Read(ref _antigravity));

    internal PanelFrame Compose(DateTimeOffset now, IReadOnlyList<Session> antigravity)
    {
        var external = MergeExternal(Volatile.Read(ref _octo), antigravity, Volatile.Read(ref _codex));
        var sessions = _store.Snapshot().Concat(external.Where(s =>
            s.Status != SessionStatus.Completed ||
            (s.LastUpdated >= now.AddMinutes(-30) &&
             (!_seenExternal.TryGetValue(s.Id, out var seen) || seen != s.LastUpdated))))
            .ToArray();
        var detected = Volatile.Read(ref _detected);
        var frame = _composer.Compose(sessions, now) with { SessionRows = SessionRows(sessions) };
        if (detected.Length == 0)
        {
            return frame;
        }

        var presence = detected.Select((name, index) =>
            new ChannelState($"process:{index}:{name}", name, SessionStatus.Idle, false));
        if (frame.HasActivity)
        {
            return frame with { Channels = [.. frame.Channels, .. presence] };
        }

        return PanelFrame.Empty($"{_detectedLabel}  ›  {string.Join(", ", detected)}") with
        {
            Channels = [.. presence],
            HasActivity = true,
        };
    }

    internal static IEnumerable<Session> MergeExternal(IReadOnlyList<Session> octo, IReadOnlyList<Session> antigravity,
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

    private List<SessionDisplayRow> SessionRows(Session[] sessions)
    {
        var known = sessions.DistinctBy(s => s.Id).ToDictionary(s => s.Id, StringComparer.Ordinal);
        var groups = sessions.Where(s => s.Status != SessionStatus.Idle)
            .GroupBy(s => s.ParentId ?? s.Id, StringComparer.Ordinal);
        var rows = new List<SessionDisplayRow>();
        foreach (var group in groups)
        {
            var parent = known.GetValueOrDefault(group.Key);
            var first = group.First();
            var engine = parent?.EngineName ?? first.EngineName ?? EngineName(parent ?? first);
            var title = parent?.Title;
            var prefix = engine + " · ";
            if (title?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                title = title[prefix.Length..];
            }

            if (string.IsNullOrWhiteSpace(title) || string.Equals(title, engine, StringComparison.OrdinalIgnoreCase)
                || string.Equals(title, "Octo · " + engine, StringComparison.OrdinalIgnoreCase))
            {
                var project = parent?.ProjectName ?? first.ProjectName;
                var home = Path.GetFileName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                var suffix = group.Key[(group.Key.LastIndexOf(':') + 1)..];
                title = string.IsNullOrWhiteSpace(project) || string.Equals(project, home, StringComparison.OrdinalIgnoreCase)
                    ? $"{(_pt ? "Sessão" : "Session")} {suffix[..Math.Min(8, suffix.Length)]}"
                    : project;
            }

            rows.Add(new SessionDisplayRow(parent?.Id, $"{engine} → {SingleLine(title)}", parent?.Status ?? SessionStatus.Idle, true));
            foreach (var child in group.Where(s => s.ParentId is not null))
            {
                rows.Add(new SessionDisplayRow(child.Id, $"{(_pt ? "Subagente" : "Subagent")} {SingleLine(child.DisplayName)}", child.Status, false));
            }
        }

        return rows;
    }

    private static string EngineName(Session session) => session.SourceId switch
    {
        "antigravity" => "Antigravity",
        "codex" => "Codex",
        "claude-code" => "Claude",
        "octo" => "Octo",
        _ => SingleLine(session.SourceId),
    };

    private static string SingleLine(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public async Task DetectProcessesAsync(CancellationToken cancellationToken)
    {
        var rules = Path.Combine(IpcEndpoints.DataDirectory, "agents.json");
        var octo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
            "Application Support", "Octo", "agents");
        var geminiHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
        var codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (string.IsNullOrWhiteSpace(codexHome))
        {
            codexHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        }

        var observedAntigravity = new HashSet<string>(StringComparer.Ordinal);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            if (await AgentProcesses.ScanAsync(rules, cancellationToken).ConfigureAwait(false) is { } detected)
            {
                Volatile.Write(ref _detected, detected);
                var sessions = Array.Exists(detected, tool => tool == "Octo")
                    ? await OctoSessions.ScanAsync(octo, cancellationToken).ConfigureAwait(false)
                    : [];
                var linked = await Task.WhenAll(sessions.Select(async session =>
                    session is { EngineName: "Codex", ConversationId: null, ProcessId: { } pid }
                        ? session with { ConversationId = await CodexProcessSessions.FindAsync(pid, cancellationToken).ConfigureAwait(false) }
                        : session)).ConfigureAwait(false);
                Volatile.Write(ref _octo, linked);
                var antigravity = Array.Exists(detected, tool => tool == "Antigravity")
                    ? await AntigravitySessions.ScanAsync(geminiHome, cancellationToken).ConfigureAwait(false)
                    : [];
                foreach (var active in antigravity.Where(s => s.Status == SessionStatus.Running))
                {
                    observedAntigravity.Add(active.Id);
                }

                var known = antigravity.Where(s => s.Status != SessionStatus.Completed || observedAntigravity.Contains(s.Id)).ToArray();
                observedAntigravity.IntersectWith(known.Select(s => s.Id));
                Volatile.Write(ref _antigravity, known);
                Volatile.Write(ref _codex, Array.Exists(detected, tool => tool == "Codex")
                    ? _codexReader.Scan(codexHome, DateTimeOffset.UtcNow) : []);
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

    public void Acknowledge(string? sessionId)
    {
        var completed = Volatile.Read(ref _octo).Concat(Volatile.Read(ref _antigravity)).Concat(Volatile.Read(ref _codex))
            .FirstOrDefault(s => s.Id == sessionId && s.Status == SessionStatus.Completed);
        if (completed is not null)
        {
            _seenExternal[completed.Id] = completed.LastUpdated;
        }

        _store.AcknowledgeCompleted(s => s.Id == sessionId);
    }

    public void Skip() => _composer.Rotation.Skip();

    public async Task ReadAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(350));
        do
        {
            try
            {
                if (Directory.Exists(IpcEndpoints.SpoolDirectory))
                {
                    await DrainAsync(IpcEndpoints.SpoolDirectory, cancellationToken).ConfigureAwait(false);
                }

                _store.PruneStale(TimeSpan.FromHours(12));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Retry a temporarily unavailable spool on the next tick.
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

    internal async Task DrainAsync(string directory, CancellationToken cancellationToken = default)
    {
        var files = new DirectoryInfo(directory).EnumerateFiles("*.json").OrderBy(f => f.Name, StringComparer.Ordinal);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (file.LinkTarget is null && file.Length <= IpcEndpoints.MaxPayloadBytes + 4096)
            {
                var data = await File.ReadAllBytesAsync(file.FullName, cancellationToken).ConfigureAwait(false);
                if (HookEnvelope.TryDecode(data, out var envelope) && envelope is not null)
                {
                    await AcceptAsync(envelope, cancellationToken).ConfigureAwait(false);
                }
            }

            file.Delete();
        }
    }

    internal async ValueTask AcceptAsync(HookEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var payload = envelope.Payload;
        if (payload.ValueKind != JsonValueKind.Object || Text(payload, "session_id") is not { Length: > 0 and <= 256 } id)
        {
            return;
        }

        if (envelope.Source == ClaudeCodeSource.Id)
        {
            var hook = Text(payload, "hook_event_name");
            if (hook is "SubagentStart" or "SubagentStop")
            {
                if (Text(payload, "agent_id") is { Length: > 0 and <= 256 } agentId)
                {
                    var at = envelope.ReceivedAt;
                    var child = $"claude-subagent:{id.Length}:{id}:{agentId}";
                    if (hook == "SubagentStart")
                    {
                        _store.Apply(new ParentIdentified(child, envelope.Source, at, id));
                        _store.Apply(new PromptSubmitted(child, envelope.Source, at, Text(payload, "agent_type") ?? agentId, null));
                    }
                    else
                    {
                        if (_store.Snapshot().Any(s => s.Id == child))
                        {
                            _store.Apply(new ResponseCompleted(child, envelope.Source, at, null));
                        }
                    }
                }

                return;
            }

            var events = await _claude.TranslateAsync(payload, envelope.ReceivedAt, envelope.Environment, cancellationToken)
                .ConfigureAwait(false);
            _store.ApplyRange(events);
        }
        else if (envelope.Source == "agent")
        {
            var agent = Text(payload, "agent");
            if (agent is not { Length: > 0 and <= 80 })
            {
                return;
            }

            var session = $"agent:{agent.Length}:{agent}:{id}";
            var at = envelope.ReceivedAt;
            var message = Text(payload, "message");
            var evt = Text(payload, "status") switch
            {
                "started" => (SessionEvent)new PromptSubmitted(session, agent, at, Text(payload, "title") ?? agent, null),
                "running" => new ActivityStarted(session, agent, at, message ?? agent),
                "waiting" => new AttentionRequested(session, agent, at, message),
                "done" => new ResponseCompleted(session, agent, at, message),
                "ended" => new SessionEnded(session, agent, at),
                _ => null,
            };
            if (evt is not null)
            {
                if (evt is not (SessionEnded or PromptSubmitted))
                {
                    _store.Apply(new TitleIdentified(session, agent, at, Text(payload, "title") ?? agent));
                }

                if (evt is not SessionEnded && Text(payload, "parent_session_id") is { Length: > 0 and <= 256 } parent && parent != id)
                {
                    _store.Apply(new ParentIdentified(session, agent, at, $"agent:{agent.Length}:{agent}:{parent}"));
                }

                _store.Apply(evt);
            }
        }
    }

    private static string? Text(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
