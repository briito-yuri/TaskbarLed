using TaskbarLed.Core.Visibility;

namespace TaskbarLed.Platform.Mac;

/// <summary>One on-screen window as macOS reports it, front to back.</summary>
/// <param name="Owner">Name of the owning app (<c>kCGWindowOwnerName</c>), e.g. <c>Claude</c>.</param>
/// <param name="Layer">Window layer; 0 is a normal app window, higher values are menus, the Dock, overlays.</param>
/// <param name="Bounds">Window rectangle in screen points.</param>
public readonly record struct MacWindow(string Owner, int Layer, PixelRect Bounds);

/// <summary>Decides whether the Claude desktop window is in sight. Pure, so it is tested without a Mac.</summary>
public static class ClaudeWindowRules
{
    /// <summary>App name of the Claude desktop app.</summary>
    public const string ClaudeOwner = "Claude";

    /// <summary>
    /// True when a normal Claude window is on screen and at least <paramref name="threshold"/> of it is
    /// not covered by normal windows in front of it. Minimized windows and windows on another Space are
    /// not reported as on screen by macOS, so they count as hidden.
    /// </summary>
    /// <param name="windowsFrontToBack">On-screen windows, frontmost first.</param>
    /// <param name="screens">Screen rectangles in the same coordinates.</param>
    /// <param name="threshold">Visible fraction (0..1) that counts as "in sight".</param>
    /// <param name="ownWindowOwner">This app's own name, so the panel never hides Claude from itself.</param>
    public static bool IsClaudeVisible(
        IReadOnlyList<MacWindow> windowsFrontToBack,
        IReadOnlyList<PixelRect> screens,
        double threshold,
        string? ownWindowOwner = null)
    {
        ArgumentNullException.ThrowIfNull(windowsFrontToBack);
        ArgumentNullException.ThrowIfNull(screens);

        var above = new List<PixelRect>();
        foreach (var window in windowsFrontToBack)
        {
            if (window.Layer != 0 || window.Bounds.IsEmpty)
            {
                continue; // Menu bar, Dock, tooltips and floating panels do not hide Claude.
            }

            if (string.Equals(window.Owner, ownWindowOwner, StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(window.Owner, ClaudeOwner, StringComparison.Ordinal)
                && OcclusionCalculator.VisibleFraction(window.Bounds, screens, above) >= threshold)
            {
                return true;
            }

            above.Add(window.Bounds);
        }

        return false;
    }
}
