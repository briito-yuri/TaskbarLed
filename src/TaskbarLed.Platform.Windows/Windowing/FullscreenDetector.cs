using static TaskbarLed.Platform.Windows.Native.NativeMethods;

namespace TaskbarLed.Platform.Windows.Windowing;

/// <summary>Kind of full-screen content on a monitor.</summary>
public enum FullscreenKind
{
    /// <summary>Nothing full screen.</summary>
    None = 0,

    /// <summary>A normal window covering the monitor (browser F11, YouTube, video players). Overlays can be drawn on top.</summary>
    Borderless = 1,

    /// <summary>Exclusive full screen (DirectX games) or presentation mode. Drawing on top would minimize the app.</summary>
    Exclusive = 2,
}

/// <summary>Detects full-screen apps so the panel can step aside or switch to a discreet mode.</summary>
public static class FullscreenDetector
{
    private static readonly HashSet<string> DesktopClasses = new(StringComparer.Ordinal) { "Progman", "WorkerW" };

    public static FullscreenKind Detect(MonitorInfo monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        var foreground = GetForegroundWindow();
        if (foreground == 0 || Monitors.FromWindow(foreground) != monitor.Handle)
        {
            return FullscreenKind.None;
        }

        if (SHQueryUserNotificationState(out var state) == 0
            && state is QUNS_RUNNING_D3D_FULL_SCREEN or QUNS_PRESENTATION_MODE)
        {
            return FullscreenKind.Exclusive;
        }

        var window = WindowReader.Read(foreground);
        if (DesktopClasses.Contains(window.ClassName) || window.ProcessId == (uint)Environment.ProcessId)
        {
            return FullscreenKind.None;
        }

        var bounds = window.Bounds;
        var covers = bounds.Left <= monitor.Bounds.Left && bounds.Top <= monitor.Bounds.Top
            && bounds.Right >= monitor.Bounds.Right && bounds.Bottom >= monitor.Bounds.Bottom;
        return covers ? FullscreenKind.Borderless : FullscreenKind.None;
    }
}
