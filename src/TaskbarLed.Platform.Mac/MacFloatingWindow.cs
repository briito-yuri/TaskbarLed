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

    public static nint AddLiquidGlass(nint nsWindow, double width, double height, double opacity)
    {
        var glassClass = MacNative.objc_lookUpClass("NSGlassEffectView");
        if (nsWindow == 0 || glassClass == 0)
        {
            return 0;
        }

        var host = MacNative.SendPointer(nsWindow, MacNative.sel_registerName("contentView"));
        if (host == 0)
        {
            return 0;
        }

        var glass = MacNative.SendPointer(glassClass, MacNative.sel_registerName("new"));
        if (glass == 0)
        {
            return 0;
        }

        MacNative.SendDouble(glass, MacNative.sel_registerName("setCornerRadius:"), 16);
        // Fill the window using the NSView autoresizing mask and the current window content rectangle.
        MacNative.SendNUInt(glass, MacNative.sel_registerName("setAutoresizingMask:"), 18);
        MacNative.SendRect(glass, MacNative.sel_registerName("setFrame:"), new MacNative.CGRect { Width = width, Height = height });
        SetGlassOpacity(glass, opacity);
        MacNative.AddSubview(host, MacNative.sel_registerName("addSubview:positioned:relativeTo:"), glass, -1, 0);
        MacNative.SendVoid(glass, MacNative.sel_registerName("release"));
        return glass;
    }

    public static void SetGlassOpacity(nint glass, double opacity) =>
        MacNative.SendDouble(glass, MacNative.sel_registerName("setAlphaValue:"), opacity);

    public static void RemoveLiquidGlass(nint glass) =>
        MacNative.SendVoid(glass, MacNative.sel_registerName("removeFromSuperview"));
}
