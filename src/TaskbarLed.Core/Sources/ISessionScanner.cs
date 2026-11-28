using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Core.Sources;

/// <summary>Reads the latest session state for a polling integration.</summary>
public interface ISessionScanner
{
    string SourceId { get; }

    ValueTask<IReadOnlyList<Session>> ScanAsync(CancellationToken cancellationToken);
}
