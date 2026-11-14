namespace TaskbarLed.Core.Usage;

/// <summary>Plan usage windows (e.g. a rolling 5-hour window and a weekly window).</summary>
/// <param name="ShortWindow">Fraction (0..1) of the short rolling window used, if known.</param>
/// <param name="Weekly">Fraction (0..1) of the weekly allowance used, if known.</param>
/// <param name="ResetsAt">When the short window resets, if known.</param>
public sealed record UsageLimits(double? ShortWindow, double? Weekly, DateTimeOffset? ResetsAt);

/// <summary>
/// Extension point for plan limits. Not implemented in v1 on purpose: the Claude desktop app does not
/// expose this data to hooks, and reading account credentials must be an explicit opt-in.
/// See docs/REQUISITOS.md, section 5.
/// </summary>
public interface IUsageLimitProvider
{
    ValueTask<UsageLimits?> GetAsync(CancellationToken cancellationToken);
}
