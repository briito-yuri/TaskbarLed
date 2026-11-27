using System.Globalization;
using TaskbarLed.Core.Ipc;
using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Sessions;
using TaskbarLed.Sources.ClaudeCode;
using TaskbarLed.Sources.Antigravity;
using TaskbarLed.Sources.Codex;
using TaskbarLed.Sources.Octo;
using TaskbarLed.Sources.Generic;
using TaskbarLed.Platform.Mac;

namespace TaskbarLed.App.Avalonia;

internal sealed class LiveFeed(CultureInfo culture, Action? playCompletion = null)
{
    private readonly SessionStore _store = new();
    private readonly ClaudeCodeSource _claude = new(new ToolActivityFormatter(ActivityStrings.For(culture), culture), includeSubagents: true);
    private readonly GenericAgentSource _generic = new();
    private readonly PanelComposer _composer = new(new SessionRotation(TimeSpan.FromSeconds(8)), TickerStrings.For(culture), culture);

    private readonly MacUiStrings _strings = MacUiStrings.For(culture.TwoLetterISOLanguageName == "pt");
    private string[] _detected = [];
    private sealed record CodexFeed(Session[] Octo, Session[] Native);
    private CodexFeed _codexFeed = new([], []);
    private Session[] _antigravity = [];
    private readonly CompletionAlerts _completions = new();
    private readonly Dictionary<string, int> _routes = new(StringComparer.Ordinal);

    public PanelFrame Compose(DateTimeOffset now) =>
        Compose(now, Volatile.Read(ref _antigravity));

    internal PanelFrame Compose(DateTimeOffset now, IReadOnlyList<Session> antigravity)
    {
        var codexFeed = Volatile.Read(ref _codexFeed);
        var external = OctoSessionReconciler.Reconcile(codexFeed.Octo, antigravity, codexFeed.Native);
        var (sessions, sound) = _completions.Update(_store.Snapshot().Concat(external), now);
        if (sound)
        {
            playCompletion?.Invoke();
        }

        var detected = Volatile.Read(ref _detected);
        var frame = _composer.Compose(sessions, now) with { SessionRows = SessionRows(sessions, now) };
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

        return PanelFrame.Empty($"{_strings.Detected}  ›  {string.Join(", ", detected)}") with
        {
            Channels = [.. presence],
            HasActivity = true,
        };
    }

    private List<SessionDisplayRow> SessionRows(Session[] sessions, DateTimeOffset now)
    {
        var known = sessions.DistinctBy(s => s.Id).ToDictionary(s => s.Id, StringComparer.Ordinal);
        var groups = sessions.Where(s => s.Status != SessionStatus.Idle)
            .GroupBy(s => s.ParentId ?? s.Id, StringComparer.Ordinal).ToArray();
        var active = groups.Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var expired in _routes.Keys.Where(key => !active.Contains(key)).ToArray())
        {
            _routes.Remove(expired);
        }

        var used = _routes.Values.ToHashSet();
        var next = 0;
        foreach (var group in groups)
        {
            if (!_routes.ContainsKey(group.Key))
            {
                while (!used.Add(next))
                {
                    next++;
                }

                _routes[group.Key] = next;
            }
        }

        var rows = new List<SessionDisplayRow>();
        foreach (var group in groups.OrderByDescending(group => group.Any(s => s.Status == SessionStatus.Completed))
            .ThenBy(group => _routes[group.Key]))
        {
            var route = RouteLabel(_routes[group.Key]);
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
                    ? $"{(_strings.Session)} {suffix[..Math.Min(8, suffix.Length)]}"
                    : project;
            }

            var children = group.Where(s => s.ParentId is not null).ToArray();
            rows.Add(new SessionDisplayRow(parent?.Id, $"{engine} → {SingleLine(title)}", parent?.Status ?? SessionStatus.Idle, true,
                _completions.SecondsRemaining(parent?.Id, now), HasStatus: parent is not null)
            {
                Engine = engine,
                RouteLetter = route,
                Heading = SingleLine(title),
                LastStation = children.Length == 0,
            });
            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];
                var name = SingleLine(child.DisplayName);
                rows.Add(new SessionDisplayRow(child.Id, $"{(_strings.Subagent)} {name}", child.Status, false,
                    _completions.SecondsRemaining(child.Id, now))
                {
                    Engine = engine,
                    RouteLetter = route,
                    StationName = name,
                    LastStation = i == children.Length - 1,
                });
            }
        }

        return rows;
    }

    private static string RouteLabel(int index)
    {
        var label = string.Empty;
        do
        {
            label = (char)('A' + (index % 26)) + label;
            index = (index / 26) - 1;
        }
        while (index >= 0);

        return label;
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
        var codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (string.IsNullOrWhiteSpace(codexHome))
        {
            codexHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        }

        var octoSource = new OctoSessions(octo);
        var codexSource = new CodexSessions(codexHome);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            if (await AgentProcesses.ScanAsync(rules, cancellationToken).ConfigureAwait(false) is { } detected)
            {
                Volatile.Write(ref _detected, detected);
                var sessions = Array.Exists(detected, tool => tool == "Octo")
                    ? await octoSource.ScanAsync(cancellationToken).ConfigureAwait(false)
                    : [];
                var linked = await Task.WhenAll(sessions.Select(async session =>
                    session is { EngineName: "Codex", ConversationId: null, ProcessId: { } pid }
                        ? session with { ConversationId = await CodexProcessSessions.FindAsync("/usr/sbin/lsof", pid, cancellationToken).ConfigureAwait(false) }
                        : session)).ConfigureAwait(false);
                var codex = detected.Contains("Codex") || linked.Any(s => s.EngineName == "Codex")
                    ? await codexSource.ScanAsync(cancellationToken).ConfigureAwait(false) : [];
                Volatile.Write(ref _codexFeed, new(linked, [.. codex]));
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task DetectAntigravityAsync(CancellationToken cancellationToken)
    {
        var home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
        var source = new AntigravitySessions(home, "/usr/bin/sqlite3", AntigravityPresence.IsAttached, AntigravityPresence.WasReleased);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        do
        {
            if (Volatile.Read(ref _detected).Contains("Antigravity"))
            {
                var sessions = await source.ScanAsync(cancellationToken).ConfigureAwait(false);
                Volatile.Write(ref _antigravity, [.. sessions]);
            }
            else
            {
                source.Reset();
                Volatile.Write(ref _antigravity, []);
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

    public void Acknowledge(string? sessionId)
    {
        if (sessionId is not null)
        {
            _completions.Dismiss(sessionId);
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
        var source = envelope.Source == ClaudeCodeSource.Id ? (TaskbarLed.Core.Sources.ISessionSource)_claude
            : envelope.Source == GenericAgentSource.Id ? _generic : null;
        if (source is not null)
        {
            var events = await source.TranslateAsync(envelope.Payload, envelope.ReceivedAt, envelope.Environment, cancellationToken)
                .ConfigureAwait(false);
            _store.ApplyRange(events);
        }
    }
}
