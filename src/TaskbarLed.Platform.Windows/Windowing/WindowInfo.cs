using TaskbarLed.Core.Visibility;
using static TaskbarLed.Platform.Windows.Native.NativeMethods;

namespace TaskbarLed.Platform.Windows.Windowing;

/// <summary>Facts about a top-level window, read once per scan.</summary>
public sealed record WindowInfo(
    nint Handle,
    uint ProcessId,
    string ClassName,
    string Title,
    PixelRect Bounds,
    bool IsVisible,
    bool IsMinimized,
    bool IsCloaked,
    bool IsToolWindow,
    bool IsClickThrough)
{
    /// <summary>True when the window actually paints something the user can see.</summary>
    public bool IsOnScreen => IsVisible && !IsMinimized && !IsCloaked && !Bounds.IsEmpty;
}

/// <summary>Reads window facts through Win32.</summary>
public static unsafe class WindowReader
{
    /// <summary>All top-level windows, topmost first.</summary>
    public static List<WindowInfo> InZOrder()
    {
        var result = new List<WindowInfo>(256);
        for (var hwnd = GetTopWindow(0); hwnd != 0; hwnd = GetWindow(hwnd, GW_HWNDNEXT))
        {
            if (IsWindowVisible(hwnd))
            {
                result.Add(Read(hwnd));
            }
        }

        return result;
    }

    public static WindowInfo Read(nint hwnd)
    {
        GetWindowThreadProcessId(hwnd, out var pid);
        var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        return new WindowInfo(
            hwnd,
            pid,
            ClassName(hwnd),
            Title(hwnd),
            Bounds(hwnd),
            IsWindowVisible(hwnd),
            IsIconic(hwnd),
            IsCloaked(hwnd),
            (exStyle & WS_EX_TOOLWINDOW) != 0,
            (exStyle & WS_EX_TRANSPARENT) != 0);
    }

    public static string ClassName(nint hwnd)
    {
        var buffer = stackalloc char[256];
        var length = GetClassName(hwnd, buffer, 256);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    public static string Title(nint hwnd)
    {
        var buffer = stackalloc char[256];
        var length = GetWindowText(hwnd, buffer, 256);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    /// <summary>Visible frame (DWM extended bounds exclude the invisible resize border).</summary>
    public static PixelRect Bounds(nint hwnd)
    {
        RECT rect;
        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, &rect, (uint)sizeof(RECT)) != 0
            && !GetWindowRect(hwnd, out rect))
        {
            return default;
        }

        return new PixelRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    /// <summary>Cloaked windows exist but are not drawn (other virtual desktop, suspended UWP, etc.).</summary>
    public static bool IsCloaked(nint hwnd)
    {
        int cloaked;
        return DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, &cloaked, sizeof(int)) == 0 && cloaked != 0;
    }
}
