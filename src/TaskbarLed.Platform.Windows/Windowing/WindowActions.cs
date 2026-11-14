using System.Diagnostics;
using System.ComponentModel;
using TaskbarLed.Core.Visibility;
using static TaskbarLed.Platform.Windows.Native.NativeMethods;

namespace TaskbarLed.Platform.Windows.Windowing;

/// <summary>Window operations the panel needs: stay above the taskbar, sit at exact pixels, open Claude.</summary>
public static class WindowActions
{
    /// <summary>
    /// Turns <paramref name="hwnd"/> into an overlay: no taskbar button, not in Alt+Tab.
    /// </summary>
    public static void MakeOverlay(nint hwnd)
    {
        var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (nint)(exStyle | WS_EX_TOOLWINDOW));
    }

    /// <summary>Lets mouse clicks pass through the window to whatever is below (used over full-screen video).</summary>
    public static void SetClickThrough(nint hwnd, bool enabled)
    {
        var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        var next = enabled ? exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED : exStyle & ~WS_EX_TRANSPARENT;
        if (next != exStyle)
        {
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, (nint)next);
        }
    }

    /// <summary>Places the window at physical-pixel coordinates and puts it on top of the taskbar.</summary>
    public static void PlaceOnTop(nint hwnd, PixelRect bounds) =>
        SetWindowPos(hwnd, HWND_TOPMOST, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            SWP_NOACTIVATE | SWP_NOOWNERZORDER);

    /// <summary>
    /// Re-asserts topmost without moving. Clicking the taskbar raises it above other topmost windows,
    /// so the panel calls this periodically.
    /// </summary>
    public static void KeepOnTop(nint hwnd) =>
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);

    /// <summary>Restores (if minimized) and focuses a window.</summary>
    public static void Activate(nint hwnd)
    {
        if (IsIconic(hwnd))
        {
            ShowWindow(hwnd, SW_RESTORE);
        }

        SetForegroundWindow(hwnd);
    }

    /// <summary>Opens the Claude desktop app when no window was found (through its URL protocol).</summary>
    /// <returns>False when Claude is not installed or the protocol is not registered.</returns>
    public static bool LaunchClaude()
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo("claude://") { UseShellExecute = true });
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
