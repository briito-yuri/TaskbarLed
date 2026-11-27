using System.Runtime.InteropServices;

namespace TaskbarLed.App.Avalonia;

internal static partial class AntigravityPresence
{
    // The CLI locks this existing file while the conversation is attached. The file itself survives
    // exit. A missing file gives no information about workers or versions that do not create it.
    public static bool WasReleased(string path) => IsLocked(path) == false;

    public static bool IsAttached(string path) => IsLocked(path) == true;

    private static bool? IsLocked(string path)
    {
        if (!OperatingSystem.IsMacOS() || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var file = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            const int sharedNonBlocking = 1 | 4;
            if (Flock(file.DangerousGetHandle().ToInt32(), sharedNonBlocking) == 0)
            {
                return false;
            }

            const int wouldBlock = 35; // EWOULDBLOCK on Darwin
            return Marshal.GetLastPInvokeError() == wouldBlock ? true : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    [LibraryImport("libSystem.B.dylib", EntryPoint = "flock", SetLastError = true)]
    private static partial int Flock(int file, int operation);
}
