using System.Runtime.InteropServices;

namespace TaskbarLed.App.Avalonia;

internal static partial class AntigravityPresence
{
    // The CLI locks this existing file while the conversation is attached. The file itself survives
    // exit. A missing file gives no information about workers or versions that do not create it.
    public static bool WasReleased(string path)
    {
        if (!OperatingSystem.IsMacOS() || !File.Exists(path))
        {
            return false;
        }

        try
        {
            using var file = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            const int sharedNonBlocking = 1 | 4;
            return Flock(file.DangerousGetHandle().ToInt32(), sharedNonBlocking) == 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    [LibraryImport("libSystem.B.dylib", EntryPoint = "flock")]
    private static partial int Flock(int file, int operation);
}
