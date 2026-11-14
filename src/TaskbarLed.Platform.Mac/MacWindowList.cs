using System.Runtime.Versioning;
using TaskbarLed.Core.Visibility;
using TaskbarLed.Platform.Mac.Native;

namespace TaskbarLed.Platform.Mac;

/// <summary>
/// Reads the on-screen window list from CoreGraphics. Owner names, layers and bounds need no
/// Screen Recording permission (only window titles do, and they are not used).
/// </summary>
[SupportedOSPlatform("macos")]
public static class MacWindowList
{
    /// <summary>On-screen windows, frontmost first, in global screen points (origin top-left).</summary>
    public static IReadOnlyList<MacWindow> OnScreen()
    {
        var result = new List<MacWindow>();
        var list = MacNative.CGWindowListCopyWindowInfo(
            MacNative.kCGWindowListOptionOnScreenOnly | MacNative.kCGWindowListExcludeDesktopElements, MacNative.kCGNullWindowID);
        if (list == 0)
        {
            return result;
        }

        var ownerKey = CfString("kCGWindowOwnerName");
        var layerKey = CfString("kCGWindowLayer");
        var boundsKey = CfString("kCGWindowBounds");
        try
        {
            var count = MacNative.CFArrayGetCount(list);
            for (nint i = 0; i < count; i++)
            {
                var window = MacNative.CFArrayGetValueAtIndex(list, i);
                var owner = ReadString(MacNative.CFDictionaryGetValue(window, ownerKey));
                var layerRef = MacNative.CFDictionaryGetValue(window, layerKey);
                var boundsRef = MacNative.CFDictionaryGetValue(window, boundsKey);
                if (owner is null || layerRef == 0 || boundsRef == 0
                    || !MacNative.CFNumberGetValue(layerRef, MacNative.kCFNumberIntType, out var layer)
                    || !MacNative.CGRectMakeWithDictionaryRepresentation(boundsRef, out var rect))
                {
                    continue;
                }

                result.Add(new MacWindow(owner, layer, ToPixelRect(rect)));
            }
        }
        finally
        {
            MacNative.CFRelease(ownerKey);
            MacNative.CFRelease(layerKey);
            MacNative.CFRelease(boundsKey);
            MacNative.CFRelease(list);
        }

        return result;
    }

    /// <summary>Display rectangles in the same global coordinates as the windows.</summary>
    public static unsafe IReadOnlyList<PixelRect> Displays()
    {
        const int Max = 16;
        var ids = stackalloc uint[Max];
        if (MacNative.CGGetActiveDisplayList(Max, ids, out var count) != 0)
        {
            return [];
        }

        var displays = new List<PixelRect>((int)count);
        for (var i = 0; i < count; i++)
        {
            displays.Add(ToPixelRect(MacNative.CGDisplayBounds(ids[i])));
        }

        return displays;
    }

    private static PixelRect ToPixelRect(MacNative.CGRect rect) => PixelRect.FromSize(
        (int)Math.Round(rect.X), (int)Math.Round(rect.Y), (int)Math.Round(rect.Width), (int)Math.Round(rect.Height));

    private static nint CfString(string value) =>
        MacNative.CFStringCreateWithCString(0, value, MacNative.kCFStringEncodingUTF8);

    private static unsafe string? ReadString(nint cfString)
    {
        if (cfString == 0)
        {
            return null;
        }

        const int Size = 512;
        var buffer = stackalloc byte[Size];
        return MacNative.CFStringGetCString(cfString, buffer, Size, MacNative.kCFStringEncodingUTF8)
            ? new string((sbyte*)buffer)
            : null;
    }
}
