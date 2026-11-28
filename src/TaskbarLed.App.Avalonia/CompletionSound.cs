using System.ComponentModel;
using System.Diagnostics;

namespace TaskbarLed.App.Avalonia;

internal static class CompletionSound
{
    public static void Play()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        try
        {
            using var sound = Process.Start(new ProcessStartInfo("/usr/bin/afplay")
            {
                ArgumentList = { "-v", "0.25", "/System/Library/Sounds/Glass.aiff" },
                UseShellExecute = false,
            });
        }
        catch (Win32Exception)
        {
            // A sound failure must not interrupt the visual alert.
        }
    }
}
