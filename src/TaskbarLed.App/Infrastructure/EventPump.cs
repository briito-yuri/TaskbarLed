using TaskbarLed.Core.Ipc;
using TaskbarLed.Core.Sessions;
using TaskbarLed.Core.Sources;

namespace TaskbarLed.App.Infrastructure;

/// <summary>Moves envelopes from the IPC server through the matching <see cref="ISessionSource"/> into the store.</summary>
public sealed class EventPump
{
    private readonly Dictionary<string, ISessionSource> _sources;
    private readonly SessionStore _store;
    private readonly AppLog _log;

    public EventPump(IEnumerable<ISessionSource> sources, SessionStore store, AppLog log)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources.ToDictionary(s => s.SourceId, StringComparer.OrdinalIgnoreCase);
        _store = store;
        _log = log;
    }

    public async Task RunAsync(System.Threading.Channels.ChannelReader<HookEnvelope> envelopes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelopes);
        await foreach (var envelope in envelopes.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_sources.TryGetValue(envelope.Source, out var source))
            {
                continue; // A hook for a source this build does not know.
            }

            try
            {
                var events = await source.TranslateAsync(envelope.Payload, envelope.ReceivedAt, cancellationToken).ConfigureAwait(false);
                _store.ApplyRange(events);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // One bad payload must not stop the pump.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _log.Error($"Source '{source.SourceId}' failed to translate an event.", ex);
            }
        }
    }
}
