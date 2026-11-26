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

    public PanelFrame Compose(DateTimeOffset now)
    {
        var sessions = _store.Snapshot();
        var detected = Volatile.Read(ref _detected);
        if (detected.Length == 0)
        {
            return _composer.Compose(sessions, now);
        }

        var presence = detected.Select((name, index) =>
            new ChannelState($"process:{index}:{name}", name, SessionStatus.Idle, false));
        var frame = _composer.Compose(sessions, now);
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

    public async Task DetectProcessesAsync(CancellationToken cancellationToken)
    {
        var rules = Path.Combine(IpcEndpoints.DataDirectory, "agents.json");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            if (await AgentProcesses.ScanAsync(rules, cancellationToken).ConfigureAwait(false) is { } detected)
            {
                Volatile.Write(ref _detected, detected);
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

    public void Acknowledge(string? sessionId) => _store.AcknowledgeCompleted(s => s.Id == sessionId);

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

                _store.Apply(evt);
            }
        }
    }

    private static string? Text(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
