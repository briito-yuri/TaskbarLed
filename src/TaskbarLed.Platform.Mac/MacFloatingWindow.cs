using System.Runtime.Versioning;
using TaskbarLed.Platform.Mac.Native;

namespace TaskbarLed.Platform.Mac;

/// <summary>Native tweaks that make the panel behave like a status overlay on macOS.</summary>
[SupportedOSPlatform("macos")]
public static class MacFloatingWindow
{
    // NSWindowCollectionBehavior flags.
    private const nuint CanJoinAllSpaces = 1 << 0;
    private const nuint Stationary = 1 << 4;
    private const nuint IgnoresCycle = 1 << 6;
    private const nuint FullScreenAuxiliary = 1 << 8;

    /// <summary>
    /// Shows <paramref name="nsWindow"/> on every Space (desktop), keeps it out of Cmd+` cycling and
    /// Mission Control, and lets it sit next to full-screen apps.
    /// </summary>
    /// <param name="nsWindow">The <c>NSWindow*</c> of the panel.</param>
    public static void JoinAllSpaces(nint nsWindow)
    {
        if (nsWindow == 0)
        {
            return;
        }

        MacNative.SendNUInt(nsWindow, MacNative.sel_registerName("setCollectionBehavior:"), CanJoinAllSpaces | Stationary | IgnoresCycle | FullScreenAuxiliary);
        MacNative.SendBool(nsWindow, MacNative.sel_registerName("setHidesOnDeactivate:"), false);
    }
}
