using System.Runtime.InteropServices;

namespace TaskbarLed.Platform.Mac.Native;

/// <summary>CoreFoundation, CoreGraphics and Objective-C runtime declarations. Only called on macOS.</summary>
internal static unsafe partial class MacNative
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    internal const uint kCGWindowListOptionOnScreenOnly = 1 << 0;
    internal const uint kCGWindowListExcludeDesktopElements = 1 << 4;
    internal const uint kCGNullWindowID = 0;
    internal const uint kCFStringEncodingUTF8 = 0x08000100;
    internal const int kCFNumberIntType = 9;

    [StructLayout(LayoutKind.Sequential)]
    internal struct CGRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    // CoreGraphics
    [LibraryImport(CoreGraphics)]
    internal static partial nint CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

    [LibraryImport(CoreGraphics)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CGRectMakeWithDictionaryRepresentation(nint dict, out CGRect rect);

    [LibraryImport(CoreGraphics)]
    internal static partial int CGGetActiveDisplayList(uint maxDisplays, uint* activeDisplays, out uint displayCount);

    [LibraryImport(CoreGraphics)]
    internal static partial CGRect CGDisplayBounds(uint display);

    // CoreFoundation
    [LibraryImport(CoreFoundation)]
    internal static partial nint CFArrayGetCount(nint array);

    [LibraryImport(CoreFoundation)]
    internal static partial nint CFArrayGetValueAtIndex(nint array, nint index);

    [LibraryImport(CoreFoundation)]
    internal static partial nint CFDictionaryGetValue(nint dict, nint key);

    [LibraryImport(CoreFoundation, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint CFStringCreateWithCString(nint allocator, string value, uint encoding);

    [LibraryImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CFStringGetCString(nint str, byte* buffer, nint bufferSize, uint encoding);

    [LibraryImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CFNumberGetValue(nint number, int type, out int value);

    [LibraryImport(CoreFoundation)]
    internal static partial void CFRelease(nint value);

    // Objective-C runtime
    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint sel_registerName(string name);

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint objc_lookUpClass(string name);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    internal static partial nint SendPointer(nint receiver, nint selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    internal static partial void SendVoid(nint receiver, nint selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    internal static partial void SendDouble(nint receiver, nint selector, double value);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    internal static partial void SendRect(nint receiver, nint selector, CGRect rectangle);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    internal static partial void AddSubview(nint receiver, nint selector, nint child, nint position, nint relativeTo);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    internal static partial void SendNUInt(nint receiver, nint selector, nuint value);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    internal static partial void SendBool(nint receiver, nint selector, [MarshalAs(UnmanagedType.U1)] bool value);
}
