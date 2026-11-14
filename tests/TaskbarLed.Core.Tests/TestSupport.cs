using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Core.Tests;

internal sealed class FakeTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class Build
{
    public const string Source = "test";

    public static readonly DateTimeOffset T0 = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    public static DateTimeOffset At(int seconds) => T0.AddSeconds(seconds);

    public static Session Session(string id, SessionStatus status, int lastUpdatedSeconds = 0) =>
        Sessions.Session.Create(id, Source, At(lastUpdatedSeconds)) with { Status = status };
}
