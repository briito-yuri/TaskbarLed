using System.Globalization;

namespace TaskbarLed.Core.Presentation;

/// <summary>Compact duration formats for a tiny display.</summary>
public static class DurationFormat
{
    /// <summary><c>04:12</c> under an hour, <c>1:04:12</c> above.</summary>
    public static string Clock(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        return duration.TotalHours >= 1
            ? string.Create(CultureInfo.InvariantCulture, $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}")
            : string.Create(CultureInfo.InvariantCulture, $"{duration.Minutes:00}:{duration.Seconds:00}");
    }

    /// <summary><c>45s</c>, <c>12m47s</c>, <c>1h04m</c>.</summary>
    public static string Human(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        if (duration.TotalHours >= 1)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{(int)duration.TotalHours}h{duration.Minutes:00}m");
        }

        return duration.TotalMinutes >= 1
            ? string.Create(CultureInfo.InvariantCulture, $"{duration.Minutes}m{duration.Seconds:00}s")
            : string.Create(CultureInfo.InvariantCulture, $"{duration.Seconds}s");
    }
}
