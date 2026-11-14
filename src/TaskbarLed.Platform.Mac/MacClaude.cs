using System.Diagnostics;
using System.Runtime.Versioning;

namespace TaskbarLed.Platform.Mac;

/// <summary>The Claude desktop app as seen from macOS: is it in sight, and bring it forward.</summary>
[SupportedOSPlatform("macos")]
public static class MacClaude
{
    /// <summary>True when a Claude window is on the current Space, not minimized, and mostly uncovered.</summary>
    /// <param name="threshold">Visible fraction (0..1) that counts as "in sight".</param>
    /// <param name="ownOwner">This app's process name, ignored as an occluder.</param>
    public static bool IsVisible(double threshold, string? ownOwner = null) =>
        ClaudeWindowRules.IsClaudeVisible(MacWindowList.OnScreen(), MacWindowList.Displays(), threshold, ownOwner);

    /// <summary>Activates the Claude app (restores it from the Dock if minimized).</summary>
    public static void Open()
    {
        using var process = Process.Start(new ProcessStartInfo("open") { ArgumentList = { "-a", ClaudeWindowRules.ClaudeOwner }, UseShellExecute = false });
    }
}
