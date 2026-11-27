using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace TaskbarLed.Platform.Mac;

/// <summary>Checks the advisory lock held by an attached conversation.</summary>
public static partial class AntigravityPresence
{
    public static bool WasReleased(string path) => IsLocked(path) == false;

    public static bool IsAttached(string path) => IsLocked(path) == true;

    private static bool? IsLocked(string path)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return null;
        }

        // FileStream can take its own advisory lock on Unix. Open the file directly
        // so the explicit flock below is the only lock this probe tries to take.
        const int readOnlyNoFollow = 0x100;
        using var file = Open(path, readOnlyNoFollow);
        if (file.IsInvalid)
        {
            return null;
        }

        const int sharedNonBlocking = 1 | 4;
        if (Flock(file, sharedNonBlocking) == 0)
        {
            return false;
        }

        const int wouldBlock = 35; // EWOULDBLOCK on Darwin.
        return Marshal.GetLastPInvokeError() == wouldBlock ? true : null;
    }

    [LibraryImport("libSystem.B.dylib", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8)]
    private static partial SafeFileHandle Open(string path, int flags);

    [LibraryImport("libSystem.B.dylib", EntryPoint = "flock", SetLastError = true)]
    private static partial int Flock(SafeFileHandle file, int operation);
}
