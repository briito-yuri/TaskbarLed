using System.Diagnostics;
using TaskbarLed.Core.Visibility;

namespace TaskbarLed.Platform.Windows.Windowing;

/// <summary>Result of one visibility check.</summary>
/// <param name="WindowFound">A Claude desktop window exists.</param>
/// <param name="VisibleFraction">Share (0..1) of that window the user can see right now.</param>
/// <param name="Handle">The Claude window, for activation.</param>
public readonly record struct ClaudeVisibility(bool WindowFound, double VisibleFraction, nint Handle);

/// <summary>Answers "can the user see Claude right now?".</summary>
public interface IClaudeVisibilityProbe
{
    ClaudeVisibility Check();
}

/// <summary>
/// Finds the Claude desktop window and measures how much of it is uncovered, by walking the
/// z-order above it. Handles minimized, cloaked (other virtual desktop), covered and snapped windows.
/// </summary>
public sealed class ClaudeVisibilityProbe : IClaudeVisibilityProbe
{
    // Shell surfaces that sit "above" everything but never hide an app.
    private static readonly HashSet<string> ShellClasses = new(StringComparer.Ordinal)
    {
        "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Progman", "WorkerW", "NotifyIconOverflowWindow",
        "TopLevelWindowForOverflowXamlIsland", "XamlExplorerHostIslandWindow",
    };

    private readonly Dictionary<uint, bool> _isClaudeProcess = [];
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;

    public ClaudeVisibility Check()
    {
        var windows = WindowReader.InZOrder();
        var index = windows.FindIndex(IsClaudeMainWindow);
        if (index < 0)
        {
            PruneProcessCache(windows);
            return new ClaudeVisibility(false, 0, 0);
        }

        var claude = windows[index];
        if (!claude.IsOnScreen)
        {
            return new ClaudeVisibility(true, 0, claude.Handle);
        }

        var occluders = new List<PixelRect>();
        for (var i = 0; i < index; i++)
        {
            var w = windows[i];
            if (w.IsOnScreen && !w.IsToolWindow && !w.IsClickThrough
                && w.ProcessId != _ownProcessId && w.ProcessId != claude.ProcessId
                && !ShellClasses.Contains(w.ClassName))
            {
                occluders.Add(w.Bounds);
            }
        }

        var screens = Monitors.All().Select(m => m.Bounds).ToList();
        var fraction = OcclusionCalculator.VisibleFraction(claude.Bounds, screens, occluders);
        return new ClaudeVisibility(true, fraction, claude.Handle);
    }

    // The desktop app is an Electron window titled "Claude" owned by a claude.exe process.
    // Matching on both avoids picking up a browser tab or folder that happens to be called "Claude".
    private bool IsClaudeMainWindow(WindowInfo window) =>
        window.Title == "Claude"
        && !window.IsToolWindow
        && window.ClassName.StartsWith("Chrome_WidgetWin", StringComparison.Ordinal)
        && IsClaudeProcess(window.ProcessId);

    private bool IsClaudeProcess(uint processId)
    {
        if (_isClaudeProcess.TryGetValue(processId, out var known))
        {
            return known;
        }

        bool isClaude;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            isClaude = string.Equals(process.ProcessName, "claude", StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            isClaude = false; // Process already exited.
        }
        catch (InvalidOperationException)
        {
            isClaude = false;
        }

        _isClaudeProcess[processId] = isClaude;
        return isClaude;
    }

    // Process ids get reused; forget the ones whose windows are gone.
    private void PruneProcessCache(List<WindowInfo> windows)
    {
        if (_isClaudeProcess.Count < 512)
        {
            return;
        }

        var alive = windows.Select(w => w.ProcessId).ToHashSet();
        foreach (var pid in _isClaudeProcess.Keys.Where(pid => !alive.Contains(pid)).ToList())
        {
            _isClaudeProcess.Remove(pid);
        }
    }
}
