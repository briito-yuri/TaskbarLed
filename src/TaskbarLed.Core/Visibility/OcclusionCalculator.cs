namespace TaskbarLed.Core.Visibility;

/// <summary>
/// Estimates how much of a window the user can actually see, given the windows stacked above it.
/// Uses a sample grid instead of exact region math: cheap, allocation-free, and precise enough
/// to tell "minimized/covered" from "snapped side by side".
/// </summary>
public static class OcclusionCalculator
{
    /// <summary>Fraction (0..1) of <paramref name="target"/> that is on screen and not covered.</summary>
    /// <param name="target">The window being checked.</param>
    /// <param name="screens">Monitor rectangles; parts of the target outside all of them count as hidden.</param>
    /// <param name="occluders">Windows above the target in z-order.</param>
    /// <param name="samplesPerAxis">Grid resolution; 24 gives ~0.2% granularity.</param>
    public static double VisibleFraction(
        PixelRect target,
        IReadOnlyList<PixelRect> screens,
        IReadOnlyList<PixelRect> occluders,
        int samplesPerAxis = 24)
    {
        ArgumentNullException.ThrowIfNull(screens);
        ArgumentNullException.ThrowIfNull(occluders);
        ArgumentOutOfRangeException.ThrowIfLessThan(samplesPerAxis, 2);

        if (target.IsEmpty)
        {
            return 0;
        }

        // Only occluders that touch the target matter.
        var relevant = new List<PixelRect>(occluders.Count);
        foreach (var occluder in occluders)
        {
            if (!occluder.Intersect(target).IsEmpty)
            {
                relevant.Add(occluder);
            }
        }

        var visible = 0;
        for (var row = 0; row < samplesPerAxis; row++)
        {
            var y = target.Top + (int)((row + 0.5) * target.Height / samplesPerAxis);
            for (var col = 0; col < samplesPerAxis; col++)
            {
                var x = target.Left + (int)((col + 0.5) * target.Width / samplesPerAxis);
                if (IsOnAnyScreen(screens, x, y) && !IsCovered(relevant, x, y))
                {
                    visible++;
                }
            }
        }

        return visible / (double)(samplesPerAxis * samplesPerAxis);
    }

    private static bool IsOnAnyScreen(IReadOnlyList<PixelRect> screens, int x, int y)
    {
        foreach (var screen in screens)
        {
            if (screen.Contains(x, y))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCovered(List<PixelRect> occluders, int x, int y)
    {
        foreach (var occluder in occluders)
        {
            if (occluder.Contains(x, y))
            {
                return true;
            }
        }

        return false;
    }
}
