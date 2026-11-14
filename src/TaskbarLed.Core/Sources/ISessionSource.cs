using System.Text.Json;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Core.Sources;

/// <summary>
/// Extension point: translates the hook payloads of one agent tool (Claude Code, and in the future
/// others) into <see cref="SessionEvent"/>s. Implementations must never throw on unknown input:
/// return an empty list instead, so a new tool version cannot take the panel down.
/// </summary>
public interface ISessionSource
{
    /// <summary>Identifier used on the hook command line and in envelopes (e.g. <c>claude-code</c>).</summary>
    string SourceId { get; }

    /// <summary>Converts one raw hook payload into zero or more session events.</summary>
    /// <param name="payload">The JSON the tool passed to the hook on stdin.</param>
    /// <param name="receivedAt">When the hook ran (used as event time).</param>
    /// <param name="cancellationToken">Cancels slow enrichment such as reading transcripts.</param>
    ValueTask<IReadOnlyList<SessionEvent>> TranslateAsync(
        JsonElement payload,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);
}
